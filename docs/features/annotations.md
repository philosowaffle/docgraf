---
layout: default
title: Grafana Annotations
parent: Features
nav_order: 0
---

# Grafana Annotations

DocGraf can be [configured]({{ site.baseurl }}{% link configuration/index.md %}#docker-config) to publish [Grafana Annotations](https://grafana.com/docs/grafana/v9.0/dashboards/annotations/) for specific [Docker Events](https://docs.docker.com/engine/reference/commandline/events/).  When a configured event occurrs, DocGraf will publish that event as an Annotation to Grafana with the following information:

* *Text*: `<event> <container> <tag>`
* *Timestamp* of the event
* Tags:
	* Event Type
	* Event
	* Container
	* Image
	* Image Tag

These annotations can be visualized in several ways on Grafana such as a list of recent events:

![Grafana Annotations List](https://github.com/philosowaffle/docgrag/raw/main/images/annotations_list.png?raw=true "Grafana Annotations List")

Or overlayed on top of other charts to correlate container events with out system metrics:

![Annotation Overlay Demo](https://github.com/philosowaffle/docgrag/raw/main/images/annotation_overlay_demo.png?raw=true "Annotation Overlay Demo")