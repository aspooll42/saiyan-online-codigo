"""Cambios para Android sobre el código descompilado, protegidos con #if ANDROID
para que la compilación de escritorio no cambie. Se aplica después de arreglar_codigo.py.

Uso: python3 -I adaptar_android.py <carpeta_descompilado>
"""
import pathlib
import sys

root = pathlib.Path(sys.argv[1]) / "Intersect_Client_Core"


def guard(rel, old, android=None):
    """Envuelve `old` en #if !ANDROID (con alternativa opcional para Android)."""
    p = root / rel
    s = p.read_text()
    if "#if !ANDROID\n" + old in s:
        return
    if old not in s:
        print(f"  AVISO: no encontrado en {rel}: {old.strip()[:60]!r}")
        return
    new = "#if !ANDROID\n" + old + ("\n#else\n" + android if android else "") + "\n#endif"
    p.write_text(s.replace(old, new, 1))
    print(f"  {rel}")


# La ventana no se puede mover en Android.
guard("Intersect.Client.MonoGame.Graphics/MonoRenderer.cs",
      "\t\tif (flag2)\n\t\t{\n\t\t\t_gameWindow.Position = new Microsoft.Xna.Framework.Point(sDL_Rect.x + (w - _screenWidth) / 2, sDL_Rect.y + (h - _screenHeight) / 2);\n\t\t}")
guard("Intersect.Client.MonoGame/IntersectGame.cs",
      "\t\tbase.Window.Position = new Microsoft.Xna.Framework.Point(_gameRenderer.ScreenWidth - _gameRenderer.ActiveResolution.X, _gameRenderer.ScreenHeight - _gameRenderer.ActiveResolution.Y) / new Microsoft.Xna.Framework.Point(2);\n\t\tbase.Window.AllowAltF4 = false;")
# Sin evento TextInput en Android (el teclado en pantalla se tratará aparte).
guard("Intersect.Client.MonoGame.Input/MonoInput.cs", "\t\tmyGame.Window.TextInput += Window_TextInput;")
# Game.Run() no bloquea en Android y el juego vive mientras viva la actividad.
guard("Intersect.Client.MonoGame/IntersectGame.cs",
      "\t\t\tusing IntersectGame intersectGame = new IntersectGame(context, postStartupAction);\n"
      "\t\t\ttry\n\t\t\t{\n\t\t\t\tintersectGame.Run();\n\t\t\t}",
      "\t\t\tIntersectGame intersectGame = new IntersectGame(context, postStartupAction);\n"
      "\t\t\ttry\n\t\t\t{\n\t\t\t\tIntersect.Client.Android.MainActivity.Actual.MostrarJuego(intersectGame);\n\t\t\t}")

# Sin SDL en Android: tamaño de pantalla desde MonoGame.
guard("Intersect.Client.MonoGame.Graphics/MonoRenderer.cs",
      "\t\tSDL_Rect[] displayBounds = Sdl2.GetDisplayBounds();\n\t\tSDL_Rect sDL_Rect = displayBounds[0];\n\t\tint w = sDL_Rect.w;\n\t\tint h = sDL_Rect.h;",
      "\t\tDisplayMode modoAndroid = _graphicsDeviceManager.GraphicsDevice.Adapter.CurrentDisplayMode;\n"
      "\t\tSDL_Rect sDL_Rect = default(SDL_Rect);\n\t\tint w = modoAndroid.Width;\n\t\tint h = modoAndroid.Height;")
# Portapapeles de SDL: desactivado en Android de momento.
guard("Intersect.Client.MonoGame.Input/MonoClipboard.cs",
      "\tpublic override bool IsEnabled => Sdl2.IsClipboardSupported;",
      "\tpublic override bool IsEnabled => false;")
guard("Intersect.Client.MonoGame.Input/MonoClipboard.cs",
      "\t\tSdl2.SDL_SetClipboardText(data);")
guard("Intersect.Client.MonoGame.Input/MonoClipboard.cs",
      "\t\treturn Sdl2.SDL_GetClipboardText();", "\t\treturn string.Empty;")
# Estadísticas de GPU con OpenGL de escritorio.
guard("Intersect.Client.MonoGame/IntersectGame.cs",
      "\t\tPlatformStatistics.GPUStatisticsProvider = GL.CreateGPUStatisticsProvider();")
# Steam: Android se trata como el Mac (sin Steam).
for rel in ["Intersect.Client.MonoGame.Input/MonoInput.cs", "Intersect.Client.MonoGame/IntersectGame.cs",
            "Intersect.Client.MonoGame.Graphics/MonoRenderer.cs"]:
    p = root / rel
    s = p.read_text()
    s2 = s.replace("if (!OperatingSystem.IsMacOS() && Steam.SteamDeck)",
                   "if (!OperatingSystem.IsMacOS() && !OperatingSystem.IsAndroid() && Steam.SteamDeck)")
    if s2 != s:
        p.write_text(s2)
        print(f"  {rel} (Steam)")
# GameWindow.IsBorderless no está implementado en Android.
guard("Intersect.Client.MonoGame/IntersectGame.cs",
      "\t\tbase.Window.IsBorderless = Context.StartupOptions.BorderlessWindow;")
