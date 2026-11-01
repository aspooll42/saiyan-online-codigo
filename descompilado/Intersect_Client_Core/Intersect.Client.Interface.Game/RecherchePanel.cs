using System;
using System.Collections.Generic;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Localization;
using Intersect.Config;
using Intersect.Framework.Core;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Interface.Game;

public sealed class RecherchePanel : Intersect.Client.Framework.Gwen.Control.Base
{
	private const int Largeur = 262;

	private const int Hauteur = 74;

	private static readonly Color Nuit = new Color(235, 10, 13, 22);

	private static readonly Color Filet = new Color(255, 52, 64, 92);

	private static readonly Color Bleu = new Color(255, 108, 184, 255);

	private static readonly Color BleuSombre = new Color(255, 38, 52, 78);

	private static readonly Color Jaune = new Color(255, 255, 214, 90);

	private static readonly Color Orange = new Color(255, 255, 138, 31);

	private static readonly Color Rouge = new Color(255, 240, 64, 52);

	private static readonly Color Vert = new Color(255, 111, 227, 161);

	private static readonly Color Terne = new Color(255, 138, 150, 176);

	private static RecherchePacket? _etat;

	private static long _recuA;

	private static long _actifA;

	private static int _passants;

	private static readonly List<(float Gain, long A)> Gains = new List<(float, long)>();

	private float _affichee;

	private float _alpha;

	protected override bool ShouldClip => false;

	public RecherchePanel(Canvas canvas)
		: base(canvas, "RecherchePanel")
	{
		base.MouseInputEnabled = false;
		SetSize(262, 74);
		base.IsHidden = true;
	}

	public static void Recevoir(RecherchePacket paquet)
	{
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		_etat = paquet;
		_recuA = millisecondsUtc;
		if (paquet.Gain > 0.05f)
		{
			Gains.Add((paquet.Gain, millisecondsUtc));
			if (Gains.Count > 8)
			{
				Gains.RemoveAt(0);
			}
		}
		_passants = ((paquet.Etat == RechercheEtat.Vu && paquet.Lieu == CasseCarteGenre.Ville) ? Figurants.PassantsAutourDeMoi(10) : 0);
	}

	public static void Oublier()
	{
		_etat = null;
		Gains.Clear();
	}

	private static bool Actif(RecherchePacket etat)
	{
		if (etat.Etat == RechercheEtat.Calme)
		{
			return etat.Chaleur > 0.5f;
		}
		return true;
	}

