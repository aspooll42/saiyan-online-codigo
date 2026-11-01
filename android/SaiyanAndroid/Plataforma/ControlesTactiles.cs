using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using Intersect.Client.Core.Controls;
using TextBox = Intersect.Client.Framework.Gwen.Control.TextBox;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using IntersectKeys = Intersect.Client.Framework.GenericClasses.Keys;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;
using KeyboardState = Microsoft.Xna.Framework.Input.KeyboardState;

namespace Intersect.Client.Android;

/// <summary>
/// Joystick y botones táctiles encima del juego. Son vistas nativas de Android (cada una recibe
/// sus propios dedos), y lo que pulsan se convierte en las teclas que el jugador tiene asignadas
/// a cada acción, igual que hace Manette con el mando.
/// La interfaz de Android escribe _direccion/_correr/_pulsados; el hilo del juego los lee en Mezclar.
/// </summary>
internal static class ControlesTactiles
{
	/// <summary>Acciones con botón; el índice es el bit en _pulsados.</summary>
	private static readonly Control[] Acciones =
	{
		Control.AttackInteract, Control.Block, Control.Dodge, Control.ChargeKi, Control.Dash, Control.PickUp, Control.AutoTarget,
	};

	/// <summary>-1 sin mover; 0 = derecha y sigue en sentido antihorario de 45 en 45 grados (como Manette).</summary>
	private static int _direccion = -1;

	private static int _correr;

	private static int _pulsados;

	private static Activity? _actividad;

	private static View? _capa;

	private static bool _visible;

