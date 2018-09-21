---
title: "MQTT"
author: Dominik Psenner
date: Sep 21, 2018
---

## Who am I

* Software engineer since 2005
* Active member of the Apache Software Foundation
* Working in the industry, providing customers with automation and control systems

## Motivation

Automation in the industry and IoT typically involves communication between:

* ERP (Enterprise Resource Planning)
* MES (Manufacturing Execution System)
* PCS (Production Control System)

## Request-Response

* The classic approach
* Application provides endpoints as a service
* A well known pattern
* Good toolings like grpc, swagger, ..

## Request-Response: Disadvantages

* Tight coupling between client and service
* Extending the application is challenging
* Distributing the same data to several hundred clients is challenging

## Publish-Subscribe

![MQTT Publisher Subscriber](mqtt_publisher_subscriber-1.png){ height=80% width=80%}\


## Publish-Subscribe: Features

* Decouples publisher and subscriber spatially
* Decouples publisher and subscriber by time

## Publish-Subscribe: Advantages

This approach shines as a more generic concept to the classic request-response pattern.

* Scalability
* Extensibility
* Naturally fits async and event driven programming
* Request-Response is a special case

## What is MQTT

MQTT is a light weight protocol that:

* Is designed to consume only very little power
* Requires only a very small memory footprint
* Requires very low bandwidth

## MQTT message

A MQTT message consists of:

* Topic
* Payload
* Quality of Service flag
* Retain flag

## MQTT: topics

Topics can be any string and it is good practice to structure them into a namespace using slashes:

* `my-home/kitchen/sensor/1/temperature`
* `my-home/kitchen/sensor/1/humidity`
* `my-home/livingroom/sensor/1/temperature`

## MQTT: topic subscriptions

Subscriptions allow pattern matching using `+` and `#`:

* `my-home/kitchen/sensor/+/temperature`
* `my-home/kitchen/#`
* `#`

## MQTT: topics best practices

* No leading forward slash
* Avoid whitespaces or non-ASCII characters
* Keep topic short and concise
* Use specific topics, not general ones
* Don't forget extensibility

## MQTT: payload

Payload can be any array of bytes up to ~256Mb:

* `json`
* `protobuf`
* Text encoded in utf8
* Headers and payloads of another protocol
* Metadata about other topics
* Picture data
* Video frame
* ...

## Hands on: hivemq demo client

[http://www.hivemq.com/demos/websocket-client/](http://www.hivemq.com/demos/websocket-client/)

## Hands on: broker

* Hostname: `mqtt.hta`
* WebSocket port: `8000`
* TCP port: `1883`

## Hands on: chat

* Chat application
* Topic: `chat/channel/+/message`
* Payload: `["author", "message"]`

## MQTT message

A MQTT message further consists of:

* Quality of Service (QoS) level
* Retain flag

## MQTT QoS

Influences the behavior of a client that sends a message and the client that receives a message:

* A client here is the publisher, the broker or the subscriber
* Messages from the publisher to the broker
* Messages from the broker to the subscriber
* Publishers and subcribers are allowed to use different QoS on the same topic

## MQTT QoS 0

![QoS-0](QoS-0.png)\


## MQTT QoS 0: features

* Fire and forget
* No guarantee of delivery
* No retransmission by the client
* Message arrives at most once
* Fastest

## MQTT QoS 0: use when ..

* Connection is very stable; like on the same host
* Application can handle if messages are lost

## MQTT QoS 1

![QoS-1](QoS-1.png)\


## MQTT QoS 1: features

* Guaranteed delivery
* Messages are queued by the sender
* A client may retransmit a message if it does not receive a puback after a reasonable amount of time
* Message arrives at least once
* Slower than QoS 0

## MQTT QoS 1: use when ..

* Application cannot tolerate the loss of messages
* Application layer handles duplicated messages

## MQTT QoS 2

![QoS-2](QoS-2.png)\


## MQTT QoS 2: features

* Guaranteed delivery
* Messages are queued
* Message arrives exactly once
* Retransmission of messages
* Slowest

## MQTT QoS 2: use when ..

* It is critical for the application that messages arrive exactly once
* The four-part handshake overhead is not an issue

## MQTT persistent session

A persistent session includes all information that the broker knows about a client:

* Existence of a client session
* Subscriptions of the client
* All messages with QoS > 0 that the client did not ack yet
* All new messages with QoS > 0 that the client would miss
* All QoS 2 messages from the client that are not yet ack'ed completely

## MQTT persistent session: mosquitto

* Persistent session data is stored in memory for performance reasons
* Persistent session data is flushed to disk:
    * At configurable periodic intervals
    * When mosquitto terminates gracefully
* Data is restored from disk on restart or signal
* Persistent client expiration: disabled by default

## MQTT persistent session: when to use?

* A client must get all messages, even if it is offline
* A client has very limited resources, like android app where the operating system restricts cpu usage to save battery

## Hands on: persistent session

* Chat application uses persistent session
* Shows messages missed while being shut down

## MQTT retained messages

* Broker stores a retained message when it is published
* Broker serves the retained message to subscribers on behalf of the publisher
* When an application subscribes, it receives the retained message stored by the broker
* Applications can be subscribed or running when a message is published

## MQTT last will

* Optional
* Must be requested when clients connects to the broker
* Last will is sent by the broker
    * On behalf of the client
    * When a client disconnects
* Last will is a complete MQTT message
    * Topic
    * Payload
    * Retain flag
    * QoS

## Hands on: application state

* Combining these two features is ideal to share the application state
* Topic: `{node}/state`
* Payload
    * `ALIVE`
    * `DEAD`

## Security concerns

* Broker provides:
    * Transport encryption
    * Authentication
    * Access control lists
* However, everyone with access can subscribe to all topics
* Application layer can sign and encrypt the payload

## Git repository

This presentation along with examples can be found here:
[https://github.com/dpsenner/mqtt-presentation](https://github.com/dpsenner/mqtt-presentation)

## Related Work

* Sparkplug (built on MQTT)
    * Designed to integrate devices (sensors and actors)
    * Implements a homologous topic and payload structure
* DDS (Data Distribution Service)
    * Open standard
    * Discovery of nodes (no broker)
    * Payload introspection
    * Persistent storage built-in
    * Backbone middleware for:
        * US Navy AOA (Aegis Open Architecture)
        * NASA Robotics
        * Volkswagen Driver Assistance
        * ...

# Questions?

# Thanks
