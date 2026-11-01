using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.General;
using Intersect.Framework.Core;

namespace Intersect.Client.Interface.Game;

public static class Disposition
{
	public enum Piece
	{
		VieEtKi,
		Cible,
		Raccourcis,
		Menus,
		Tchat,
		MiniCarte,
		Suivi,
		Police,
		Effets,
		Groupe
	}

	public sealed class Reglage
	{
		public bool Deplacee;

		public float Fx;

		public float Fy;

		public int Dx;

		public int Dy;

		public float Taille = 1f;

		public bool Masquee;
	}

	public sealed class Vue
	{
		public Rectangle Bornes;

		public Rectangle Origine;

		public long VueA;

		public bool Presente;

		public Rectangle Zone;

		public long ZoneA;
	}

	private const int FraicheurMs = 400;

	public const float TailleMin = 0.7f;

	public const float TailleMax = 1.4f;

	private const string Cle = "DispositionInterface";

	public static readonly Piece[] Pieces = Enum.GetValues<Piece>();

	private static readonly Dictionary<Piece, Reglage> Reglages = new Dictionary<Piece, Reglage>();

	private static readonly Dictionary<Piece, Vue> Vues = new Dictionary<Piece, Vue>();

	private static readonly HashSet<Piece> MasqueesIci = new HashSet<Piece>();

	private static bool _lue;

	public static bool EnEdition { get; internal set; }

	public static bool Redimensionnable(Piece piece)
	{
		if ((uint)piece <= 1u)
		{
			return true;
		}
		return false;
	}

	public static Reglage? De(Piece piece)
	{
		Lire();
		return Reglages.GetValueOrDefault(piece);
	}

	public static bool Masquee(Piece piece)
	{
		return De(piece)?.Masquee ?? false;
	}

	public static float Taille(Piece piece)
	{
		if (Redimensionnable(piece))
		{
			Reglage reglage = De(piece);
			if (reglage != null)
			{
				return Math.Clamp(reglage.Taille, 0.7f, 1.4f);
			}
		}
		return 1f;
	}

	public static Vue VueDe(Piece piece)
	{
		if (!Vues.TryGetValue(piece, out Vue value))
		{
			value = (Vues[piece] = new Vue());
		}
		return value;
	}

	public static Point Poser(Piece piece, int largeur, int hauteur, int origineX, int origineY, Base? ecran, bool presente = true)
	{
		Vue vue = VueDe(piece);
		vue.Origine = new Rectangle(origineX, origineY, largeur, hauteur);
		Point result = new Point(origineX, origineY);
		if (ecran != null && ecran.Width > 0 && ecran.Height > 0)
		{
			Reglage reglage = De(piece);
			if (reglage != null && reglage.Deplacee)
			{
				result = Lieu(reglage, largeur, hauteur, ecran.Width, ecran.Height);
			}
		}
		vue.Bornes = new Rectangle(result.X, result.Y, largeur, hauteur);
		vue.VueA = Timing.Global.MillisecondsUtc;
		vue.Presente = presente;
		return result;
	}

	public static void Aligner(Piece piece, Base controle, Alignments[] alignement, int vueHaut = 0, int vueHauteur = 0)
	{
		Base parent = controle.Parent;
		if (parent == null || parent.Width <= 0 || parent.Height <= 0)
		{
			return;
		}
		if (Masquee(piece))
		{
			if (!controle.IsHidden)
			{
				controle.IsHidden = true;
				MasqueesIci.Add(piece);
			}
		}
		else if (MasqueesIci.Remove(piece))
		{
			controle.IsHidden = false;
		}
		int width = controle.Width;
		int num = ((vueHauteur > 0) ? vueHauteur : controle.Height);
		Vue vue = VueDe(piece);
		Reglage reglage = De(piece);
		if (reglage != null && reglage.Deplacee)
		{
			if (controle.Alignment.Length != 0)
			{
				controle.Alignment = Array.Empty<Alignments>();
			}
			Point point = Lieu(reglage, width, num, parent.Width, parent.Height);
			int num2 = Math.Max(0, point.Y - vueHaut);
			if (controle.X != point.X || controle.Y != num2)
			{
				controle.SetPosition(point.X, num2);
			}
			vue.Bornes = new Rectangle(point.X, num2 + vueHaut, width, num);
		}
		else
		{
			if (controle.Alignment.Length == 0)
			{
				controle.Alignment = alignement;
				controle.ProcessAlignments();
			}
			vue.Bornes = new Rectangle(controle.X, controle.Y + vueHaut, width, num);
			vue.Origine = vue.Bornes;
		}
		vue.VueA = Timing.Global.MillisecondsUtc;
		vue.Presente = !controle.IsHidden;
	}

	public static void Occuper(Piece piece, Rectangle zone)
	{
		Vue vue = VueDe(piece);
		vue.Zone = zone;
		vue.ZoneA = Timing.Global.MillisecondsUtc;
	}