	internal static void Montar(Activity actividad, FrameLayout raiz)
	{
		_actividad = actividad;
		// Todo se mide en centésimas del lado corto de la pantalla, para que ocupe lo mismo en cualquier móvil.
		global::Android.Util.DisplayMetrics medidas = actividad.Resources!.DisplayMetrics!;
		float u = Math.Min(medidas.WidthPixels, medidas.HeightPixels) / 100f;
		FrameLayout capa = new FrameLayout(actividad) { Visibility = ViewStates.Gone };

		// Joystick abajo a la izquierda, por encima del chat.
		capa.AddView(new Joystick(actividad), Colocar(24, 3, 16, GravityFlags.Left, u));

		// A la izquierda de la columna derecha del HUD (minimapa, misiones, objetos del suelo, que llega
		// al 40 % del alto desde el borde) y por encima de la barra de menús y de la de acciones.
		// (tamaño, margen derecho, margen inferior) en centésimas.
		string[] textos = Textos();
		(float Tam, float Derecha, float Abajo)[] sitio =
		{
			(15f, 41f, 14f),     // atacar
			(8.5f, 57f, 15.5f),  // bloquear
			(8.5f, 65.5f, 15.5f), // esquivar
			(8.5f, 65.5f, 25f),  // ki
			(8.5f, 57f, 25f),    // dash
			(8.5f, 74f, 25f),    // recoger
			(8.5f, 44f, 30.5f),  // objetivo
		};
		for (int i = 0; i < Acciones.Length; i++)
		{
			capa.AddView(new BotonTactil(actividad, i, textos[i], (i == 0 ? 2.4f : 1.6f) * u, u), Colocar(sitio[i].Tam, sitio[i].Derecha, sitio[i].Abajo, GravityFlags.Right, u));
		}
		raiz.AddView(capa, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
		_capa = capa;
	}

	private static FrameLayout.LayoutParams Colocar(float tam, float margenLado, float margenAbajo, GravityFlags lado, float u)
	{
		int px = (int)(tam * u);
		FrameLayout.LayoutParams parametros = new FrameLayout.LayoutParams(px, px, GravityFlags.Bottom | lado);
		if (lado == GravityFlags.Left)
		{
			parametros.LeftMargin = (int)(margenLado * u);
		}
		else
		{
			parametros.RightMargin = (int)(margenLado * u);
		}
		parametros.BottomMargin = (int)(margenAbajo * u);
		return parametros;
	}

	private static string[] Textos()
	{
		return Java.Util.Locale.Default.Language switch
		{
			"es" => new[] { "ATACAR", "BLOQ.", "ESQUIVA", "KI", "DASH", "COGER", "OBJETIVO" },
			"fr" => new[] { "FRAPPE", "GARDE", "ESQUIVE", "KI", "DASH", "RAMASSER", "CIBLE" },
			_ => new[] { "ATTACK", "BLOCK", "DODGE", "KI", "DASH", "PICK UP", "TARGET" },
		};
	}

	internal static void Mover(int direccion, bool correr)
	{
		Volatile.Write(ref _direccion, direccion);
		Volatile.Write(ref _correr, correr ? 1 : 0);
	}

	internal static void Pulsar(int accion, bool pulsado)
	{
		int bit = 1 << accion;
		int actual, nuevo;
		do
		{
			actual = Volatile.Read(ref _pulsados);
			nuevo = pulsado ? actual | bit : actual & ~bit;
		}
		while (Interlocked.CompareExchange(ref _pulsados, nuevo, actual) != actual);
	}

	/// <summary>Llamado en cada actualización de MonoInput (hilo del juego): añade las teclas de los controles táctiles.</summary>
	internal static KeyboardState Mezclar(KeyboardState estado, Dictionary<IntersectKeys, XnaKeys> mapa)
	{
		bool visible = Globals.GameState == GameStates.InGame && Interface.Interface.HasInGameUI && InputHandler.KeyboardFocus is not TextBox;
		if (visible != _visible)
		{
			_visible = visible;
			_actividad?.RunOnUiThread(() =>
			{
				if (_capa != null)
				{
					_capa.Visibility = visible ? ViewStates.Visible : ViewStates.Gone;
				}
			});
			if (!visible)
			{
				Mover(-1, correr: false);
				Volatile.Write(ref _pulsados, 0);
			}
		}
		if (!visible)
		{
			return estado;
		}
		List<IntersectKeys> teclas = new List<IntersectKeys>();
		int direccion = Volatile.Read(ref _direccion);
		if (direccion >= 0)
		{
			if (direccion <= 1 || direccion == 7)
			{
				Teclas(Control.MoveRight, teclas);
			}
			else if (direccion >= 3 && direccion <= 5)
			{
				Teclas(Control.MoveLeft, teclas);
			}
			if (direccion >= 1 && direccion <= 3)
			{
				Teclas(Control.MoveUp, teclas);
			}
			else if (direccion >= 5 && direccion <= 7)
			{
				Teclas(Control.MoveDown, teclas);
			}
			if (Volatile.Read(ref _correr) != 0)
			{
				Teclas(Control.Sprint, teclas);
			}
		}
		int pulsados = Volatile.Read(ref _pulsados);
		for (int i = 0; i < Acciones.Length; i++)
		{
			if ((pulsados & (1 << i)) != 0)
			{
				Teclas(Acciones[i], teclas);
			}
		}
		if (teclas.Count == 0)
		{
			return estado;
		}
		IEnumerable<XnaKeys> extra = teclas.Select(t => mapa.TryGetValue(t, out XnaKeys x) ? x : XnaKeys.None).Where(x => x != XnaKeys.None);
		return new KeyboardState(estado.GetPressedKeys().Concat(extra).Distinct().ToArray(), estado.CapsLock, estado.NumLock);
	}

	/// <summary>Primera asignación de teclado de la acción (las de ratón se ignoran).</summary>
	private static void Teclas(Control control, List<IntersectKeys> teclas)
	{
		if (!Controls.ActiveControls.TryGetMappingFor(control, out ControlMapping mapping))
		{
			return;
		}
		foreach (ControlBinding binding in mapping.Bindings)
		{
			if (binding.Key == IntersectKeys.None || binding.IsMouseKey)
			{
				continue;
			}
			if (binding.Modifier != IntersectKeys.None)
			{
				teclas.Add(binding.Modifier);
			}
			teclas.Add(binding.Key);
			return;
		}
	}

	private sealed class Joystick : View
	{
		private readonly Paint _base = new Paint(PaintFlags.AntiAlias) { Color = global::Android.Graphics.Color.Argb(45, 255, 255, 255) };

		private readonly Paint _borde = new Paint(PaintFlags.AntiAlias) { Color = global::Android.Graphics.Color.Argb(150, 255, 170, 40) };

		private readonly Paint _pomo = new Paint(PaintFlags.AntiAlias) { Color = global::Android.Graphics.Color.Argb(190, 255, 140, 20) };

		private float _dx;

		private float _dy;

		public Joystick(Context contexto) : base(contexto)
		{
			_borde.SetStyle(Paint.Style.Stroke);
			_borde.StrokeWidth = 3 * contexto.Resources!.DisplayMetrics!.Density;
		}

		protected override void OnDraw(Canvas canvas)
		{
			float cx = Width / 2f, cy = Height / 2f, r = Math.Min(cx, cy);
			canvas.DrawCircle(cx, cy, r * 0.95f, _base);
			canvas.DrawCircle(cx, cy, r * 0.95f, _borde);
			canvas.DrawCircle(cx + _dx * r * 0.55f, cy + _dy * r * 0.55f, r * 0.4f, _pomo);
		}

		public override bool OnTouchEvent(MotionEvent? e)
		{
			if (e == null)
			{
				return false;
			}
			switch (e.ActionMasked)
			{
			case MotionEventActions.Down:
			case MotionEventActions.Move:
				float r = Math.Min(Width, Height) / 2f;
				float x = (e.GetX() - Width / 2f) / r, y = (e.GetY() - Height / 2f) / r;
				float largo = MathF.Sqrt(x * x + y * y);
				if (largo > 1f)
				{
					x /= largo;
					y /= largo;
				}
				_dx = x;
				_dy = y;
				if (largo < 0.25f)
				{
					ControlesTactiles.Mover(-1, correr: false);
				}
				else
				{
					// Ángulo con el eje Y hacia arriba; 8 sectores de 45 grados.
					double angulo = Math.Atan2(-y, x) * 180.0 / Math.PI;
					int direccion = ((int)Math.Round(angulo / 45.0) + 8) % 8;
					ControlesTactiles.Mover(direccion, largo > 0.9f);
				}
				Invalidate();
				return true;
			case MotionEventActions.Up:
			case MotionEventActions.Cancel:
				_dx = _dy = 0f;
				ControlesTactiles.Mover(-1, correr: false);
				Invalidate();
				return true;
			}
			return true;
		}
	}

	private sealed class BotonTactil : View
	{
		private readonly int _accion;

		private readonly string _texto;

		private readonly Paint _fondo = new Paint(PaintFlags.AntiAlias);

		private readonly Paint _borde = new Paint(PaintFlags.AntiAlias) { Color = global::Android.Graphics.Color.Argb(130, 255, 170, 40) };

		private readonly Paint _letra = new Paint(PaintFlags.AntiAlias) { Color = global::Android.Graphics.Color.White, TextAlign = Paint.Align.Center };

		private bool _pulsado;

		public BotonTactil(Context contexto, int accion, string texto, float tamLetra, float u) : base(contexto)
		{
			_accion = accion;
			_texto = texto;
			_borde.SetStyle(Paint.Style.Stroke);
			_borde.StrokeWidth = 0.4f * u;
			_letra.TextSize = tamLetra;
			_letra.SetTypeface(Typeface.DefaultBold);
			_letra.SetShadowLayer(0.4f * u, 0, 0, global::Android.Graphics.Color.Black);
		}

		protected override void OnDraw(Canvas canvas)
		{
			float cx = Width / 2f, cy = Height / 2f, r = Math.Min(cx, cy) * 0.95f;
			_fondo.Color = _pulsado ? global::Android.Graphics.Color.Argb(210, 255, 140, 20) : global::Android.Graphics.Color.Argb(80, 20, 24, 40);
			canvas.DrawCircle(cx, cy, r, _fondo);
			canvas.DrawCircle(cx, cy, r, _borde);
			canvas.DrawText(_texto, cx, cy - (_letra.Descent() + _letra.Ascent()) / 2f, _letra);
		}

		public override bool OnTouchEvent(MotionEvent? e)
		{
			switch (e?.ActionMasked)
			{
			case MotionEventActions.Down:
				Cambiar(true);
				return true;
			case MotionEventActions.Up:
			case MotionEventActions.Cancel:
				Cambiar(false);
				return true;
			}
			return true;
		}

		private void Cambiar(bool pulsado)
		{
			_pulsado = pulsado;
			ControlesTactiles.Pulsar(_accion, pulsado);
			Invalidate();
		}
	}
}
