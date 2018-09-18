#!/usr/bin/env python3
import paho.mqtt.client as mqtt
import argparse
import socket
import time

class SomeNode:
    def __init__(self, host, port, application_id):
        self._host = host
        self._port = port
        self._application_id = application_id

    def run(self):
        self._client = mqtt.Client()
        self._client.on_connect = self._on_connect
        self._client.on_message = self._on_message
        self._client.on_disconnect = self._on_disconnect
        # Set last will of the node, the broker stores this message and publishes
        # it on behalf of this node when it detects that this node disconnects from
        # the broker.
        self._client.will_set(self._get_topic_application_state(), "DEAD", qos=1, retain=True)
        self._client.connect(self._host, self._port, 60)
        self._client.loop_start()
        while True:
            time.sleep(1.0)
    
    def _get_topic_application_state(self):
        return "{0}/state".format(self._application_id)

    def _on_message(self, client, userdata, msg):
        pass

    def _on_connect(self, client, userdata, flags, rc):
        print("Connected to {0}:{1}".format(self._host, self._port))
        self._client.publish(self._get_topic_application_state(), "ALIVE", qos=1, retain=True)

    def _on_disconnect(self, client, userdata, rc):
        print("Disconnected from {0}:{1}".format(self._host, self._port))


def main():
    parser = argparse.ArgumentParser(description="Publish temperature sensors of this machine.")
    parser.add_argument("--host", help="the host of the mqtt broker")
    parser.add_argument("--port", type=int, help="the port of the mqtt broker")
    parser.add_argument("--application-id", help="a custom application id, defaults to the hostname of the machine")
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
    node = SomeNode(host, port, application_id)
    node.run()

main()