#!/usr/bin/env pwsh

param([String]$name)

if ($name -eq "")
{
    Write-Error "must specify migration name"
    exit
}

dotnet ef migrations add --context BenchmarkContext -o Migrations $name
