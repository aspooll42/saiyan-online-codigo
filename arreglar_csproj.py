"""Ajusta los .csproj de ILSpy: rutas a extraido/ y quita referencias que ya da el SDK."""
import pathlib, re, sys
root = pathlib.Path(sys.argv[1])
for p in root.glob("*/*.csproj"):
    s = p.read_text()
    s = re.sub(r'\s*<Reference Include="(System(?!\.IO\.Hashing)|Microsoft\.(Win32|CSharp|VisualBasic)|netstandard|mscorlib)[^"]*">\s*<HintPath>[^<]*</HintPath>\s*</Reference>', "", s)
    s = s.replace("<HintPath>extraido/", "<HintPath>$(MSBuildThisFileDirectory)../../extraido/")
    s = s.replace("netcoreapp8.0", "net8.0")
    p.write_text(s)
    print(p.name, s.count("<Reference "), "referencias")
