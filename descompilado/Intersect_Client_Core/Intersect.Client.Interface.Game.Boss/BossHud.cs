using System;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Interface.Game.Boss;

internal sealed class BossHud : Intersect.Client.Framework.Gwen.Control.Base
{
	private static readonly Color Cristal = new Color(255, 127, 212, 255);

	protected override bool ShouldClip => false;

	public BossHud(Canvas canvas)
		: base(canvas, "BossHud")
	{
		base.MouseInputEnabled = false;
		base.KeyboardInputEnabled = false;
		base.IsHidden = true;
	}

	public void Update()
	{
		Intersect.Client.Framework.Gwen.Control.Base parent = base.Parent;
		if (parent != null && (base.Width != parent.Width || base.Height != parent.Height))
		{
			SetBounds(0, 0, parent.Width, parent.Height);
		}
		base.IsHidden = Globals.Me == null || (BossMondial.Etat == 0 && !BossMondial.BandeauVisible(out float fondu, out string _, out string _) && !BossMondial.ResultatVisible(out fondu));
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		if (base.IsHidden)
		{
			return;
		}
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		CharteScreen.Design d = CharteScreen.Design.For(base.Width, base.Height);
		if (BossMondial.Etat == 2 && BossMondial.SurPlace)
		{
			Barre(renderer, d);
		}
		else
		{
			int etat = BossMondial.Etat;
			if ((uint)(etat - 1) <= 1u)
			{
				Pastille(renderer, d);
			}
		}
		if (BossMondial.BandeauVisible(out float fondu, out string titre, out string sous))
		{
			Bandeau(renderer, d, titre, sous, fondu);
		}
		if (BossMondial.ResultatVisible(out var fondu2))
		{
			BossMondialResultPacket resultat = BossMondial.Resultat;
			if (resultat != null)
			{
				Resultat(renderer, d, resultat, fondu2);
			}
		}
	}

