---
layout: default
title: JSON Config File
parent: Configuration
nav_order: 0
---

# Json Config File

This is the preferred way to provide configuration details to DocGraf.  By default, DocGraf looks for a file named `configuration.local.json` in the same directory where the program is run.

The config file is written in JSON and supports hot-reload for all fields except the following:

1. `Observability` Section

The config file is organized into the below sections.

| Section      | Description       |
|:-------------|:------------------|
| [Docker Config](#format-config) | This section provides settings related to Docker.  |
| [Grafana Config](#peloton-config) | This section provides settings related to Grafana.      |
| [Observability Config](#observability-config) | This section provides settings related to Metrics, Logs, and Traces for monitoring purposes. |

## Docker Config

This section provides settings related to connecting to the Docker daemon and which events should be acted on.

```json
"Docker": {
    "Uri": "http://docker-proxy:2375",
    "ContainerEvents": [ "start", "stop", "restart" ],
    "ImageEvents": [],
    "PluginEvents": [],
    "VolumeEvents": [],
    "DaemonEvents": [],
    "ServiceEvents": [],
    "NodeEvents": [],
    "SecretEvents": [],
    "ConfigEvents": [],
  }
```

| Field      | Required | Default | Description |
|:-----------|:---------|:--------|:------------|
| Uri | no | `http://localhost:4243` | The full protocol, host, and port to connect to the docker daemon. |
| ContainerEvents | no | `["start", "stop", "restart"]` | The list of [Container events](https://docs.docker.com/engine/reference/commandline/events/#object-types) that should be recorded as annotations.  |
| ImageEvents | no | `[]` | The list of [Image events](https://docs.docker.com/engine/reference/commandline/events/#object-types) that should be recorded as annotations.  |
| PluginEvents | no | `[]` | The list of [Plugin events](https://docs.docker.com/engine/reference/commandline/events/#object-types) that should be recorded as annotations.  |
| VolumeEvents | no | `[]` | The list of [Volume events](https://docs.docker.com/engine/reference/commandline/events/#object-types) that should be recorded as annotations.  |
| DaemonEvents | no | `[]` | The list of [Daemon events](https://docs.docker.com/engine/reference/commandline/events/#object-types) that should be recorded as annotations.  |
| ServiceEvents | no | `[]` | The list of [Service events](https://docs.docker.com/engine/reference/commandline/events/#object-types) that should be recorded as annotations.  |
| NodeEvents | no | `[]` | The list of [Node events](https://docs.docker.com/engine/reference/commandline/events/#object-types) that should be recorded as annotations.  |
| SecretEvents | no | `[]` | The list of [Secret events](https://docs.docker.com/engine/reference/commandline/events/#object-types) that should be recorded as annotations.  |
| ConfigEvents | no | `[]` | The list of [Config events](https://docs.docker.com/engine/reference/commandline/events/#object-types) that should be recorded as annotations.  |

## Grafana Config

This section provides settings related to connecting and publishing annotations to Grafana.

```json
"Grafana": {
    "ApiKey":  "yourApiKey==",
    "Uri": "http://grafana:3000"
  },
```

| Field      | Required | Default | Description |
|:-----------|:---------|:--------|:------------|
| ApiKey | yes | `null` | Your [Grafana Api Token](https://grafana.com/docs/grafana/latest/http_api/auth/#create-api-token). |
| Uri | no | `http://grafana:3000` | The full protocol, host, and port to connect to Grafana.  |

## Observability Config

DocGraf supports publishing OpenTelemetry Metrics, Logs, and Trace. This section provides settings related to those pillars.

The Observability config section contains three main sub-sections:

1. [Prometheus](#prometheus-config) - Metrics
1. [Jaeger](#jaeger-config) - Traces
1. [Serilog](#serilog-config) - Logs

```json
"Observability": {

    "Prometheus": {
      "Enabled": false,
      "Port": 4000
    },

    "Tracing": {
      "Enabled": false,
      "Url": "http://localhost:4317"
    },

    "Serilog": {
      "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File" ],
      "MinimumLevel": "Information",
      "WriteTo": [
        { "Name": "Console" },
        {
          "Name": "File",
          "Args": {
            "path": "./output/log.txt",
            "rollingInterval": "Day",
            "retainedFileCountLimit": 7
          }
        }
      ]
    }
  }
```

### Prometheus Config

```json
"Prometheus": {
      "Enabled": false,
      "Port": 4000
    }
```

| Field      | Required | Default | Description |
|:-----------|:---------|:--------|:------------|
| Enabled | no | `false` | Whether or not to expose metrics. Metrics will be available at `http://localhost:{port}/metrics` |
| Port | no | `false` | The port the metrics endpoint should be served on. |

If you are using Docker, ensure you have exposed the port from your container.

#### Example Prometheus scraper config

```yaml
- job_name: 'p2g'
    scrape_interval: 60s
    static_configs:
      - targets: [<p2gIPaddress>:<p2gPort>]
    tls_config:
      insecure_skip_verify: true
```

### Tracing Config

```json
"Tracing": {
      "Enabled": false,
      "Url": "http://localhost:4317"
    }
```

| Field      | Required | Default | Description |
|:-----------|:---------|:--------|:------------|
| Enabled | no | `false` | Whether or not to generate traces. |
| Url | **yes - if Enalbed=true** | `null` | The host address for your trace collector. Traces are published using OTLP. |

### Serilog Config

```json
"Serilog": {
      "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File",  "Serilog.Sinks.Grafana.Loki" ],
      "MinimumLevel": "Debug",
      "WriteTo": [
        { "Name": "Console" },
        {
          "Name": "File",
          "Args": {
            "path": "./output/log.txt",
            "rollingInterval": "Day",
            "retainedFileCountLimit": 7
          }
        },
        {
          "Name": "GrafanaLoki",
          "Args": {
            "uri": "http://192.168.1.95:3100",
            "textFormatter": "Serilog.Sinks.Grafana.Loki.LokiJsonTextFormatter, Serilog.Sinks.Grafana.Loki",
            "labels": [
              {
                "key": "app",
                "value": "p2g"
              }
            ]
          }
        }]
}
```

| Field      | Required | Default | Description |
|:-----------|:---------|:--------|:------------|
| Using | no | `null` | A list of sinks you would like use. The valid sinks are listed in the examplea above. |
| MinimumLevel | no | `null` | The minimum level to write. `[Verbose, Debug, Information, Warning, Error, Fatal]` |
| WriteTo | no | `null` | Additional config for various sinks you are writing to. |

More detailed information about configuring Logging can be found on the [Serilog Config Repo](https://github.com/serilog/serilog-settings-configuration#serilogsettingsconfiguration--).