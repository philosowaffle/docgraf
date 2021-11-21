---
layout: default
title: Build from Source
parent: Install
nav_order: 1
---

# Build from Source

To compile and run DocGraf on your machine, follow the below steps:

1. Install the [dotnet 6.0 sdk](https://dotnet.microsoft.com/download/dotnet/6.0)
1. Clone this repository locally
1. In the local repo, find the file named `configuration.example.json`. Make a copy of it and name it `configuration.local.json`.
1. Move `configuration.local.json` into the `src/Console` directory
1. Open `configuration.local.json` in a text editor of your choice and configure your settings.
1. Open a terminal and run the below:

```bash
> cd docgraf
> dotnet restore
> dotnet build
> dotnet run --project ./src/Console/Console.csproj
```