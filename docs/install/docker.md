---
layout: default
title: Docker
parent: Install
nav_order: 0
---

# Docker

The recommended and easiest way to get started is with Docker. To learn more about Docker head on over to their [website](https://www.docker.com/).

## docker-compose

A sample [docker-compose.yaml](https://github.com/philosowaffle/docgraf/blob/master/docker-compose.yaml) file and [configuration.local.json](https://github.com/philosowaffle/docgraf/blob/master/configuration.example.json) can be found in the project repo.

The Docker container expects a valid `configuration.local.json` file is mounted into the container.  You can learn more about the configuration file over in the [Configuration Section](/{{ site.baseurl }}{% link configuration/index.md %}).

```yaml
version: "3.9"
services:
  docgraf:
    image: philosowaffle/docgraf
    container_name: docgraf
    restart: unless-stopped
    environment:
      - TZ=America/Chicago
    volumes:
      - ./configuration.local.json:/app/configuration.local.json

  docker-proxy: 
    image: tecnativa/docker-socket-proxy
    container_name: docker-proxy 
    restart: unless-stopped 
    environment: 
      - TZ=America/Chicago
    volumes:
        - /var/run/docker.sock:/var/run/docker.sock:ro
```

### Docker Daemon

It is recommened to run DocGraf against a docker proxy like `tecnativa/docker-socket-proxy`.

### Prometheus

If you configure DocGraf to serve Prometheus metrics then you will also need to map the corresponding port for your docker container. By default, Prometheus metrics will be served on port `4000`. You can learn more about DocGraf and Prometheus in the [Observability Configuration]({{ site.baseurl }}{% link configuration/index.md %}) section.

```yaml
version: "3.9"
services:
  docgraf:
    image: philosowaffle/docgraf
    container_name: docgraf
    restart: unless-stopped
    environment:
      - TZ=America/Chicago
    ports:
      - 4000:4000
    volumes:
      - ./configuration.local.json:/app/configuration.local.json
```

## Docker Tags

The DocGraf docker image is available on [DockerHub](https://hub.docker.com/r/philosowaffle/docgraf). The following tags are provided:

1. `stable` - Always points to the latest release
1. `latest` - The bleeding edge of the master branch
1. `vX.Y.Z` - For using a specific released version