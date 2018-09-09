#!/usr/bin/python3
import paho.mqtt.client as mqtt
import argparse
import json
import re
import socket
import subprocess
import time

class SensorNode:
    def __init__(self, host, port, application_id, scan_rate):
        self._host = host
        self._port = port
        self._application_id = application_id
        self._scan_rate = scan_rate
        self._connected = False
        self._alive = False
        self._client = None

    def run(self):
        self._client = mqtt.Client()
        self._client.on_connect = self._on_connect
        self._client.on_message = self._on_message
        self._client.on_disconnect = self._on_disconnect
        self._client.will_set("{0}/STATE".format(self._application_id), "DEAD", retain=True)
        self._client.connect(self._host, self._port, 60)
        self._client.loop_start()
        self._alive = True
        while self._alive:
            if time.time() - self._last_published_on > self._scan_rate:
                self._publish_sensors()
                self._last_published_on = time.time()
            time.sleep(0.1)
        self._client.loop_stop()
        self._client = None
        self._alive = False
    
    def _subscribe_application_topic(self, topic, qos=0):
        self._client.subscribe(self._get_application_topic(topic), qos)

    def _publish_application_topic(self, topic, payload=None, qos=0, retain=False):
        self._client.publish(self._get_application_topic(topic), payload, qos, retain)

    def _read_sensors(self):
        sensors = subprocess.check_output(["sensors"]).decode('utf8')
        for line in sensors.splitlines():
            m = re.match("^([^:]+):[^0-9]+([0-9\.]+)[ ]?([^ ]+)", line)
            if m:
                sensor = m.group(1)
                raw_value = float(m.group(2))
                unit = m.group(3)
                yield sensor, raw_value, unit

    def _get_application_topic(self, topic):
        return "{0}/{1}".format(self._application_id, topic)

    def _publish_scan_rate(self):
        self._publish_application_topic("property/scan_rate", "{0}".format(scan_rate))

    def _publish_sensors(self):
        for sensor, value, unit in self._read_sensors():
            topic = "property/{0}".format(sensor)
            payload = "{0}".format(value)
            self._publish_application_topic(topic, payload)

    def _get_birth_topics(self):
        yield {
            "topic": self._get_application_topic("command/rebirth"),
            "modes": ["sub"],
            "type": "null",
        }
        yield {
            "topic": self._get_application_topic("command/shutdown"),
            "modes": ["sub"],
            "type": "null",
        }
        yield {
            "topic": self._get_application_topic("property/scan_rate"),
            "modes": ["pub"],
            "type": "numeric",
            "quantity": "duration",
            "unit": "seconds",
        }
        yield {
            "topic": self._get_application_topic("property/scan_rate/set"),
            "modes": ["sub"],
            "type": "numeric",
            "quantity": "duration",
            "unit": "seconds",
        }
        for sensor, value, unit in self._read_sensors():
            if unit in ["RPM"]:
                quantity = "rate"
                unit = "rotations_per_minute"
            elif unit in ["°C"]:
                quantity = "temperature"
                unit = "degree_celsius"
            yield {
                "topic": self._get_application_topic("property/{0}".format(sensor)),
                "modes": ["pub"],
                "type": "numeric",
                "quantity": quantity,
                "unit": unit,
            }

    def _publish_birth(self):
        self._publish_application_topic("STATE", "ALIVE", retain=True)
        self._publish_application_topic("BIRTH", json.dumps(list(self._get_birth_topics()), indent=2), retain=True)

    # The callback for when a PUBLISH message is received from the server.
    def _on_message(self, client, userdata, msg):
        try:
            if msg.topic == self._get_application_topic("property/scan_rate/set"):
                new_scan_rate = float(msg.payload.decode('utf-8'))
                if new_scan_rate >= 1.0:
                    print("Changing scan rate from {0} to {1}".format(self._scan_rate, new_scan_rate))
                    self._scan_rate = new_scan_rate
                else:
                    print("Refusing to change the scan rate below 1.0")
            elif msg.topic == self._get_application_topic("command/shutdown"):
                print("Shutting down by remote request")
                self._alive = False
            elif msg.topic == self._get_application_topic("command/rebirth"):
                print("Publishing birth as requested from remote")
                self._publish_birth()
            else:
                print("Received an unhandled message on topic {0}".format(msg.topic))
        except Exception as ex:
            print("An unhandled exception occurred while processing a message on topic {0}: {1}".format(msg.topic, ex))

    def _on_connect(self, client, userdata, flags, rc):
        print("Connected to {0}:{1}".format(self._host, self._port))
        self._subscribe_application_topic("command/rebirth")
        self._subscribe_application_topic("command/shutdown")
        self._subscribe_application_topic("property/scan_rate/set")
        self._publish_birth()
        self._last_published_on = 0
        self._connected = True

    def _on_disconnect(self, client, userdata, rc):
        print("Disconnected from {0}:{1}".format(self._host, self._port))
        self._connected = False

def main():
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
    sensor_node = SensorNode(host, port, application_id, scan_rate)
    sensor_node.run()

main()