#!/bin/bash
# Monta en ./ejecutable el cliente recompilado listo para "dotnet 'Intersect Client.dll'".
set -e
cd "$(dirname "$0")"
R=ejecutable
rm -rf "$R"; mkdir -p "$R"
cp -R extraido/. "$R/"
for d in descompilado/*/bin/Release/net8.0; do cp "$d"/*.dll "$R/"; done
cp -R original/resources "$R/"
cp original/*.dylib original/version.client.json "$R/"
cp "descompilado/Intersect_Client/bin/Release/net8.0/Intersect Client.runtimeconfig.json" "$R/"
python3 -I - <<'PY'
import json
# deps.json del original sin el runtime empaquetado: conserva versiones como System.Collections.Immutable 9.0.
o = json.load(open("extraido/Intersect Client.deps.json"))
rid = next(k for k in o["targets"] if "/" in k)
t = o["targets"][rid]
for k in [k for k, v in o["libraries"].items() if v.get("type") == "runtimepack"]:
    t.pop(k, None); o["libraries"].pop(k, None)
for v in t.values():
    for dep in [d for d in v.get("dependencies", {}) if d.startswith("runtimepack.")]:
        v["dependencies"].pop(dep)
o["targets"] = {".NETCoreApp,Version=v8.0": t}
o["runtimeTarget"] = {"name": ".NETCoreApp,Version=v8.0", "signature": ""}
json.dump(o, open("ejecutable/Intersect Client.deps.json", "w"), indent=2)
# Sin actualizador: no debe sobrescribir las DLLs recompiladas.
p = "ejecutable/resources/config.json"
c = json.load(open(p, encoding="utf-8-sig")); c["UpdateUrl"] = ""
json.dump(c, open(p, "w"), indent=2)
PY
echo "Listo: cd $R && DOTNET_ROOT=~/.dotnet ~/.dotnet/dotnet 'Intersect Client.dll'"
