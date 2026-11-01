using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Android.App;
using Android.Graphics;
using Android.Views;
using Android.Widget;

namespace Intersect.Client.Android;

/// <summary>
/// Los recursos del juego (resources/coffre, ~540 MB) no caben en la APK: la primera vez se
/// descargan como un zip desde la dirección de Assets/resources/descarga.txt y se descomprimen
/// en el almacenamiento interno de la app.
/// </summary>
internal static class DescargaRecursos
{
	internal static bool Faltan(string raiz)
	{
		return !Directory.Exists(System.IO.Path.Combine(raiz, "resources", "coffre", "00"));
	}

	internal static void Iniciar(Activity actividad, string raiz, Action alTerminar)
	{
		float u = Math.Min(actividad.Resources!.DisplayMetrics!.WidthPixels, actividad.Resources.DisplayMetrics.HeightPixels) / 100f;
		LinearLayout caja = new LinearLayout(actividad) { Orientation = Orientation.Vertical };
		caja.SetGravity(GravityFlags.Center);
		caja.SetBackgroundColor(global::Android.Graphics.Color.Rgb(12, 16, 32));
		caja.SetPadding((int)(10 * u), 0, (int)(10 * u), 0);
		TextView texto = new TextView(actividad) { Gravity = GravityFlags.Center };
		texto.SetTextColor(global::Android.Graphics.Color.White);
		texto.SetTextSize(global::Android.Util.ComplexUnitType.Px, 4.5f * u);
		ProgressBar barra = new ProgressBar(actividad, null, global::Android.Resource.Attribute.ProgressBarStyleHorizontal) { Max = 1000 };
		Button reintentar = new Button(actividad) { Text = Texto("reintentar"), Visibility = ViewStates.Gone };
		caja.AddView(texto, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
		caja.AddView(barra, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, (int)(6 * u)) { TopMargin = (int)(4 * u) });
		caja.AddView(reintentar, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent) { TopMargin = (int)(4 * u) });
		actividad.SetContentView(caja);

		void Empezar()
		{
			reintentar.Visibility = ViewStates.Gone;
			texto.Text = Texto("descargando");
			barra.Progress = 0;
			Task.Run(async () =>
			{
				try
				{
					await Descargar(actividad, raiz, (progreso, mensaje) => actividad.RunOnUiThread(() =>
					{
						barra.Progress = (int)(progreso * 1000);
						texto.Text = mensaje;
					}));
					actividad.RunOnUiThread(alTerminar);
				}
				catch (Exception ex)
				{
					actividad.RunOnUiThread(() =>
					{
						texto.Text = Texto("error") + "\n" + ex.Message;
						reintentar.Visibility = ViewStates.Visible;
					});
				}
			});
		}

		reintentar.Click += (_, _) => Empezar();
		Empezar();
	}

	private static async Task Descargar(Activity actividad, string raiz, Action<double, string> informar)
	{
		string url;
		using (StreamReader lector = new StreamReader(actividad.Assets!.Open("resources/descarga.txt")))
		{
			url = (await lector.ReadToEndAsync()).Trim();
		}
		string zip = System.IO.Path.Combine(actividad.CacheDir!.AbsolutePath, "recursos.zip");
		using (HttpClient cliente = new HttpClient { Timeout = TimeSpan.FromHours(2) })
		using (HttpResponseMessage respuesta = await cliente.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
		{
			respuesta.EnsureSuccessStatusCode();
			long total = respuesta.Content.Headers.ContentLength ?? -1;
			await using Stream origen = await respuesta.Content.ReadAsStreamAsync();
			await using FileStream destino = File.Create(zip);
			byte[] bufer = new byte[1 << 16];
			long leidos = 0;
			long ultimoAviso = 0;
			int n;
			while ((n = await origen.ReadAsync(bufer)) > 0)
			{
				await destino.WriteAsync(bufer.AsMemory(0, n));
				leidos += n;
				if (leidos - ultimoAviso > 1 << 20)
				{
					ultimoAviso = leidos;
					string mb = total > 0 ? $"{leidos >> 20} / {total >> 20} MB" : $"{leidos >> 20} MB";
					informar(total > 0 ? 0.9 * leidos / total : 0, Texto("descargando") + "\n" + mb);
				}
			}
		}
		informar(0.9, Texto("instalando"));
		string resources = System.IO.Path.Combine(raiz, "resources");
		string temporal = System.IO.Path.Combine(resources, "coffre.tmp");
		if (Directory.Exists(temporal))
		{
			Directory.Delete(temporal, recursive: true);
		}
		// El zip contiene la carpeta coffre/; se descomprime aparte y se mueve al final para no dejarla a medias.
		ZipFile.ExtractToDirectory(zip, temporal);
		string coffre = System.IO.Path.Combine(resources, "coffre");
		if (Directory.Exists(coffre))
		{
			Directory.Delete(coffre, recursive: true);
		}
		string dentro = System.IO.Path.Combine(temporal, "coffre");
		Directory.Move(Directory.Exists(dentro) ? dentro : temporal, coffre);
		if (Directory.Exists(temporal))
		{
			Directory.Delete(temporal, recursive: true);
		}
		File.Delete(zip);
		informar(1, Texto("listo"));
	}

	private static string Texto(string clave)
	{
		string idioma = Java.Util.Locale.Default.Language ?? "en";
		return (idioma, clave) switch
		{
			("es", "descargando") => "Descargando los recursos del juego (solo la primera vez)…",
			("es", "instalando") => "Instalando los recursos…",
			("es", "listo") => "¡Listo!",
			("es", "error") => "No se pudieron descargar los recursos. Comprueba la conexión.",
			("es", "reintentar") => "Reintentar",
			("fr", "descargando") => "Téléchargement des ressources du jeu (la première fois seulement)…",
			("fr", "instalando") => "Installation des ressources…",
			("fr", "listo") => "Prêt !",
			("fr", "error") => "Impossible de télécharger les ressources. Vérifiez la connexion.",
			("fr", "reintentar") => "Réessayer",
			(_, "descargando") => "Downloading game resources (first launch only)…",
			(_, "instalando") => "Installing resources…",
			(_, "listo") => "Ready!",
			(_, "error") => "Could not download the resources. Check your connection.",
			_ => "Retry",
		};
	}
}
