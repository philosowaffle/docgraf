---
layout: default
title: Install
nav_order: 1
has_children: true
---

# Install

DocGraf can be run on all major operating systems. The recommended and easiest way to get started is with [Docker]({{ site.baseurl }}{% link install/docker.md %}).

1. [Create a service account](https://grafana.com/docs/grafana/latest/administration/service-accounts/#create-a-service-account-in-grafana) in Grafana for DocGraf
	1. Role: Editor
1. [Add a token to the service account](https://grafana.com/docs/grafana/latest/administration/service-accounts/#add-a-token-to-a-service-account-in-grafana)
	1. Make note of this token
1. Run DocGraf
	1. [Docker]({{ site.baseurl }}{% link install/docker.md %})
	1. [Build from Source]({{ site.baseurl }}{% link install/source.md %})