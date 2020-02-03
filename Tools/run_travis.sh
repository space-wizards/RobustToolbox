#!/usr/bin/env bash

set -e

# Use manually installed .NET.
# Travis is shitting itself. Wonderful.
PATH="~/.dotnet:$PATH"

dotnet build RobustToolbox.sln /p:Python=python3.6
dotnet test Robust.UnitTesting/Robust.UnitTesting.csproj
