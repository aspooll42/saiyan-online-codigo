using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Entities.Events;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.General;
using Intersect.Client.Housing;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.Framework.Core.GameObjects.Maps.Attributes;
using Intersect.Localization;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Entities;

public static class Figurants
{
	internal sealed class Ville
	{
		public required MapInstance Carte;

		public required Meteo.DonneesVille Donnees;

		public readonly List<Figurant> Gens = new List<Figurant>();

		public readonly List<Figurant> Betes = new List<Figurant>();

		public long ProchaineNaissance;

		public int Suivant;

		public long PeurJusqua;

		public long ProchainCri;

		public (int X, int Y)[]? Abris;

		public bool[]? Accessibles;

		public bool[]? DevantLesPortes;
	}

	private static readonly string[] Civils = new string[18]
	{
		"villageois.png", "villageoise.png", "femme_blonde.png", "homme_roux.png", "fille_salopette.png", "vieil_homme.png", "garcon_bleu.png", "femme_chignon.png", "homme_champignon.png", "villageois2.png",
		"fillette_rose.png", "garcon_bonnet.png", "fille_blonde.png", "vieux_moine.png", "villageois3.png", "fillette_rouge.png", "fermier.png", "fille_petard.png"
	};

	private const string SpriteChien = "chien_ville.png";

	private const string SpriteChat = "chat_ville.png";

	private static readonly Dictionary<Guid, Ville> Villes = new Dictionary<Guid, Ville>();

	private static string[]? _civilsPresents;

	private static bool _chienPresent;

	private static bool _chatPresent;

	private static bool _enPanne;

	private static long _t;

	private static long _prochainePhrase = 8000L;

	public static void NouvelleImage(TimeSpan duree)
	{
		if (_enPanne)
		{
			return;
		}
		try
		{
			_t += (long)Math.Clamp(duree.TotalMilliseconds, 0.0, 100.0);
			if (!(Globals.Me?.MapInstance is MapInstance mapInstance) || Globals.MapGrid == null)
			{
				ToutRetirer();
				return;
			}
			if (_civilsPresents == null)
			{
				_civilsPresents = Civils.Where((string n) => Globals.ContentManager.GetTexture(TextureType.Entity, n) != null).ToArray();
			}
			if (_civilsPresents.Length == 0)
			{
				return;
			}
			HashSet<Guid> vues = new HashSet<Guid>();
			for (int num = Math.Max(mapInstance.GridX - 1, 0); num <= Math.Min(mapInstance.GridX + 1, Globals.MapGridWidth - 1); num++)
			{
				for (int num2 = Math.Max(mapInstance.GridY - 1, 0); num2 <= Math.Min(mapInstance.GridY + 1, Globals.MapGridHeight - 1); num2++)
				{
					Guid guid = Globals.MapGrid[num, num2];
					if (guid == Guid.Empty || !MapInstance.TryGet(guid, out MapInstance instance))
					{
						continue;
					}
					Meteo.DonneesVille donneesVille = Meteo.VilleDe(instance);
					if (donneesVille == null || !donneesVille.Urbaine)
					{
						continue;
					}
					bool[] trottoir = donneesVille.Trottoir;
					if (trottoir == null || trottoir.Length <= 0)
					{
						continue;
					}
					vues.Add(guid);
					if (!Villes.TryGetValue(guid, out Ville value) || value.Carte != instance)
					{
						if (value != null)
						{
							Retirer(value);
						}
						value = new Ville
						{
							Carte = instance,
							Donnees = donneesVille
						};
						Villes[guid] = value;
					}
					Peupler(value);
					Animer(value);
				}
			}
			Guid[] array = Villes.Keys.Where((Guid k) => !vues.Contains(k)).ToArray();
			foreach (Guid key in array)
			{
				Retirer(Villes[key]);
				Villes.Remove(key);
			}
			if (_t >= _prochainePhrase && Villes.Count > 0)
			{
				_prochainePhrase = _t + 14000 + Globals.Random.Next(16000);
				FloatRect vue = Graphics.WorldViewport;
				Figurant[] array2 = (from f in Villes.Values.SelectMany((Ville v) => v.Gens)
					where ALEcran(f, vue) && !f.IsMoving
					select f).ToArray();
				if (array2.Length != 0 && Strings.Figurants.Phrases.Count > 0)
				{
					LocalizedString[] array3 = Strings.Figurants.Phrases.Values.ToArray();
					array2[Globals.Random.Next(array2.Length)].AddChatBubble(array3[Globals.Random.Next(array3.Length)].ToString());
				}
			}
		}
		catch (Exception exception)
		{
			_enPanne = true;
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Figurants coupes : ils ne doivent pas faire planter le jeu");
			try
			{
				ToutRetirer();
			}
			catch
			{
			}
		}
	}