	public static void Occupees(List<Rectangle> zones)
	{
		zones.Clear();
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		Piece[] pieces = Pieces;
		foreach (Piece piece in pieces)
		{
			if (Vues.TryGetValue(piece, out Vue value) && value.Presente && millisecondsUtc - value.VueA <= 400 && !Masquee(piece))
			{
				Rectangle item = ((value.Zone.Width > 0 && millisecondsUtc - value.ZoneA <= 400) ? value.Zone : value.Bornes);
				if (item.Width > 0 && item.Height > 0)
				{
					zones.Add(item);
				}
			}
		}
	}

	public static Point Lieu(Reglage reglage, int largeur, int hauteur, int ecranL, int ecranH)
	{
		int value = (int)MathF.Round(reglage.Fx * (float)ecranL + (float)reglage.Dx - reglage.Fx * (float)largeur);
		int value2 = (int)MathF.Round(reglage.Fy * (float)ecranH + (float)reglage.Dy - reglage.Fy * (float)hauteur);
		return new Point(Math.Clamp(value, 0, Math.Max(0, ecranL - largeur)), Math.Clamp(value2, 0, Math.Max(0, ecranH - hauteur)));
	}

	public static (float Fx, float Fy) Ancre(Rectangle bornes, int ecranL, int ecranH)
	{
		float num = (float)bornes.X + (float)bornes.Width / 2f;
		float num2 = (float)bornes.Y + (float)bornes.Height / 2f;
		return (Fx: (num < (float)ecranL / 3f) ? 0f : ((num > 2f * (float)ecranL / 3f) ? 1f : 0.5f), Fy: (num2 < (float)ecranH / 3f) ? 0f : ((num2 > 2f * (float)ecranH / 3f) ? 1f : 0.5f));
	}

	private static Reglage Avoir(Piece piece)
	{
		Lire();
		if (!Reglages.TryGetValue(piece, out Reglage value))
		{
			value = (Reglages[piece] = new Reglage());
		}
		return value;
	}

	public static void Deplacer(Piece piece, Rectangle bornes, int ecranL, int ecranH)
	{
		Reglage reglage = Avoir(piece);
		(float Fx, float Fy) tuple = Ancre(bornes, ecranL, ecranH);
		float item = tuple.Fx;
		float item2 = tuple.Fy;
		reglage.Deplacee = true;
		reglage.Fx = item;
		reglage.Fy = item2;
		reglage.Dx = (int)MathF.Round((float)bornes.X + item * (float)bornes.Width - item * (float)ecranL);
		reglage.Dy = (int)MathF.Round((float)bornes.Y + item2 * (float)bornes.Height - item2 * (float)ecranH);
		VueDe(piece).Bornes = bornes;
	}

	public static void Tailler(Piece piece, float taille)
	{
		if (Redimensionnable(piece))
		{
			Avoir(piece).Taille = Math.Clamp(MathF.Round(taille * 20f) / 20f, 0.7f, 1.4f);
		}
	}

	public static void Masquer(Piece piece, bool masquee)
	{
		Avoir(piece).Masquee = masquee;
	}

	public static void Remettre(Piece piece)
	{
		Lire();
		Reglages.Remove(piece);
	}

	public static void ToutRemettre()
	{
		Lire();
		Reglages.Clear();
	}

	public static void Garder()
	{
		IEnumerable<string> values = from paire in Reglages
			where paire.Value.Deplacee || paire.Value.Masquee || MathF.Abs(paire.Value.Taille - 1f) > 0.001f
			select string.Join(';', paire.Key, paire.Value.Deplacee ? 1 : 0, paire.Value.Fx.ToString(CultureInfo.InvariantCulture), paire.Value.Fy.ToString(CultureInfo.InvariantCulture), paire.Value.Dx, paire.Value.Dy, paire.Value.Taille.ToString(CultureInfo.InvariantCulture), paire.Value.Masquee ? 1 : 0);
		try
		{
			Globals.Database?.SavePreference("DispositionInterface", string.Join('|', values));
		}
		catch (Exception)
		{
		}
	}

	private static void Lire()
	{
		if (_lue)
		{
			return;
		}
		_lue = true;
		string text;
		try
		{
			text = Globals.Database?.LoadPreference("DispositionInterface");
		}
		catch (Exception)
		{
			return;
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		string[] array = text.Split('|', StringSplitOptions.RemoveEmptyEntries);
		foreach (string text2 in array)
		{
			string[] array2 = text2.Split(';');
			if (array2.Length >= 8 && Enum.TryParse<Piece>(array2[0], out var result) && float.TryParse(array2[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var result2) && float.TryParse(array2[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var result3) && int.TryParse(array2[4], out var result4) && int.TryParse(array2[5], out var result5) && float.TryParse(array2[6], NumberStyles.Float, CultureInfo.InvariantCulture, out var result6))
			{
				Reglages[result] = new Reglage
				{
					Deplacee = (array2[1] == "1"),
					Fx = Math.Clamp(result2, 0f, 1f),
					Fy = Math.Clamp(result3, 0f, 1f),
					Dx = result4,
					Dy = result5,
					Taille = Math.Clamp(result6, 0.7f, 1.4f),
					Masquee = (array2[7] == "1")
				};
			}
		}
	}
}
