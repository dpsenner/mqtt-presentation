---
title: "MQTT"
author: Dominik Psenner
date: Sep 21, 2018
---

# Welcome

## Who am I

* Software engineer since 2005
* Active member of the Apache Software Foundation
* Open Source Software that makes the world a better place for humankind
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
* Extending existing endpoints with more functionality is hard
* Distributing data to several hundred endpoints is challenging
* Scalability is challenging

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

## MQTT: topics

* Topics can be any string
* Should be structured into a namespace with slashes
* Subscriptions allow pattern matching with:
    * `+` matching up to the next `/`
    * `#` matching any string that follows
* `foo/+/baz` matches `foo/bar/baz` and `foo/boo/baz`
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

## MQTT QoS

Quality of service flag influences the message delivery:

* Messages from the publisher to the broker
* Messages from the broker to the subscriber
* Publishers and subcribers are allowed to use different QoS on the same topic

## MQTT QoS 0

![QoS-0](QoS-0.png)

## MQTT QoS 0: features

* Message arrives at most once
* No guarantee of delivery
* No retransmission by the client
* No message queuing
* Fastest

## MQTT QoS 0: use when ..

* Connection is very stable; like wired lan
* Messages can be lost occasionally
* Message queuing is not needed

## MQTT QoS 1

![QoS-1](QoS-1.png)

## MQTT QoS 1: features

* Message arrives at least once
* Guaranteed delivery
* A client may retransmit a message if it does not receive a puback after a reasonable amount of time
* Messages are queued by the sender
* Slower

## MQTT QoS 1: use when ..

* You need to get every message
* Application layer handles duplicates
* Functional requirements do not tolerate QoS 2 overhead

## MQTT QoS 2

![QoS-2](QoS-2.png)

## MQTT QoS 2: features

* Message arrives exactly once
* Guaranteed delivery
* Retransmission of messages
* Messages are queued
* Slowest

## MQTT QoS 2: use when ..

* It is critical for the application that messages arrive exactly once
* The four-part handshake overhead is not an issue

## MQTT retained messages

* Applications may not be subscribed or running when a message is published
* Retaining a message is an effective way to deliver the message anyway
* Broker stores the retained message
* When an application subscribes, it receives the retained message in store

## MQTT last will

* A client can instruct the broker to publish a message when it disconnects
* Both topic and payload are sent by the client when connecting to the broker
* Last will can be a retained message
* When the broker detects that the client is disconnected it publishes the last will on behalf of the client

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

## MQTT persistent session: when to use?

* A client must get all messages, even if it is offline
* A client has very limited resources, likw android app where the operating system restricts cpu usage to save battery

## MQTT persistent session: mosquitto

* Persistent session data is stored in memory
* Persistent session data is Written to disk:
    * When mosquitto closes
    * At configurable periodic intervals
* Data is restored from disk on restart or signal
* Persistent client expiration is disabled by default

## MQTT limitations

* A message can transport at most ~256Mb in the payload
* Broker is (not necessarily) a single point of failure
* A publisher may assume that a subscriber is listening, when in fact it is not
* Assuring delivery to subscriber involves additional roundtrips
* Publishes by malicious or faulty nodes: signatures
* Hiding information from subscribers: encryption

## MQTT topic: best practice

* A topic should transport one piece of information that belongs together
* Design topic with the same kind of information to be on the same nesting level

## MQTT payload: best practice

* May be any binary format, even a protocol with headers and payloads
* `JSON` is supported by many applications and is human readable
* `protobuf` shines on embedded devices with very limited resources

## Implementing Request-Response?

* Better design topics and payloads for
    * The state of the system
    * The information that needs to be exchanged
* Use a higher QoS than 0
* One or more topics to know whether the node and its components is alive
* One or more topics for the request data
* One or more topics for the response data
    * Successful response
    * Failure responses

## Questions?

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

## Hands on: sensor data

* Topic: `{node}/property/temperature/{sensor}`
* Payload: `34.8`
* Publishing sensor data
    * Stateless shell application
    * Stateful python application
* Subscription: `+/property/temperature/#`

## Hands on: controlling nodes

* Read topic: `{node}/property/scan-rate`
* Write topic: `{node}/property/scan-rate/set`
* Payload: seconds, for example `3.0`

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

## Q&A

Questions?

## Thanks