	private static int Voulus(Ville ville)
	{
		int num = ville.Donnees.Trottoir.Count((bool t) => t);
		int num2 = Math.Clamp(num / 60, 3, 10);
		if (!(Meteo.LampesActuelles > 0.6f))
		{
			return num2;
		}
		return Math.Max(1, num2 / 3);
	}

	private static void Peupler(Ville ville)
	{
		int num = Voulus(ville);
		FloatRect vue = Graphics.WorldViewport;
		if (ville.Gens.Count > num)
		{
			Figurant figurant = ville.Gens.FirstOrDefault((Figurant f) => !ALEcran(f, vue));
			if (figurant != null)
			{
				Retirer(ville, figurant);
			}
		}
		else if (ville.Gens.Count < num && _t >= ville.ProchaineNaissance && Timing.Global.MillisecondsUtc >= ville.PeurJusqua)
		{
			ville.ProchaineNaissance = _t + 400;
			(int, int)? tuple = CaseLibre(ville, horsEcran: true) ?? CaseLibre(ville, horsEcran: false);
			if (tuple.HasValue)
			{
				(int, int) valueOrDefault = tuple.GetValueOrDefault();
				string sprite = _civilsPresents[ville.Suivant++ % _civilsPresents.Length];
				Figurant figurant2 = new Figurant(ville, sprite, valueOrDefault.Item1, valueOrDefault.Item2);
				ville.Gens.Add(figurant2);
				ville.Carte.LocalCritters[figurant2.Id] = figurant2;
			}
		}
		Chien chien = ville.Betes.OfType<Chien>().FirstOrDefault((Chien ch) => ch.SansMaitre && !ALEcran(ch, vue));
		if (chien != null)
		{
			Retirer(ville, chien);
		}
		_chienPresent = Globals.ContentManager.GetTexture(TextureType.Entity, "chien_ville.png") != null;
		_chatPresent = Globals.ContentManager.GetTexture(TextureType.Entity, "chat_ville.png") != null;
		int num2 = (_chienPresent ? ((Meteo.LampesActuelles > 0.6f) ? 1 : 2) : 0);
		int num3 = ville.Betes.Count((Figurant b) => b is Chien);
		bool flag = Timing.Global.MillisecondsUtc >= ville.PeurJusqua;
		if (num3 < num2 && ville.Gens.Count >= 3 && flag)
		{
			Figurant figurant3 = ville.Gens.FirstOrDefault((Figurant g) => !ville.Betes.OfType<Chien>().Any((Chien ch) => ch.Maitre == g));
			if (figurant3 != null)
			{
				Chien chien2 = new Chien(ville, figurant3);
				ville.Betes.Add(chien2);
				ville.Carte.LocalCritters[chien2.Id] = chien2;
			}
		}
		else if (num3 > num2)
		{
			Chien chien3 = ville.Betes.OfType<Chien>().FirstOrDefault((Chien b) => !ALEcran(b, vue));
			if (chien3 != null)
			{
				Retirer(ville, chien3);
			}
		}
		if (_chatPresent && flag && ville.Betes.Count((Figurant b) => b is Chat) < 2)
		{
			(int, int)? tuple2 = PlaceDeChat(ville);
			if (tuple2.HasValue)
			{
				(int, int) valueOrDefault2 = tuple2.GetValueOrDefault();
				Chat chat = new Chat(ville, valueOrDefault2.Item1, valueOrDefault2.Item2);
				ville.Betes.Add(chat);
				ville.Carte.LocalCritters[chat.Id] = chat;
			}
		}
	}

