#!/bin/bash

dotnet build -c Release

pid=$!

wait $pid

clear

dotnet run Geno.csproj -c Release