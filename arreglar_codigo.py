"""Corrige artefactos de ILSpy que impiden compilar el código descompilado.

Uso: python3 -I arreglar_codigo.py <carpeta_descompilado>
"""
import pathlib
import re
import sys

root = pathlib.Path(sys.argv[1])


def edit(rel, old, new, count=1):
    p = root / rel
    s = p.read_text()
    if old not in s:
        print(f"  AVISO: no encontrado en {rel}: {old[:50]!r}")
        return
    p.write_text(s.replace(old, new, count))
    print(f"  {rel}")


# 1. Llamadas explícitas al constructor base (implícitas en C#).
for p in root.rglob("*.cs"):
    s = p.read_text()
    if "base._002Ector();" in s:
        p.write_text(re.sub(r"\n\s*base\._002Ector\(\);", "", s))

# 2. Tipos generados por el compilador sin sus campos.
for p in root.glob("*/--z__ReadOnlySingleElementList.cs"):
    s = p.read_text()
    s = s.replace("\tprivate sealed class Enumerator : IDisposable, IEnumerator, IEnumerator<T>\n\t{\n",
                  "\tprivate sealed class Enumerator : IDisposable, IEnumerator, IEnumerator<T>\n\t{\n"
                  "\t\tprivate readonly T _item;\n\n\t\tprivate bool _moveNextCalled;\n\n", 1)
    s = s.replace("IList<T>\n{\n", "IList<T>\n{\n\tprivate readonly T _item;\n\n", 1)
    p.write_text(s)
for p in root.glob("*/--y__InlineArray*.cs"):
    s = p.read_text()
    p.write_text(s.replace("\n{\n}", "\n{\n\tprivate T _element0;\n}"))

# 3. Parámetros capturados de constructores primarios.
edit("Intersect.Framework/Intersect.Framework.Collections/ListExtensions.cs",
     "_003CkeySelector_003EP", "keySelector", count=-1)
for rel in ["Intersect.Client.Framework/Intersect.Client.Framework.Graphics/Font.cs",
            "Intersect_Client_Core/Intersect.Client.Classes.MonoGame.Graphics/MonoTileBuffer.cs"]:
    p = root / rel
    s = p.read_text()
    for name in sorted(set(re.findall(r"_003C(\w+?)_003EP", s))):
        # Tipo tomado de la lista de parámetros del constructor.
        m = re.search(r"([\w.?\[\]]+(?:<[\w.?, ]+>)?)\s+" + name + r"\s*[,)]", s)
        field = f"\tprivate readonly {m.group(1).strip()} _003C{name}_003EP;\n\n"
        s = re.sub(r"(\n(?:public |internal |)(?:abstract |sealed |)class [^\n]*\n\{\n)", r"\1" + field.replace("\\", "\\\\"), s, count=1)
    p.write_text(s)
    print(f"  {rel}")

# 4. Casos sueltos.
edit("Intersect_Client/Program.cs", "_003CMain_003E_0024", "Main")
edit("Intersect_Client_Core/Intersect.Client.Maps/MapInstance.cs",
     "MapZone IMapInstance.get_ZoneType()\n\t{\n\t\treturn base.ZoneType;\n\t}",
     "MapZone IMapInstance.ZoneType => base.ZoneType;")
edit("Intersect.Framework.Core/Intersect.Models/ObjectCacheKey.cs",
     "\tpublic ObjectCacheKey(Id<TObject> Id, string? Checksum = null, string? Version = null)\n\t{\n"
     "\t\tthis.Id = Id;\n\t\tthis.Checksum = Checksum;\n\t\tthis.Version = Version;\n\t}\n\n", "")
edit("Intersect_Client_Core/Intersect.Client.Localization/Touches.cs",
     "\t[GeneratedCode(\"System.Text.RegularExpressions.Generator\", \"8.0.14.42015\")]\n"
     "\tprivate static Regex Jeton()\n\t{\n\t\treturn _003CRegexGenerator_g_003EF9F149524D662D65CE48B66900A94E16EB046298933008D70962E066F465F6062__Jeton_0.Instance;\n\t}",
     "\tprivate static partial Regex Jeton();")
edit("Intersect_Client_Core/Intersect.Client.Localization/Touches.cs",
     "public static class Touches", "public static partial class Touches")