	private static void Animer(Ville ville)
	{
		Figurant[] array = ville.Gens.Concat(ville.Betes).ToArray();
		foreach (Figurant figurant in array)
		{
			if (!figurant.IsMoving && Houses.IsBlocked(figurant.MapId, figurant.X, figurant.Y))
			{
				Retirer(ville, figurant);
			}
			else
			{
				figurant.Update();
			}
		}
		for (int j = 0; j < ville.Gens.Count; j++)
		{
			Figurant figurant2 = ville.Gens[j];
			if (figurant2.Etat != Figurant.Etats.Pause || figurant2.IsMoving)
			{
				continue;
			}
			for (int k = j + 1; k < ville.Gens.Count; k++)
			{
				Figurant figurant3 = ville.Gens[k];
				if (figurant3.Etat == Figurant.Etats.Pause && !figurant3.IsMoving && figurant2.Y == figurant3.Y && Math.Abs(figurant2.X - figurant3.X) == 1)
				{
					figurant2.DirectionFacing = ((figurant2.X < figurant3.X) ? Direction.Right : Direction.Left);
					figurant3.DirectionFacing = ((figurant3.X < figurant2.X) ? Direction.Right : Direction.Left);
				}
			}
		}
	}

	private static bool ALEcran(Entity e, FloatRect vue)
	{
		if (!MapInstance.TryGet(e.MapId, out MapInstance instance))
		{
			return false;
		}
		int num = instance.X + e.X * Options.Instance.Map.TileWidth;
		int num2 = instance.Y + e.Y * Options.Instance.Map.TileHeight;
		if ((float)(num + 32) >= vue.Left - 64f && (float)num <= vue.Right + 64f && (float)(num2 + 48) >= vue.Top - 64f)
		{
			return (float)num2 <= vue.Bottom + 64f;
		}
		return false;
	}

	private static void Retirer(Ville ville, Figurant f)
	{
		ville.Gens.Remove(f);
		ville.Betes.Remove(f);
		ville.Carte.LocalCritters.Remove(f.Id);
		f.Dispose();
	}

	private static void Retirer(Ville ville)
	{
		Figurant[] array = ville.Gens.Concat(ville.Betes).ToArray();
		foreach (Figurant figurant in array)
		{
			ville.Carte.LocalCritters.Remove(figurant.Id);
			figurant.Dispose();
		}
		ville.Gens.Clear();
		ville.Betes.Clear();
	}

	private static void ToutRetirer()
	{
		foreach (Ville value in Villes.Values)
		{
			Retirer(value);
		}
		Villes.Clear();
	}

	public static void Alerte(Guid mapId, float x, float y, int portee)
	{
		if (_enPanne || !Villes.TryGetValue(mapId, out Ville value))
		{
			return;
		}
		try
		{
			long millisecondsUtc = Timing.Global.MillisecondsUtc;
			value.PeurJusqua = millisecondsUtc + 15000;
			int tileWidth = Options.Instance.Map.TileWidth;
			int tileHeight = Options.Instance.Map.TileHeight;
			List<Figurant> list = new List<Figurant>();
			Figurant[] array = value.Gens.Concat(value.Betes).ToArray();
			foreach (Figurant figurant in array)
			{
				if (figurant.Etat == Figurant.Etats.Fuite)
				{
					continue;
				}
				float num = (float)(figurant.X * tileWidth) + (float)tileWidth / 2f;
				float num2 = (float)(figurant.Y * tileHeight) + (float)tileHeight / 2f;
				if (!((num - x) * (num - x) + (num2 - y) * (num2 - y) > (float)(portee * portee)))
				{
					List<(int, int)> list2 = CheminVersUnAbri(value, figurant, x / (float)tileWidth, y / (float)tileHeight);
					if (list2 != null)
					{
						figurant.Fuir(millisecondsUtc, list2);
						list.Add(figurant);
					}
				}
			}
			if (list.Count == 0 || millisecondsUtc < value.ProchainCri)
			{
				return;
			}
			value.ProchainCri = millisecondsUtc + 3000;
			LocalizedString[] array2 = Strings.Recherche.Cris.Values.ToArray();
			List<Figurant> list3 = (from _ in list.Where(delegate(Figurant f)
				{
					bool flag = ((f is Chien || f is Chat) ? true : false);
					return !flag;
				})
				orderby Globals.Random.Next()
				select _).Take(2).ToList();
			foreach (Figurant item in list3)
			{
				if (array2.Length != 0)
				{
					item.AddChatBubble(array2[Globals.Random.Next(array2.Length)].ToString());
				}
			}
			foreach (Chien item2 in list.OfType<Chien>())
			{
				item2.AddChatBubble(Strings.Recherche.Chien.ToString());
			}
		}
		catch (Exception exception)
		{
			_enPanne = true;
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Figurants coupes : ils ne doivent pas faire planter le jeu");
		}
	}

