---
layout: default
title: Prometheus Metrics
parent: Features
nav_order: 1
---

# Prometheus Metrics

DocGraf can be [configured]({{ site.baseurl }}{% link configuration/index.md %}#metrics-config) to expose [Prometheus Metrics](https://prometheus.io/).  The metrics exposed are grouped into two categories:

1. `docgraf_docker_events_recv`
	1. This metric will always capture all received events from the Docker daemon
1. `docgraf_docker_events_recorded`
	1. This metric will be a subset of `docgraf_docker_events_recv`, it is only the events that were recorded as Annotations

Both metrics expose the following labels:

1. Event Type
1. Event
1. Container
1. Image
1. Image Tag

![Grafana Metrics Example](https://github.com/philosowaffle/docgraf/raw/main/images/metrics_example.png?raw=true "Grafana Metrics Example")