p = root / "Intersect.Client.Framework/Intersect.Client.Interface.Data/DataProvider.cs"
s = p.read_text()
start = s.index("\tprotected bool TrySetValue(TValue value)")
end = s.index("\tprotected void EmitValueChanged")
p.write_text(s[:start] + """\tprotected bool TrySetValue(TValue value)
\t{
\t\tTValue value2 = Value;
\t\tbool num;
\t\tif (value != null)
\t\t{
\t\t\tnum = value.Equals(value2);
\t\t}
\t\telse if (value2 != null)
\t\t{
\t\t\tnum = value2.Equals(value);
\t\t}
\t\telse
\t\t{
\t\t\treturn false;
\t\t}
\t\tif (num)
\t\t{
\t\t\treturn false;
\t\t}
\t\tValue = value;
\t\tEmitValueChanged(value, value2);
\t\treturn true;
\t}

""" + s[end:])
print("  DataProvider.cs")

# 5. Más tipos generados por el compilador.
for p in root.glob("*/--z__ReadOnlyArray.cs"):
    s = p.read_text()
    p.write_text(s.replace("IList<T>\n{\n", "IList<T>\n{\n\tprivate readonly T[] _items;\n\n", 1))
for d in root.iterdir():
    if d.is_dir() and any("_003CPrivateImplementationDetails_003E.InlineArray" in f.read_text()
                          for f in d.rglob("*.cs")):
        (d / "-InlineArrayHelpers-.cs").write_text("""using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static partial class _003CPrivateImplementationDetails_003E
{
\tinternal static ref TElement InlineArrayElementRef<TBuffer, TElement>(ref TBuffer buffer, int index)
\t{
\t\treturn ref Unsafe.Add(ref Unsafe.As<TBuffer, TElement>(ref buffer), index);
\t}

\tinternal static Span<TElement> InlineArrayAsSpan<TBuffer, TElement>(ref TBuffer buffer, int length)
\t{
\t\treturn MemoryMarshal.CreateSpan(ref Unsafe.As<TBuffer, TElement>(ref buffer), length);
\t}

\tinternal static ReadOnlySpan<TElement> InlineArrayAsReadOnlySpan<TBuffer, TElement>(in TBuffer buffer, int length)
\t{
\t\treturn MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TBuffer, TElement>(ref Unsafe.AsRef(in buffer)), length);
\t}
}
""")
        print(f"  {d.name}/-InlineArrayHelpers-.cs")

edit("Intersect.Framework.Core/Intersect.Localization/LocalizedString.cs",
     "return localizedString.value;", "return localizedString._value;")


# 6. Asignaciones a propiedades init/required -> inicializador de objeto.
def statement_end(lines, i):
    """Índice de la última línea de la sentencia que empieza en lines[i]."""
    depth = 0
    for j in range(i, len(lines)):
        depth += sum(lines[j].count(c) for c in "({[") - sum(lines[j].count(c) for c in ")}]")
        if depth == 0 and lines[j].rstrip().endswith(";"):
            return j
    return None


decl = re.compile(r"^(\s*)((?:[\w.<>\[\]?, ]+) (\w+) = new [\w.<>\[\]?, ]+\([^;]*\));\s*$")
total = 0
for p in root.rglob("*.cs"):
    lines = p.read_text().split("\n")
    out, i, changed = [], 0, False
    while i < len(lines):
        m = decl.match(lines[i])
        if m:
            indent, head, var = m.groups()
            props, j = [], i + 1
            while j < len(lines):
                a = re.match(r"^" + re.escape(indent) + re.escape(var) + r"\.(\w+) = ", lines[j])
                end = statement_end(lines, j) if a else None
                if not a or end is None:
                    break
                body = "\n".join(lines[j:end + 1])
                rhs = body[len(indent) + len(var) + 1:].rstrip()[:-1]
                if re.search(r"\b" + re.escape(var) + r"\b", rhs.split("=", 1)[1]):
                    break
                props.append(rhs)
                j = end + 1
            if props:
                out.append(indent + head)
                out.append(indent + "{")
                out.extend(indent + "\t" + pr.replace("\n", "\n\t") + "," for pr in props)
                out.append(indent + "};")
                i, changed = j, True
                total += 1
                continue
        out.append(lines[i])
        i += 1
    if changed:
        p.write_text("\n".join(out))
print(f"  inicializadores de objeto: {total}")

