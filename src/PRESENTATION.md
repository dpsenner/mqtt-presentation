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

* Automation in the industry gains new momentum
* IoT
* Industry 4.0
* Good slogans, communication and collaboration is the key

## Request-Response

* The classic approach
* Application provides endpoints as a service
* A well known pattern
* Good toolings like grpc, swagger, ..

## Request-Response: Disadvantages

* Tight coupling between client and service
* Extending the application is challenging
* Distributing data to several hundred endpoints is challenging

## Publish-Subscribe

![MQTT Publisher Subscriber](mqtt_publisher_subscriber-1.png)

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
* QoS
* Retain flag

## MQTT: topics

* Topics can be any string
* Can be structured into a namespace with slashes
* Subscriptions allow pattern matching with:
    * `+` matching up to the next `/`
    * `#` matching any string that follows
* `foo/+/baz` to match `foo/bar/baz` and `foo/boo/baz`
* `foo/#` matches also `foo/bar/baz/doh`
* `#` matches any topic
* But `foo/#/baz` is not allowed
* Designing a topic namespace is a challenging task

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

## Hands on: git repository

This presentation along with examples can be found here:

[https://github.com/dpsenner/mqtt-presentation.git](https://github.com/dpsenner/mqtt-presentation.git)

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

![QoS-0](QoS-0.png)

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

![QoS-1](QoS-1.png)

## MQTT QoS 1: features

* Guaranteed delivery
* Messages are queued by the sender
* A client may retransmit a message if it does not receive a puback after a reasonable amount of time
* Message arrives at least once
* Slower than QoS 1

## MQTT QoS 1: use when ..

* Application cannot tolerate the loss of messages
* Application layer handles duplicated messages

## MQTT QoS 2

![QoS-2](QoS-2.png)

## MQTT QoS 2: features

* Guaranteed delivery
* Messages are queued
* Message arrives exactly once
* Retransmission of messages
* Slowest

## MQTT QoS 2: use when ..

* It is critical for the application that messages arrive exactly once
* The four-part handshake overhead is not an issue

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

## MQTT persistent session

A persistent session includes all information that the broker knows about a client:

* Existence of a client session
* Subscriptions of the client
* All messages with QoS > 0 that the client did not ack yet
* All new messages with QoS > 0 that the client would miss
* All QoS 2 messages from the client that are not yet ack'ed completely

## MQTT persistent session: mosquitto

* Persistent session data is stored in memory
* Persistent session data is flushed to disk:
    * When mosquitto terminates gracefully
    * At configurable periodic intervals
* Data is restored from disk on restart or signal
* Persistent client expiration is disabled by default

## MQTT persistent session: when to use?

* A client must get all messages, even if it is offline
* A client has very limited resources, like android app where the operating system restricts cpu usage to save battery

## Hands on: transforming data to other topics

* Subscribing to sensor data
    * Temperature alarms
    * Temperature histogram

## Related Work: Sparkplug

* Both define a very small set of topics
* This allows discovering nodes and their capabilities
* We are not going into the details of these specifications

## Related work: DDS

* Data Distribution Service is an open standard
* There are commercial and open source implementations
* The big advantages over MQTT are:
    * Discovery of publishers and subscribers
    * Ownership of topics
    * Persistent storage of messages

## Questions?

## Thanks