	public static int PassantsAutourDeMoi(int cases)
	{
		Player moi = Globals.Me;
		if (moi == null || !Villes.TryGetValue(moi.MapId, out Ville value))
		{
			return 0;
		}
		return value.Gens.Count((Figurant f) => f.Etat != Figurant.Etats.Fuite && Math.Abs(f.X - moi.X) <= cases && Math.Abs(f.Y - moi.Y) <= cases);
	}

	internal static void Cacher(Ville ville, Figurant f)
	{
		Retirer(ville, f);
	}

	private static List<(int X, int Y)>? CheminVersUnAbri(Ville ville, Figurant f, float dangerX, float dangerY)
	{
		if (ville.Abris == null)
		{
			ville.Abris = Abris(ville);
		}
		float ici = Distance((int)f.X, (int)f.Y, dangerX, dangerY);
		foreach (var (num, num2) in (from abri in ville.Abris
			where Distance(abri.X, abri.Y, dangerX, dangerY) > ici + 2f
			orderby Math.Abs(abri.X - f.X) + Math.Abs(abri.Y - f.Y)
			select abri).Take(3))
		{
			if (num == f.X && num2 == f.Y)
			{
				return new List<(int, int)>();
			}
			List<(int, int)> list = Chemin(ville, f.X, f.Y, num, num2);
			if (list.Count > 0)
			{
				return list;
			}
		}
		return null;
	}

	private static float Distance(float ax, float ay, float bx, float by)
	{
		return MathF.Sqrt((ax - bx) * (ax - bx) + (ay - by) * (ay - by));
	}

	private static (int X, int Y)[] Abris(Ville ville)
	{
		Meteo.DonneesVille donnees = ville.Donnees;
		List<(int, int)> list = new List<(int, int)>();
		MapAttribute[,] attributes = ville.Carte.Attributes;
		for (int i = 0; i < donnees.TrottoirH; i++)
		{
			for (int j = 0; j < donnees.TrottoirL; j++)
			{
				if (!Trottoir(ville, j, i) || BloqueeParLaCarte(ville, j, i) || !Accessible(ville, j, i))
				{
					continue;
				}
				bool flag = j == 0 || i == 0 || j == donnees.TrottoirL - 1 || i == donnees.TrottoirH - 1;
				bool flag2 = false;
				if (attributes != null)
				{
					(int, int)[] array = new(int, int)[4]
					{
						(1, 0),
						(-1, 0),
						(0, 1),
						(0, -1)
					};
					for (int k = 0; k < array.Length; k++)
					{
						(int, int) tuple = array[k];
						int item = tuple.Item1;
						int item2 = tuple.Item2;
						int num = j + item;
						int num2 = i + item2;
						if (num >= 0 && num2 >= 0 && num < attributes.GetLength(0) && num2 < attributes.GetLength(1) && attributes[num, num2] is MapWarpAttribute)
						{
							flag2 = true;
							break;
						}
					}
				}
				if (flag || flag2)
				{
					list.Add((j, i));
				}
			}
		}
		return list.ToArray();
	}

	internal static bool Trottoir(Ville ville, int x, int y)
	{
		Meteo.DonneesVille donnees = ville.Donnees;
		if (x >= 0 && y >= 0 && x < donnees.TrottoirL && y < donnees.TrottoirH)
		{
			return donnees.Trottoir[y * donnees.TrottoirL + x];
		}
		return false;
	}