# 7. Casos sueltos (segunda ronda).
C = "Intersect_Client_Core/"
for f in ["Intersect.Framework.Core/Intersect.Collections/Sanitizer.cs"]:
    p = root / f
    p.write_text(re.sub(r"\n\s*reference\d* = ref val\d*;", "", p.read_text()))
edit("Intersect.Framework.Core/Intersect.Framework.Core.Serialization/AggregateContractResolver.cs",
     "IRestrictedContractResolver[] contractResolvers = contractResolvers;",
     "IRestrictedContractResolver[] contractResolvers = _contractResolvers;")
p = root / (C + "Intersect.Client.MonoGame.Graphics/MonoRenderer.cs")
p.write_text(re.sub(r"(?<=[\w)\]])\.platformBuffer\b", ".PlatformBuffer", p.read_text()))
edit(C + "Intersect.Client.Interface.Game/StoryThread.cs", ".Max()).GetValueOrDefault();", ".Max());")
edit(C + "Intersect.Client.Maps/Meteo.cs", "P_1.lues", "lues", count=-1)
edit(C + "Intersect.Client.Entities/Recolte.cs",
     "\t\t\tfloat arriveeH = bout.H - 11f;\n",
     "\t\t\tfloat arriveeH = bout.H - 11f;\n"
     "\t\t\tPoint3 Arc(float k) => new Point3(Lerp(poisson.DepartX, arriveeX, k), Lerp(poisson.DepartY, arriveeY, k), "
     "Lerp(0f, arriveeH, k) + MathF.Sin(MathF.PI * k) * 30f);\n")
edit(C + "Intersect.Client.Maps/Casse.cs",
     "if (!carte.Layers.TryGetValue(couche, out value) || x < 0 || y < 0 || x >= value.GetLength(0) || y >= value.GetLength(1))",
     "if (!carte.Layers.TryGetValue(couche, out Tile[,] grille) || x < 0 || y < 0 || x >= grille.GetLength(0) || y >= grille.GetLength(1))")
edit(C + "Intersect.Client.Maps/Casse.cs", "Tile tile = value[x, y];", "Tile tile = grille[x, y];")
edit(C + "Intersect.Client.Interface.Shared/ConfirmCardWindow.cs",
     "\t\tConfirmCardRequest obj = new ConfirmCardRequest\n\t\t{\n\t\t\tTitle = Strings.ConfirmCard.QuitTitle,\n"
     "\t\t\tIcon = \"saiyan_carte_icone_quitter.png\"\n\t\t};\n\t\tstring text = Globals.Me?.Name;\n",
     "\t\tstring text = Globals.Me?.Name;\n\t\tConfirmCardRequest obj = new ConfirmCardRequest\n\t\t{\n"
     "\t\t\tTitle = Strings.ConfirmCard.QuitTitle,\n\t\t\tIcon = \"saiyan_carte_icone_quitter.png\",\n"
     "\t\t\tHeading = ((text != null && text.Length > 0) ? text : Strings.ConfirmCard.GameName.ToString()),\n"
     "\t\t\tPrompt = Strings.ConfirmCard.QuitPrompt,\n\t\t\tDetail = Strings.ConfirmCard.QuitGoodbye,\n"
     "\t\t\tChip = (inCombat ? ((string)Strings.ConfirmCard.QuitInCombat) : (OutlawChip() ?? ((string)Strings.ConfirmCard.QuitSaved))),\n"
     "\t\t\tChipDanger = inCombat || OutlawChip() != null,\n\t\t\tConfirmLabel = Strings.ConfirmCard.Quit,\n"
     "\t\t\tCancelLabel = Strings.ConfirmCard.Stay,\n\t\t\tDanger = true,\n\t\t\tShowMe = true,\n\t\t\tCentered = true\n\t\t};\n")
p = root / (C + "Intersect.Client.Interface.Shared/ConfirmCardWindow.cs")
s = p.read_text()
s = re.sub(r"\n\t\tobj\.(Heading|Prompt|Detail|Chip|ChipDanger|ConfirmLabel|CancelLabel|Danger|ShowMe|Centered) = [^\n]*;(?=[\s\S]*?Open\(obj, onConfirm, onCancel\);)", "", s, count=10)
p.write_text(s)

