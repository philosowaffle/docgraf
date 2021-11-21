#!/bin/bash
set -e

if [[ "$1" == "console" ]]; then
    dotnet /app/Console.dll
fi