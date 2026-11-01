using System;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Framework.Core;

namespace Intersect.Client.Interface.Game;

internal sealed class BandesDeCinema : Intersect.Client.Framework.Gwen.Control.Base
{
	private const float Hauteur = 0.11f;

	private const float GlissementMs = 380f;

	private static Canvas? _canvas;

	private static BandesDeCinema? _bandes;

	private float _ouverture;

	private long _derniere;

	protected override bool ShouldClip => false;

	public static bool InterfaceEscamotee
	{
		get
		{
			if (Globals.EventHolds.Count <= 0)
			{
				BandesDeCinema bandes = _bandes;
				if (bandes != null)
				{
					return bandes._ouverture > 0.001f;
				}
				return false;
			}
			return true;
		}
	}

	private BandesDeCinema(Canvas canvas)
		: base(canvas, "BandesDeCinema")
	{
		base.MouseInputEnabled = false;
		base.KeyboardInputEnabled = false;
		base.IsHidden = true;
		SetBounds(0, 0, canvas.Width, canvas.Height);
	}

	public static void Update()
	{
		if (!Interface.HasInGameUI)
		{
			return;
		}
		Canvas canvas = Interface.GameUi?.GameCanvas;
		if (canvas != null)
		{
			if (canvas != _canvas)
			{
				_canvas = canvas;
				_bandes = new BandesDeCinema(canvas);
			}
			_bandes?.Avancer();
		}
	}

	private void Avancer()
	{
		Intersect.Client.Framework.Gwen.Control.Base parent = base.Parent;
		if (parent != null && (base.Width != parent.Width || base.Height != parent.Height))
		{
			SetBounds(0, 0, parent.Width, parent.Height);
		}
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		long num = ((_derniere == 0L) ? 0 : Math.Clamp(millisecondsUtc - _derniere, 0L, 100L));
		_derniere = millisecondsUtc;
		float num2 = ((Globals.EventHolds.Count > 0) ? 1f : 0f);
		_ouverture = ((num2 > _ouverture) ? Math.Min(num2, _ouverture + (float)num / 380f) : Math.Max(num2, _ouverture - (float)num / 380f));
		base.IsHidden = _ouverture <= 0.001f;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		float num = _ouverture * _ouverture * (3f - 2f * _ouverture);
		int num2 = (int)MathF.Round((float)base.Height * 0.11f * num);
		if (num2 > 0)
		{
			TechniqueArt.Fill(skin.Renderer, new Rectangle(0, 0, base.Width, num2), new Color(255, 0, 0, 0));
			TechniqueArt.Fill(skin.Renderer, new Rectangle(0, base.Height - num2, base.Width, num2), new Color(255, 0, 0, 0));
		}
	}
}