# 8. Tuplas que perdieron los nombres de sus elementos: se usan los nombres
#    que aparecen en otro sitio del mismo archivo.
TUPLE = re.compile(r"\(((?:[\w.]+(?:<[^<>()]*>)?\??(?:\[\])? \w+)(?:, [\w.]+(?:<[^<>()]*>)?\??(?:\[\])? \w+)+)\)")
for f in ["Intersect.Client.Interface.Game.Hdv/FenetreHdv.cs", "Intersect.Client.Interface.Game.Inventory/InventoryWindow.cs"]:
    p = root / (C + f)
    s = p.read_text()
    named = {}
    for m in TUPLE.finditer(s):
        parts = [x.rsplit(" ", 1) for x in m.group(1).split(", ")]
        named.setdefault("(" + ", ".join(t for t, _ in parts) + ")", m.group(0))
    for bare, full in named.items():
        s = re.sub(r"(?<![\w.])" + re.escape(bare) + r"(?=[>\s?\[)])", full, s)
    p.write_text(s)
    print(f"  tuplas: {f} ({len(named)})")

# 9. Tuplas y casos sueltos restantes.
P = C + "Intersect.Client.Interface.Game.Personnage/PersonnageScreen.cs"
edit(P, "List<(int, ItemDescriptor, int)> list = new List<(int, ItemDescriptor, int)>();",
     "List<(int Case, ItemDescriptor Objet, int Quantite)> list = new List<(int Case, ItemDescriptor Objet, int Quantite)>();")
edit(P, "List<(int, ItemDescriptor, int)> list3 = (from entree in list",
     "List<(int Case, ItemDescriptor Objet, int Quantite)> list3 = (from entree in list")
edit(P, "\t\t\t_ => list3, ", "\t\t\t_ => list3.OrderBy(entree => 0), ")
edit(C + "Intersect.Client.Interface.Game.Inventory/InventoryWindow.cs", "\t\t\t_ => list, ", "\t\t\t_ => list.OrderBy(entry => 0), ")
edit(C + "Intersect.Client.Interface.Game/QuestsWindow.cs",
     "List<(QuestDescriptor, QuestState)> list = new List<(QuestDescriptor, QuestState)>();",
     "List<(QuestDescriptor Quest, QuestState State)> list = new List<(QuestDescriptor Quest, QuestState State)>();")
K = C + "Intersect.Client.Maps/Casse.cs"
edit(K, "List<(int, int, int)> source = braises.Amas.Where(", "List<(int X, int Y, int N)> source = braises.Amas.Where(")
edit(K, "\t\tHashSet<(int, int)> hashSet = new HashSet<(int, int)>();\n\t\tHashSet<(int, int)> hashSet2",
     "\t\tHashSet<(int X, int Y)> hashSet = new HashSet<(int X, int Y)>();\n\t\tHashSet<(int, int)> hashSet2")
M = C + "Intersect.Client.Maps/Meteo.cs"
edit(M, "\t\tList<(short, short, short)> list = new List<(short, short, short)>();",
     "\t\tList<(short X, short Haut, short Bas)> list = new List<(short X, short Haut, short Bas)>();")
edit(M, "(byte[], int, int)? tuple2 = Pixels(item2);", "(byte[], int, int)? tuple2 = Pixels(item2, lues);")
edit(M, "((ValueType)Pixels(item13.Image))", "((ValueType)Pixels(item13.Image, lues))")
edit(M, "(byte[] Rgba, int L, int H)? Pixels(IGameTexture texture)",
     "(byte[] Rgba, int L, int H)? Pixels(IGameTexture texture, Dictionary<IGameTexture, (byte[] Rgba, int L, int H)?> lues)")

# 10. ILSpy pierde [JsonConstructor] al convertir en constructor primario: sin él,
# Newtonsoft no sabe qué constructor usar y no carga las teclas guardadas.
CB = "Intersect.Client.Framework/Intersect.Client.Framework.Input/ControlBinding.cs"
if "[method: JsonConstructor]" not in (root / CB).read_text():
    edit(CB, "using Intersect.Client.Framework.Gwen.DragDrop;\n",
         "using Intersect.Client.Framework.Gwen.DragDrop;\nusing Newtonsoft.Json;\n")
    edit(CB, "public class ControlBinding(Keys modifier, Keys key)",
         "[method: JsonConstructor]\npublic class ControlBinding(Keys modifier, Keys key)")
