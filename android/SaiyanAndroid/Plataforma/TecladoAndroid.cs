using System.Numerics;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Input;

namespace Intersect.Client.Android;

/// <summary>
/// Teclado en pantalla de Android para los campos de texto del juego.
/// La vista de MonoGame no recibe texto del teclado de Android, así que un EditText invisible
/// encima del juego recoge lo que se escribe y se copia al campo de Gwen que tiene el foco.
/// El bucle del juego corre en otro hilo: el EditText solo se toca en el hilo de la interfaz
/// de Android y el campo de Gwen solo en el del juego; se comunican con _textoPendiente y _enterPendiente.
/// </summary>
internal static class TecladoAndroid
{
	private static Activity? _actividad;

	private static View? _vistaJuego;

	private static EditText? _editor;

	/// <summary>Evita reenviar al juego el texto que el propio juego acaba de poner en el editor.</summary>
	private static bool _copiandoDelJuego;

	private static TextBox? _campo;

	private static string? _textoPendiente;

	private static int _enterPendiente;

	/// <summary>Monta la vista del juego con el editor invisible encima.</summary>
	internal static FrameLayout Montar(Activity actividad, View vistaJuego)
	{
		_actividad = actividad;
		_vistaJuego = vistaJuego;
		_editor = new EditText(actividad)
		{
			Alpha = 0f,
			ImeOptions = ImeAction.Done,
		};
		_editor.SetSingleLine(true);
		_editor.AfterTextChanged += (_, _) =>
		{
			if (!_copiandoDelJuego)
			{
				Volatile.Write(ref _textoPendiente, _editor.Text ?? string.Empty);
			}
		};
		_editor.EditorAction += (_, e) =>
		{
			Volatile.Write(ref _enterPendiente, 1);
			e.Handled = true;
		};
		FrameLayout raiz = new FrameLayout(actividad);
		raiz.AddView(vistaJuego, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
		raiz.AddView(_editor, new FrameLayout.LayoutParams(1, 1));
		return raiz;
	}

	/// <summary>Llamado en cada actualización de MonoInput (hilo del juego).</summary>
	internal static void Actualizar(Vector2 raton)
	{
		TextBox? foco = InputHandler.KeyboardFocus as TextBox;
		if (foco != null && (foco.IsDisabled || !foco.IsVisibleInTree))
		{
			foco = null;
		}
		if (foco != _campo)
		{
			_campo = foco;
			Volatile.Write(ref _textoPendiente, null);
			Volatile.Write(ref _enterPendiente, 0);
			if (foco != null)
			{
				Abrir(foco.Text ?? string.Empty, foco is TextBoxPassword, foco is TextBoxNumeric, foco.MaximumLength);
			}
			else
			{
				Cerrar();
			}
		}
		if (_campo == null)
		{
			return;
		}
		// Tocar el campo que ya tiene el foco vuelve a abrir el teclado si se había cerrado.
		if (TactilAndroid.ToqueNuevo && _campo.GlobalBounds.Contains((int)raton.X, (int)raton.Y))
		{
			Abrir(_campo.Text ?? string.Empty, _campo is TextBoxPassword, _campo is TextBoxNumeric, _campo.MaximumLength);
		}
		string? texto = Interlocked.Exchange(ref _textoPendiente, null);
		if (texto != null && texto != _campo.Text)
		{
			_campo.Text = texto;
			_campo.CursorPos = texto.Length;
			_campo.CursorEnd = texto.Length;
		}
		if (Interlocked.Exchange(ref _enterPendiente, 0) != 0)
		{
			Interface.Interface.GwenInput.ProcessMessage(new GwenInputMessage(IntersectInput.InputEvent.KeyDown, raton, MouseButton.None, Keys.Return));
			Interface.Interface.GwenInput.ProcessMessage(new GwenInputMessage(IntersectInput.InputEvent.KeyUp, raton, MouseButton.None, Keys.Return));
		}
	}

	private static void Abrir(string texto, bool clave, bool numerico, int maximo)
	{
		_actividad?.RunOnUiThread(() =>
		{
			if (_editor == null)
			{
				return;
			}
			_copiandoDelJuego = true;
			_editor.InputType = numerico ? InputTypes.ClassNumber
				: clave ? InputTypes.ClassText | InputTypes.TextVariationPassword
				: InputTypes.ClassText;
			_editor.SetFilters(maximo >= 0 ? new IInputFilter[] { new InputFilterLengthFilter(maximo) } : System.Array.Empty<IInputFilter>());
			_editor.Text = texto;
			_editor.SetSelection(_editor.Text?.Length ?? 0);
			_copiandoDelJuego = false;
			_editor.RequestFocus();
			Gestor()?.ShowSoftInput(_editor, ShowFlags.Implicit);
		});
	}

	private static void Cerrar()
	{
		_actividad?.RunOnUiThread(() =>
		{
			if (_editor == null)
			{
				return;
			}
			Gestor()?.HideSoftInputFromWindow(_editor.WindowToken, HideSoftInputFlags.None);
			_editor.ClearFocus();
			// Las teclas físicas (movimiento, atajos) vuelven a la vista de MonoGame.
			_vistaJuego?.RequestFocus();
		});
	}

	private static InputMethodManager? Gestor()
	{
		return _actividad?.GetSystemService(Context.InputMethodService) as InputMethodManager;
	}
}
