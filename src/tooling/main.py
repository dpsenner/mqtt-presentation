#!/usr/bin/env python3
import paho.mqtt.client as mqtt
import argparse
import socket
import time
import queue

class PublishNullForeachRetained:
    def __init__(self, host, port):
        self._host = host
        self._port = port
        self._connected = False
        self._inbound_messages = queue.Queue()
    
    def run(self):
        self._client = mqtt.Client()
        self._client.on_connect = self._on_connect
        self._client.on_message = self._on_message
        self._client.on_disconnect = self._on_disconnect
        self._client.connect(self._host, self._port, 60)
        self._client.subscribe("#")
        self._client.loop_start()

        # wait for connection to be established
        while not self._connected:
            time.sleep(0.1)
        
        alive = True
        while alive:
            retained_messages = {}
            try:
                msg = self._inbound_messages.get(timeout=1.0)
                if msg.retain:
                    retained_messages[msg.topic] = msg
                    print("--> {0}[QoS{1}, Retain={2}]: {3}".format(msg.topic, msg.qos, msg.retain, ""))
                    self._client.publish(msg.topic, qos=1, retain=1, payload="")
                elif msg.topic in retained_messages:
                    retained_messages.pop(msg.topic)
            except queue.Empty:
                if len(retained_messages) > 0:
                    print("No new message but there are pending retained messages..")
                else:
                    print("No new message within the last 1.0 second, terminating..")
                    alive = False

    def _on_message(self, client, userdata, msg):
        print("<-- {0}[QoS{1}, Retain={2}]: {3}".format(msg.topic, msg.qos, msg.retain, msg.payload.decode('utf8')))
        self._inbound_messages.put(msg)

    def _on_connect(self, client, userdata, flags, rc):
        print("Connected to {0}:{1}".format(self._host, self._port))
        self._connected = True

    def _on_disconnect(self, client, userdata, rc):
        print("Disconnected from {0}:{1}".format(self._host, self._port))
        self._connected = False


def main():
    command_publish_null_foreach_retained = "publish-null-foreach-retained"
    parser = argparse.ArgumentParser(description="Publish temperature sensors of this machine.")
    parser.add_argument("--host", help="the host of the mqtt broker")
    parser.add_argument("--port", type=int, help="the port of the mqtt broker")
    parser.add_argument("command", choices=[command_publish_null_foreach_retained])
    args = parser.parse_args()

    if args.host:
        host = args.host
    else:
        host = "localhost"
    if args.port:
        port = args.port
    else:
        port = 1883
    if args.command == command_publish_null_foreach_retained:
        node = PublishNullForeachRetained(host, port)
        node.run()
    else:
        print("Unknown command")

main()