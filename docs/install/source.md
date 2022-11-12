---
layout: default
title: Build from Source
parent: Install
nav_order: 1
---

# Build from Source

To compile and run the server on your machine, follow the below steps.

## dotnet 6.0

1. Install the latest [dotnet 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
1. Clone the GitHub repository locally
1. In the local repo, find the file named `configuration.example.json`. Make a copy of it and name it `configuration.local.json`.
1. Move `configuration.local.json` into the `src/Console` directory
1. Open a terminal and run the below one-time setup steps:

```bash
> cd src/Console
> dotnet restore
> dotnet build
```

## To run

```bash
> dotnet run --project ./src/Console/Console.csproj
```

## Updating

```bash
> git fetch
> git pull
> cd `docgraf`
> dotnet restore ./src/Console/Console.csproj
> dotnet build ./src/Console/Console.csproj
> dotnet run --project ./src/Console/Console.csproj
```