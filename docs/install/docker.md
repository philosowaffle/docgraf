---
layout: default
title: Docker
parent: Install
nav_order: 0
---

# Docker

The recommended and easiest way to get started is with [Docker](https://www.docker.com/).

## docker-compose

*Pre-requisite:* You have either `docker-compose` or `Docker Desktop` installed

1. Create a directory `docgraf`
    1. Inside this folder create a [docker-compose.yaml](https://github.com/philosowaffle/docgraf/blob/main/docker/docker-compose.yaml) file in the directory
    1. Also create a [configuration.local.json](https://github.com/philosowaffle/docgraf/blob/main/configuration.example.json) file in the directory.
1. Edit the config file to set your Grafana host and Service Account token
1. Open a terminal in this folder
1. Run: `docker-compose pull && docker-compose up -d`

You can learn more about customizing your configuration over in the [Configuration Section]({{ site.baseurl }}{% link configuration/index.md %}).

### Docker Daemon

It is recommened to run DocGraf against a docker proxy like the provided `tecnativa/docker-socket-proxy`.

## Repositories

### [DockerHub](https://hub.docker.com/r/philosowaffle/docgraf)

```bash
docker run -v /full/path/to/configuration.local.json:/app/configuration.local.json -v /full/path/to/output:/app/output philosowaffle/docgraf:stable
```

### [GitHub Package](https://github.com/philosowaffle/docgraf/pkgs/container/docgraf)

```bash
docker run -v /full/path/to/configuration.local.json:/app/configuration.local.json -v /full/path/to/output:/app/output ghcr.io/philosowaffle/docgraf:stable
```

## Tags

1. `stable` - Always points to the latest release
1. `latest` - The bleeding edge of the master branch, breaking changes may happen
1. `X.Y.Z` - For using a specific released version

## Docker User

The images run the process under the user and group `docgraf:docgraf` with uid and gid `1019:1019`.  To access files created by `docgraf`:

1. Create a group on the local machine `docgraf` with group id `1019`
1. Add your user on the local machine to the `docgraf` group