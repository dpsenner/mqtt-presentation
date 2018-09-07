#!/usr/bin/python3
import paho.mqtt.client as mqtt
import argparse
import json
import re
import socket
import subprocess
import time

parser = argparse.ArgumentParser(description="Publish temperature sensors of this machine.")
parser.add_argument("--host", help="the host of the mqtt broker")
parser.add_argument("--port", type=int, help="the port of the mqtt broker")
parser.add_argument("--application-id", help="a custom application id, defaults to the hostname of the machine")
parser.add_argument("--scan-rate", type=float, help="the initial scan rate, defaults to 1.0")
args = parser.parse_args()

if args.host:
    host = args.host
else:
    host = "localhost"
if args.port:
    port = args.port
else:
    port = 1883
if args.application_id:
    application_id = args.application_id
else:
    application_id = socket.gethostname()
if args.scan_rate:
    scan_rate = args.scan_rate
else:
    scan_rate = 1.0
connected = False
alive = True

def read_sensors():
    sensors = subprocess.check_output(["sensors"]).decode('utf8')
    for line in sensors.splitlines():
        m = re.match("^([^:]+):[^0-9]+([0-9\.]+)[ ]?([^ ]+)", line)
        if m:
            sensor = m.group(1)
            raw_value = float(m.group(2))
            unit = m.group(3)
            yield sensor, raw_value, unit

def get_application_topic(topic):
    return "{0}/{1}".format(application_id, topic)

def publish(topic, payload=None, qos=0, retain=False):
    client.publish(get_application_topic(topic), payload, qos, retain)

def publish_scan_rate():
    publish("property/scan_rate", "{0}".format(scan_rate))

def publish_sensors():
    for sensor, value, unit in read_sensors():
        topic = "property/{0}".format(sensor)
        payload = "{0}".format(value)
        publish(topic, payload)

def get_birth_topics():
    yield {
        "topic": get_application_topic("command/rebirth"),
        "modes": ["sub"]
    }
    yield {
        "topic": get_application_topic("command/shutdown"),
        "modes": ["sub"],
    }
    yield {
        "topic": get_application_topic("property/scan_rate"),
        "modes": ["pub"],
    }
    yield {
        "topic": get_application_topic("property/scan_rate/set"),
        "modes": ["sub"],
    }
    for sensor, value, unit in read_sensors():
        yield {
            "topic": get_application_topic("property/{0}".format(sensor)),
            "modes": ["pub"],
        }

def publish_birth():
    publish("STATE", "ALIVE", retain=True)
    # TODO publish capabilities
    publish("BIRTH", json.dumps(list(get_birth_topics()), indent=2), retain=True)
    publish_scan_rate()
    publish_sensors()

# The callback for when a PUBLISH message is received from the server.
def on_message(client, userdata, msg):
    global alive
    global connected
    global scan_rate
    try:
        if msg.topic == get_application_topic("property/scan_rate/set"):
            new_scan_rate = float(msg.payload.decode('utf-8'))
            if new_scan_rate >= 1.0:
                print("Changing scan rate from {0} to {1}".format(scan_rate, new_scan_rate))
                scan_rate = new_scan_rate
            else:
                print("Refusing to change the scan rate below 1.0")
        elif msg.topic == get_application_topic("command/shutdown"):
            print("Shutting down by remote request")
            alive = False
        elif msg.topic == get_application_topic("command/rebirth"):
            print("Publishing birth as requested from remote")
            publish_birth()
        else:
            print("Received an unhandled message on topic {0}".format(msg.topic))
    except Exception as ex:
        print("An unhandled exception occurred while processing a message on topic {0}: {1}".format(msg.topic, ex))


# The callback for when the client receives a CONNACK response from the server.
def on_connect(client, userdata, flags, rc):
    global alive
    global connected
    print("Connected to {0}:{1}".format(host, port))
    connected = True

    # Subscribing in on_connect() means that if we lose the connection and
    # reconnect then subscriptions will be renewed.
    client.subscribe(get_application_topic("command/rebirth"))
    client.subscribe(get_application_topic("command/shutdown"))
    client.subscribe(get_application_topic("property/scan_rate/set"))
    publish_birth()

def on_disconnect(client, userdata, rc):
    global alive
    global connected
    print("Disconnected from {0}:{1}".format(host, port))
    connected = False

client = mqtt.Client()
client.on_connect = on_connect
client.on_message = on_message
client.on_disconnect = on_disconnect
client.will_set("{0}/STATE".format(application_id), "DEAD", retain=True)
client.connect(host, port, 60)
client.loop_start()

while alive:
    last_published_on = time.time()

    # begin publishing temperature data
    while connected:
        elapsed = time.time() - last_published_on
        if elapsed > scan_rate:
            should_publish_now = True
            last_published_on = time.time()
        else:
            should_publish_now = False

        if should_publish_now:
            publish_sensors()
        time.sleep(0.1)

    # sleep until reconnected
    time.sleep(1.0)