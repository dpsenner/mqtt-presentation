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
* Publisher is decoupled from subscriber both spatially and by time
* Request-Response is a special case

## First hands on

* Clients that publish temperature information

## Publish-Subscribe: Request-Response

* Client publishes requests to `{Endpoint}/Request/{RequestId}`
* Service subscribes to `{Endpoint}/Request/+`
* Service publishes responses to `{Endpoint}/Response/{RequestId}`
* Client subscribes to response topic
* Clients should implement deadlines

## Second hands on

## Questions?

## Thanks