	private static void Plaque(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect, Color accent, float alpha = 1f)
	{
		TechniqueArt.FillVertical(renderer, rect, TechniqueArt.Alpha(new Color(255, 6, 8, 14), 0.93f * alpha), TechniqueArt.Alpha(new Color(255, 6, 8, 14), 0.86f * alpha));
		TechniqueArt.Outline(renderer, rect, TechniqueArt.Alpha(TechniqueArt.Line2, alpha));
		TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, rect.Width, Math.Max(2, rect.Height / 26)), TechniqueArt.Alpha(accent, alpha));
	}

	private static void Pastille(Intersect.Client.Framework.Gwen.Renderer.Base renderer, CharteScreen.Design d)
	{
		bool flag = BossMondial.Etat == 2;
		Color color = (flag ? TechniqueArt.Red : TechniqueArt.Gold);
		int num = d.L(430f);
		Rectangle rect = new Rectangle(d.X(800f) - num / 2, d.Y(14f), num, d.L(52f));
		Plaque(renderer, rect, color);
		int num2 = rect.Y + rect.Height / 2;
		int num3 = rect.X + d.L(16f);
		int num4 = d.L(11f);
		Losange(renderer, num3 + num4, num2, num4, color);
		int x = num3 + num4 * 2 + d.L(10f);
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, d.Pt(12f), flag ? Strings.WorldBoss.BadgeOngoing.ToString() : Strings.WorldBoss.BadgeSoon.ToString(), x, num2 - d.L(11f), d.Em(0.16f, 12f), TechniqueArt.Alpha(color, 0.9f));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, d.Pt(17f), BossMondial.NomAffiche + " · " + BossMondial.ZoneAffichee, x, num2 + d.L(10f), TechniqueArt.Text);
		TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, d.Pt(21f), BossMondial.Horloge(BossMondial.RestantMs), rect.Right - d.L(16f), num2, color);
	}

	private static void Barre(Intersect.Client.Framework.Gwen.Renderer.Base renderer, CharteScreen.Design d)
	{
		bool flag = BossMondial.Cristaux > 0;
		Color color = (flag ? Cristal : TechniqueArt.Ki);
		int num = d.L(760f);
		int h = d.L(flag ? 92 : 76);
		Rectangle rect = new Rectangle(d.X(800f) - num / 2, d.Y(14f), num, h);
		Plaque(renderer, rect, color);
		int num2 = rect.X + d.L(18f);
		int num3 = rect.Right - d.L(18f);
		TechniqueArt.Styled(renderer, TechniqueArt.Display, d.Pt(25f), BossMondial.NomAffiche.ToUpperInvariant(), num2, rect.Y + d.L(14f), new TechniqueArt.Stops(TechniqueArt.White, 0.4f, TechniqueArt.Mix(color, TechniqueArt.White, 0.5f), 1f, color));
		BossMondialPacket paquet = BossMondial.Paquet;
		if (paquet != null)
		{
			string text = Strings.WorldBoss.Difficulty.ToString(NomDifficulte(paquet.Difficulte).ToUpperInvariant());
			int num4 = TechniqueArt.Measure(renderer, TechniqueArt.Label, d.Pt(12f), text).X + d.L(16f);
			Rectangle rect2 = new Rectangle(num3 - num4, rect.Y + d.L(14f), num4, d.L(20f));
			TechniqueArt.Fill(renderer, rect2, TechniqueArt.Alpha(TechniqueArt.White, 0.06f));
			TechniqueArt.TextCenter(renderer, TechniqueArt.Label, d.Pt(12f), text, rect2.X + rect2.Width / 2, rect2.Y + rect2.Height / 2, Couleur(paquet.Difficulte));
			TechniqueArt.TextRight(renderer, TechniqueArt.Label, d.Pt(12f), Strings.WorldBoss.Players.ToString(paquet.Joueurs), rect2.X - d.L(14f), rect2.Y + rect2.Height / 2, TechniqueArt.Soft);
		}
		Rectangle rect3 = new Rectangle(num2, rect.Y + d.L(44f), num3 - num2, d.L(14f));
		TechniqueArt.Fill(renderer, rect3, new Color(255, 10, 14, 24));
		TechniqueArt.Outline(renderer, rect3, TechniqueArt.Line);
		float vie = BossMondial.Vie;
		Rectangle rect4 = new Rectangle(rect3.X + 1, rect3.Y + 1, (int)((float)(rect3.Width - 2) * vie), rect3.Height - 2);
		if (rect4.Width > 0)
		{
			Color color2 = (flag ? new Color(255, 120, 150, 178) : TechniqueArt.Ki);
			TechniqueArt.FillHorizontal(renderer, rect4, TechniqueArt.Mix(color2, TechniqueArt.Black, 0.55f), color2, TechniqueArt.Mix(color2, TechniqueArt.White, 0.65f));
		}
		float[] array = new float[3] { 0.75f, 0.5f, 0.25f };
		foreach (float num5 in array)
		{
			int x = rect3.X + (int)((float)rect3.Width * num5);
			TechniqueArt.Fill(renderer, new Rectangle(x, rect3.Y - d.L(3f), Math.Max(1, d.L(2f)), rect3.Height + d.L(6f)), TechniqueArt.Alpha(Cristal, (vie <= num5 + 0.001f) ? 0.3f : 0.85f));
		}
		TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, d.Pt(13f), Strings.WorldBoss.HealthPercent.ToString(Math.Round(vie * 100f)), rect3.X + rect3.Width / 2, rect3.Y + rect3.Height / 2, TechniqueArt.Text);
		TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, d.Pt(15f), BossMondial.Horloge(BossMondial.RestantMs), num3, rect.Y + d.L(34f), TechniqueArt.Gold);
		if (flag)
		{
			int centerY = rect.Y + d.L(72f);
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, d.Pt(12f), Strings.WorldBoss.Shield.ToString(BossMondial.Cristaux, BossMondial.CristauxTotal), num2, centerY, Cristal);
			for (int j = 0; j < BossMondial.CristauxTotal; j++)
			{
				int centerX = num3 - d.L(14f) - j * d.L(20f);
				Losange(renderer, centerX, centerY, d.L(7f), (j < BossMondial.Cristaux) ? Cristal : TechniqueArt.Alpha(TechniqueArt.White, 0.14f));
			}
		}
	}

	private static void Bandeau(Intersect.Client.Framework.Gwen.Renderer.Base renderer, CharteScreen.Design d, string titre, string sous, float fondu)
	{
		Color color = ((titre.Contains("BOUCLIER", StringComparison.OrdinalIgnoreCase) || titre == Traduction.Contenu("BOUCLIER BRISÉ")) ? Cristal : ((titre.Contains("VAINCU", StringComparison.OrdinalIgnoreCase) || titre == Strings.WorldBoss.Victory.ToString()) ? TechniqueArt.Gold : TechniqueArt.Ki));
		int num = d.L(700f);
		Rectangle rect = new Rectangle(d.X(800f) - num / 2, d.Y(210f), num, d.L(96f));
		Plaque(renderer, rect, color, fondu);
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, d.Pt(34f), titre);
		TechniqueArt.Glow(renderer, (float)rect.X + (float)rect.Width / 2f, rect.Y + d.L(40f), (float)point.X * 0.6f, TechniqueArt.Alpha(color, 0.25f * fondu));
		TechniqueArt.Styled(renderer, TechniqueArt.Display, d.Pt(34f), titre, rect.X + rect.Width / 2 - point.X / 2, rect.Y + d.L(20f), new TechniqueArt.Stops(TechniqueArt.White, 0.4f, TechniqueArt.Mix(color, TechniqueArt.White, 0.5f), 1f, color), fondu);
		TechniqueArt.TextCenter(renderer, TechniqueArt.Body, d.Pt(15f), sous, rect.X + rect.Width / 2, rect.Y + d.L(74f), TechniqueArt.Alpha(TechniqueArt.Text, fondu));
	}

	private static void Resultat(Intersect.Client.Framework.Gwen.Renderer.Base renderer, CharteScreen.Design d, BossMondialResultPacket resultat, float fondu)
	{
		Color color = (resultat.Victoire ? TechniqueArt.Gold : TechniqueArt.Red);
		int num = d.L(620f);
		string[] butin = resultat.Butin;
		int num2 = ((butin != null) ? butin.Length : 0);
		Rectangle rect = new Rectangle(d.X(800f) - num / 2, d.Y(150f), num, d.L(250 + num2 * 26));
		Plaque(renderer, rect, color, fondu);
		int num3 = rect.X + d.L(22f);
		int num4 = rect.Right - d.L(22f);
		string text = (resultat.Victoire ? Strings.WorldBoss.Victory.ToString() : Strings.WorldBoss.Retreat.ToString());
		TechniqueArt.Styled(renderer, TechniqueArt.Display, d.Pt(30f), text, num3, rect.Y + d.L(16f), new TechniqueArt.Stops(TechniqueArt.White, 0.4f, TechniqueArt.Mix(color, TechniqueArt.White, 0.5f), 1f, color), fondu);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, d.Pt(12f), Strings.WorldBoss.Summary.ToString(Traduction.Contenu(resultat.Nom), Traduction.Contenu(resultat.Zone), BossMondial.Horloge(resultat.DureeMs), resultat.Joueurs), num3, rect.Y + d.L(58f), TechniqueArt.Alpha(TechniqueArt.Soft, fondu));
		TechniqueArt.Fill(renderer, new Rectangle(num3, rect.Y + d.L(70f), num4 - num3, 1), TechniqueArt.Alpha(TechniqueArt.Line, fondu));
		long num5 = Math.Max(1L, resultat.Degats + resultat.Soins + resultat.Aggro);
		Rectangle rect2 = new Rectangle(num3, rect.Y + d.L(84f), num4 - num3, d.L(16f));
		TechniqueArt.Fill(renderer, rect2, TechniqueArt.Alpha(new Color(255, 10, 14, 24), fondu));
		int num6 = rect2.X;
		(long, Color)[] array = new(long, Color)[3]
		{
			(resultat.Degats, TechniqueArt.Ki),
			(resultat.Soins, new Color(255, 143, 208, 138)),
			(resultat.Aggro, Cristal)
		};
		for (int i = 0; i < array.Length; i++)
		{
			(long, Color) tuple = array[i];
			long item = tuple.Item1;
			Color item2 = tuple.Item2;
			int num7 = (int)((float)rect2.Width * ((float)item / (float)num5));
			if (num7 > 0)
			{
				TechniqueArt.Fill(renderer, new Rectangle(num6, rect2.Y, num7, rect2.Height), TechniqueArt.Alpha(item2, fondu));
				num6 += num7;
			}
		}
		TechniqueArt.Outline(renderer, rect2, TechniqueArt.Alpha(TechniqueArt.Line2, fondu));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, d.Pt(12f), Strings.WorldBoss.Damage.ToString(), num3, rect2.Bottom + d.L(14f), TechniqueArt.Alpha(TechniqueArt.Ki, fondu));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, d.Pt(12f), Strings.WorldBoss.Healing.ToString(), num3 + d.L(90f), rect2.Bottom + d.L(14f), TechniqueArt.Alpha(new Color(255, 143, 208, 138), fondu));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, d.Pt(12f), Strings.WorldBoss.AggroHeld.ToString(), num3 + d.L(160f), rect2.Bottom + d.L(14f), TechniqueArt.Alpha(Cristal, fondu));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, d.Pt(21f), Strings.WorldBoss.Points.ToString(resultat.Points.ToString("N0")), num3, rect2.Bottom + d.L(42f), TechniqueArt.Alpha(TechniqueArt.Text, fondu));
		TechniqueArt.TextRight(renderer, TechniqueArt.Label, d.Pt(12f), Strings.WorldBoss.Rank.ToString(resultat.Rang, resultat.Total, resultat.Jetons, resultat.Tirages), num4, rect2.Bottom + d.L(42f), TechniqueArt.Alpha(TechniqueArt.Soft, fondu));
		int num8 = rect2.Bottom + d.L(64f);
		string[] array2 = resultat.Butin ?? Array.Empty<string>();
		foreach (string text2 in array2)
		{
			string[] array3 = text2.Split('|');
			string text3 = ((array3.Length != 0) ? array3[0] : text2);
			string text4 = ((array3.Length > 1) ? array3[1] : string.Empty);
			int result;
			int num9 = ((array3.Length > 2 && int.TryParse(array3[2], out result)) ? result : 0);
			Color color2 = ((num9 >= 4) ? new Color(255, 255, 176, 58) : (num9 switch
			{
				3 => new Color(255, 181, 123, 255), 
				2 => new Color(255, 111, 168, 255), 
				1 => new Color(255, 143, 208, 138), 
				_ => TechniqueArt.Text, 
			}));
			Color color3 = color2;
			TechniqueArt.Fill(renderer, new Rectangle(num3, num8, num4 - num3, d.L(22f)), TechniqueArt.Alpha(TechniqueArt.White, 0.04f * fondu));
			TechniqueArt.Fill(renderer, new Rectangle(num3, num8, Math.Max(2, d.L(3f)), d.L(22f)), TechniqueArt.Alpha(color3, fondu));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, d.Pt(13f), text3, num3 + d.L(12f), num8 + d.L(11f), TechniqueArt.Alpha(color3, fondu));
			TechniqueArt.TextRight(renderer, TechniqueArt.Label, d.Pt(12f), text4, num4 - d.L(8f), num8 + d.L(11f), TechniqueArt.Alpha(TechniqueArt.Dim, fondu));
			num8 += d.L(26f);
		}
	}

	private static void Losange(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int centerX, int centerY, int rayon, Color couleur)
	{
		for (int i = 0; i < rayon; i++)
		{
			int num = (rayon - i) * 2;
			TechniqueArt.Fill(renderer, new Rectangle(centerX - num / 2, centerY - i, num, 1), couleur);
			TechniqueArt.Fill(renderer, new Rectangle(centerX - num / 2, centerY + i, num, 1), couleur);
		}
	}

	private static string NomDifficulte(string difficulte)
	{
		return difficulte switch
		{
			"Normale" => Strings.WorldBoss.DifficultyNormal.ToString(), 
			"Renforcée" => Strings.WorldBoss.DifficultyReinforced.ToString(), 
			"Élevée" => Strings.WorldBoss.DifficultyHigh.ToString(), 
			"Maximale" => Strings.WorldBoss.DifficultyMaximum.ToString(), 
			_ => difficulte, 
		};
	}

	private static Color Couleur(string difficulte)
	{
		return difficulte switch
		{
			"Normale" => new Color(255, 143, 208, 138), 
			"Renforcée" => TechniqueArt.Gold, 
			"Élevée" => new Color(255, 255, 155, 74), 
			_ => TechniqueArt.Red, 
		};
	}
}