	internal static bool Chaussee(Ville ville, int x, int y)
	{
		int num = x * Options.Instance.Map.TileWidth + Options.Instance.Map.TileWidth / 2;
		int num2 = y * Options.Instance.Map.TileHeight + Options.Instance.Map.TileHeight / 2;
		(short, short, bool)[] routes = ville.Donnees.Routes;
		for (int i = 0; i < routes.Length; i++)
		{
			(short, short, bool) tuple = routes[i];
			short item = tuple.Item1;
			short item2 = tuple.Item2;
			int num3 = (tuple.Item3 ? num2 : num);
			if (num3 >= item && num3 <= item2)
			{
				return true;
			}
		}
		return false;
	}

	internal static bool BloqueeParLaCarte(Ville ville, int x, int y)
	{
		MapAttribute[,] attributes = ville.Carte.Attributes;
		if (attributes == null || x < 0 || y < 0 || x >= attributes.GetLength(0) || y >= attributes.GetLength(1))
		{
			return true;
		}
		if (Houses.IsBlocked(ville.Carte.Id, x, y))
		{
			return true;
		}
		MapAttribute mapAttribute = attributes[x, y];
		if (mapAttribute != null)
		{
			if (!(mapAttribute is MapBlockedAttribute))
			{
				if (!(mapAttribute is MapNpcAvoidAttribute))
				{
					if (mapAttribute is MapAnimationAttribute mapAnimationAttribute)
					{
						if (mapAnimationAttribute.IsBlock)
						{
							return true;
						}
					}
					else if (mapAttribute is MapWarpAttribute)
					{
						return true;
					}
					return false;
				}
				return true;
			}
			return true;
		}
		return false;
	}

	internal static bool LigneBarre(Ville ville, int x, int y, int nx, int ny)
	{
		IndexDesLignes indexDesLignes = ville.Carte.IndexDesLignes;
		if (indexDesLignes != null && !indexDesLignes.Aucune)
		{
			return IndexDesLignes.BarreLePas(indexDesLignes, x, y, indexDesLignes, nx, ny, IndexDesLignes.DuPas(nx - x, ny - y));
		}
		return false;
	}

	internal static bool Accessible(Ville ville, int x, int y)
	{
		PreparerLesZones(ville);
		Meteo.DonneesVille donnees = ville.Donnees;
		if (x >= 0 && y >= 0 && x < donnees.TrottoirL && y < donnees.TrottoirH)
		{
			return ville.Accessibles[y * donnees.TrottoirL + x];
		}
		return false;
	}

	internal static bool DevantUnePorte(Ville ville, int x, int y)
	{
		PreparerLesZones(ville);
		Meteo.DonneesVille donnees = ville.Donnees;
		if (x >= 0 && y >= 0 && x < donnees.TrottoirL && y < donnees.TrottoirH)
		{
			return ville.DevantLesPortes[y * donnees.TrottoirL + x];
		}
		return false;
	}

	internal static bool PeutSArreter(Ville ville, int x, int y, Figurant? sauf)
	{
		if (!Accessible(ville, x, y) || DevantUnePorte(ville, x, y) || BloqueeParLaCarte(ville, x, y) || Occupee(ville, x, y, sauf))
		{
			return false;
		}
		Guid id = ville.Carte.Id;
		if (!Evenement(id, x, y) && !Evenement(id, x + 1, y) && !Evenement(id, x - 1, y) && !Evenement(id, x, y + 1))
		{
			return !Evenement(id, x, y - 1);
		}
		return false;
	}

