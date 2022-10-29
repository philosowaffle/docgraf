#!/bin/bash
set -e

chown -R docgraf:docgraf /app
chmod 770 -R /app

if [[ "$1" == "console" ]]; then
    dotnet /app/Console.dll
fi