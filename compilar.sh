#!/bin/bash
# Compila cada proyecto de <carpeta> y resume los errores. Uso: ./compilar.sh <carpeta>
export DOTNET_ROOT=~/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
cd "$(dirname "$0")"; D="$1"; mkdir -p logs
for p in "$D"/*/*.csproj; do n=$(basename "$(dirname "$p")")
  ~/.dotnet/dotnet build "$p" -c Release -v q -clp:NoSummary > "logs/$D-$n.log" 2>&1
  echo "$n: errores=$(grep -hE 'error [A-Z]+[0-9]+' "logs/$D-$n.log" | sed -E 's| \[/[^]]*\]$||' | sort -u | wc -l | tr -d ' ')"
done
