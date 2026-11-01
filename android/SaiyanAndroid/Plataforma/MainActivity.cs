using System;
using System.IO;
using System.Reflection;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using Intersect.Client.Core;
using Microsoft.Xna.Framework;

namespace Intersect.Client.Android;

/// <summary>Punto de entrada en Android: equivale a Program.Main del escritorio.</summary>
[Activity(
	Label = "Saiyan Online",
	MainLauncher = true,
	AlwaysRetainTaskState = true,
	LaunchMode = LaunchMode.SingleInstance,
	ScreenOrientation = ScreenOrientation.SensorLandscape,
	ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.UiMode
)]
public class MainActivity : AndroidGameActivity
{
	/// <summary>Actividad en curso, para que el runner de MonoGame pueda mostrar la vista del juego.</summary>
	internal static MainActivity Actual { get; private set; }

	protected override void OnCreate(Bundle bundle)
	{
		base.OnCreate(bundle);
		Actual = this;
		Window?.AddFlags(WindowManagerFlags.Fullscreen | WindowManagerFlags.KeepScreenOn);
		if (Build.VERSION.SdkInt >= BuildVersionCodes.P && Window != null)
		{
			// Dibuja también junto a la cámara frontal (muesca).
			Window.Attributes!.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
		}

		// El juego lee "resources/..." relativo al directorio actual. Si los recursos están en la carpeta
		// externa de la app (Android/data/<paquete>/files, accesible por USB sin root) se usa esa.
		string raiz = FilesDir!.AbsolutePath;
		string? externa = GetExternalFilesDir(null)?.AbsolutePath;
		if (externa != null && Directory.Exists(Path.Combine(externa, "resources", "coffre")))
		{
			raiz = externa;
		}
		Directory.CreateDirectory(Path.Combine(raiz, "resources"));
		CopiarSiFalta("resources/config.json", raiz);
		CopiarSiFalta("resources/default_settings.json", raiz);
		System.Environment.CurrentDirectory = raiz;

		void Arrancar() => Bootstrapper.Start(typeof(MainActivity).Assembly, Array.Empty<string>());
		if (DescargaRecursos.Faltan(raiz))
		{
			// Primera vez: se descargan los recursos y se recrea la actividad, que arranca el juego por el
			// camino normal (MonoGame empieza a dibujar en OnResume, que ya pasó durante la descarga).
			DescargaRecursos.Iniciar(this, raiz, Recreate);
		}
		else
		{
			Arrancar();
		}
	}

	private static readonly MethodInfo? TeclaPulsada = typeof(Microsoft.Xna.Framework.Input.Keyboard).GetMethod("KeyDown", BindingFlags.Static | BindingFlags.NonPublic, new[] { typeof(Keycode) });

	private static readonly MethodInfo? TeclaSoltada = typeof(Microsoft.Xna.Framework.Input.Keyboard).GetMethod("KeyUp", BindingFlags.Static | BindingFlags.NonPublic, new[] { typeof(Keycode) });

	/// <summary>
	/// El botón Atrás de Android hace de Escape (cerrar ventana, abrir el menú) en vez de cerrar la app.
	/// Con el teclado en pantalla abierto, Android lo usa antes para ocultar el teclado.
	/// </summary>
	public override bool DispatchKeyEvent(KeyEvent? e)
	{
		if (e?.KeyCode == Keycode.Back)
		{
			MethodInfo? metodo = e.Action == KeyEventActions.Down ? TeclaPulsada : e.Action == KeyEventActions.Up ? TeclaSoltada : null;
			metodo?.Invoke(null, new object[] { Keycode.Escape });
			return true;
		}
		return base.DispatchKeyEvent(e);
	}

	public override void OnWindowFocusChanged(bool hasFocus)
	{
		base.OnWindowFocusChanged(hasFocus);
		if (hasFocus)
		{
			OcultarBarras();
		}
	}

	/// <summary>Pantalla completa inmersiva: sin barra de estado ni de navegación (vuelven deslizando).</summary>
	private void OcultarBarras()
	{
		if (Window == null)
		{
			return;
		}
		if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
		{
			Window.SetDecorFitsSystemWindows(false);
			Window.InsetsController?.Hide(WindowInsets.Type.SystemBars());
			if (Window.InsetsController != null)
			{
				Window.InsetsController.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
			}
		}
		else
		{
#pragma warning disable CA1422
			Window.DecorView.SystemUiVisibility = (StatusBarVisibility)(SystemUiFlags.ImmersiveSticky | SystemUiFlags.Fullscreen | SystemUiFlags.HideNavigation | SystemUiFlags.LayoutStable | SystemUiFlags.LayoutFullscreen | SystemUiFlags.LayoutHideNavigation);
#pragma warning restore CA1422
		}
	}

	/// <summary>Copia un archivo de los assets del APK al almacenamiento de la app la primera vez.</summary>
	private void CopiarSiFalta(string ruta, string raiz)
	{
		string destino = Path.Combine(raiz, ruta);
		if (File.Exists(destino))
		{
			return;
		}
		using Stream origen = Assets!.Open(ruta);
		using FileStream salida = File.Create(destino);
		origen.CopyTo(salida);
	}

	/// <summary>Llamado por MonoGameRunner en vez de Game.Run() bloqueante.</summary>
	internal void MostrarJuego(Game juego)
	{
		View vista = (View)juego.Services.GetService(typeof(View))!;
		FrameLayout raiz = TecladoAndroid.Montar(this, vista);
		ControlesTactiles.Montar(this, raiz);
		SetContentView(raiz);
		OcultarBarras();
		juego.Run();
	}
}
