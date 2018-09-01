---
title: "MQTT"
author: Dominik Psenner
date: Aug 31, 2018
---

# Welcome

Welcome to HTA 2018.

## Who am I

* Software engineer since 2005
* Active member of the Apache Software Foundation
* Working in the industry, providing customers with automation and control systems

## Request - Response

* Application provides endpoints as a service
* Well known pattern

## Request - Response: Considerations

* Tight coupling between client and service
* Extending existing endpoints with more functionality
* Endpoints as a facade or proxy
* Service bus behind facade or proxy

## Publish-Subscribe

![MQTT Publisher Subscriber](mqtt_publisher_subscriber-1.png)

## What is MQTT

MQTT is a light weight protocol that:

* provides publish-subscribe pattern
* is designed to consume only very little power
* requires only a very small memory footprint
* requires very low bandwith

## Publish-Subscribe: Features

* Decouples publisher and subscriber spatially
* Decouples publisher and subscriber by time

## Publish-Subscribe: Benefits

* Scalability
* Extensibility
* Naturally fits async and event driven programming

## Publish-Subscribe: Considerations

* Designing topics can be challenging
* Designing payloads can be challenging
* Publisher is decoupled from subscriber
* Request-Response is a special case

## First hands on

* Clients that publish temperature information
es

## Publish-Subscribe: Request-Response

* Request topic:  `{Endpoint}/Request/{RequestId}`
* Response topic: `{Endpoint}/Response/{RequestId}`

## Publish-Subscribe: Echo

* Service subscribes on `Echo/Request/+`
* Client subscribes on `Echo/Response/1`
* Client publishes on `Echo/Request/1`
* Service publishes on `Echo/Response/1`

## Second hands on

## Questions?

## Thanks

* dpsenner@gmail.com
* dpsenner@apache.org