	private static void PreparerLesZones(Ville ville)
	{
		if (ville.Accessibles != null && ville.DevantLesPortes != null)
		{
			return;
		}
		Meteo.DonneesVille donnees = ville.Donnees;
		int trottoirL = donnees.TrottoirL;
		int trottoirH = donnees.TrottoirH;
		IndexDesLignes indexDesLignes = ville.Carte.IndexDesLignes;
		IndexDesLignes indexDesLignes2 = ((indexDesLignes != null && !indexDesLignes.Aucune) ? indexDesLignes : null);
		bool[] array = new bool[trottoirL * trottoirH];
		for (int i = 0; i < trottoirH; i++)
		{
			for (int j = 0; j < trottoirL; j++)
			{
				array[i * trottoirL + j] = (Trottoir(ville, j, i) || Chaussee(ville, j, i)) && !BloqueeParLaCarte(ville, j, i) && (indexDesLignes2 == null || !indexDesLignes2.Coupee(j, i));
			}
		}
		int[] array2 = new int[trottoirL * trottoirH];
		Array.Fill(array2, -1);
		List<int> list = new List<int>();
		Queue<int> queue = new Queue<int>();
		for (int k = 0; k < trottoirL * trottoirH; k++)
		{
			if (!array[k] || array2[k] >= 0)
			{
				continue;
			}
			int count = list.Count;
			list.Add(0);
			array2[k] = count;
			queue.Enqueue(k);
			while (queue.Count > 0)
			{
				int num = queue.Dequeue();
				list[count]++;
				int num2 = num % trottoirL;
				int num3 = num / trottoirL;
				(int, int)[] array3 = new(int, int)[4]
				{
					(1, 0),
					(-1, 0),
					(0, 1),
					(0, -1)
				};
				for (int l = 0; l < array3.Length; l++)
				{
					(int, int) tuple = array3[l];
					int item = tuple.Item1;
					int item2 = tuple.Item2;
					int num4 = num2 + item;
					int num5 = num3 + item2;
					if (num4 >= 0 && num5 >= 0 && num4 < trottoirL && num5 < trottoirH)
					{
						int num6 = num5 * trottoirL + num4;
						if (array[num6] && array2[num6] < 0 && !LigneBarre(ville, num2, num3, num4, num5))
						{
							array2[num6] = count;
							queue.Enqueue(num6);
						}
					}
				}
			}
		}
		int num7 = ((list.Count == 0) ? (-1) : list.IndexOf(list.Max()));
		bool[] array4 = new bool[trottoirL * trottoirH];
		for (int m = 0; m < trottoirL * trottoirH; m++)
		{
			array4[m] = num7 >= 0 && array2[m] == num7;
		}
		bool[] array5 = new bool[trottoirL * trottoirH];
		MapAttribute[,] attributes = ville.Carte.Attributes;
		if (attributes != null)
		{
			for (int n = 0; n < trottoirH; n++)
			{
				for (int num8 = 0; num8 < trottoirL; num8++)
				{
					(int, int)[] array6 = new(int, int)[4]
					{
						(1, 0),
						(-1, 0),
						(0, 1),
						(0, -1)
					};
					for (int num9 = 0; num9 < array6.Length; num9++)
					{
						(int, int) tuple2 = array6[num9];
						int item3 = tuple2.Item1;
						int item4 = tuple2.Item2;
						int num10 = num8 + item3;
						int num11 = n + item4;
						if (num10 >= 0 && num11 >= 0 && num10 < attributes.GetLength(0) && num11 < attributes.GetLength(1) && attributes[num10, num11] is MapWarpAttribute)
						{
							array5[n * trottoirL + num8] = true;
							break;
						}
					}
				}
			}
		}
		ville.Accessibles = array4;
		ville.DevantLesPortes = array5;
	}

	internal static bool Evenement(Guid mapId, int x, int y)
	{
		foreach (Entity value in Globals.Entities.Values)
		{
			if (value is Event && value.MapId == mapId && value.X == x && value.Y == y)
			{
				return true;
			}
		}
		return false;
	}

	internal static bool Occupee(Ville ville, int x, int y, Figurant? sauf)
	{
		foreach (Figurant gen in ville.Gens)
		{
			if (gen != sauf && gen.X == x && gen.Y == y)
			{
				return true;
			}
		}
		foreach (Figurant bete in ville.Betes)
		{
			if (bete != sauf && bete.X == x && bete.Y == y)
			{
				return true;
			}
		}
		return false;
	}

