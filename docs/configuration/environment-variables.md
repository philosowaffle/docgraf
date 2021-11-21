---
layout: default
title: Environmnet Variables
parent: Configuration
nav_order: 2
---

# Environment Variable Configuration

All of the values defined in the [Json config file]({{ site.baseurl }}{% link configuration/json.md %}) can also be defined as environment variables. This functionality is provided by the default dotnet [IConfiguration interface](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-5.0#environment-variables-1).

The variables use the following convention, note the use of both single and double underscores:

```bash
DOCGRAF_CONFIGSECTION__CONFIGPROPERTY=value
```

#### Example Grafana Config

```bash
DOCGRAF_GRAFANA__URI
DOCGRAF_GRAFANA__APIKEY
```

#### Example Arrays

```bash
DOCGRAF_GRAFANA__CONTAINEREVENTS__0="start"
DOCGRAF_GRAFANA__CONTAINEREVENTS__1="stop"
DOCGRAF_GRAFANA__CONTAINEREVENTS__2="restart"
...and so on
```