# Hardware.Info no soporta Android: si falla, se usa la memoria que ve el GC de .NET.
p = pathlib.Path(sys.argv[1]) / "Intersect.Framework/Intersect.Framework.SystemInformation/PlatformStatistics.cs"
s = p.read_text()
s2 = (s.replace("private static readonly HardwareInfo HardwareInfo;", "private static readonly HardwareInfo? HardwareInfo;")
       .replace("(long)HardwareInfo.MemoryStatus.AvailablePhysical", "(long)(HardwareInfo?.MemoryStatus.AvailablePhysical ?? MemoriaGC)")
       .replace("(long)HardwareInfo.MemoryStatus.TotalPhysical", "(long)(HardwareInfo?.MemoryStatus.TotalPhysical ?? MemoriaGC)")
       .replace("(long)HardwareInfo.MemoryStatus.AvailableVirtual", "(long)(HardwareInfo?.MemoryStatus.AvailableVirtual ?? MemoriaGC)")
       .replace("(long)HardwareInfo.MemoryStatus.TotalVirtual", "(long)(HardwareInfo?.MemoryStatus.TotalVirtual ?? MemoriaGC)")
       .replace("\t\t\tHardwareInfo.RefreshMemoryStatus();", "\t\t\tHardwareInfo?.RefreshMemoryStatus();")
       .replace("\t\tHardwareInfo = new HardwareInfo();\n",
                "\t\ttry\n\t\t{\n\t\t\tHardwareInfo = new HardwareInfo();\n\t\t}\n\t\tcatch\n\t\t{\n\t\t\tHardwareInfo = null;\n\t\t}\n"))
if "MemoriaGC =>" not in s2:
    s2 = s2.replace("\tpublic static void Refresh()",
                    "\tprivate static ulong MemoriaGC => (ulong)System.GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;\n\n\tpublic static void Refresh()")
if s2 != s:
    p.write_text(s2)
    print("  PlatformStatistics.cs (Hardware.Info opcional)")
# En Android no hay ensamblado de entrada (GetEntryAssembly() es null).
d = pathlib.Path(sys.argv[1])
for rel in ["Intersect.Client.Framework/Intersect.Client.Framework.Database/JsonDatabase.cs",
            "Intersect.Framework.Core/Intersect.IO.Files/FileSystemHelper.cs"]:
    p = d / rel
    s = p.read_text()
    s2 = s.replace("Assembly.GetEntryAssembly().GetName().Name",
                   "(Assembly.GetEntryAssembly()?.GetName().Name ?? \"Intersect Client\")")
    if s2 != s:
        p.write_text(s2)
        print(f"  {rel} (sin ensamblado de entrada)")
p = d / "Intersect.Framework.Core/Intersect.Utilities/ReflectionUtils.cs"
s = p.read_text()
s2 = (s.replace("Assembly.GetEntryAssembly()?.GetManifestResourceStream(resourceName)",
                "AbrirRecurso(Assembly.GetEntryAssembly(), resourceName)")
       .replace("(assembly ?? Assembly.GetEntryAssembly())?.GetManifestResourceStream(resourceName)",
                "AbrirRecurso(assembly ?? Assembly.GetEntryAssembly(), resourceName)"))
if "AbrirRecurso(Assembly? " not in s2:
    s2 = s2.replace("\tpublic static bool ExtractResource(string resourceName, Stream destinationStream, Assembly? assembly = null)",
                    "\tprivate static Stream? AbrirRecurso(Assembly? assembly, string resourceName)\n\t{\n"
                    "\t\tStream? stream = assembly?.GetManifestResourceStream(resourceName);\n"
                    "\t\tif (stream != null)\n\t\t{\n\t\t\treturn stream;\n\t\t}\n"
                    "\t\tforeach (Assembly cargado in System.AppDomain.CurrentDomain.GetAssemblies())\n\t\t{\n"
                    "\t\t\tif (!cargado.IsDynamic && (stream = cargado.GetManifestResourceStream(resourceName)) != null)\n\t\t\t{\n"
                    "\t\t\t\treturn stream;\n\t\t\t}\n\t\t}\n\t\treturn null;\n\t}\n\n"
                    "\tpublic static bool ExtractResource(string resourceName, Stream destinationStream, Assembly? assembly = null)", 1)
if s2 != s:
    p.write_text(s2)
    print("  ReflectionUtils.cs (recursos en cualquier ensamblado)")
# Steamworks.NET.dll es una compilación x64 de Windows que Mono para Android rechaza:
# en Android la clase Steam es un sustituto sin Steam con la misma API pública.
p = root / "Intersect.Client.ThirdParty/Steam.cs"
s = p.read_text()
if not s.startswith("#if !ANDROID"):
    p.write_text("#if !ANDROID\n" + s.rstrip("\n") + """
#else
using System;
using System.Reflection;
using Intersect.Client.Framework.GenericClasses;

namespace Intersect.Client.ThirdParty;

public static class Steam
{
	[AttributeUsage(AttributeTargets.Assembly)]
	public sealed class SupportedAttribute : Attribute
	{
		public static bool IsPresent(Assembly? assembly = null) => false;
	}

	public static bool Initialized => false;

	public static string Language => string.Empty;

	public static string[] Languages => Array.Empty<string>();

	public static bool SteamDeck => false;

	public static bool ShowKeyboard(Action<string?> inputHandler, string description, string? existingInput, bool password, uint maxLength = 1024u, Rectangle? inputBounds = null) => false;

	public static void PumpEvents()
	{
	}
}
#endif
""")
    print("  Intersect.Client.ThirdParty/Steam.cs (sustituto sin Steam)")
