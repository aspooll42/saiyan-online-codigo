#!/bin/bash
# Descompila los ensamblados de Intersect/Saiyan Online a proyectos C# compilables.
# Uso: ./descompilar.sh <carpeta_salida> [versión_C#]
set -u
OUT="$1"; LV="${2:-Latest}"
export DOTNET_ROOT=~/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
cd "$(dirname "$0")"
rm -rf "$OUT"; mkdir -p "$OUT"
for a in "Intersect Client" "Intersect Client Core" "Intersect Core" "Intersect.Client.Framework" \
         "Intersect.Framework.Core" "Intersect.Framework" "Intersect.Network" "Intersect.Building" "Intersect.Rsa"; do
  ~/.dotnet/tools/ilspycmd --disable-updatecheck -lv "$LV" -p -o "$OUT/${a// /_}" -r extraido "extraido/$a.dll" > /dev/null 2>&1
done
python3 -I arreglar_csproj.py "$OUT"
cp Directory.Build.props "$OUT/"
# Artefactos conocidos de ILSpy
grep -rlE '\)\(ref [A-Za-z_][A-Za-z0-9_]*\);' --include='*.cs' "$OUT" | while read -r f; do
  perl -pi -e 's/= \([^;=]*?\)\(ref ([A-Za-z_]\w*)\);/= $1;/g' "$f"; done
grep -rl 'RefSafetyRules' --include='AssemblyInfo.cs' "$OUT" | while read -r f; do
  perl -ni -e 'print unless /RefSafetyRules/' "$f"; done
