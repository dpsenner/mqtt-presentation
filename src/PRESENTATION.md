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

## Request - Response

* The classic approach
* Application provides endpoints as a service
* A well known pattern
* Good toolings like grpc, swagger, ..

## Request - Response: Disadvantages

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
* Assuring delivery of messages involves additional topics and overheads
* Hiding information from subscribers (can be solved with encryption)
* Rogue publishes by malicious or faulty nodes (can be solved with signatures)

## What is MQTT

MQTT is a light weight protocol that:

* provides publish-subscribe pattern
* is designed to consume only very little power
* requires only a very small memory footprint
* requires very low bandwith

## MQTT topics and payloads

* The structure of information can be encoded into topics or the payload
* What's better depends on the application
* A topic should transport a piece of information that belongs together
* What is encoded into the topic or the payload depends on the functional requirements
* To MQTT payload is an array of bytes and can transport up to 256Mb

## Serializing payload

* May be any format
* Even a protocol with headers and payloads
* `JSON` is supported by many applications and is human readable
* `protobuf` shines on embedded devices with limited resources

## First hands on

* Clients that publish temperature information
* A larger number of topics
* Transporting primitives

## Implementing Request-Response

* Request topic:  `{endpoint}/request/{requestId}`
* Response topic: `{endpoint}/response/{requestId}`

## Implementing Request-Response: Echo

* Service subscribes to `echo/request/+`
* Client subscribes to `echo/response/1`
* Client publishes to `echo/request/1`
* Service publishes to `echo/response/1`

## Request-Response Alternative 1

In case changes require a response both on success and failure:

* Publish values: `{applicationId}/property/{name}`
* Topic for changes: `{applicationId}/property/{name}/request`
* Topic for change responses: `{applicationId}/property/{name}/response`

## Request-Response Alternative 2

In case changes require a response both on success and failure where change
requests can be identified uniquely:

* Publish values: `{applicationId}/property/{name}`
* Topic for changes: `{applicationId}/property/{name}/request/{id}`
* Topic for change responses: `{applicationId}/property/{name}/response/{id}`

## Request-Response Alternative 3

In case changes require a response only on success:

* Publish topic: `{applicationId}/property/{name}`
* Topic for changes: `{applicationId}/property/{name}/set`

## Second hands on

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

## Q&A

Questions?

## Thanks

* dpsenner@gmail.com
* dpsenner@apache.org

