---
title: "MQTT"
author: Dominik Psenner
date: Aug 31, 2018
---

# Welcome

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

## Publish-Subscribe: Considerations

* Designing topics can be challenging
* Designing payloads can be challenging
* A publisher may assume that a subscriber is listening, when in fact it is not
* Assuring delivery of messages to subscriber involves additional topics and overheads
* Hiding information from subscribers (can be solved with encryption)
* Rogue publishes by malicious or faulty nodes (can be solved with signatures)

## What is MQTT

MQTT is a light weight protocol that:

* Provides publish-subscribe pattern
* Is designed to consume only very little power
* Requires only a very small memory footprint
* Requires very low bandwith

## MQTT topics and payloads

* Information can be encoded into topics
* Information can be encoded into the payload
* Information can be encoded into both
* What's better depends on the functional requirements

## MQTT QoS

Quality of service flag influences the message delivery:

* Messages from the publisher to the broker
* Messages from the subscriber to the broker
* But not messages from the publisher to the subcriber

## MQTT QoS 0

* Message arrives at most once
* No guarantee of delivery
* No retransmission by the client
* No message queuing
* Fastest

## MQTT QoS 0: flow

```text
Client --pub qos0--> Broker
```

## MQTT QoS 1

* Message arrives at least once
* Guaranteed delivery
* No retransmission by the client
* Messages are queued
* Slower

## MQTT QoS 1: flow

```text
Client ---pub qos1--> Broker
Client <--pub ack---- Broker
```

## MQTT QoS 2

* Message arrives exactly once
* Guaranteed delivery
* Retransmission of messages
* Messages are queued
* Slowest

## MQTT QoS 2: flow

```text
Client ---pub qos2--> Broker
Client <--pub rec---- Broker
Client ---pub rel---> Broker
Client <--pub comp--- Broker
```

## MQTT QoS 0: use when..

* Connection is stable (wired lan)
* Messages can be lost occasionally
* Message queuing is not needed

## MQTT QoS 1: use when..

* You need to get every message
* Application layer handles duplicates
* Functional requirements do not tolerate QoS 2 overhead

## MQTT QoS 2: use when..

* It is critical for the application that messages arrive exactly once
* Be aware of the four-part handshake overhead

## MQTT retained messages

* Applications may not be subscribed or running when a message is published
* Retaining a message is an effective way to deliver the message anyway
* Broker stores the retained message
* When an application subscribes, it receives the retained message

## MQTT rebirth topic

* An alternative to retained messages if clients are alive
* Applications subscribe to a topic like `rebirth`
* When a message arrives the application publishes its state
* Does not require a retained message

## MQTT limitations

* A message can transport at most ~256Mb in the payload

## MQTT topic: best practice

* A topic should transport one piece of information that belongs together
* Do design topic with the same kind of information to be on the same nesting level

## MQTT payload: best practice

* May be any format
* Even a protocol with headers and payloads
* `JSON` is supported by many applications and is human readable
* `protobuf` shines on embedded devices with limited resources

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

## Hands on

* Clients publish sensor data
* Shell script
* Python script

## Hands on

* Temperature alarm service

## Related Work: Sparkplug

* Defines a very small set of topics
* This allows discovering nodes and their capabilities
* We are not going into the details of the sparkplug specification

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

* dpsenner@gmail.com
* dpsenner@apache.org

