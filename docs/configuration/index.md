---
layout: default
title: Configuration
nav_order: 3
has_children: true
---

# Configuration

DocGraf supports configuration via [command line arguments]({{ site.baseurl }}{% link configuration/command-line.md %}), [environment variables]({{ site.baseurl }}{% link configuration/environment-variables.md %}), and [json config file]({{ site.baseurl }}{% link configuration/json.md %}). By default, DocGraf looks for a file named `configuration.local.json` in the same directory where it is run.  This is the preferred way to provide configuration details to DocGraf.

## Example Config

```json
{
  "Docker": {
    "Uri": "http://docker-proxy:2375",
    "ContainerEvents": [ "start", "stop", "restart" ]
  },

  "Grafana": {
    "ApiKey":  "",
    "Uri": "http://grafana:3000"
  },

  "Observability": {

    "Prometheus": {
      "Enabled": false,
      "Port": 4000
    },

    "Tracing": {
      "Enabled": true,
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
}
```

## Config Precedence

The following defines the precedence in which config definitions are honored. With the first item overriding any below it.

1. Command Line
1. Environment Variables
1. Config File

For example, if you defined your Peloton credentials ONLY in the Config file, then the Config file credentials will be used.

If you defined your credentials in both the Config file AND the Environment variables, then the Environment variable credentials will be used.

If you defined credentials using all 3 methods (config file, env, and command line), then the credentials provided via the command line will be used.