	private static (int X, int Y)? CaseLibre(Ville ville, bool horsEcran)
	{
		Meteo.DonneesVille donnees = ville.Donnees;
		FloatRect worldViewport = Graphics.WorldViewport;
		for (int i = 0; i < 40; i++)
		{
			int num = Globals.Random.Next(donnees.TrottoirL);
			int num2 = Globals.Random.Next(donnees.TrottoirH);
			if (!Trottoir(ville, num, num2) || !PeutSArreter(ville, num, num2, null))
			{
				continue;
			}
			if (horsEcran)
			{
				int num3 = ville.Carte.X + num * Options.Instance.Map.TileWidth;
				int num4 = ville.Carte.Y + num2 * Options.Instance.Map.TileHeight;
				if ((float)(num3 + 32) >= worldViewport.Left - 32f && (float)num3 <= worldViewport.Right + 32f && (float)(num4 + 48) >= worldViewport.Top - 32f && (float)num4 <= worldViewport.Bottom + 32f)
				{
					continue;
				}
			}
			return (num, num2);
		}
		return null;
	}

	private static (int X, int Y)? PlaceDeChat(Ville ville)
	{
		Meteo.DonneesVille donnees = ville.Donnees;
		for (int i = 0; i < 60; i++)
		{
			int x = Globals.Random.Next(donnees.TrottoirL);
			int y = Globals.Random.Next(donnees.TrottoirH);
			if (Trottoir(ville, x, y) && PeutSArreter(ville, x, y, null) && (BloqueeParLaCarte(ville, x - 1, y) || BloqueeParLaCarte(ville, x + 1, y) || BloqueeParLaCarte(ville, x, y - 1)) && !ville.Betes.OfType<Chat>().Any((Chat c) => Math.Abs(c.Repos.X - x) + Math.Abs(c.Repos.Y - y) < 8))
			{
				return (x, y);
			}
		}
		return null;
	}

	internal static List<(int X, int Y)> Chemin(Ville ville, int x0, int y0, int x1, int y1, int maxPas = 40)
	{
		Meteo.DonneesVille donnees = ville.Donnees;
		int trottoirL = donnees.TrottoirL;
		int trottoirH = donnees.TrottoirH;
		if (x0 < 0 || y0 < 0 || x0 >= trottoirL || y0 >= trottoirH || x1 < 0 || y1 < 0 || x1 >= trottoirL || y1 >= trottoirH)
		{
			return new List<(int, int)>();
		}
		int[] array = new int[trottoirL * trottoirH];
		int[] array2 = new int[trottoirL * trottoirH];
		Array.Fill(array, int.MaxValue);
		PriorityQueue<int, int> priorityQueue = new PriorityQueue<int, int>();
		array[y0 * trottoirL + x0] = 0;
		array2[y0 * trottoirL + x0] = -1;
		priorityQueue.Enqueue(y0 * trottoirL + x0, 0);
		int num = y1 * trottoirL + x1;
		while (priorityQueue.Count > 0)
		{
			int num2 = priorityQueue.Dequeue();
			if (num2 == num)
			{
				break;
			}
			int num3 = num2 % trottoirL;
			int num4 = num2 / trottoirL;
			(int, int)[] array3 = new(int, int)[4]
			{
				(1, 0),
				(-1, 0),
				(0, 1),
				(0, -1)
			};
			for (int i = 0; i < array3.Length; i++)
			{
				(int, int) tuple = array3[i];
				int item = tuple.Item1;
				int item2 = tuple.Item2;
				int num5 = num3 + item;
				int num6 = num4 + item2;
				if (num5 < 0 || num6 < 0 || num5 >= trottoirL || num6 >= trottoirH)
				{
					continue;
				}
				bool flag = Trottoir(ville, num5, num6);
				if (Accessible(ville, num5, num6) && !BloqueeParLaCarte(ville, num5, num6) && !LigneBarre(ville, num3, num4, num5, num6))
				{
					int num7 = (flag ? 1 : 6);
					int num8 = num6 * trottoirL + num5;
					int num9 = array[num2] + num7;
					if (num9 < array[num8] && num9 <= maxPas * 6)
					{
						array[num8] = num9;
						array2[num8] = num2;
						priorityQueue.Enqueue(num8, num9 + Math.Abs(num5 - x1) + Math.Abs(num6 - y1));
					}
				}
			}
		}
		List<(int, int)> list = new List<(int, int)>();
		if (array[num] == int.MaxValue)
		{
			return list;
		}
		for (int num10 = num; num10 != y0 * trottoirL + x0; num10 = array2[num10])
		{
			list.Add((num10 % trottoirL, num10 / trottoirL));
		}
		list.Reverse();
		return list;
	}
}