	public void Update()
	{
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		RecherchePacket etat = _etat;
		if (Globals.Me == null || etat == null || !Options.Instance.Recherche.Active)
		{
			base.IsHidden = true;
			_alpha = 0f;
			return;
		}
		if (Actif(etat))
		{
			_actifA = millisecondsUtc;
		}
		float num = ((millisecondsUtc - _actifA < 2500) ? 1f : 0f);
		_alpha += Math.Clamp(num - _alpha, -0.05f, 0.12f);
		if (_alpha <= 0.01f)
		{
			base.IsHidden = true;
			_affichee = etat.Chaleur;
			return;
		}
		_affichee += (etat.Chaleur - _affichee) * 0.15f;
		if (Disposition.Masquee(Disposition.Piece.Police))
		{
			base.IsHidden = true;
			return;
		}
		Point point = Disposition.Poser(Disposition.Piece.Police, 262, 74, 12, 12, base.Parent);
		SetPosition(point.X, point.Y);
		base.IsHidden = false;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		RecherchePacket etat = _etat;
		if (etat == null || _alpha <= 0.01f)
		{
			return;
		}
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		IGameTexture gameTexture = SaiyanHud.Texture("panneau_blanc.png");
		float alpha = _alpha;
		bool flag = etat.Etat == RechercheEtat.Poursuite;
		Color color;
		if (flag)
		{
			color = SaiyanHud.Lerp(Rouge, Bleu, (MathF.Sin((float)millisecondsUtc / 90f) + 1f) / 2f);
		}
		else
		{
			RechercheEtat etat2 = etat.Etat;
			bool flag2 = etat2 - 7 <= RechercheEtat.Vu;
			color = (flag2 ? Rouge : Bleu);
		}
		Color color2 = color;
		SaiyanHud.Draw(renderer, gameTexture, 0, 0, 262, 74, SaiyanHud.WithAlpha(Nuit, alpha));
		SaiyanHud.Draw(renderer, gameTexture, 0, 0, 262, 1, SaiyanHud.WithAlpha(Filet, alpha));
		SaiyanHud.Draw(renderer, gameTexture, 0, 73, 262, 1, SaiyanHud.WithAlpha(Filet, alpha));
		SaiyanHud.Draw(renderer, gameTexture, 261, 0, 1, 74, SaiyanHud.WithAlpha(Filet, alpha));
		SaiyanHud.Draw(renderer, gameTexture, 0, 0, 3, 74, SaiyanHud.WithAlpha(color2, alpha));
		SaiyanHud.TextLeft(renderer, SaiyanHud.StrongFont, 11, Strings.Recherche.Titre, 12, 4, 18, SaiyanHud.WithAlpha(Bleu, alpha));
		for (int i = 0; i < 3; i++)
		{
			bool flag3 = etat.Cran > i;
			Color color3 = ((!flag3) ? BleuSombre : (i switch
			{
				0 => Jaune, 
				1 => Orange, 
				_ => Rouge, 
			}));
			Color color4 = color3;
			if (flag3 && flag && i == etat.Cran - 1)
			{
				color4 = SaiyanHud.Lerp(color4, SaiyanHud.White, (MathF.Sin((float)millisecondsUtc / 110f) + 1f) / 4f);
			}
			Insigne(renderer, gameTexture, 244 - (2 - i) * 20, 5, SaiyanHud.WithAlpha(color4, alpha));
		}
		SaiyanHud.Draw(renderer, gameTexture, 12, 26, 238, 8, SaiyanHud.WithAlpha(BleuSombre, alpha));
		int num = (int)Math.Round(238f * Math.Clamp(_affichee / 100f, 0f, 1f));
		byte cran = etat.Cran;
		Color color5 = ((cran >= 3) ? Rouge : (cran switch
		{
			2 => Orange, 
			1 => Jaune, 
			_ => Bleu, 
		}));
		Color color6 = color5;
		if (num > 0)
		{
			SaiyanHud.Draw(renderer, gameTexture, 12, 26, num, 8, SaiyanHud.WithAlpha(color6, alpha));
			SaiyanHud.Draw(renderer, gameTexture, 12, 26, num, 2, SaiyanHud.WithAlpha(SaiyanHud.Lerp(color6, SaiyanHud.White, 0.45f), alpha));
		}
		int[] array = Options.Instance.Recherche.Crans ?? Array.Empty<int>();
		foreach (int num2 in array)
		{
			int x = 12 + (int)Math.Round(238f * Math.Clamp((float)num2 / 100f, 0f, 1f));
			SaiyanHud.Draw(renderer, gameTexture, x, 24, 1, 12, SaiyanHud.WithAlpha(Filet, alpha));
		}
		for (int num3 = Gains.Count - 1; num3 >= 0; num3--)
		{
			(float Gain, long A) tuple = Gains[num3];
			float item = tuple.Gain;
			long item2 = tuple.A;
			long num4 = millisecondsUtc - item2;
			if (num4 > 1400)
			{
				Gains.RemoveAt(num3);
			}
			else
			{
				float num5 = (float)num4 / 1400f;
				string text = $"+{Math.Max(1, (int)MathF.Round(item))}";
				int x2 = 12 + Math.Clamp(num, 12, 226) - 8;
				SaiyanHud.Text(renderer, SaiyanHud.StrongFont, 12, text, x2, 12 - (int)(num5 * 18f), SaiyanHud.WithAlpha(SaiyanHud.GoldLight, alpha * (1f - num5 * num5)));
			}
		}
		var (text2, color7) = Phrase(etat);
		SaiyanHud.TextLeft(renderer, SaiyanHud.SmallFont, 10, text2, 12, 38, 18, SaiyanHud.WithAlpha(color7, alpha));
		Player me = Globals.Me;
		if (me != null)
		{
			KarmaTier tier = Options.Instance.Karma.TierOf(me.Karma);
			string text3 = ((me.Karma > 0) ? $"+{me.Karma}" : me.Karma.ToString());
			string text4 = Strings.Recherche.Karma.ToString(text3, KarmaDisplay.TierName(tier));
			SaiyanHud.TextLeft(renderer, SaiyanHud.SmallFont, 9, text4, 12, 55, 16, SaiyanHud.WithAlpha(Terne, alpha));
		}
	}

	private static (string Texte, Color Couleur) Phrase(RecherchePacket etat)
	{
		switch (etat.Etat)
		{
		case RechercheEtat.Vu:
		{
			int num = Math.Max(etat.Temoins + _passants, 1);
			return (Texte: (num == 1) ? Strings.Recherche.VuUn.ToString() : Strings.Recherche.Vu.ToString(num), Couleur: SaiyanHud.White);
		}
		case RechercheEtat.TemoinParti:
			return (Texte: Strings.Recherche.TemoinParti, Couleur: Jaune);
		case RechercheEtat.PersonneVu:
			return (Texte: Strings.Recherche.PersonneVu, Couleur: Vert);
		case RechercheEtat.Police:
			return (Texte: Strings.Recherche.Police, Couleur: Bleu);
		case RechercheEtat.Enquete:
			return (Texte: Strings.Recherche.Enquete, Couleur: Bleu);
		case RechercheEtat.Poursuite:
			return (Texte: Strings.Recherche.Poursuite, Couleur: Rouge);
		case RechercheEtat.Amende:
			return (Texte: (etat.Montant > 0) ? Strings.Recherche.Amende.ToString(SaiyanHud.Amount(etat.Montant)) : Strings.Recherche.AmendeVide.ToString(), Couleur: Rouge);
		case RechercheEtat.Prison:
			return (Texte: Strings.Recherche.Prison, Couleur: Rouge);
		case RechercheEtat.Semee:
			return (Texte: Strings.Recherche.Semee, Couleur: Vert);
		default:
			return (Texte: string.Empty, Couleur: SaiyanHud.White);
		}
	}

	private static void Insigne(Intersect.Client.Framework.Gwen.Renderer.Base renderer, IGameTexture? blanc, int x, int y, Color couleur)
	{
		for (int i = 0; i < 16; i++)
		{
			int num = ((i < 9) ? 14 : (14 - (i - 8) * 2));
			if (num <= 0)
			{
				break;
			}
			SaiyanHud.Draw(renderer, blanc, x + (14 - num) / 2, y + i, num, 1, couleur);
		}
		Color color = SaiyanHud.Lerp(couleur, SaiyanHud.White, 0.55f);
		SaiyanHud.Draw(renderer, blanc, x + 6, y + 3, 2, 8, color);
		SaiyanHud.Draw(renderer, blanc, x + 3, y + 6, 8, 2, color);
	}
}
