#!/usr/bin/env bash

if [ -z "$1" ] ; then
    echo "Must specify migration name"
    exit 1
fi

dotnet ef migrations add --context BenchmarkContext -o Migrations "$1"