# Android: el juego usa siempre la resolución real de la pantalla (MonoGame Android no escala
# el búfer, solo deja bandas negras si la proporción no coincide).
guard("Intersect.Client.MonoGame.Graphics/MonoRenderer.cs",
      "\t\tif (fullScreen)\n\t\t{\n\t\t\twidth = w;\n\t\t\theight = h;\n\t\t}",
      "\t\twidth = w;\n\t\theight = h;")
# Android: teclado en pantalla para los campos de texto (android/SaiyanAndroid/Plataforma/TecladoAndroid.cs).
p = root / "Intersect.Client.MonoGame.Input/MonoInput.cs"
s = p.read_text()
gancho = "\t\tif (mMyGame.IsActive)\n\t\t{\n#if ANDROID\n\t\t\tIntersect.Client.Android.TecladoAndroid.Actualizar(SourisPourLInterface());\n#endif\n"
if "TecladoAndroid" not in s:
    s = s.replace("\tpublic override void Update(TimeSpan elapsed)\n\t{\n\t\tif (mMyGame.IsActive)\n\t\t{\n",
                  "\tpublic override void Update(TimeSpan elapsed)\n\t{\n" + gancho, 1)
    p.write_text(s)
    print("  Intersect.Client.MonoGame.Input/MonoInput.cs (teclado Android)")
# Android: el tacto hace de ratón (android/SaiyanAndroid/Plataforma/TactilAndroid.cs).
guard("Intersect.Client.MonoGame.Input/MonoInput.cs",
      "\t\t\tMouseState mouseState = Mouse.GetState();",
      "\t\t\tMouseState mouseState = Intersect.Client.Android.TactilAndroid.Leer(Mouse.GetState());")
# Android: joystick y botones táctiles (android/SaiyanAndroid/Plataforma/ControlesTactiles.cs),
# mezclados en el teclado igual que las teclas del mando.
p = root / "Intersect.Client.MonoGame.Input/MonoInput.cs"
s = p.read_text()
if "ControlesTactiles" not in s:
    s = s.replace("\t\t\tIntersect.Client.Framework.GenericClasses.Keys pressedModifier = GetPressedModifier(keyboardState);",
                  "#if ANDROID\n\t\t\tkeyboardState = Intersect.Client.Android.ControlesTactiles.Mezclar(keyboardState, _intersectToMonoGameKeyMap);\n#endif\n"
                  "\t\t\tIntersect.Client.Framework.GenericClasses.Keys pressedModifier = GetPressedModifier(keyboardState);", 1)
    p.write_text(s)
    print("  Intersect.Client.MonoGame.Input/MonoInput.cs (controles táctiles)")
# Android: un toque en la barra de acciones usa la habilidad/objeto (en PC hace falta doble clic).
guard("Intersect.Client.Interface.Game.Hotbar/HotbarItem.cs",
      "\t\tif (arguments.MouseButton == MouseButton.Right)\n\t\t{\n\t\t\tGlobals.Me?.AddToHotbar(SlotIndex, -1, -1);\n\t\t}",
      "\t\tif (arguments.MouseButton == MouseButton.Left)\n\t\t{\n\t\t\tActivate();\n\t\t}")
p = root / "Intersect.Client.Interface.Game.Hotbar/HotbarItem.cs"
s = p.read_text()
cuerpo = "\t{\n\t\tif (arguments.MouseButton == MouseButton.Left)\n\t\t{\n\t\t\tActivate();\n\t\t}\n\t}"
cabecera = "\tprivate void Icon_DoubleClicked(Intersect.Client.Framework.Gwen.Control.Base sender, MouseButtonState arguments)\n"
if cabecera + cuerpo in s:
    s = s.replace(cabecera + cuerpo, cabecera + "\t{\n#if !ANDROID" + cuerpo[2:-2] + "\n#endif\n\t}", 1)
    p.write_text(s)
    print("  Intersect.Client.Interface.Game.Hotbar/HotbarItem.cs (doble clic)")
# Android: la interfaz está pensada para monitores de PC (mínimo 1600 px de ancho virtual) y en un
# móvil los textos quedan diminutos; se escala por la altura para una interfaz virtual de 600 px de alto.
guard("Intersect.Client.Core/Graphics.cs",
      "\t\tif (largeurEcran > 0 && largeurEcran < 1600)\n\t\t{\n\t\t\treturn (float)largeurEcran / 1600f;\n\t\t}",
      "\t\tif (hauteurEcran > 0)\n\t\t{\n\t\t\treturn MathF.Max(1f, (float)hauteurEcran / 600f);\n\t\t}")
