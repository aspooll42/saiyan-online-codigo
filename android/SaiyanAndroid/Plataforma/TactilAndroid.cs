using System;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace Intersect.Client.Android;

/// <summary>
/// MonoGame Android solo da el tacto por TouchPanel y el juego solo lee el ratón:
/// el primer dedo se convierte en el ratón.
///  - Toque corto: clic izquierdo (al levantar el dedo).
///  - Arrastrar: botón izquierdo pulsado mientras se mueve (arrastrar objetos, barras).
///  - Mantener sin moverse: clic derecho (menús contextuales, quitar de la barra...).
/// Mientras el dedo está quieto el ratón se queda encima, así que también salen las descripciones.
/// Se llama una vez por actualización desde MonoInput.Update (hilo del juego).
/// </summary>
internal static class TactilAndroid
{
	private enum Estado
	{
		Libre,
		Decidiendo,
		Arrastrando,
		DerechoPulsado,
		Esperando,
	}

	private const long MsPulsacionLarga = 450;

	private const float PxArrastre = 24f;

	private static Estado _estado;

	private static int _x;

	private static int _y;

	private static float _inicioX;

	private static float _inicioY;

	private static long _inicioMs;

	/// <summary>Toque corto ya enviado como pulsación: se suelta en la lectura siguiente.</summary>
	private static bool _soltarDespues;

	private static bool _estabaPulsado;

	/// <summary>True en la lectura en la que empieza un clic izquierdo.</summary>
	internal static bool ToqueNuevo { get; private set; }

	internal static MouseState Leer(MouseState raton)
	{
		ToqueNuevo = false;
		TouchCollection toques = TouchPanel.GetState();
		bool dedo = false;
		bool toqueRapido = false;
		foreach (TouchLocation toque in toques)
		{
			_x = (int)toque.Position.X;
			_y = (int)toque.Position.Y;
			if (toque.State == TouchLocationState.Pressed || toque.State == TouchLocationState.Moved)
			{
				dedo = true;
			}
			else if (toque.State == TouchLocationState.Released && _estado == Estado.Libre)
			{
				// Empezó y acabó entre dos lecturas.
				toqueRapido = true;
			}
			break;
		}
		if (_soltarDespues)
		{
			_soltarDespues = false;
			return Raton(izquierdo: false);
		}
		long ahora = Environment.TickCount64;
		if (dedo)
		{
			switch (_estado)
			{
			case Estado.Libre:
				_estado = Estado.Decidiendo;
				_inicioX = _x;
				_inicioY = _y;
				_inicioMs = ahora;
				return Raton(izquierdo: false);
			case Estado.Decidiendo:
				if (MathF.Abs(_x - _inicioX) > PxArrastre || MathF.Abs(_y - _inicioY) > PxArrastre)
				{
					_estado = Estado.Arrastrando;
					return Raton(izquierdo: true);
				}
				if (ahora - _inicioMs >= MsPulsacionLarga)
				{
					_estado = Estado.DerechoPulsado;
					return Raton(izquierdo: false, derecho: true);
				}
				return Raton(izquierdo: false);
			case Estado.Arrastrando:
				return Raton(izquierdo: true);
			case Estado.DerechoPulsado:
				_estado = Estado.Esperando;
				return Raton(izquierdo: false);
			default:
				return Raton(izquierdo: false);
			}
		}
		Estado anterior = _estado;
		_estado = Estado.Libre;
		if (anterior == Estado.Decidiendo || toqueRapido)
		{
			_soltarDespues = true;
			return Raton(izquierdo: true);
		}
		if (anterior == Estado.Libre && toques.Count == 0 && (raton.LeftButton == ButtonState.Pressed || raton.RightButton == ButtonState.Pressed || raton.X != 0 || raton.Y != 0))
		{
			// Sin dedos: ratón real si lo hay (emulador, ratón Bluetooth).
			return raton;
		}
		return Raton(izquierdo: false);
	}

	private static MouseState Raton(bool izquierdo, bool derecho = false)
	{
		ToqueNuevo = izquierdo && !_estabaPulsado;
		_estabaPulsado = izquierdo;
		return new MouseState(_x, _y, 0, izquierdo ? ButtonState.Pressed : ButtonState.Released, ButtonState.Released, derecho ? ButtonState.Pressed : ButtonState.Released, ButtonState.Released, ButtonState.Released);
	}
}
