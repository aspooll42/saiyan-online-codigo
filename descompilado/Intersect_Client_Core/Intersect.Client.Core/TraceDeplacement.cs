using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using Intersect.Client.Entities;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Maps;

namespace Intersect.Client.Core;

internal static class TraceDeplacement
{
	private static readonly StreamWriter? Sortie = Ouvrir();

	private static int _lignes;

	private static double _derniereEcriture;

	private static readonly StringBuilder Ligne = new StringBuilder(160);

	private static StreamWriter? Ouvrir()
	{
		try
		{
			if (!File.Exists("trace-deplacement.on"))
			{
				return null;
			}
			Directory.CreateDirectory("logs");
			StreamWriter streamWriter = new StreamWriter(Path.Combine("logs", "trace-deplacement.csv"), append: false, Encoding.ASCII, 65536);
			streamWriter.WriteLine("t,carte,mode,dir,bouge,caseX,caseY,decX,decY,centreX,centreY,vueX,vueY,retardX,retardY");
			return streamWriter;
		}
		catch
		{
			return null;
		}
	}

	public static void Noter(Entity joueur, float retardX, float retardY, FloatRect vue)
	{
		if (Sortie == null || _lignes >= 400000)
		{
			return;
		}
		double maintenant = ChronoImage.Maintenant;
		char value = (joueur.EstPiloteLibrement ? 'L' : (joueur.SeRecentre ? 'G' : ((joueur.Dashing != null) ? 'D' : (joueur.IsMoving ? 'P' : 'A'))));
		string text = MapInstance.Get(joueur.MapId)?.Name ?? "?";
		Vector2 center = joueur.Center;
		CultureInfo invariantCulture = CultureInfo.InvariantCulture;
		Ligne.Clear();
		Ligne.Append(maintenant.ToString("0.000", invariantCulture)).Append(',').Append(text.Replace(',', ' '))
			.Append(',')
			.Append(value)
			.Append(',')
			.Append((int)joueur.DirectionFacing)
			.Append(',')
			.Append(joueur.IsMoving ? 1 : 0)
			.Append(',')
			.Append(joueur.X)
			.Append(',')
			.Append(joueur.Y)
			.Append(',')
			.Append(joueur.OffsetX.ToString("0.00", invariantCulture))
			.Append(',')
			.Append(joueur.OffsetY.ToString("0.00", invariantCulture))
			.Append(',')
			.Append(center.X.ToString("0.00", invariantCulture))
			.Append(',')
			.Append(center.Y.ToString("0.00", invariantCulture))
			.Append(',')
			.Append(vue.X.ToString("0.00", invariantCulture))
			.Append(',')
			.Append(vue.Y.ToString("0.00", invariantCulture))
			.Append(',')
			.Append(retardX.ToString("0", invariantCulture))
			.Append(',')
			.Append(retardY.ToString("0", invariantCulture));
		try
		{
			Sortie.WriteLine(Ligne.ToString());
			_lignes++;
			if (maintenant - _derniereEcriture > 1000.0)
			{
				_derniereEcriture = maintenant;
				Sortie.Flush();
			}
		}
		catch
		{
		}
	}
}
