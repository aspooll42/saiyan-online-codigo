using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Intersect.Client.Core;
using Intersect.Client.Core.Sounds;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Maps;
using Intersect.Client.MonoGame.Graphics;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Dungeons;
using Intersect.Framework.Core.GameObjects.Mapping.Tilesets;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.Models;
using Intersect.Network.Packets.Server;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Dungeons;

public static class DungeonWorld
{
	private sealed class Colosse
	{
		public bool Vu;

		public bool Enrage;

		public long EnrageDepuis;

		public long MortDepuis;

		public Guid MapId;

		public Vector2 Pieds;

		public FloatRect Corps;

		public long DessineA;

		public readonly List<Vector2> Yeux = new List<Vector2>();
	}

	private sealed class Attaque
	{
		public bool Choc;

		public bool Embrasement;

		public bool Tir;

		public int BlocsTombes;

		public int Lances;

		public long DerniereEtincelle;

		public long DernierImpact;

		public Vector2[][]? Fissures;

		public long VueA;
	}

	private enum Genre : byte
	{
		Poussiere,
		Etincelle,
		Eclat,
		Anneau,
		EtincelleKi
	}

	private struct Particule
	{
		public Genre Genre;

		public float X;

		public float Y;

		public float Angle;

		public float Vitesse;

		public float Taille;

		public long Ne;

		public int VieMs;

		public bool Clair;
	}

	private readonly record struct Flash(float X, float Y, int Taille, byte Intensite, long Ne, int VieMs, Color Couleur);

	private readonly record struct EclairEcran(float Force, long Ne, int VieMs);

	private sealed class Planche
	{
		public volatile bool Lue;

		public bool ParLaCarte;

		public int Essais;

		public bool Faite;

		public int L;

		public int H;

		public byte[]? Silhouette;

		public byte[]? Halo;

		public List<(int X, int Y)> Pupilles = new List<(int, int)>();

		public IGameTexture? Masque;

		public IGameTexture? Contour;

		public readonly Dictionary<(int, int, int, int), ((int X, int Y)[] Pixels, Vector2[] Centres)> Images = new Dictionary<(int, int, int, int), ((int, int)[], Vector2[])>();
	}

	private sealed class DalleDecoupee
	{
		public int Empreinte;

		public bool Faite;

		public bool Entiere;

		public IGameTexture? Disque;

		public float[] Libre = Array.Empty<float>();

		public int Cote;
	}

	private enum GenreKi : byte
	{
		Onde,
		Explosion,
		Trainee
	}

	private readonly record struct EffetKi(GenreKi Genre, float X, float Y, float X2, float Y2, float Taille, long Ne, int VieMs);

	public const long RayonMs = 650L;

	public const long TraineeMs = 3200L;

	public const long FissuresApresMs = 3500L;

	public const long BlocsApresMs = 1000L;

	private const float LueurParCoeur = 4.4f;

	private const float HauteurDeChutePx = 260f;

	private const long ChuteMs = 600L;

	private const long PeriodeDesVaguesMs = 2600L;

	private const float VagueMsParPixel = 3.2f;

	private const string PlancheDesDalles = "tombeau.png";

	private static readonly Color OrSombre = new Color(255, 110, 80, 12);

	private static readonly Color OrFonce = new Color(255, 201, 150, 30);

	private static readonly Color Or = new Color(255, 242, 194, 58);

	private static readonly Color OrClair = new Color(255, 255, 226, 122);

	private static readonly Color OrBlanc = new Color(255, 255, 246, 192);

	private static readonly Dictionary<Guid, Colosse> Colosses = new Dictionary<Guid, Colosse>();

	private static readonly Dictionary<(Guid BossId, long RecuA), Attaque> Attaques = new Dictionary<(Guid, long), Attaque>();

	private static readonly List<Particule> Particules = new List<Particule>();

	private static readonly List<Flash> Flashs = new List<Flash>();

	private static readonly List<EclairEcran> EclairsEcran = new List<EclairEcran>();

	private static float _tempete;

	private static MapSound? _vent;

	private static long _derniereImage;

	internal static Func<long>? HorlogeDuBanc;

	private static IGameTexture? _lueur;

	private static IGameTexture? _vignette;

	private static readonly Dictionary<string, Planche> Planches = new Dictionary<string, Planche>();

	private static readonly Dictionary<Guid, Vector2[]> DallesParCarte = new Dictionary<Guid, Vector2[]>();

	private const int FinesseDeDalle = 2;

	private static readonly Dictionary<Guid, DalleDecoupee> Dalles = new Dictionary<Guid, DalleDecoupee>();

	private static readonly List<Tile[,]> Dressees = new List<Tile[,]>();

	private static IGameTexture? _disque;

	private static readonly HashSet<Guid> Saiyans = new HashSet<Guid>();

	private static readonly List<EffetKi> EffetsKi = new List<EffetKi>();

	private const long VolDuKiMs = 420L;

	private static long Maintenant => HorlogeDuBanc?.Invoke() ?? Timing.Global.MillisecondsUtc;

	private static int TileWidth => Options.Instance.Map.TileWidth;

	private static int TileHeight => Options.Instance.Map.TileHeight;

	private static float Pixel => 1f / Math.Max(0.25f, Graphics.WorldScale);

	private static float TailleDuBloc(float rayonCases)
	{
		return Math.Clamp(rayonCases * (float)TileWidth * 0.62f, 24f, 40f);
	}

	private static bool EstUnePupille(byte r, byte g, byte b)
	{
		if (r != byte.MaxValue || g != 24 || b != 57)
		{
			if (r == 206 && g == 33)
			{
				return b == 66;
			}
			return false;
		}
		return true;
	}

	private static float Ph(long t, long a, long b)
	{
		if (b > a)
		{
			return Math.Clamp((float)(t - a) / (float)(b - a), 0f, 1f);
		}
		if (t < b)
		{
			return 0f;
		}
		return 1f;
	}

	private static float Lisse(float t)
	{
		return t * t * (3f - 2f * t);
	}

	private static float Hasard(int graine, int i)
	{
		uint num = (uint)(graine * 374761393 + i * 668265263 + 625341585);
		num = (num ^ (num >> 13)) * 1274126177;
		num ^= num >> 16;
		return (float)(num & 0xFFFFFF) / 16777216f;
	}

	private static Color Teinte(int r, int g, int b, float alpha)
	{
		return new Color((int)(Math.Clamp(alpha, 0f, 1f) * 255f), r, g, b);
	}

	private static IGameTexture? TextureDeLueur()
	{
		if (_lueur != null || Graphics.Renderer == null)
		{
			return _lueur;
		}
		byte[] array = new byte[65536];
		for (int i = 0; i < 128; i++)
		{
			for (int j = 0; j < 128; j++)
			{
				float num = MathF.Sqrt(MathF.Pow((float)j + 0.5f - 64f, 2f) + MathF.Pow((float)i + 0.5f - 64f, 2f)) / 64f;
				float num2 = ((num >= 1f) ? 0f : ((1f - num) * (1f - num)));
				int num3 = (i * 128 + j) * 4;
				int num4 = num3 + 1;
				byte b;
				array[num3 + 2] = (b = byte.MaxValue);
				array[num3] = (array[num4] = b);
				array[num3 + 3] = (byte)(num2 * 255f);
			}
		}
		_lueur = Graphics.Renderer.CreateTextureFromPixels("colosse:lueur", 128, 128, array);
		return _lueur;
	}

	private static IGameTexture? TextureDeVignette()
	{
		if (_vignette != null || Graphics.Renderer == null)
		{
			return _vignette;
		}
		byte[] array = new byte[65536];
		for (int i = 0; i < 128; i++)
		{
			for (int j = 0; j < 128; j++)
			{
				float num = MathF.Sqrt(MathF.Pow((float)j + 0.5f - 64f, 2f) + MathF.Pow((float)i + 0.5f - 64f, 2f)) / 64f;
				float t = Math.Clamp((num - 0.45f) / 0.95f, 0f, 1f);
				int num2 = (i * 128 + j) * 4;
				int num3 = num2 + 1;
				byte b;
				array[num2 + 2] = (b = byte.MaxValue);
				array[num2] = (array[num3] = b);
				array[num2 + 3] = (byte)(Lisse(t) * 255f);
			}
		}
		_vignette = Graphics.Renderer.CreateTextureFromPixels("colosse:vignette", 128, 128, array);
		return _vignette;
	}

	private static void Lueur(float x, float y, float rayon, Color couleur, GameBlendModes mode = GameBlendModes.Add)
	{
		if (!(rayon <= 0f) && couleur.A != 0)
		{
			IGameTexture gameTexture = TextureDeLueur();
			if (gameTexture != null)
			{
				Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, gameTexture.Width, gameTexture.Height), new FloatRect(x - rayon, y - rayon, rayon * 2f, rayon * 2f), couleur, null, mode);
			}
		}
	}

	private static void Segment(float x0, float y0, float x1, float y1, float epaisseur, Color couleur, GameBlendModes mode = GameBlendModes.None)
	{
		float num = x1 - x0;
		float num2 = y1 - y0;
		float num3 = MathF.Sqrt(num * num + num2 * num2);
		if (!(num3 <= 0.01f))
		{
			Bande((x0 + x1) / 2f, (y0 + y1) / 2f, num3, epaisseur, MathF.Atan2(num2, num) * 180f / (float)Math.PI, couleur, mode);
		}
	}

	private static void Pointilles(float x0, float y0, float x1, float y1, float tiret, float vide, float decalage, float epaisseur, Color couleur, GameBlendModes mode = GameBlendModes.Add)
	{
		float num = x1 - x0;
		float num2 = y1 - y0;
		float num3 = MathF.Sqrt(num * num + num2 * num2);
		if (num3 <= 0.01f)
		{
			return;
		}
		float num4 = num / num3;
		float num5 = num2 / num3;
		float num6 = num4;
		float num7 = tiret + vide;
		float num8 = 0f - (decalage % num7 + num7) % num7;
		for (float num9 = num8; num9 < num3; num9 += num7)
		{
			float num10 = MathF.Max(0f, num9);
			float num11 = MathF.Min(num3, num9 + tiret);
			if (num11 > num10)
			{
				Segment(x0 + num6 * num10, y0 + num5 * num10, x0 + num6 * num11, y0 + num5 * num11, epaisseur, couleur, mode);
			}
		}
	}

	private static void Pave(float x, float y, float largeur, float hauteur, Color couleur, GameBlendModes mode = GameBlendModes.None)
	{
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture != null && couleur.A != 0 && !(largeur <= 0f) && !(hauteur <= 0f))
		{
			Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(x, y, largeur, hauteur), couleur, null, mode);
		}
	}

	private static void Ellipse(float cx, float cy, float rx, float ry, float epaisseur, Color couleur)
	{
		for (int i = 0; i < 28; i++)
		{
			float x = (float)i * ((float)Math.PI * 2f) / 28f;
			float x2 = (float)(i + 1) * ((float)Math.PI * 2f) / 28f;
			Segment(cx + MathF.Cos(x) * rx, cy + MathF.Sin(x) * ry, cx + MathF.Cos(x2) * rx, cy + MathF.Sin(x2) * ry, epaisseur, couleur);
		}
	}

	private static void PaveTourne(float cx, float cy, float ox, float oy, float largeur, float hauteur, float angle, Color couleur)
	{
		float num = MathF.Cos(angle);
		float num2 = MathF.Sin(angle);
		float num3 = num;
		Bande(cx + ox * num3 - oy * num2, cy + ox * num2 + oy * num3, largeur, hauteur, angle * 180f / (float)Math.PI, couleur);
	}

	private static void BlocDOr(float x, float y, float taille, float angle)
	{
		float num = MathF.Round(taille * 0.72f);
		PaveTourne(x, y, 0f, 0f, taille + 2f, num + 2f, angle, OrSombre);
		PaveTourne(x, y, 0f, 0f, taille, num, angle, OrFonce);
		PaveTourne(x, y, -1.5f, -1.5f, taille - 3f, num - 3f, angle, Or);
		PaveTourne(x, y, -1.5f, (0f - num) / 2f + 1.5f, taille - 3f, 3f, angle, OrClair);
		PaveTourne(x, y, (0f - taille) / 2f + 3.5f, (0f - num) / 2f + 1.5f, 3f, 1f, angle, OrBlanc);
	}

	private static Vector2 Monde(MapInstance carte, float x, float y)
	{
		return new Vector2((float)carte.X + x * (float)TileWidth, (float)carte.Y + y * (float)TileHeight);
	}

	private static Vector2 BoutDe(MapInstance carte, DungeonTelegraphShape bande)
	{
		return Monde(carte, bande.X + bande.DirX * bande.Length, bande.Y + bande.DirY * bande.Length);
	}

	private static bool EstAuColosse(BossAbilityKind genre)
	{
		if (genre - 3 <= BossAbilityKind.Shockwave)
		{
			return true;
		}
		return false;
	}

	private static float DistanceAMoi(float x, float y)
	{
		Player me = Globals.Me;
		if (me == null)
		{
			return float.MaxValue;
		}
		float num = me.Origin.X - x;
		float num2 = me.Origin.Y - y;
		return MathF.Sqrt(num * num + num2 * num2) / (float)TileWidth;
	}

	private static void Secouer(float force, long dureeMs, float x, float y)
	{
		float num = Math.Clamp(1f - (DistanceAMoi(x, y) - 5f) / 9f, 0f, 1f);
		if (force * num >= 0.3f)
		{
			CombatFeedback.StartShake(force * num, dureeMs);
		}
	}

	private static Attaque EtatDe(Dungeon.Telegraph telegraph, long now)
	{
		(Guid, long) key = (telegraph.Packet.BossId, telegraph.ReceivedAt);
		if (!Attaques.TryGetValue(key, out Attaque value))
		{
			value = (Attaques[key] = new Attaque());
		}
		value.VueA = now;
		return value;
	}

	private static Entity? EntiteDe(Guid id)
	{
		Player me = Globals.Me;
		if (me == null || !(me.Id == id))
		{
			if (!Globals.Entities.TryGetValue(id, out Entity value))
			{
				return null;
			}
			return value;
		}
		return me;
	}

	private static Vector2? Regard(Guid bossId, long now)
	{
		if (!Colosses.TryGetValue(bossId, out Colosse value) || now - value.DessineA > 400)
		{
			Entity entity = EntiteDe(bossId);
			if (entity == null)
			{
				return null;
			}
			return new Vector2(entity.WorldPos.X + entity.WorldPos.Width / 2f, entity.WorldPos.Y + entity.WorldPos.Height * 0.5f);
		}
		if (value.Yeux.Count == 0)
		{
			return new Vector2(value.Corps.X + value.Corps.Width / 2f, value.Corps.Y + value.Corps.Height * 0.5f);
		}
		Vector2 zero = Vector2.Zero;
		foreach (Vector2 item in value.Yeux)
		{
			zero += item;
		}
		return zero / value.Yeux.Count;
	}

	private static void Son(string fichier, float x, float y, float hauteur = 0f, int portee = 14)
	{
		if (HorlogeDuBanc != null)
		{
			return;
		}
		Player me = Globals.Me;
		if (me != null && MapInstance.TryGet(me.MapId, out MapInstance instance))
		{
			MapSound mapSound = Audio.AddMapSound(fichier, Math.Clamp((int)MathF.Floor((x - (float)instance.X) / (float)TileWidth), 0, Options.Instance.Map.MapWidth - 1), Math.Clamp((int)MathF.Floor((y - (float)instance.Y) / (float)TileHeight), 0, Options.Instance.Map.MapHeight - 1), instance.Id, loop: false, 0, portee);
			if (mapSound != null && hauteur != 0f)
			{
				mapSound.Pitch = hauteur;
			}
		}
	}

	private static void VentDeLaTempete()
	{
		if (HorlogeDuBanc != null)
		{
			return;
		}
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		if (_tempete > 0.02f)
		{
			MapSound vent = _vent;
			if (vent == null || !vent.Loaded)
			{
				_vent = Audio.AddMapSound("meteo_sable.wav", -1, -1, me.MapId, loop: true, 0, 0, me);
			}
			if (_vent != null)
			{
				_vent.Fade = 0.5f * _tempete;
			}
		}
		else if (_vent != null)
		{
			_vent.Stop();
			_vent = null;
		}
	}

	private static Planche? PlancheDe(IGameTexture texture)
	{
		if (string.IsNullOrEmpty(texture.Name))
		{
			return null;
		}
		if (!Planches.TryGetValue(texture.Name, out Planche planche))
		{
			Planches[texture.Name] = (planche = new Planche());
			int largeur = texture.Width;
			int hauteur = texture.Height;
			Task.Run(delegate
			{
				try
				{
					using Stream stream = texture.OuvrirSource();
					if (stream != null)
					{
						(byte[], int, int)? tuple = PngBrut.Lire(stream);
						if (tuple.HasValue)
						{
							(byte[], int, int) valueOrDefault = tuple.GetValueOrDefault();
							if (valueOrDefault.Item2 == largeur && valueOrDefault.Item3 == hauteur)
							{
								Analyser(planche, valueOrDefault.Item1, valueOrDefault.Item2, valueOrDefault.Item3);
								return;
							}
						}
					}
				}
				catch (Exception exception)
				{
					ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Colosse d'or : planche {Planche} illisible dans son fichier", texture.Name);
				}
				planche.ParLaCarte = true;
			});
		}
		if (planche.ParLaCarte)
		{
			planche.ParLaCarte = false;
			GameRenderer renderer = Graphics.Renderer;
			if (renderer != null && renderer.TryReadPixels(texture, out byte[] rgba, out int width, out int height))
			{
				Analyser(planche, rgba, width, height);
			}
			else if (++planche.Essais < 10)
			{
				planche.ParLaCarte = true;
			}
		}
		if (!planche.Lue)
		{
			return null;
		}
		if (!planche.Faite)
		{
			GameRenderer renderer2 = Graphics.Renderer;
			if (renderer2 != null && planche.Silhouette != null && planche.Halo != null)
			{
				planche.Masque = renderer2.CreateTextureFromPixels("colosse:silhouette/" + texture.Name, planche.L, planche.H, planche.Silhouette);
				planche.Contour = renderer2.CreateTextureFromPixels("colosse:contour/" + texture.Name, planche.L, planche.H, planche.Halo);
				planche.Silhouette = null;
				planche.Halo = null;
				planche.Faite = true;
			}
		}
		return planche;
	}

	private static void Analyser(Planche planche, byte[] rgba, int largeur, int hauteur)
	{
		bool[] array = new bool[largeur * hauteur];
		List<(int, int)> list = new List<(int, int)>();
		for (int i = 0; i < array.Length; i++)
		{
			int num = i * 4;
			array[i] = rgba[num + 3] >= 128;
			if (rgba[num + 3] > 200 && EstUnePupille(rgba[num], rgba[num + 1], rgba[num + 2]))
			{
				list.Add((i % largeur, i / largeur));
			}
		}
		byte[] array2 = new byte[largeur * hauteur * 4];
		byte[] array3 = new byte[largeur * hauteur * 4];
		for (int j = 0; j < hauteur; j++)
		{
			for (int k = 0; k < largeur; k++)
			{
				int num2 = (j * largeur + k) * 4;
				int num3 = num2 + 1;
				byte b;
				array2[num2 + 2] = (b = byte.MaxValue);
				array2[num2] = (array2[num3] = b);
				int num4 = num2 + 1;
				array3[num2 + 2] = (b = byte.MaxValue);
				array3[num2] = (array3[num4] = b);
				if (array[j * largeur + k])
				{
					array2[num2 + 3] = byte.MaxValue;
					array3[num2 + 3] = byte.MaxValue;
					continue;
				}
				bool flag = false;
				for (int l = -2; l <= 2; l++)
				{
					if (flag)
					{
						break;
					}
					for (int m = -2; m <= 2; m++)
					{
						if (flag)
						{
							break;
						}
						int num5 = k + m;
						int num6 = j + l;
						int num7 = num5;
						flag = num7 >= 0 && num6 >= 0 && num7 < largeur && num6 < hauteur && m * m + l * l <= 5 && array[num6 * largeur + num7];
					}
				}
				array3[num2 + 3] = (byte)(flag ? byte.MaxValue : 0);
			}
		}
		planche.L = largeur;
		planche.H = hauteur;
		planche.Silhouette = array2;
		planche.Halo = array3;
		planche.Pupilles = list;
		planche.Lue = true;
	}

	private static ((int X, int Y)[] Pixels, Vector2[] Centres) YeuxDe(Planche planche, FloatRect source)
	{
		(int, int, int, int) cle = ((int)source.X, (int)source.Y, (int)source.Width, (int)source.Height);
		if (planche.Images.TryGetValue(cle, out ((int, int)[], Vector2[]) value))
		{
			return value;
		}
		(int, int)[] array = (from p in planche.Pupilles
			where p.X >= cle.Item1 && p.Y >= cle.Item2 && p.X < cle.Item1 + cle.Item3 && p.Y < cle.Item2 + cle.Item4
			orderby p.X
			select p).ToArray();
		List<Vector2> list = new List<Vector2>();
		List<(int, int)> list2 = new List<(int, int)>();
		(int, int)[] array2 = array;
		for (int num = 0; num < array2.Length; num++)
		{
			(int, int) item = array2[num];
			if (list2.Count > 0)
			{
				if (item.Item1 - list2[list2.Count - 1].Item1 > 1)
				{
					list.Add(Centre(list2));
					list2.Clear();
				}
			}
			list2.Add(item);
		}
		if (list2.Count > 0)
		{
			list.Add(Centre(list2));
		}
		((int, int)[], Vector2[]) tuple = (array, list.ToArray());
		planche.Images[cle] = tuple;
		return tuple;
		static Vector2 Centre(List<(int X, int Y)> groupe)
		{
			return new Vector2((float)groupe.Average(((int X, int Y) p) => p.X) + 0.5f, (float)groupe.Average(((int X, int Y) p) => p.Y) + 0.5f);
		}
	}

	private static void SuivreLesColosses(long now)
	{
		Player moi = Globals.Me;
		if (moi == null)
		{
			return;
		}
		foreach (Entity value2 in Globals.Entities.Values)
		{
			if (value2 == null || value2.BossVisual != BossVisual.GoldenColossus)
			{
				continue;
			}
			if (!Colosses.TryGetValue(value2.Id, out Colosse value))
			{
				value = (Colosses[value2.Id] = new Colosse());
			}
			long num = value2.Vital[0];
			long num2 = Math.Max(1L, value2.MaxVital[0]);
			bool flag = num > 0 && num * 100 <= num2 * value2.BossEnragePercent;
			bool flag2 = value2.MapId == moi.MapId;
			if (flag && !value.Enrage)
			{
				value.EnrageDepuis = (value.Vu ? now : (now - 5000));
				if (value.Vu && flag2)
				{
					EclairsEcran.Add(new EclairEcran(0.38f, now, 500));
					CombatFeedback.StartShake(5f, 900L);
					Flashs.Add(new Flash(value2.Origin.X, value2.Origin.Y - 34f, 160, byte.MaxValue, now, 700, new Color(255, 255, 190, 80)));
					Son("meteo_tonnerre_proche_1.wav", value2.Origin.X, value2.Origin.Y);
					Son("Aura_intro.wav", value2.Origin.X, value2.Origin.Y, -0.15f);
				}
			}
			value.Vu = true;
			value.Enrage = flag;
			value.MapId = value2.MapId;
			value.Pieds = value2.Origin;
			value.MortDepuis = ((num > 0) ? 0 : ((value.MortDepuis == 0L) ? now : value.MortDepuis));
		}
		float num3 = (Colosses.Any<KeyValuePair<Guid, Colosse>>((KeyValuePair<Guid, Colosse> paire) => paire.Value.Enrage && paire.Value.MortDepuis == 0L && paire.Value.MapId == moi.MapId && Globals.Entities.ContainsKey(paire.Key)) ? Colosses.Where<KeyValuePair<Guid, Colosse>>((KeyValuePair<Guid, Colosse> paire) => paire.Value.Enrage && paire.Value.MapId == moi.MapId).Max((KeyValuePair<Guid, Colosse> paire) => Ph(now, paire.Value.EnrageDepuis + 400, paire.Value.EnrageDepuis + 1600)) : 0f);
		float num4 = Math.Clamp((float)(now - _derniereImage) / 1500f, 0f, 1f);
		_tempete = ((num3 > _tempete) ? num3 : MathF.Max(num3, _tempete - num4));
		_derniereImage = now;
		VentDeLaTempete();
		Guid[] array = Colosses.Keys.Where((Guid id) => !Globals.Entities.ContainsKey(id)).ToArray();
		foreach (Guid key in array)
		{
			Colosses.Remove(key);
		}
	}

	private static float Aura(Colosse colosse, long now)
	{
		if (!colosse.Enrage || colosse.MortDepuis != 0L)
		{
			return 0f;
		}
		return Ph(now, colosse.EnrageDepuis + 300, colosse.EnrageDepuis + 900) * (0.7f + 0.3f * MathF.Sin((float)now / 160f));
	}

	private static float ForceDesYeux(Guid bossId, Colosse colosse, long now)
	{
		float num = 0.75f + 0.25f * MathF.Sin((float)now / 300f);
		if (colosse.Enrage)
		{
			num = 1f;
		}
		foreach (Dungeon.Telegraph activeTelegraph in Dungeon.ActiveTelegraphs)
		{
			if (activeTelegraph.Packet.BossId == bossId && activeTelegraph.Kind == BossAbilityKind.EyeBeam && now < activeTelegraph.LastFiresAt + 650)
			{
				num = MathF.Max(num, 0.75f + 0.25f * Ph(now, activeTelegraph.ReceivedAt, activeTelegraph.ReceivedAt + 500));
			}
		}
		return num;
	}

	public static void DerriereLeBoss(Entity boss, FloatRect destination, IGameTexture texture, FloatRect source)
	{
		if (boss.BossVisual == BossVisual.Saiyan)
		{
			AuraSaiyan(boss, destination);
		}
		else
		{
			if (boss.BossVisual != BossVisual.GoldenColossus || !Colosses.TryGetValue(boss.Id, out Colosse value))
			{
				return;
			}
			long maintenant = Maintenant;
			value.Corps = destination;
			float num = Aura(value, maintenant);
			if (num <= 0.01f || source.Width <= 0f)
			{
				return;
			}
			Planche planche = PlancheDe(texture);
			if (planche == null)
			{
				return;
			}
			IGameTexture contour = planche.Contour;
			if (contour != null)
			{
				float num2 = destination.X + destination.Width / 2f;
				float num3 = destination.Y + destination.Height / 2f;
				Lueur(num2, num3 + 8f, 66f, Teinte(255, 170, 40, 0.42f * num));
				(float, float)[] array = new(float, float)[3]
				{
					(1.16f, 0.22f),
					(1.08f, 0.32f),
					(1f, 0.55f)
				};
				for (int i = 0; i < array.Length; i++)
				{
					(float, float) tuple = array[i];
					float item = tuple.Item1;
					float item2 = tuple.Item2;
					float num4 = destination.Width * item;
					float num5 = destination.Height * item;
					Graphics.DrawGameTexture(contour, source, new FloatRect(num2 - num4 / 2f, destination.Bottom - num5 + (num5 - destination.Height) * 0.35f, num4, num5), Teinte(255, 210, 74, item2 * num), null, GameBlendModes.Add);
				}
			}
		}
	}

	public static void DevantLeBoss(Entity boss, IGameTexture texture, FloatRect source, FloatRect corps)
	{
		if (boss.BossVisual != BossVisual.GoldenColossus || !Colosses.TryGetValue(boss.Id, out Colosse value))
		{
			return;
		}
		long num = (value.DessineA = Maintenant);
		value.Corps = corps;
		value.Yeux.Clear();
		if (source.Width <= 0f)
		{
			return;
		}
		Planche planche = PlancheDe(texture);
		if (planche != null)
		{
			((int X, int Y)[] Pixels, Vector2[] Centres) tuple = YeuxDe(planche, source);
			(int, int)[] item = tuple.Pixels;
			Vector2[] item2 = tuple.Centres;
			float num2 = corps.Width / source.Width;
			float num3 = corps.Height / source.Height;
			float num4 = ForceDesYeux(boss.Id, value, num);
			float num5 = 0.8f + 0.2f * MathF.Sin((float)num / 90f);
			(int, int)[] array = item;
			for (int i = 0; i < array.Length; i++)
			{
				var (num6, num7) = array[i];
				Pave(corps.X + ((float)num6 - source.X) * num2, corps.Y + ((float)num7 - source.Y) * num3, num2, num3, Teinte(255, 236, 190, 0.85f * num4), GameBlendModes.Add);
			}
			Vector2[] array2 = item2;
			for (int j = 0; j < array2.Length; j++)
			{
				Vector2 vector = array2[j];
				Vector2 item3 = new Vector2(corps.X + (vector.X - source.X) * num2, corps.Y + (vector.Y - source.Y) * num3);
				value.Yeux.Add(item3);
				Lueur(item3.X, item3.Y, 9f * num4 * num5, Teinte(255, 90, 40, 0.85f * num4));
				Lueur(item3.X, item3.Y, 3f, Teinte(255, 240, 200, num4));
			}
		}
	}

	public static void PasDuBoss(Entity boss, int image)
	{
		if (boss.BossVisual == BossVisual.GoldenColossus && image % 2 != 0)
		{
			Player me = Globals.Me;
			if (me != null && !(boss.MapId != me.MapId) && boss.Vital[0] > 0)
			{
				long maintenant = Maintenant;
				Vector2 origin = boss.Origin;
				Secouer(2.2f, 160L, origin.X, origin.Y);
				Son("heritage_rocher_2.wav", origin.X, origin.Y, -0.35f, 10);
				Direction directionFacing = boss.DirectionFacing;
				bool flag = (uint)directionFacing <= 1u;
				bool flag2 = flag;
				int num = ((image % 4 != 1) ? 1 : (-1));
				float x = origin.X + (float)(flag2 ? (num * 12) : (num * 4));
				float y = origin.Y + (float)(flag2 ? 2 : (num * 3));
				Poussiere(x, y, maintenant, (int)(maintenant % 9973) + image, 5, 16f);
			}
		}
	}

	public static void BossFrappe(Entity boss, Entity? attaquant)
	{
		if (boss.BossVisual != BossVisual.GoldenColossus)
		{
			return;
		}
		Player me = Globals.Me;
		if (me != null && !(boss.MapId != me.MapId))
		{
			long maintenant = Maintenant;
			FloatRect worldPos = boss.WorldPos;
			float num = worldPos.X + worldPos.Width / 2f;
			float num2 = worldPos.Y + worldPos.Height * 0.62f;
			if (attaquant != null)
			{
				num += Math.Clamp(attaquant.Origin.X - num, -14f, 14f);
			}
			int num3 = (int)(maintenant % 100003);
			Etincelles(num, num2, maintenant, num3, 16, 40f);
			Eclats(num, num2 + 4f, maintenant, num3 + 7, 9);
			Flashs.Add(new Flash(num, num2, 46, 170, maintenant, 220, new Color(255, 255, 200, 90)));
			Son("recolte_butin_cuivre.wav", num, num2, (Hasard(num3, 99) - 0.5f) * 0.2f, 10);
		}
	}

	private static void Ajouter(Particule particule)
	{
		if (Particules.Count < 900)
		{
			Particules.Add(particule);
		}
	}

	private static void Poussiere(float x, float y, long now, int graine, int nombre, float portee)
	{
		for (int i = 0; i < nombre; i++)
		{
			Ajouter(new Particule
			{
				Genre = Genre.Poussiere,
				X = x,
				Y = y,
				Angle = (float)Math.PI + Hasard(graine, i * 3) * (float)Math.PI,
				Vitesse = (0.4f + Hasard(graine, i * 3 + 1) * 0.6f) * portee,
				Taille = Hasard(graine, i * 3 + 2),
				Ne = now,
				VieMs = 700
			});
		}
	}

	private static void Etincelles(float x, float y, long now, int graine, int nombre, float portee)
	{
		for (int i = 0; i < nombre; i++)
		{
			Ajouter(new Particule
			{
				Genre = Genre.Etincelle,
				X = x,
				Y = y,
				Angle = Hasard(graine, i * 3) * ((float)Math.PI * 2f),
				Vitesse = (0.5f + 0.5f * Hasard(graine, i * 3 + 1)) * portee,
				Taille = Hasard(graine, i * 3 + 2),
				Ne = now,
				VieMs = 450
			});
		}
	}

	private static void Eclats(float x, float y, long now, int graine, int nombre)
	{
		for (int i = 0; i < nombre; i++)
		{
			Ajouter(new Particule
			{
				Genre = Genre.Eclat,
				X = x,
				Y = y,
				Angle = (float)Math.PI + Hasard(graine, i * 3) * (float)Math.PI,
				Vitesse = 30f + Hasard(graine, i * 3 + 1) * 50f,
				Clair = (Hasard(graine, i * 3 + 2) > 0.5f),
				Ne = now,
				VieMs = 900
			});
		}
	}

	private static void DessinerLesParticules(long now, bool auSol)
	{
		for (int num = Particules.Count - 1; num >= 0; num--)
		{
			Particule particule = Particules[num];
			long num2 = now - particule.Ne;
			if (num2 >= particule.VieMs)
			{
				if (auSol)
				{
					Particules.RemoveAt(num);
				}
			}
			else
			{
				float num3 = 1f - (float)num2 / (float)particule.VieMs;
				switch (particule.Genre)
				{
				case Genre.Poussiere:
					if (auSol)
					{
						float num13 = Lisse(Math.Clamp((float)num2 / 600f, 0f, 1f)) * particule.Vitesse;
						float centreX = particule.X + MathF.Cos(particule.Angle) * num13;
						float centreY = particule.Y + MathF.Sin(particule.Angle) * num13 * 0.45f - (float)num2 / 60f * particule.Taille;
						Disque(centreX, centreY, 3f + 5f * (1f - num3), Teinte(236, 212, 170, 0.6f * num3));
					}
					break;
				case Genre.Anneau:
					if (auSol)
					{
						float num14 = 6f + (float)num2 / 25f;
						Ellipse(particule.X, particule.Y, num14, num14 * 0.45f, 2f, Teinte(236, 212, 170, 0.65f * num3));
					}
					break;
				case Genre.Etincelle:
					if (!auSol)
					{
						float num9 = (float)num2 / (float)particule.VieMs;
						float num10 = (1f - (1f - num9) * (1f - num9)) * particule.Vitesse;
						float num11 = particule.X + MathF.Cos(particule.Angle) * num10;
						float num12 = particule.Y + MathF.Sin(particule.Angle) * num10 - (float)num2 / 50f;
						Segment(num11, num12, num11 - MathF.Cos(particule.Angle) * 5f, num12 - MathF.Sin(particule.Angle) * 5f, 1.5f, Teinte(255, 200 + (int)(55f * particule.Taille), 90, num3), GameBlendModes.Add);
					}
					break;
				case Genre.EtincelleKi:
					if (!auSol)
					{
						float num5 = (float)num2 / (float)particule.VieMs;
						float num6 = (1f - (1f - num5) * (1f - num5)) * particule.Vitesse;
						float num7 = particule.X + MathF.Cos(particule.Angle) * num6;
						float num8 = particule.Y + MathF.Sin(particule.Angle) * num6 - (float)num2 / 50f;
						Segment(num7, num8, num7 - MathF.Cos(particule.Angle) * 5f, num8 - MathF.Sin(particule.Angle) * 5f, 1.5f, Teinte(200 + (int)(55f * particule.Taille), 120, 255, num3), GameBlendModes.Add);
					}
					break;
				case Genre.Eclat:
					if (!auSol)
					{
						float num4 = (float)num2 / 1000f;
						float x = particule.X + MathF.Cos(particule.Angle) * particule.Vitesse * num4;
						float x2 = particule.Y + MathF.Sin(particule.Angle) * particule.Vitesse * num4 + 120f * num4 * num4 - 4f;
						Pave(MathF.Round(x), MathF.Round(x2), 3f, 3f, particule.Clair ? Teinte(255, 226, 122, num3) : Teinte(242, 194, 58, num3));
					}
					break;
				}
			}
		}
	}

	private static void DessinerLeColosseAuSol(long now)
	{
		SuivreLesColosses(now);
		if (Attaques.Count > 0)
		{
			(Guid, long)[] array = (from paire in Attaques
				where now - paire.Value.VueA > 1000
				select paire.Key).ToArray();
			foreach ((Guid, long) key in array)
			{
				Attaques.Remove(key);
			}
		}
		DessinerLesDalles(now);
		DessinerLesParticules(now, auSol: true);
	}

	private static void DessinerLesDalles(long now)
	{
		foreach (var (x, num, num2) in DallesAllumees(now))
		{
			Lueur(x, num, 26f * num2 + 6f, Teinte(255, 70, 30, 0.85f * num2));
			Lueur(x, num - 6f, 10f * num2, Teinte(255, 190, 80, 0.8f * num2));
		}
	}

	private static Vector2[] DallesDe(MapInstance carte)
	{
		if (DallesParCarte.TryGetValue(carte.Id, out Vector2[] value))
		{
			return value;
		}
		HashSet<(int, int)> hashSet = new HashSet<(int, int)>();
		TilesetDescriptor tilesetDescriptor = DatabaseObject<TilesetDescriptor>.Lookup.Values.OfType<TilesetDescriptor>().FirstOrDefault((TilesetDescriptor planche) => string.Equals(planche.Name, "tombeau.png", StringComparison.OrdinalIgnoreCase));
		if (tilesetDescriptor != null && carte.Layers != null)
		{
			foreach (Tile[,] value2 in carte.Layers.Values)
			{
				if (value2 == null)
				{
					continue;
				}
				for (int num = 0; num < value2.GetLength(0); num++)
				{
					for (int num2 = 0; num2 < value2.GetLength(1); num2++)
					{
						if (value2[num, num2].TilesetId == tilesetDescriptor.Id)
						{
							hashSet.Add((num, num2));
						}
					}
				}
			}
		}
		Vector2[] array = hashSet.Select<(int, int), Vector2>(((int X, int Y) d) => new Vector2((float)carte.X + ((float)d.X + 0.5f) * (float)TileWidth, (float)carte.Y + ((float)d.Y + 0.5f) * (float)TileHeight)).ToArray();
		DallesParCarte[carte.Id] = array;
		return array;
	}

	private static IEnumerable<(float X, float Y, float A)> DallesAllumees(long now)
	{
		foreach (Colosse colosse in Colosses.Values)
		{
			if (!colosse.Enrage || colosse.MortDepuis != 0L)
			{
				continue;
			}
			Player me = Globals.Me;
			if (me == null || colosse.MapId != me.MapId || !MapInstance.TryGet(colosse.MapId, out MapInstance instance))
			{
				continue;
			}
			long num = now - colosse.EnrageDepuis - 1000;
			if (num < 0)
			{
				continue;
			}
			long vague = num % 2600;
			Vector2[] array = DallesDe(instance);
			for (int i = 0; i < array.Length; i++)
			{
				Vector2 value = array[i];
				float num2 = Vector2.Distance(value, colosse.Pieds);
				float num3 = ((float)vague - num2 * 3.2f) / 220f;
				float num4 = MathF.Exp((0f - num3) * num3);
				if (num4 >= 0.02f)
				{
					yield return (X: value.X, Y: value.Y, A: num4);
				}
			}
		}
	}

	private static void AttaqueAuSol(Dungeon.Telegraph telegraph, MapInstance carte, long now)
	{
		Attaque attaque = EtatDe(telegraph, now);
		DungeonTelegraphShape[] array = telegraph.Packet.Shapes ?? Array.Empty<DungeonTelegraphShape>();
		switch (telegraph.Kind)
		{
		case BossAbilityKind.EyeBeam:
		{
			if (telegraph.Packet.Locked && now < telegraph.FiresAt && array.Length == 1)
			{
				float num4 = 0.5f + 0.5f * MathF.Sin((float)now / 90f);
				BandeAuSol(carte, array[0], 0f, array[0].Length, Teinte(255, 120, 40, (0.1f + 0.08f * num4) * Ph(now, telegraph.ReceivedAt, telegraph.ReceivedAt + 150)));
			}
			if (array.Length > 1 && now < telegraph.FiresAt)
			{
				SecteurAuSol(carte, array, now, telegraph);
			}
			for (int j = 0; j < array.Length; j++)
			{
				long num5 = now - (telegraph.FiresAt + array[j].DelayMs);
				if (num5 >= 0)
				{
					if (array.Length == 1)
					{
						Trainee(carte, array[j], num5, 26, (int)(telegraph.ReceivedAt % 7919));
					}
					else
					{
						Sillon(carte, array[j], num5);
					}
				}
			}
			break;
		}
		case BossAbilityKind.Fissures:
		{
			int num6 = Math.Max(1, telegraph.Packet.DurationMs);
			long num7 = telegraph.ReceivedAt + num6 / 4;
			if (!attaque.Choc && now >= num7)
			{
				Entity entity = EntiteDe(telegraph.Packet.BossId);
				if (entity != null)
				{
					attaque.Choc = true;
					Secouer(4f, 400L, entity.Origin.X, entity.Origin.Y);
					Poussiere(entity.Origin.X, entity.Origin.Y, now, (int)(now % 7919), 10, 26f);
					Son("heritage_falaise_1.wav", entity.Origin.X, entity.Origin.Y, -0.2f);
				}
			}
			Attaque attaque2 = attaque;
			if (attaque2.Fissures == null)
			{
				attaque2.Fissures = array.Select((DungeonTelegraphShape bande, int num12) => TracerFissure(carte, bande, (int)(telegraph.ReceivedAt % 104729) + num12 * 31)).ToArray();
			}
			float num8 = Lisse(Ph(now, num7, telegraph.ReceivedAt + num6 * 85 / 100));
			float num9 = Ph(now, telegraph.ReceivedAt + num6 * 85 / 100, telegraph.FiresAt) * (1f - Ph(now, telegraph.FiresAt + 700, telegraph.FiresAt + 1300));
			if (now >= telegraph.FiresAt)
			{
				num9 = MathF.Max(num9, 1f - Ph(now, telegraph.FiresAt + 700, telegraph.FiresAt + 1300));
			}
			float froid = Ph(now, telegraph.FiresAt + 1300, telegraph.FiresAt + 3500);
			float num10 = Ph(now, telegraph.ReceivedAt + num6 * 4 / 10, telegraph.FiresAt) * ((now < telegraph.FiresAt) ? 1f : 0f);
			for (int num11 = 0; num11 < array.Length; num11++)
			{
				if (num10 > 0f)
				{
					BandeAuSol(carte, array[num11], 0f, array[num11].Length * MathF.Max(num8, 0.05f), Teinte(255, 120, 40, 0.15f * num10));
				}
				Fissure(attaque.Fissures[num11], num8, num9, froid, 1f - Ph(now, telegraph.FiresAt + 3500 - 800, telegraph.FiresAt + 3500));
			}
			break;
		}
		case BossAbilityKind.GoldRain:
		{
			for (int i = 0; i < array.Length; i++)
			{
				long num = telegraph.FiresAt + array[i].DelayMs;
				if (now < num)
				{
					float num2 = Ph(now, telegraph.ReceivedAt, num);
					Vector2 vector = Monde(carte, array[i].X, array[i].Y);
					float num3 = array[i].Radius * (float)TileWidth;
					Disque(vector.X, vector.Y, num3 * (0.3f + 0.7f * num2), Teinte(20, 10, 4, 0.15f + 0.45f * num2));
					Cercle(vector.X, vector.Y, num3 - 1f, 1.5f, Teinte(30, 14, 4, 0.5f * Ph(now, num - 900, num - 300)));
				}
			}
			break;
		}
		}
	}

	private static void BandeAuSol(MapInstance carte, DungeonTelegraphShape bande, float de, float a, Color couleur)
	{
		if (!(a <= de))
		{
			Vector2 vector = Monde(carte, bande.X + bande.DirX * de, bande.Y + bande.DirY * de);
			Vector2 vector2 = Monde(carte, bande.X + bande.DirX * a, bande.Y + bande.DirY * a);
			Segment(vector.X, vector.Y, vector2.X, vector2.Y, bande.Width * (float)TileWidth, couleur);
		}
	}

	private static void SecteurAuSol(MapInstance carte, DungeonTelegraphShape[] bandes, long now, Dungeon.Telegraph telegraph)
	{
		float num = Ph(now, telegraph.ReceivedAt, telegraph.ReceivedAt + 300);
		Vector2 vector = Monde(carte, bandes[0].X, bandes[0].Y);
		float num2 = MathF.Atan2(bandes[0].DirY, bandes[0].DirX);
		float num3 = MathF.Atan2(bandes[^1].DirY, bandes[^1].DirX);
		float num4 = MathF.IEEERemainder(num3 - num2, (float)Math.PI * 2f);
		float num5 = MathF.Min(4f, bandes.Min((DungeonTelegraphShape bande) => bande.Length)) * (float)TileWidth;
		for (int num6 = 0; num6 < bandes.Length; num6 += 2)
		{
			BandeAuSol(carte, bandes[num6], 0.6f, bandes[num6].Length, Teinte(255, 90, 50, 0.035f * num));
		}
		Vector2 vector2 = BoutDe(carte, bandes[^1]);
		Pointilles(vector.X, vector.Y, vector2.X, vector2.Y, 6f, 5f, 0f, 1.2f, Teinte(220, 60, 40, 0.5f * num), GameBlendModes.None);
		int num7 = Math.Max(6, (int)(MathF.Abs(num4) * 10f));
		for (int num8 = 0; num8 < num7; num8++)
		{
			if (num8 % 2 != 1)
			{
				float x = num2 + num4 * (float)num8 / (float)num7;
				float x2 = num2 + num4 * (float)(num8 + 1) / (float)num7;
				Segment(vector.X + MathF.Cos(x) * num5, vector.Y + MathF.Sin(x) * num5, vector.X + MathF.Cos(x2) * num5, vector.Y + MathF.Sin(x2) * num5, 1.5f, Teinte(255, 120, 60, 0.45f * num), GameBlendModes.Add);
			}
		}
		int num9 = MathF.Sign(num4);
		for (int num10 = 0; num10 < 3; num10++)
		{
			float num11 = ((float)now / 900f + (float)num10 / 3f) % 1f;
			float num12 = num2 + num4 * (0.15f + 0.7f * num11);
			float num13 = vector.X + MathF.Cos(num12) * num5;
			float num14 = vector.Y + MathF.Sin(num12) * num5;
			float num15 = num13;
			float num16 = num12 + (float)num9 * (float)Math.PI / 2f;
			for (int num17 = -1; num17 <= 1; num17 += 2)
			{
				float x3 = num16 + (float)Math.PI + (float)num17 * 0.6f;
				Segment(num15, num14, num15 + MathF.Cos(x3) * 6f, num14 + MathF.Sin(x3) * 6f, 1.5f, Teinte(255, 140, 70, 0.7f * num * MathF.Sin(num11 * (float)Math.PI)), GameBlendModes.Add);
			}
		}
	}

	private static void Trainee(MapInstance carte, DungeonTelegraphShape bande, long age, int grains, int graine)
	{
		float num = Math.Clamp(1f - (float)age / 3200f, 0f, 1f);
		if (num <= 0f)
		{
			return;
		}
		float num2 = Math.Clamp(1f - (float)age / 1400f, 0f, 1f);
		Vector2 vector = Monde(carte, bande.X, bande.Y);
		Vector2 value = BoutDe(carte, bande);
		float dirX = bande.DirX;
		float dirY = bande.DirY;
		float num3 = dirX;
		Vector2 value2 = new Vector2(vector.X + num3 * 10f, vector.Y + dirY * 10f);
		Segment(value2.X, value2.Y, value.X, value.Y, 10f, Teinte(60, 30, 12, 0.55f * num));
		Segment(value2.X, value2.Y, value.X, value.Y, 4f, Teinte(255, (int)(120f + 80f * num2), 40, 0.75f * num * (0.4f + 0.6f * num2)), GameBlendModes.Add);
		if (!(num2 <= 0f))
		{
			float num4 = Vector2.Distance(value2, value);
			for (int i = 0; i < grains; i++)
			{
				float num5 = 4f + Hasard(graine, i * 2) * (num4 - 10f);
				float num6 = (Hasard(graine, i * 2 + 1) - 0.5f) * 8f;
				float num7 = 0.5f + 0.5f * MathF.Sin((float)age / 120f + (float)i);
				Pave(value2.X + num3 * num5 - dirY * num6, value2.Y + dirY * num5 + num3 * num6, 2f, 2f, Teinte(255, 200, 90, 0.7f * num * num2 * num7), GameBlendModes.Add);
			}
		}
	}

	private static void Sillon(MapInstance carte, DungeonTelegraphShape bande, long age)
	{
		float num = Math.Clamp(1f - (float)age / 1200f, 0f, 1f);
		if (!(num <= 0f))
		{
			Vector2 vector = Monde(carte, bande.X + bande.DirX * 0.4f, bande.Y + bande.DirY * 0.4f);
			Vector2 vector2 = BoutDe(carte, bande);
			Segment(vector.X, vector.Y, vector2.X, vector2.Y, 2f, Teinte(255, (int)(130f + 90f * num), 50, 0.55f * num), GameBlendModes.Add);
		}
	}

	private static Vector2[] TracerFissure(MapInstance carte, DungeonTelegraphShape bande, int graine)
	{
		Vector2 item = Monde(carte, bande.X, bande.Y);
		float num = bande.Length * (float)TileWidth;
		float dirX = bande.DirX;
		float dirY = bande.DirY;
		float num2 = dirX;
		List<Vector2> list = new List<Vector2> { item };
		float num3 = 0f;
		int num4 = 0;
		while (num3 < num && num4 < 200)
		{
			num3 += 6f + Hasard(graine, num4 * 2) * 6f;
			float num5 = (Hasard(graine, num4 * 2 + 1) - 0.5f) * 9f;
			float num6 = MathF.Min(num3, num);
			list.Add(new Vector2(item.X + num2 * num6 - dirY * num5, item.Y + dirY * num6 + num2 * num5));
			num4++;
		}
		return list.ToArray();
	}

	private static void Fissure(Vector2[] points, float progression, float feu, float froid, float fondu = 1f)
	{
		if (progression <= 0f || points.Length < 2)
		{
			return;
		}
		int num = Math.Clamp((int)MathF.Round((float)points.Length * progression), 2, points.Length);
		Color couleur = Teinte(40, 22, 8, 0.85f * (1f - froid * 0.6f) * fondu);
		float num2 = (0.35f + 0.65f * feu) * (1f - froid);
		Color couleur2 = Teinte(255, (int)(170f + 85f * feu), (int)(40f + 180f * feu), num2);
		Color couleur3 = Teinte(255, 190, 60, 0.28f * num2);
		for (int i = 1; i < num; i++)
		{
			Segment(points[i - 1].X, points[i - 1].Y, points[i].X, points[i].Y, 5f, couleur);
		}
		if (!(num2 <= 0.01f))
		{
			for (int j = 1; j < num; j++)
			{
				Segment(points[j - 1].X, points[j - 1].Y, points[j].X, points[j].Y, 7f, couleur3, GameBlendModes.Add);
				Segment(points[j - 1].X, points[j - 1].Y, points[j].X, points[j].Y, 2f, couleur2, GameBlendModes.Add);
			}
		}
	}

	public static void DrawAfterDark()
	{
		if (Globals.Me == null)
		{
			return;
		}
		long maintenant = Maintenant;
		foreach (Dungeon.Telegraph activeTelegraph in Dungeon.ActiveTelegraphs)
		{
			MapInstance instance2;
			if (maintenant <= activeTelegraph.EndsAt && EstSaiyan(activeTelegraph.Packet.BossId) && MapInstance.TryGet(activeTelegraph.Packet.MapId, out MapInstance instance))
			{
				SaiyanApresLaNuit(activeTelegraph, EtatDe(activeTelegraph, maintenant), instance, maintenant);
			}
			else if (EstAuColosse(activeTelegraph.Kind) && maintenant <= activeTelegraph.EndsAt && MapInstance.TryGet(activeTelegraph.Packet.MapId, out instance2))
			{
				Attaque attaque = EtatDe(activeTelegraph, maintenant);
				switch (activeTelegraph.Kind)
				{
				case BossAbilityKind.EyeBeam:
					RayonApresLaNuit(activeTelegraph, attaque, instance2, maintenant);
					break;
				case BossAbilityKind.Fissures:
					ColonnesApresLaNuit(activeTelegraph, attaque, maintenant);
					break;
				case BossAbilityKind.GoldRain:
					BlocsApresLaNuit(activeTelegraph, attaque, instance2, maintenant);
					break;
				}
			}
		}
		DessinerLesEffetsKi(maintenant);
		DessinerLesParticules(maintenant, auSol: false);
		Tempete(maintenant);
		float num = 0f;
		for (int num2 = EclairsEcran.Count - 1; num2 >= 0; num2--)
		{
			EclairEcran eclairEcran = EclairsEcran[num2];
			if (maintenant - eclairEcran.Ne >= eclairEcran.VieMs)
			{
				EclairsEcran.RemoveAt(num2);
			}
			else
			{
				num = MathF.Max(num, eclairEcran.Force * (1f - (float)(maintenant - eclairEcran.Ne) / (float)eclairEcran.VieMs));
			}
		}
		if (num > 0.005f)
		{
			FloatRect worldViewport = Graphics.WorldViewport;
			Pave(worldViewport.X, worldViewport.Y, worldViewport.Width, worldViewport.Height, Teinte(255, 246, 220, num));
		}
	}

	private static void RayonApresLaNuit(Dungeon.Telegraph telegraph, Attaque attaque, MapInstance carte, long now)
	{
		DungeonTelegraphShape[] array = telegraph.Packet.Shapes ?? Array.Empty<DungeonTelegraphShape>();
		Entity entity = EntiteDe(telegraph.Packet.BossId);
		bool flag = entity != null && entity.Vital[0] > 0;
		Vector2? vector = Regard(telegraph.Packet.BossId, now);
		if (!vector.HasValue)
		{
			return;
		}
		Vector2 valueOrDefault = vector.GetValueOrDefault();
		if (!telegraph.Packet.Locked && array.Length == 0)
		{
			if (flag)
			{
				Entity entity2 = EntiteDe(telegraph.Packet.TargetId);
				if (entity2 != null)
				{
					float alpha = Ph(now, telegraph.ReceivedAt + 150, telegraph.ReceivedAt + 450);
					Vector2 origin = entity2.Origin;
					Visee(valueOrDefault, new Vector2(origin.X, origin.Y - 6f), alpha, now);
				}
			}
			return;
		}
		if (array.Length > 1 && now < telegraph.FiresAt)
		{
			if (flag)
			{
				float alpha2 = Ph(now, telegraph.ReceivedAt, telegraph.ReceivedAt + 300);
				Visee(valueOrDefault, BoutDe(carte, array[0]), alpha2, now);
			}
			return;
		}
		if (array.Length == 1 && now < telegraph.FiresAt)
		{
			if (flag)
			{
				Vector2 vector2 = BoutDe(carte, array[0]);
				Segment(valueOrDefault.X, valueOrDefault.Y, vector2.X, vector2.Y, 4f, Teinte(255, 200, 90, 0.35f), GameBlendModes.Add);
				Segment(valueOrDefault.X, valueOrDefault.Y, vector2.X, vector2.Y, 2f, Teinte(255, 236, 160, 0.95f));
				Lueur(vector2.X, vector2.Y, 9f, Teinte(255, 230, 140, 0.9f));
			}
			return;
		}
		long num = now - telegraph.FiresAt;
		int num2 = ((array.Length != 0) ? array[^1].DelayMs : 0);
		if (array.Length != 0 && num >= 0 && num <= (long)num2 + 650L && flag)
		{
			Vector2 fin = BoutDuBalayage(carte, array, num);
			float a = Ph(now, telegraph.FiresAt, telegraph.FiresAt + 100) * (1f - Ph(now, telegraph.LastFiresAt + 650 - 200, telegraph.LastFiresAt + 650));
			if (!attaque.Tir)
			{
				attaque.Tir = true;
				EclairsEcran.Add(new EclairEcran(0.22f, now, 150));
				CombatFeedback.StartShake(2.5f, (long)num2 + 650L);
				Son("ki_depart.wav", valueOrDefault.X, valueOrDefault.Y, -0.25f);
			}
			if (now - attaque.DernierImpact >= 600)
			{
				attaque.DernierImpact = now;
				Son("ki_impact.wav", fin.X, fin.Y, -0.1f);
			}
			Rayon(valueOrDefault, fin, a, now, array[0].Width * (float)TileWidth / 4.4f);
			if (now - attaque.DerniereEtincelle >= 350)
			{
				attaque.DerniereEtincelle = now;
				Etincelles(fin.X, fin.Y, now, (int)(now % 7919), 8, 22f);
			}
		}
	}

	private static void Visee(Vector2 de, Vector2 a, float alpha, long now)
	{
		Pointilles(de.X, de.Y, a.X, a.Y, 6f, 4f, (float)now / 20f, 3f, Teinte(255, 60, 40, 0.25f * alpha));
		Pointilles(de.X, de.Y, a.X, a.Y, 6f, 4f, (float)now / 20f, 1.5f, Teinte(235, 40, 30, alpha * (0.65f + 0.35f * MathF.Sin((float)now / 40f))), GameBlendModes.None);
		Lueur(a.X, a.Y, 7f, Teinte(255, 80, 60, alpha));
	}

	private static Vector2 BoutDuBalayage(MapInstance carte, DungeonTelegraphShape[] bandes, long depuis)
	{
		if (bandes.Length == 1 || depuis <= bandes[0].DelayMs)
		{
			return BoutDe(carte, bandes[0]);
		}
		for (int i = 1; i < bandes.Length; i++)
		{
			if (depuis <= bandes[i].DelayMs)
			{
				float amount = (float)(depuis - bandes[i - 1].DelayMs) / (float)Math.Max(1, bandes[i].DelayMs - bandes[i - 1].DelayMs);
				return Vector2.Lerp(BoutDe(carte, bandes[i - 1]), BoutDe(carte, bandes[i]), amount);
			}
		}
		return BoutDe(carte, bandes[^1]);
	}

	private static void Rayon(Vector2 debut, Vector2 fin, float a, long now, float l)
	{
		if (a <= 0f)
		{
			return;
		}
		float num = Vector2.Distance(debut, fin);
		if (!(num < 1f))
		{
			float num2 = 0.88f + 0.12f * MathF.Sin((float)now / 25f) + 0.06f * MathF.Sin((float)now / 7f);
			Segment(debut.X, debut.Y, fin.X, fin.Y, l * 4.4f, Teinte(255, 150, 30, 0.12f * a), GameBlendModes.Add);
			Segment(debut.X, debut.Y, fin.X, fin.Y, l * 2.6f, Teinte(255, 160, 40, 0.2f * a), GameBlendModes.Add);
			Segment(debut.X, debut.Y, fin.X, fin.Y, l * num2, Teinte(255, 205, 80, 0.9f * a), GameBlendModes.Add);
			Segment(debut.X, debut.Y, fin.X, fin.Y, l * 0.36f, Teinte(255, 255, 236, a), GameBlendModes.Add);
			float num3 = (fin.X - debut.X) / num;
			float num4 = (fin.Y - debut.Y) / num;
			float num5 = num3;
			for (int i = 0; i < 9; i++)
			{
				float num6 = ((float)now / 1.6f + (float)(i * 47)) % num;
				Bande(debut.X + num5 * (num6 + 3f), debut.Y + num4 * (num6 + 3f), 6f, l * 1.1f, MathF.Atan2(num4, num5) * 180f / (float)Math.PI, Teinte(255, 250, 220, 0.5f * a), GameBlendModes.Add);
			}
			Lueur(debut.X, debut.Y, l * 1.6f, Teinte(255, 230, 160, 0.8f * a));
			Lueur(fin.X, fin.Y, l * 2.4f * num2, Teinte(255, 200, 90, 0.8f * a));
		}
	}

	private static void ColonnesApresLaNuit(Dungeon.Telegraph telegraph, Attaque attaque, long now)
	{
		if (now < telegraph.FiresAt || attaque.Fissures == null)
		{
			return;
		}
		if (!attaque.Embrasement)
		{
			attaque.Embrasement = true;
			EclairsEcran.Add(new EclairEcran(0.1f, now, 160));
			CombatFeedback.StartShake(3f, 400L);
			int num = (int)(now % 7919);
			if (attaque.Fissures.Length != 0 && attaque.Fissures[0].Length != 0)
			{
				Son("recolte_roche_craque_fort_1.wav", attaque.Fissures[0][0].X, attaque.Fissures[0][0].Y);
				Son("explosion.wav", attaque.Fissures[0][0].X, attaque.Fissures[0][0].Y, -0.15f);
			}
			Vector2[][] fissures = attaque.Fissures;
			foreach (Vector2[] array in fissures)
			{
				for (int j = 1; j < array.Length; j += 3)
				{
					Poussiere(array[j].X, array[j].Y, now, num + j, 4, 14f);
				}
				for (int k = 5; k < array.Length; k += 10)
				{
					Flashs.Add(new Flash(array[k].X, array[k].Y, 56, 190, now, 900, new Color(255, 255, 180, 60)));
				}
			}
		}
		float num2 = 1f - Ph(now, telegraph.FiresAt, telegraph.FiresAt + 900);
		if (num2 <= 0f)
		{
			return;
		}
		for (int l = 0; l < attaque.Fissures.Length; l++)
		{
			Vector2[] array2 = attaque.Fissures[l];
			for (int m = 2; m < array2.Length; m += 2)
			{
				float num3 = (30f + Hasard(70 + l, m) * 34f) * num2;
				float num4 = 9f + Hasard(170 + l, m) * 6f;
				float x = array2[m].X;
				float y = array2[m].Y;
				float x2 = x;
				Colonne(x2, y, num4, num3, Teinte(255, 220, 120, 0.9f * num2));
				Colonne(x2, y, num4 * 0.4f, num3 * 0.8f, Teinte(255, 250, 220, 0.7f * num2));
			}
		}
	}

	private static void Colonne(float x, float y, float largeur, float hauteur, Color couleur)
	{
		IGameTexture gameTexture = TextureDeLueur();
		if (gameTexture != null && !(hauteur <= 0f) && couleur.A != 0)
		{
			Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, gameTexture.Width, (float)gameTexture.Height / 2f), new FloatRect(x - largeur / 2f, y - hauteur, largeur, hauteur), couleur, null, GameBlendModes.Add);
		}
	}

	private static void BlocsApresLaNuit(Dungeon.Telegraph telegraph, Attaque attaque, MapInstance carte, long now)
	{
		DungeonTelegraphShape[] array = telegraph.Packet.Shapes ?? Array.Empty<DungeonTelegraphShape>();
		for (int i = 0; i < array.Length; i++)
		{
			long num = telegraph.FiresAt + array[i].DelayMs;
			Vector2 vector = Monde(carte, array[i].X, array[i].Y);
			if (now >= num)
			{
				float num2 = 1f - Ph(now, num, num + 220);
				if (num2 > 0f)
				{
					float num3 = array[i].Radius * (float)TileWidth;
					Lueur(vector.X, vector.Y - 2f, num3 * 1.3f, Teinte(255, 220, 130, 0.65f * num2));
					Lueur(vector.X, vector.Y - 2f, num3 * 0.5f, Teinte(255, 250, 225, 0.8f * num2));
				}
				if (i >= attaque.BlocsTombes)
				{
					attaque.BlocsTombes = i + 1;
					int num4 = (int)(telegraph.ReceivedAt % 7919) + i * 13;
					Eclats(vector.X, vector.Y, now, num4, 9);
					Etincelles(vector.X, vector.Y - 4f, now, num4 + 5, 9, 20f);
					Poussiere(vector.X, vector.Y, now, num4 + 9, 6, 16f);
					Ajouter(new Particule
					{
						Genre = Genre.Anneau,
						X = vector.X,
						Y = vector.Y,
						Ne = now,
						VieMs = 900
					});
					Flashs.Add(new Flash(vector.X, vector.Y, 56, 220, now, 300, new Color(255, 255, 210, 110)));
					Secouer(2.5f, 220L, vector.X, vector.Y);
					Son((i % 2 == 0) ? "heritage_rocher_1.wav" : "heritage_rocher_2.wav", vector.X, vector.Y, (Hasard(num4, 7) - 0.5f) * 0.16f);
				}
			}
			else
			{
				long num5 = num - 600;
				if (now >= num5)
				{
					float num6 = Ph(now, num5, num);
					float num7 = 260f * (1f - num6 * num6);
					Pave(vector.X - 3f, vector.Y - num7 - 60f, 6f, 50f, Teinte(255, 220, 120, 0.3f), GameBlendModes.Add);
					BlocDOr(vector.X, vector.Y - num7 - 8f, TailleDuBloc(array[i].Radius), 0.25f * (float)i);
				}
			}
		}
	}

	private static void Tempete(long now)
	{
		float tempete = _tempete;
		if (!(tempete <= 0.01f))
		{
			FloatRect worldViewport = Graphics.WorldViewport;
			Pave(worldViewport.X, worldViewport.Y, worldViewport.Width, worldViewport.Height, Teinte(78, 48, 16, 0.22f * tempete));
			float num = worldViewport.Width + 40f;
			for (int i = 0; i < 170; i++)
			{
				float num2 = 0.25f + Hasard(99, i * 6) * 0.5f;
				float num3 = Hasard(99, i * 6 + 1) * worldViewport.Height;
				float num4 = 9f + Hasard(99, i * 6 + 2) * 20f;
				float num5 = worldViewport.X + (Hasard(99, i * 6 + 3) * num + (float)now * num2) % num - 20f;
				float num6 = worldViewport.Y + (num3 + (float)now * num2 * 0.18f) % worldViewport.Height;
				Segment(num5 - num4, num6 - num4 * 0.18f, num5, num6, 0.5f + Hasard(99, i * 6 + 4) * 0.75f, Teinte(232, 200, 140, (0.12f + Hasard(99, i * 6 + 5) * 0.28f) * tempete), GameBlendModes.Add);
			}
			IGameTexture gameTexture = TextureDeVignette();
			if (gameTexture != null)
			{
				Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, gameTexture.Width, gameTexture.Height), worldViewport, Teinte(20, 10, 2, 0.45f * tempete));
			}
		}
	}

	public static void AddLights()
	{
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		long maintenant = Maintenant;
		foreach (var (bossId, colosse2) in Colosses)
		{
			if (colosse2.MapId != me.MapId || maintenant - colosse2.DessineA > 300 || colosse2.MortDepuis != 0L)
			{
				continue;
			}
			float num = ForceDesYeux(bossId, colosse2, maintenant);
			foreach (Vector2 item in colosse2.Yeux)
			{
				Graphics.AddLight((int)item.X, (int)item.Y, 26, (byte)(200f * num), 0f, Color.FromArgb(255, 255, 90, 40));
			}
			float num2 = Aura(colosse2, maintenant);
			if (num2 > 0.02f)
			{
				Graphics.AddLight((int)(colosse2.Corps.X + colosse2.Corps.Width / 2f), (int)(colosse2.Corps.Y + colosse2.Corps.Height * 0.6f), 96, (byte)(130f * num2), 0f, Color.FromArgb(255, 255, 170, 40));
			}
		}
		foreach (var (num3, num4, num5) in DallesAllumees(maintenant))
		{
			if (num5 > 0.05f)
			{
				Graphics.AddLight((int)num3, (int)num4, (int)(30f * num5 + 8f), (byte)(220f * num5), 0f, Color.FromArgb(255, 255, 90, 40));
			}
		}
		LumieresDesEffetsKi(maintenant);
		foreach (Dungeon.Telegraph activeTelegraph in Dungeon.ActiveTelegraphs)
		{
			if (maintenant <= activeTelegraph.EndsAt && EstSaiyan(activeTelegraph.Packet.BossId) && MapInstance.TryGet(activeTelegraph.Packet.MapId, out MapInstance instance))
			{
				LumieresSaiyan(activeTelegraph, instance, maintenant);
			}
			else
			{
				if (!EstAuColosse(activeTelegraph.Kind) || maintenant > activeTelegraph.EndsAt || !MapInstance.TryGet(activeTelegraph.Packet.MapId, out MapInstance instance2))
				{
					continue;
				}
				DungeonTelegraphShape[] array = activeTelegraph.Packet.Shapes ?? Array.Empty<DungeonTelegraphShape>();
				if (activeTelegraph.Kind == BossAbilityKind.EyeBeam && array.Length != 0)
				{
					LumieresDuRayon(activeTelegraph, instance2, array, maintenant);
				}
				else
				{
					if (activeTelegraph.Kind != BossAbilityKind.Fissures || !Attaques.TryGetValue((activeTelegraph.Packet.BossId, activeTelegraph.ReceivedAt), out Attaque value) || value.Fissures == null)
					{
						continue;
					}
					float num6 = ((maintenant < activeTelegraph.FiresAt) ? (0.35f * Ph(maintenant, activeTelegraph.ReceivedAt + activeTelegraph.Packet.DurationMs * 85 / 100, activeTelegraph.FiresAt)) : (1f - Ph(maintenant, activeTelegraph.FiresAt + 700, activeTelegraph.FiresAt + 1600)));
					if (!(num6 > 0.02f))
					{
						continue;
					}
					Vector2[][] fissures = value.Fissures;
					foreach (Vector2[] array2 in fissures)
					{
						for (int j = 4; j < array2.Length; j += 7)
						{
							Graphics.AddLight((int)array2[j].X, (int)array2[j].Y, 46, (byte)(170f * num6), 0f, Color.FromArgb(255, 255, 180, 60));
						}
					}
				}
			}
		}
		for (int num7 = Flashs.Count - 1; num7 >= 0; num7--)
		{
			Flash flash = Flashs[num7];
			long num8 = maintenant - flash.Ne;
			if (num8 >= flash.VieMs)
			{
				Flashs.RemoveAt(num7);
			}
			else
			{
				Graphics.AddLight((int)flash.X, (int)flash.Y, flash.Taille, (byte)((float)(int)flash.Intensite * (1f - (float)num8 / (float)flash.VieMs)), 0f, flash.Couleur);
			}
		}
	}

	private static void LumieresDuRayon(Dungeon.Telegraph telegraph, MapInstance carte, DungeonTelegraphShape[] bandes, long now)
	{
		long num = now - telegraph.FiresAt;
		if (num >= 0 && num <= (long)bandes[^1].DelayMs + 650L)
		{
			Vector2? vector = Regard(telegraph.Packet.BossId, now);
			if (vector.HasValue)
			{
				Vector2 valueOrDefault = vector.GetValueOrDefault();
				float num2 = 1f - Ph(now, telegraph.LastFiresAt + 650 - 200, telegraph.LastFiresAt + 650);
				Vector2 value = BoutDuBalayage(carte, bandes, num);
				float num3 = Vector2.Distance(valueOrDefault, value);
				for (float num4 = 0f; num4 < num3; num4 += 56f)
				{
					Vector2 vector2 = Vector2.Lerp(valueOrDefault, value, num4 / MathF.Max(1f, num3));
					Graphics.AddLight((int)vector2.X, (int)vector2.Y, 44, (byte)(170f * num2), 0f, Color.FromArgb(255, 255, 190, 80));
				}
				Graphics.AddLight((int)value.X, (int)value.Y, 60, (byte)(210f * num2), 0f, Color.FromArgb(255, 255, 200, 90));
			}
		}
		if (bandes.Length != 1 || num < 0)
		{
			return;
		}
		float num5 = Math.Clamp(1f - (float)num / 1400f, 0f, 1f);
		if (num5 > 0.02f)
		{
			Vector2 value2 = Monde(carte, bandes[0].X, bandes[0].Y);
			Vector2 value3 = BoutDe(carte, bandes[0]);
			float num6 = Vector2.Distance(value2, value3);
			for (float num7 = 24f; num7 < num6; num7 += 80f)
			{
				Vector2 vector3 = Vector2.Lerp(value2, value3, num7 / MathF.Max(1f, num6));
				Graphics.AddLight((int)vector3.X, (int)vector3.Y, 30, (byte)(120f * num5), 0f, Color.FromArgb(255, 255, 150, 50));
			}
		}
	}

	public static void OnTelegraph(DungeonTelegraphPacket packet)
	{
		if (packet.Locked)
		{
			return;
		}
		Entity entity = EntiteDe(packet.BossId);
		if (entity == null)
		{
			return;
		}
		if (entity.BossVisual == BossVisual.Saiyan)
		{
			AnnonceSaiyan(packet, entity);
			return;
		}
		switch ((BossAbilityKind)packet.Kind)
		{
		case BossAbilityKind.Fissures:
			entity.JouerLeGeste(0);
			break;
		case BossAbilityKind.GoldRain:
			entity.JouerLeGeste(1);
			CombatFeedback.StartShake(1.2f, 1100L);
			Son("meteo_tonnerre_loin_1.wav", entity.Origin.X, entity.Origin.Y - 60f);
			break;
		}
	}

	public static void DrawGround()
	{
		DessinerLesEntrees();
		long maintenant = Maintenant;
		DessinerLeColosseAuSol(maintenant);
		IReadOnlyList<Dungeon.Telegraph> activeTelegraphs = Dungeon.ActiveTelegraphs;
		if (activeTelegraphs.Count == 0 || Globals.Me == null)
		{
			return;
		}
		foreach (Dungeon.Telegraph item in activeTelegraphs)
		{
			if (maintenant > item.EndsAt || !MapInstance.TryGet(item.Packet.MapId, out MapInstance instance))
			{
				continue;
			}
			if (EstSaiyan(item.Packet.BossId))
			{
				SaiyanAuSol(item, instance, maintenant);
				continue;
			}
			if (EstAuColosse(item.Kind))
			{
				AttaqueAuSol(item, instance, maintenant);
				continue;
			}
			int num = Math.Max(1, item.Packet.DurationMs);
			float progress = Math.Clamp((float)(maintenant - item.ReceivedAt) / (float)num, 0f, 1f);
			float impact = ((maintenant >= item.FiresAt) ? (1f - (float)(maintenant - item.FiresAt) / 280f) : 0f);
			DungeonTelegraphShape[] array = item.Packet.Shapes ?? Array.Empty<DungeonTelegraphShape>();
			foreach (DungeonTelegraphShape shape in array)
			{
				DrawShape(instance, shape, progress, impact, maintenant);
			}
		}
	}

	private static void DessinerLesEntrees()
	{
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		float num = 0.5f + 0.5f * MathF.Sin((float)Timing.Global.MillisecondsUtc / 900f);
		foreach (DungeonDescriptor item in DatabaseObject<DungeonDescriptor>.Lookup.Values.OfType<DungeonDescriptor>())
		{
			if (item.Rooms.Count != 0 && MapInstance.TryGet(item.EntranceMapId, out MapInstance instance))
			{
				float centreX = (float)instance.X + ((float)item.EntranceX + 0.5f) * (float)TileWidth;
				float centreY = (float)instance.Y + ((float)item.EntranceY + 0.5f) * (float)TileHeight;
				float rayon = 2.5f * (float)TileWidth;
				bool flag = item.EntranceMapId == me.MapId && Math.Max(Math.Abs(me.X - item.EntranceX), Math.Abs(me.Y - item.EntranceY)) <= 2;
				Color color = new Color((int)(22f + 16f * num + (float)(flag ? 34 : 0)), 255, 206, 122);
				Color color2 = new Color((int)(70f + 34f * num + (float)(flag ? 80 : 0)), 255, 228, 156);
				float num2 = Math.Max(2f * Pixel, 1.5f);
				if (!DessinerSousLeDecor(item, instance, centreX, centreY, rayon, num2, color, color2))
				{
					Disque(centreX, centreY, rayon, color);
					Cercle(centreX, centreY, rayon, num2, color2);
				}
			}
		}
	}

	private static bool DessinerSousLeDecor(DungeonDescriptor donjon, MapInstance carte, float centreX, float centreY, float rayon, float trait, Color fond, Color bord)
	{
		if (donjon.EntranceY != 0 || carte.Up == Guid.Empty || !MapInstance.TryGet(carte.Up, out MapInstance instance) || instance.Layers == null)
		{
			return false;
		}
		int num = (int)MathF.Ceiling(rayon) + 2;
		float num2 = centreX - (float)num - (float)instance.X;
		float num3 = centreY - (float)num - (float)instance.Y;
		int num4 = Math.Max(0, (int)MathF.Floor(num2 / (float)TileWidth));
		int num5 = Math.Min(Options.Instance.Map.MapWidth - 1, (int)MathF.Floor((num2 + (float)(num * 2)) / (float)TileWidth));
		int num6 = Math.Max(0, (int)MathF.Floor(num3 / (float)TileHeight));
		int num7 = Options.Instance.Map.MapHeight - 1;
		if (num4 > num5 || num6 > num7)
		{
			return false;
		}
		HashCode hashCode = default(HashCode);
		hashCode.Add(instance.Id);
		hashCode.Add(donjon.EntranceX);
		List<Tile[,]> dressees = Dressees;
		dressees.Clear();
		foreach (string lowerLayer in Options.Instance.Map.Layers.LowerLayers)
		{
			if (lowerLayer == "Ground" || !instance.Layers.TryGetValue(lowerLayer, out var value) || value == null || value.GetLength(0) <= num5 || value.GetLength(1) <= num7)
			{
				continue;
			}
			dressees.Add(value);
			for (int i = num6; i <= num7; i++)
			{
				for (int j = num4; j <= num5; j++)
				{
					Tile tile = value[j, i];
					hashCode.Add(tile.TilesetId);
					hashCode.Add(tile.X);
					hashCode.Add(tile.Y);
					hashCode.Add(tile.Autotile);
				}
			}
		}
		if (!Dalles.TryGetValue(donjon.Id, out DalleDecoupee value2))
		{
			value2 = (Dalles[donjon.Id] = new DalleDecoupee());
		}
		int num8 = hashCode.ToHashCode();
		if (!value2.Faite || value2.Empreinte != num8)
		{
			value2.Faite = false;
			if (!Decouper(value2, donjon.Id, dressees, num4, num6, num5, num7, num2, num3, num, rayon))
			{
				return true;
			}
			value2.Empreinte = num8;
			value2.Faite = true;
		}
		if (!value2.Entiere)
		{
			IGameTexture disque = value2.Disque;
			if (disque != null)
			{
				Graphics.DrawGameTexture(disque, new FloatRect(0f, 0f, disque.Width, disque.Height), new FloatRect(centreX - (float)num, centreY - (float)num, num * 2, num * 2), fond);
				Cercle(centreX, centreY, rayon, trait, bord, value2);
				return true;
			}
		}
		return false;
	}

	private static bool Decouper(DalleDecoupee dalle, Guid donjon, List<Tile[,]> dressees, int x0, int y0, int x1, int y1, float gauche, float haut, int demi, float rayon)
	{
		int num = x1 - x0 + 1;
		List<byte[][]> list = new List<byte[][]>();
		bool flag = false;
		foreach (Tile[,] dressee in dressees)
		{
			byte[][] array = new byte[num * (y1 - y0 + 1)][];
			for (int i = y0; i <= y1; i++)
			{
				for (int j = x0; j <= x1; j++)
				{
					Tile tile = dressee[j, i];
					if (tile.TilesetId == Guid.Empty || tile.Autotile != 0 || !DatabaseObject<TilesetDescriptor>.TryGet(tile.TilesetId, out var databaseObject))
					{
						continue;
					}
					IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Tileset, databaseObject.Name);
					if (gameTexture != null)
					{
						if (!Casse.PixelsDeTuile(gameTexture, tile.X, tile.Y, out byte[] tuile))
						{
							return false;
						}
						array[(i - y0) * num + j - x0] = tuile;
						flag = flag || tuile != null;
					}
				}
			}
			list.Add(array);
		}
		dalle.Entiere = !flag;
		if (!flag)
		{
			return true;
		}
		int num2 = demi * 2;
		float[] array2 = new float[num2 * num2];
		for (int k = 0; k < num2; k++)
		{
			for (int l = 0; l < num2; l++)
			{
				int num3 = (int)MathF.Floor(gauche + (float)l);
				int num4 = (int)MathF.Floor(haut + (float)k);
				float num5 = 1f;
				if (num3 >= 0 && num4 >= 0)
				{
					int num6 = num3 / TileWidth;
					int num7 = num4 / TileHeight;
					if (num6 >= x0 && num6 <= x1 && num7 >= y0 && num7 <= y1)
					{
						int num8 = (num4 % TileHeight * TileWidth + num3 % TileWidth) * 4 + 3;
						foreach (byte[][] item in list)
						{
							byte[] array3 = item[(num7 - y0) * num + num6 - x0];
							if (array3 != null && num8 < array3.Length)
							{
								num5 *= 1f - (float)(int)array3[num8] / 255f;
							}
						}
					}
				}
				array2[k * num2 + l] = num5;
			}
		}
		int num9 = num2 * 2;
		byte[] array4 = new byte[num9 * num9 * 4];
		for (int m = 0; m < num9; m++)
		{
			for (int n = 0; n < num9; n++)
			{
				float num10 = ((float)n + 0.5f) / 2f - (float)demi;
				float num11 = ((float)m + 0.5f) / 2f - (float)demi;
				float num12 = Math.Clamp((rayon - MathF.Sqrt(num10 * num10 + num11 * num11)) * 2f + 0.5f, 0f, 1f);
				byte b = (byte)(num12 * array2[m / 2 * num2 + n / 2] * 255f);
				int num13 = (m * num9 + n) * 4;
				array4[num13] = b;
				array4[num13 + 1] = b;
				array4[num13 + 2] = b;
				array4[num13 + 3] = b;
			}
		}
		dalle.Libre = array2;
		dalle.Cote = num2;
		IGameTexture disque = dalle.Disque;
		if (disque == null || disque.Width != num9 || !MonoRenderer.RemplacerPixels(disque, array4))
		{
			dalle.Disque = Graphics.Renderer?.CreateTextureFromPixels($"donjons:zone-decoupee/{donjon}/{num9}", num9, num9, array4);
		}
		return true;
	}

	private static void DrawShape(MapInstance map, DungeonTelegraphShape shape, float progress, float impact, long now)
	{
		float num = 0.5f + 0.5f * MathF.Sin((float)now / 110f);
		Color couleur = ((impact > 0f) ? new Color((int)(170f * impact), 255, 236, 220) : new Color((int)(46f + 22f * num), 255, 60, 80));
		Color couleur2 = new Color(92, 255, 85, 102);
		Color couleur3 = new Color(235, 255, 85, 102);
		float num2 = 2f * Pixel;
		float centreX = (float)map.X + shape.X * (float)TileWidth;
		float centreY = (float)map.Y + shape.Y * (float)TileHeight;
		if (!shape.IsLane)
		{
			float num3 = shape.Radius * (float)TileWidth;
			Disque(centreX, centreY, num3, couleur);
			if (!(impact > 0f))
			{
				if (progress > 0f)
				{
					Disque(centreX, centreY, num3 * progress, couleur2);
				}
				Cercle(centreX, centreY, num3 - num2 / 2f, num2, couleur3);
			}
			return;
		}
		float dirX = shape.DirX;
		float dirY = shape.DirY;
		float dirY2 = dirY;
		float dirX2 = dirX;
		float angle = MathF.Atan2(dirY2, dirX2) * 180f / (float)Math.PI;
		float num4 = -0.5f * (float)TileWidth;
		float num5 = shape.Length * (float)TileWidth;
		float num6 = shape.Width * (float)TileWidth;
		Portion(num4, num5, num6, 0f, couleur);
		if (!(impact > 0f))
		{
			if (progress > 0f)
			{
				Portion(num4, num4 + (num5 - num4) * progress, num6, 0f, couleur2);
			}
			Portion(num4, num5, num2, num6 / 2f - num2 / 2f, couleur3);
			Portion(num4, num5, num2, 0f - (num6 / 2f - num2 / 2f), couleur3);
			Portion(num4, num4 + num2, num6, 0f, couleur3);
			Portion(num5 - num2, num5, num6, 0f, couleur3);
		}
		void Portion(float de, float a, float epaisseur, float decalage, Color couleur4)
		{
			float num7 = (de + a) / 2f;
			Bande(centreX + dirX2 * num7 - dirY2 * decalage, centreY + dirY2 * num7 + dirX2 * decalage, a - de, epaisseur, angle, couleur4);
		}
	}

	private static IGameTexture? TextureDuDisque()
	{
		if (_disque != null || Graphics.Renderer == null)
		{
			return _disque;
		}
		byte[] array = new byte[262144];
		float num = 128f;
		float num2 = num - 1f;
		for (int i = 0; i < 256; i++)
		{
			for (int j = 0; j < 256; j++)
			{
				float num3 = MathF.Sqrt(((float)j + 0.5f - num) * ((float)j + 0.5f - num) + ((float)i + 0.5f - num) * ((float)i + 0.5f - num));
				byte b = (byte)(Math.Clamp(num2 - num3 + 0.5f, 0f, 1f) * 255f);
				int num4 = (i * 256 + j) * 4;
				array[num4] = b;
				array[num4 + 1] = b;
				array[num4 + 2] = b;
				array[num4 + 3] = b;
			}
		}
		_disque = Graphics.Renderer.CreateTextureFromPixels("donjons:zone-disque", 256, 256, array);
		return _disque;
	}

	private static void Disque(float centreX, float centreY, float rayon, Color couleur)
	{
		IGameTexture gameTexture = TextureDuDisque();
		if (gameTexture != null && !(rayon <= 0f) && couleur.A != 0)
		{
			Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, gameTexture.Width, gameTexture.Height), new FloatRect(centreX - rayon, centreY - rayon, rayon * 2f, rayon * 2f), couleur);
		}
	}

	private static void Bande(float centreX, float centreY, float longueur, float epaisseur, float angle, Color couleur, GameBlendModes mode = GameBlendModes.None)
	{
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture != null && couleur.A != 0 && !(longueur <= 0f) && !(epaisseur <= 0f))
		{
			Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(centreX - longueur / 2f, centreY - epaisseur / 2f, longueur, epaisseur), couleur, null, mode, null, angle);
		}
	}

	private static void Cercle(float centreX, float centreY, float rayon, float epaisseur, Color couleur, DalleDecoupee? dalle = null)
	{
		if (rayon <= 0f)
		{
			return;
		}
		int num = ((dalle == null) ? Math.Clamp((int)(rayon / 2f), 24, 128) : Math.Clamp((int)((float)Math.PI * 2f * rayon / 2f), 24, 512));
		float longueur = 2f * rayon * MathF.Sin((float)Math.PI / (float)num) + epaisseur * 0.5f;
		for (int i = 0; i < num; i++)
		{
			float num2 = ((float)i + 0.5f) * ((float)Math.PI * 2f) / (float)num;
			if (dalle != null)
			{
				int num3 = (int)MathF.Floor((float)dalle.Cote / 2f + MathF.Cos(num2) * rayon);
				int num4 = (int)MathF.Floor((float)dalle.Cote / 2f + MathF.Sin(num2) * rayon);
				if (num3 >= 0 && num4 >= 0 && num3 < dalle.Cote && num4 < dalle.Cote && dalle.Libre[num4 * dalle.Cote + num3] < 0.5f)
				{
					continue;
				}
			}
			Bande(centreX + MathF.Cos(num2) * rayon, centreY + MathF.Sin(num2) * rayon, longueur, epaisseur, num2 * 180f / (float)Math.PI + 90f, couleur);
		}
	}

	private static bool EstSaiyan(Guid bossId)
	{
		Entity entity = EntiteDe(bossId);
		if (entity != null && entity.BossVisual == BossVisual.Saiyan)
		{
			Saiyans.Add(bossId);
			return true;
		}
		return Saiyans.Contains(bossId);
	}

	private static Vector2? MainsDe(Guid bossId, Vector2? vers = null)
	{
		Entity entity = EntiteDe(bossId);
		if (entity == null)
		{
			return null;
		}
		Vector2 vector = new Vector2(entity.WorldPos.X + entity.WorldPos.Width / 2f, entity.WorldPos.Y + entity.WorldPos.Height * 0.56f);
		if (vers.HasValue)
		{
			Vector2 valueOrDefault = vers.GetValueOrDefault();
			Vector2 vector2 = valueOrDefault - vector;
			float num = vector2.Length();
			if (num > 1f)
			{
				vector += vector2 / num * 9f;
			}
		}
		return vector;
	}

	private static void EtincellesKi(float x, float y, long now, int graine, int nombre, float portee)
	{
		for (int i = 0; i < nombre; i++)
		{
			Ajouter(new Particule
			{
				Genre = Genre.EtincelleKi,
				X = x,
				Y = y,
				Angle = Hasard(graine, i * 3) * ((float)Math.PI * 2f),
				Vitesse = portee * (0.4f + Hasard(graine, i * 3 + 1)),
				Taille = Hasard(graine, i * 3 + 2),
				Ne = now,
				VieMs = 360 + (int)(Hasard(graine + 7, i) * 300f)
			});
		}
	}

	private static void SaiyanAuSol(Dungeon.Telegraph telegraph, MapInstance carte, long now)
	{
		Attaque attaque = EtatDe(telegraph, now);
		DungeonTelegraphShape[] array = telegraph.Packet.Shapes ?? Array.Empty<DungeonTelegraphShape>();
		bool flag = now < telegraph.FiresAt;
		float num = Ph(now, telegraph.ReceivedAt, telegraph.ReceivedAt + 200);
		float progres = Ph(now, telegraph.ReceivedAt, telegraph.FiresAt);
		float num2 = 0.5f + 0.5f * MathF.Sin((float)now / 95f);
		switch (telegraph.Kind)
		{
		case BossAbilityKind.Charge:
			if (flag)
			{
				DungeonTelegraphShape[] array2 = array;
				foreach (DungeonTelegraphShape forme in array2)
				{
					BandeDeRuee(carte, forme, progres, num2, num, now);
				}
			}
			break;
		case BossAbilityKind.RockFall:
		{
			DungeonTelegraphShape[] array4 = array;
			foreach (DungeonTelegraphShape dungeonTelegraphShape in array4)
			{
				long num10 = telegraph.FiresAt + dungeonTelegraphShape.DelayMs;
				if (now < num10)
				{
					CibleDeKi(carte, dungeonTelegraphShape, Ph(now, telegraph.ReceivedAt, num10), num2, num);
				}
			}
			break;
		}
		case BossAbilityKind.Shockwave:
			if (flag)
			{
				DungeonTelegraphShape[] array3 = array;
				foreach (DungeonTelegraphShape forme2 in array3)
				{
					KiQuiSeRassemble(carte, forme2, num2, num, now);
				}
			}
			break;
		case BossAbilityKind.EyeBeam:
		{
			if (telegraph.Packet.Locked && flag && array.Length == 1)
			{
				BandeAuSol(carte, array[0], 0f, array[0].Length, Teinte(200, 110, 255, (0.08f + 0.07f * num2) * num));
			}
			if (array.Length > 1 && flag)
			{
				SecteurDuDoubleSunday(carte, array, now, telegraph);
			}
			for (int num11 = 0; num11 < array.Length; num11++)
			{
				long num12 = now - (telegraph.FiresAt + array[num11].DelayMs);
				if (num12 >= 0)
				{
					Brulure(carte, array[num11], num12, array.Length == 1);
				}
			}
			break;
		}
		case BossAbilityKind.Fissures:
		{
			int num3 = Math.Max(1, telegraph.Packet.DurationMs);
			long num4 = telegraph.ReceivedAt + num3 / 4;
			if (!attaque.Choc && now >= num4)
			{
				Entity entity = EntiteDe(telegraph.Packet.BossId);
				if (entity != null)
				{
					attaque.Choc = true;
					Secouer(3.5f, 380L, entity.Origin.X, entity.Origin.Y);
					Poussiere(entity.Origin.X, entity.Origin.Y, now, (int)(now % 7919), 10, 26f);
					EtincellesKi(entity.Origin.X, entity.Origin.Y - 6f, now, (int)(now % 6007), 10, 28f);
					Son("ki_impact.wav", entity.Origin.X, entity.Origin.Y, -0.3f);
				}
			}
			Attaque attaque2 = attaque;
			if (attaque2.Fissures == null)
			{
				attaque2.Fissures = array.Select((DungeonTelegraphShape bande, int num13) => TracerFissure(carte, bande, (int)(telegraph.ReceivedAt % 104729) + num13 * 31)).ToArray();
			}
			float num5 = Lisse(Ph(now, num4, telegraph.ReceivedAt + num3 * 85 / 100));
			float feu = Ph(now, telegraph.ReceivedAt + num3 * 85 / 100, telegraph.FiresAt);
			if (now >= telegraph.FiresAt)
			{
				feu = 1f - Ph(now, telegraph.FiresAt + 600, telegraph.FiresAt + 1200);
			}
			float froid = Ph(now, telegraph.FiresAt + 1200, telegraph.FiresAt + 3500);
			float num6 = (flag ? Ph(now, telegraph.ReceivedAt + num3 * 4 / 10, telegraph.FiresAt) : 0f);
			for (int num7 = 0; num7 < array.Length; num7++)
			{
				if (num6 > 0f)
				{
					BandeAuSol(carte, array[num7], 0f, array[num7].Length * MathF.Max(num5, 0.05f), Teinte(180, 80, 255, 0.16f * num6));
				}
				LigneDeKi(attaque.Fissures[num7], num5, feu, froid, 1f - Ph(now, telegraph.FiresAt + 3500 - 800, telegraph.FiresAt + 3500));
			}
			break;
		}
		default:
			if (EstAuColosse(telegraph.Kind))
			{
				AttaqueAuSol(telegraph, carte, now);
			}
			break;
		}
	}

	private static void BandeDeRuee(MapInstance carte, DungeonTelegraphShape forme, float progres, float bat, float montee, long now)
	{
		float length = forme.Length;
		BandeAuSol(carte, forme, -0.5f, length, Teinte(140, 50, 220, (0.1f + 0.05f * bat) * montee));
		BandeAuSol(carte, forme, -0.5f, -0.5f + (length + 0.5f) * progres, Teinte(185, 95, 255, 0.17f * montee));
		Vector2 vector = Monde(carte, forme.X, forme.Y);
		float dirX = forme.DirX;
		float dirY = forme.DirY;
		float num = dirX;
		float num2 = forme.Width * (float)TileWidth / 2f;
		Vector2 vector2 = new Vector2(vector.X - num * 0.5f * (float)TileWidth, vector.Y - dirY * 0.5f * (float)TileWidth);
		Vector2 vector3 = new Vector2(vector.X + num * length * (float)TileWidth, vector.Y + dirY * length * (float)TileWidth);
		for (int i = -1; i <= 1; i += 2)
		{
			Segment(vector2.X - dirY * num2 * (float)i, vector2.Y + num * num2 * (float)i, vector3.X - dirY * num2 * (float)i, vector3.Y + num * num2 * (float)i, 1.5f, Teinte(225, 150, 255, 0.8f * montee), GameBlendModes.Add);
		}
		float num3 = MathF.Atan2(dirY, num);
		for (int j = 0; j < 4; j++)
		{
			float num4 = ((float)now / 520f + (float)j / 4f) % 1f;
			float num5 = (-0.5f + (length + 0.5f) * num4) * (float)TileWidth;
			float num6 = vector.X + num * num5;
			float num7 = vector.Y + dirY * num5;
			float num8 = num6;
			for (int k = -1; k <= 1; k += 2)
			{
				float x = num3 + (float)Math.PI + (float)k * 0.65f;
				Segment(num8, num7, num8 + MathF.Cos(x) * num2 * 0.8f, num7 + MathF.Sin(x) * num2 * 0.8f, 2f, Teinte(240, 195, 255, 0.85f * montee * MathF.Sin(num4 * (float)Math.PI)), GameBlendModes.Add);
			}
		}
	}

	private static void CibleDeKi(MapInstance carte, DungeonTelegraphShape forme, float p, float bat, float montee)
	{
		Vector2 vector = Monde(carte, forme.X, forme.Y);
		float num = forme.Radius * (float)TileWidth;
		Disque(vector.X, vector.Y, num, Teinte(110, 35, 180, (0.1f + 0.06f * bat) * montee));
		Disque(vector.X, vector.Y, num * p, Teinte(165, 75, 240, 0.2f * montee));
		Cercle(vector.X, vector.Y, num - 1f, 1.5f, Teinte(220, 140, 255, 0.85f * montee));
		for (int i = 0; i < 4; i++)
		{
			float x = (float)i * (float)Math.PI / 2f + p * 1.4f;
			float num2 = MathF.Cos(x);
			float num3 = MathF.Sin(x);
			float num4 = num2;
			Segment(vector.X + num4 * (num - 6f), vector.Y + num3 * (num - 6f), vector.X + num4 * (num + 3f), vector.Y + num3 * (num + 3f), 2f, Teinte(245, 205, 255, 0.9f * montee));
		}
	}

	private static void KiQuiSeRassemble(MapInstance carte, DungeonTelegraphShape forme, float bat, float montee, long now)
	{
		Vector2 vector = Monde(carte, forme.X, forme.Y);
		float num = forme.Radius * (float)TileWidth;
		Disque(vector.X, vector.Y, num, Teinte(110, 35, 180, (0.08f + 0.06f * bat) * montee));
		Cercle(vector.X, vector.Y, num - 1f, 2f, Teinte(220, 140, 255, 0.85f * montee));
		for (int i = 0; i < 3; i++)
		{
			float num2 = ((float)now / 640f + (float)i / 3f) % 1f;
			Cercle(vector.X, vector.Y, MathF.Max(3f, num * (1f - num2)), 1.5f, Teinte(235, 185, 255, 0.55f * montee * num2));
		}
	}

	private static void SecteurDuDoubleSunday(MapInstance carte, DungeonTelegraphShape[] formes, long now, Dungeon.Telegraph telegraph)
	{
		float num = Ph(now, telegraph.ReceivedAt, telegraph.ReceivedAt + 300);
		Vector2 vector = Monde(carte, formes[0].X, formes[0].Y);
		float num2 = MathF.Atan2(formes[0].DirY, formes[0].DirX);
		float num3 = MathF.Atan2(formes[^1].DirY, formes[^1].DirX);
		float num4 = MathF.IEEERemainder(num3 - num2, (float)Math.PI * 2f);
		float num5 = MathF.Min(4f, formes.Min((DungeonTelegraphShape forme) => forme.Length)) * (float)TileWidth;
		for (int num6 = 0; num6 < formes.Length; num6 += 2)
		{
			BandeAuSol(carte, formes[num6], 0.6f, formes[num6].Length, Teinte(160, 70, 255, 0.04f * num));
		}
		Vector2 vector2 = BoutDe(carte, formes[^1]);
		Pointilles(vector.X, vector.Y, vector2.X, vector2.Y, 6f, 5f, 0f, 1.2f, Teinte(190, 100, 255, 0.55f * num), GameBlendModes.None);
		int num7 = Math.Max(6, (int)(MathF.Abs(num4) * 10f));
		for (int num8 = 0; num8 < num7; num8 += 2)
		{
			float x = num2 + num4 * (float)num8 / (float)num7;
			float x2 = num2 + num4 * (float)(num8 + 1) / (float)num7;
			Segment(vector.X + MathF.Cos(x) * num5, vector.Y + MathF.Sin(x) * num5, vector.X + MathF.Cos(x2) * num5, vector.Y + MathF.Sin(x2) * num5, 1.5f, Teinte(215, 140, 255, 0.5f * num), GameBlendModes.Add);
		}
		int num9 = MathF.Sign(num4);
		for (int num10 = 0; num10 < 3; num10++)
		{
			float num11 = ((float)now / 900f + (float)num10 / 3f) % 1f;
			float num12 = num2 + num4 * (0.15f + 0.7f * num11);
			float num13 = vector.X + MathF.Cos(num12) * num5;
			float num14 = vector.Y + MathF.Sin(num12) * num5;
			float num15 = num13;
			float num16 = num12 + (float)num9 * (float)Math.PI / 2f;
			for (int num17 = -1; num17 <= 1; num17 += 2)
			{
				float x3 = num16 + (float)Math.PI + (float)num17 * 0.6f;
				Segment(num15, num14, num15 + MathF.Cos(x3) * 6f, num14 + MathF.Sin(x3) * 6f, 1.5f, Teinte(235, 180, 255, 0.75f * num * MathF.Sin(num11 * (float)Math.PI)), GameBlendModes.Add);
			}
		}
	}

	private static void Brulure(MapInstance carte, DungeonTelegraphShape forme, long age, bool large)
	{
		float num = Math.Clamp(1f - (float)age / 3200f, 0f, 1f);
		if (num <= 0f)
		{
			return;
		}
		float num2 = Math.Clamp(1f - (float)age / 1100f, 0f, 1f);
		Vector2 vector = Monde(carte, forme.X + forme.DirX * 0.4f, forme.Y + forme.DirY * 0.4f);
		Vector2 vector2 = BoutDe(carte, forme);
		if (!large)
		{
			Segment(vector.X, vector.Y, vector2.X, vector2.Y, forme.Width * (float)TileWidth * 0.5f, Teinte(36, 10, 56, 0.07f * num));
			return;
		}
		Segment(vector.X, vector.Y, vector2.X, vector2.Y, 12f, Teinte(36, 10, 56, 0.45f * num));
		if (num2 > 0f)
		{
			Segment(vector.X, vector.Y, vector2.X, vector2.Y, 4f, Teinte(210, 120, 255, 0.7f * num2 * num), GameBlendModes.Add);
		}
	}

	private static void LigneDeKi(Vector2[] points, float progression, float feu, float froid, float fondu = 1f)
	{
		if (progression <= 0f || points.Length < 2)
		{
			return;
		}
		int num = Math.Clamp((int)MathF.Round((float)points.Length * progression), 2, points.Length);
		Color couleur = Teinte(30, 8, 46, 0.85f * (1f - froid * 0.6f) * fondu);
		float num2 = (0.35f + 0.65f * feu) * (1f - froid);
		Color couleur2 = Teinte((int)(170f + 70f * feu), (int)(80f + 140f * feu), 255, num2);
		Color couleur3 = Teinte(170, 70, 255, 0.3f * num2);
		for (int i = 1; i < num; i++)
		{
			Segment(points[i - 1].X, points[i - 1].Y, points[i].X, points[i].Y, 5f, couleur);
		}
		if (!(num2 <= 0.01f))
		{
			for (int j = 1; j < num; j++)
			{
				Segment(points[j - 1].X, points[j - 1].Y, points[j].X, points[j].Y, 7f, couleur3, GameBlendModes.Add);
				Segment(points[j - 1].X, points[j - 1].Y, points[j].X, points[j].Y, 2f, couleur2, GameBlendModes.Add);
			}
		}
	}

	private static void SaiyanApresLaNuit(Dungeon.Telegraph telegraph, Attaque attaque, MapInstance carte, long now)
	{
		switch (telegraph.Kind)
		{
		case BossAbilityKind.EyeBeam:
			DoubleSunday(telegraph, attaque, carte, now);
			break;
		case BossAbilityKind.RockFall:
			RafaleDeKi(telegraph, attaque, carte, now);
			break;
		case BossAbilityKind.Shockwave:
			Kiai(telegraph, attaque, carte, now);
			break;
		case BossAbilityKind.Charge:
			Ruee(telegraph, attaque, carte, now);
			break;
		case BossAbilityKind.Fissures:
			ColonnesDeKi(telegraph, attaque, now);
			break;
		case BossAbilityKind.GoldRain:
			BlocsApresLaNuit(telegraph, attaque, carte, now);
			break;
		}
	}

	private static void DoubleSunday(Dungeon.Telegraph telegraph, Attaque attaque, MapInstance carte, long now)
	{
		DungeonTelegraphShape[] array = telegraph.Packet.Shapes ?? Array.Empty<DungeonTelegraphShape>();
		Entity entity = EntiteDe(telegraph.Packet.BossId);
		bool flag = entity != null && entity.Vital[0] > 0;
		float num = Ph(now, telegraph.ReceivedAt, telegraph.FiresAt);
		Vector2? vector2;
		if (!telegraph.Packet.Locked && array.Length == 0)
		{
			if (!flag)
			{
				return;
			}
			Entity entity2 = EntiteDe(telegraph.Packet.TargetId);
			if (entity2 != null)
			{
				Vector2 vector = new Vector2(entity2.Origin.X, entity2.Origin.Y - 6f);
				vector2 = MainsDe(telegraph.Packet.BossId, vector);
				if (vector2.HasValue)
				{
					Vector2 valueOrDefault = vector2.GetValueOrDefault();
					float alpha = Ph(now, telegraph.ReceivedAt + 120, telegraph.ReceivedAt + 420);
					ViseeKi(valueOrDefault, vector, alpha, now);
					BoulesDeKi(valueOrDefault, vector, 0.25f + 0.45f * num, now);
				}
			}
			return;
		}
		Vector2 vector3 = ((array.Length != 0) ? BoutDe(carte, array[0]) : default(Vector2));
		vector2 = MainsDe(telegraph.Packet.BossId, (array.Length != 0) ? new Vector2?(vector3) : ((Vector2?)null));
		if (!vector2.HasValue)
		{
			return;
		}
		Vector2 valueOrDefault2 = vector2.GetValueOrDefault();
		if (array.Length > 1 && now < telegraph.FiresAt)
		{
			if (flag)
			{
				ViseeKi(valueOrDefault2, vector3, Ph(now, telegraph.ReceivedAt, telegraph.ReceivedAt + 300), now);
				BoulesDeKi(valueOrDefault2, vector3, 0.4f + 0.6f * num, now);
			}
			return;
		}
		if (array.Length == 1 && now < telegraph.FiresAt)
		{
			if (flag)
			{
				Segment(valueOrDefault2.X, valueOrDefault2.Y, vector3.X, vector3.Y, 4f, Teinte(170, 80, 255, 0.35f), GameBlendModes.Add);
				Segment(valueOrDefault2.X, valueOrDefault2.Y, vector3.X, vector3.Y, 2f, Teinte(235, 190, 255, 0.95f));
				Lueur(vector3.X, vector3.Y, 9f, Teinte(215, 150, 255, 0.9f));
				BoulesDeKi(valueOrDefault2, vector3, 0.7f + 0.3f * num, now);
			}
			return;
		}
		long num2 = now - telegraph.FiresAt;
		int num3 = ((array.Length != 0) ? array[^1].DelayMs : 0);
		if (array.Length != 0 && num2 >= 0 && num2 <= (long)num3 + 650L && flag)
		{
			Vector2 fin = BoutDuBalayage(carte, array, num2);
			float a = Ph(now, telegraph.FiresAt, telegraph.FiresAt + 90) * (1f - Ph(now, telegraph.LastFiresAt + 650 - 200, telegraph.LastFiresAt + 650));
			if (!attaque.Tir)
			{
				attaque.Tir = true;
				EclairsEcran.Add(new EclairEcran(0.18f, now, 150));
				CombatFeedback.StartShake(2.5f, (long)num3 + 650L);
				Son("kamehameha_ha.wav", valueOrDefault2.X, valueOrDefault2.Y, -0.2f);
				Son("ki_depart.wav", valueOrDefault2.X, valueOrDefault2.Y, -0.3f);
			}
			if (now - attaque.DernierImpact >= 600)
			{
				attaque.DernierImpact = now;
				Son("ki_impact.wav", fin.X, fin.Y, -0.15f);
			}
			RayonDeKi(valueOrDefault2, fin, a, now, array[0].Width * (float)TileWidth / 4.4f);
			if (now - attaque.DerniereEtincelle >= 300)
			{
				attaque.DerniereEtincelle = now;
				EtincellesKi(fin.X, fin.Y, now, (int)(now % 7919), 8, 24f);
			}
		}
	}

	private static void ViseeKi(Vector2 de, Vector2 a, float alpha, long now)
	{
		Pointilles(de.X, de.Y, a.X, a.Y, 6f, 4f, (float)now / 20f, 3f, Teinte(170, 70, 255, 0.25f * alpha));
		Pointilles(de.X, de.Y, a.X, a.Y, 6f, 4f, (float)now / 20f, 1.5f, Teinte(215, 130, 255, alpha * (0.65f + 0.35f * MathF.Sin((float)now / 40f))), GameBlendModes.None);
		Lueur(a.X, a.Y, 7f, Teinte(205, 120, 255, alpha));
	}

	private static void BoulesDeKi(Vector2 mains, Vector2 vers, float force, long now)
	{
		Vector2 vector = vers - mains;
		float num = MathF.Max(1f, vector.Length());
		float num2 = (0f - vector.Y) / num;
		float num3 = vector.X / num;
		float num4 = num2;
		float num5 = 2.5f + 5f * force + 1.2f * MathF.Sin((float)now / 40f);
		for (int i = -1; i <= 1; i += 2)
		{
			float num6 = mains.X + num4 * 5f * (float)i;
			float y = mains.Y + num3 * 5f * (float)i;
			float x = num6;
			Lueur(x, y, num5 * 2.8f, Teinte(180, 75, 255, 0.55f * force));
			Lueur(x, y, num5, Teinte(255, 230, 255, 0.95f * force));
		}
	}

	private static void RayonDeKi(Vector2 debut, Vector2 fin, float a, long now, float l)
	{
		if (a <= 0f)
		{
			return;
		}
		float num = Vector2.Distance(debut, fin);
		if (num < 1f)
		{
			return;
		}
		float num2 = 0.88f + 0.12f * MathF.Sin((float)now / 25f) + 0.06f * MathF.Sin((float)now / 7f);
		Segment(debut.X, debut.Y, fin.X, fin.Y, l * 4.6f, Teinte(140, 45, 230, 0.14f * a), GameBlendModes.Add);
		Segment(debut.X, debut.Y, fin.X, fin.Y, l * 2.7f, Teinte(165, 65, 255, 0.24f * a), GameBlendModes.Add);
		Segment(debut.X, debut.Y, fin.X, fin.Y, l * num2, Teinte(205, 120, 255, 0.9f * a), GameBlendModes.Add);
		Segment(debut.X, debut.Y, fin.X, fin.Y, l * 0.38f, Teinte(255, 238, 255, a), GameBlendModes.Add);
		float num3 = (fin.X - debut.X) / num;
		float num4 = (fin.Y - debut.Y) / num;
		float num5 = num3;
		float num6 = 0f - num4;
		float num7 = num5;
		float num8 = num6;
		Vector2? vector = null;
		Vector2? vector2 = null;
		for (float num9 = 0f; num9 <= num; num9 += 9f)
		{
			float num10 = MathF.Sin(num9 / 17f - (float)now / 45f) * l * 0.6f;
			Vector2 value = new Vector2(debut.X + num5 * num9 + num8 * num10, debut.Y + num4 * num9 + num7 * num10);
			Vector2 value2 = new Vector2(debut.X + num5 * num9 - num8 * num10, debut.Y + num4 * num9 - num7 * num10);
			if (vector.HasValue)
			{
				Vector2 valueOrDefault = vector.GetValueOrDefault();
				if (vector2.HasValue)
				{
					Vector2 valueOrDefault2 = vector2.GetValueOrDefault();
					Segment(valueOrDefault.X, valueOrDefault.Y, value.X, value.Y, 2.2f, Teinte(255, 205, 255, 0.75f * a), GameBlendModes.Add);
					Segment(valueOrDefault2.X, valueOrDefault2.Y, value2.X, value2.Y, 2.2f, Teinte(255, 205, 255, 0.75f * a), GameBlendModes.Add);
				}
			}
			Vector2? vector3 = value;
			vector2 = value2;
			vector = vector3;
		}
		Lueur(debut.X, debut.Y, l * 1.9f, Teinte(230, 155, 255, 0.85f * a));
		Lueur(fin.X, fin.Y, l * 2.5f * num2, Teinte(195, 95, 255, 0.85f * a));
	}

	private static void RafaleDeKi(Dungeon.Telegraph telegraph, Attaque attaque, MapInstance carte, long now)
	{
		DungeonTelegraphShape[] array = telegraph.Packet.Shapes ?? Array.Empty<DungeonTelegraphShape>();
		for (int i = 0; i < array.Length; i++)
		{
			long num = telegraph.FiresAt + array[i].DelayMs;
			Vector2 cible = Monde(carte, array[i].X, array[i].Y);
			if (now >= num)
			{
				if (i >= attaque.BlocsTombes)
				{
					attaque.BlocsTombes = i + 1;
					float taille = array[i].Radius * (float)TileWidth;
					EffetsKi.Add(new EffetKi(GenreKi.Explosion, cible.X, cible.Y, 0f, 0f, taille, now, 520));
					EtincellesKi(cible.X, cible.Y - 4f, now, (int)(now % 7919) + i * 13, 9, 22f);
					Poussiere(cible.X, cible.Y, now, (int)(now % 6007) + i * 7, 5, 14f);
					Secouer(2.2f, 200L, cible.X, cible.Y);
					Son("ki_impact.wav", cible.X, cible.Y, (Hasard((int)(telegraph.ReceivedAt % 7919), i) - 0.5f) * 0.2f);
				}
				continue;
			}
			long num2 = num - 420;
			if (now < num2)
			{
				continue;
			}
			Vector2? vector = MainsDe(telegraph.Packet.BossId, cible);
			Vector2 mains;
			if (vector.HasValue)
			{
				mains = vector.GetValueOrDefault();
				if (i >= attaque.Lances)
				{
					attaque.Lances = i + 1;
					Son("kiblast.wav", mains.X, mains.Y, (Hasard((int)(telegraph.ReceivedAt % 6007), i) - 0.5f) * 0.25f);
					EntiteDe(telegraph.Packet.BossId)?.PlaySpellPose(320L);
				}
				float num3 = Ph(now, num2, num);
				Vector2 vector2 = Ou(num3);
				for (int j = 1; j <= 5; j++)
				{
					Vector2 vector3 = Ou(MathF.Max(0f, num3 - (float)j * 0.05f));
					Lueur(vector3.X, vector3.Y, 14f - (float)j * 2f, Teinte(175, 80, 255, 0.55f * (1f - (float)j / 6f)));
				}
				Lueur(vector2.X, vector2.Y, 26f, Teinte(180, 80, 255, 0.75f));
				Lueur(vector2.X, vector2.Y, 11f, Teinte(235, 170, 255, 0.95f));
				Lueur(vector2.X, vector2.Y, 5f, Teinte(255, 245, 255, 1f));
			}
			Vector2 Ou(float u)
			{
				return Vector2.Lerp(mains, cible, u) - new Vector2(0f, MathF.Sin(u * (float)Math.PI) * 70f);
			}
		}
	}

	private static void Kiai(Dungeon.Telegraph telegraph, Attaque attaque, MapInstance carte, long now)
	{
		DungeonTelegraphShape[] array = telegraph.Packet.Shapes ?? Array.Empty<DungeonTelegraphShape>();
		if (array.Length != 0)
		{
			Vector2 vector = Monde(carte, array[0].X, array[0].Y);
			float num = array[0].Radius * (float)TileWidth;
			if (now >= telegraph.FiresAt && !attaque.Choc)
			{
				attaque.Choc = true;
				EffetsKi.Add(new EffetKi(GenreKi.Onde, vector.X, vector.Y, 0f, 0f, num, now, 460));
				EffetsKi.Add(new EffetKi(GenreKi.Onde, vector.X, vector.Y, 0f, 0f, num * 0.7f, now + 90, 420));
				EffetsKi.Add(new EffetKi(GenreKi.Explosion, vector.X, vector.Y - 14f, 0f, 0f, 26f, now, 260));
				EclairsEcran.Add(new EclairEcran(0.16f, now, 160));
				Secouer(4f, 360L, vector.X, vector.Y);
				Poussiere(vector.X, vector.Y, now, (int)(now % 7919), 12, (int)num);
				EtincellesKi(vector.X, vector.Y - 10f, now, (int)(now % 6007), 14, num * 0.8f);
				Son("explosion.wav", vector.X, vector.Y, -0.1f);
			}
		}
	}

	private static void Ruee(Dungeon.Telegraph telegraph, Attaque attaque, MapInstance carte, long now)
	{
		DungeonTelegraphShape[] array = telegraph.Packet.Shapes ?? Array.Empty<DungeonTelegraphShape>();
		if (array.Length != 0)
		{
			Entity entity = EntiteDe(telegraph.Packet.BossId);
			if (entity != null && now >= telegraph.FiresAt && !attaque.Tir)
			{
				attaque.Tir = true;
				DungeonTelegraphShape dungeonTelegraphShape = array[0];
				Vector2 vector = Monde(carte, dungeonTelegraphShape.X, dungeonTelegraphShape.Y);
				Vector2 vector2 = BoutDe(carte, dungeonTelegraphShape);
				EffetsKi.Add(new EffetKi(GenreKi.Trainee, vector.X, vector.Y - 16f, vector2.X, vector2.Y - 16f, dungeonTelegraphShape.Width * (float)TileWidth * 0.8f, now, 460));
				Poussiere(vector.X, vector.Y, now, (int)(now % 7919), 8, 20f);
				Son("ki_depart.wav", vector.X, vector.Y, 0.25f);
			}
		}
	}

	private static void ColonnesDeKi(Dungeon.Telegraph telegraph, Attaque attaque, long now)
	{
		if (now < telegraph.FiresAt || attaque.Fissures == null)
		{
			return;
		}
		if (!attaque.Embrasement)
		{
			attaque.Embrasement = true;
			EclairsEcran.Add(new EclairEcran(0.1f, now, 160));
			CombatFeedback.StartShake(3f, 400L);
			if (attaque.Fissures.Length != 0 && attaque.Fissures[0].Length != 0)
			{
				Son("explosion.wav", attaque.Fissures[0][0].X, attaque.Fissures[0][0].Y, -0.2f);
			}
			int num = (int)(now % 7919);
			Vector2[][] fissures = attaque.Fissures;
			foreach (Vector2[] array in fissures)
			{
				for (int j = 4; j < array.Length; j += 8)
				{
					EtincellesKi(array[j].X, array[j].Y - 6f, now, num + j, 4, 16f);
				}
			}
		}
		float num2 = 1f - Ph(now, telegraph.FiresAt, telegraph.FiresAt + 900);
		if (num2 <= 0f)
		{
			return;
		}
		for (int k = 0; k < attaque.Fissures.Length; k++)
		{
			Vector2[] array2 = attaque.Fissures[k];
			for (int l = 2; l < array2.Length; l += 2)
			{
				float num3 = (30f + Hasard(70 + k, l) * 34f) * num2;
				float num4 = 9f + Hasard(170 + k, l) * 6f;
				Colonne(array2[l].X, array2[l].Y, num4, num3, Teinte(190, 100, 255, 0.9f * num2));
				Colonne(array2[l].X, array2[l].Y, num4 * 0.4f, num3 * 0.8f, Teinte(255, 225, 255, 0.7f * num2));
			}
		}
	}

	private static void DessinerLesEffetsKi(long now)
	{
		for (int num = EffetsKi.Count - 1; num >= 0; num--)
		{
			EffetKi effetKi = EffetsKi[num];
			long num2 = now - effetKi.Ne;
			if (num2 >= effetKi.VieMs)
			{
				EffetsKi.RemoveAt(num);
			}
			else if (num2 >= 0)
			{
				float num3 = (float)num2 / (float)effetKi.VieMs;
				float num4 = 1f - num3;
				switch (effetKi.Genre)
				{
				case GenreKi.Onde:
				{
					float num9 = MathF.Max(4f, effetKi.Taille * (1f - (1f - num3) * (1f - num3)));
					Ellipse(effetKi.X, effetKi.Y, num9 * 0.95f, num9 * 0.52f, 12f * num4 + 1f, Teinte(165, 70, 255, 0.4f * num4));
					Ellipse(effetKi.X, effetKi.Y, num9, num9 * 0.55f, 5f * num4 + 1.5f, Teinte(240, 185, 255, 0.95f * num4));
					break;
				}
				case GenreKi.Explosion:
				{
					float num8 = effetKi.Taille * (0.45f + 0.85f * Lisse(MathF.Min(1f, num3 * 2f)));
					Lueur(effetKi.X, effetKi.Y, num8 * 1.9f, Teinte(185, 80, 255, 0.85f * num4));
					Lueur(effetKi.X, effetKi.Y, num8 * 0.9f, Teinte(255, 232, 255, num4 * num4));
					Ellipse(effetKi.X, effetKi.Y, num8, num8 * 0.5f, 4f * num4 + 1.5f, Teinte(235, 170, 255, 0.9f * num4));
					break;
				}
				case GenreKi.Trainee:
				{
					Vector2 value = new Vector2(effetKi.X, effetKi.Y);
					Vector2 value2 = new Vector2(effetKi.X2, effetKi.Y2);
					float num5 = MathF.Max(1f, Vector2.Distance(value, value2));
					for (float num6 = 0f; num6 <= num5; num6 += 12f)
					{
						float num7 = num6 / num5;
						Vector2 vector = Vector2.Lerp(value, value2, num7);
						Lueur(vector.X, vector.Y, effetKi.Taille * (0.7f + 0.3f * num4), Teinte(175, 75, 255, 0.28f * num4 * (0.25f + 0.75f * num7)));
					}
					Segment(effetKi.X, effetKi.Y, effetKi.X2, effetKi.Y2, 2.5f, Teinte(235, 185, 255, 0.45f * num4), GameBlendModes.Add);
					break;
				}
				}
			}
		}
	}

	private static void AuraSaiyan(Entity boss, FloatRect corps)
	{
		long maintenant = Maintenant;
		float num = ForceDeLAura(boss.Id, maintenant);
		if (!(num <= 0.01f))
		{
			float num2 = corps.X + corps.Width / 2f;
			float num3 = corps.Y + corps.Height;
			Lueur(num2, num3 - corps.Height * 0.45f, corps.Height * 0.75f, Teinte(160, 60, 255, 0.35f * num));
			for (int i = 0; i < 5; i++)
			{
				float num4 = ((float)maintenant / 260f + (float)i * 0.37f) % 1f;
				float x = num2 + (float)(i - 2) * corps.Width * 0.18f;
				float hauteur = corps.Height * (0.75f + 0.45f * MathF.Sin(num4 * (float)Math.PI)) * num;
				Colonne(x, num3 - 2f, corps.Width * 0.35f, hauteur, Teinte(190, 100, 255, 0.45f * num * (1f - num4 * 0.5f)));
			}
		}
	}

	private static float ForceDeLAura(Guid bossId, long now)
	{
		float num = 0f;
		foreach (Dungeon.Telegraph activeTelegraph in Dungeon.ActiveTelegraphs)
		{
			bool flag = activeTelegraph.Packet.BossId != bossId || now >= activeTelegraph.FiresAt;
			bool flag2 = flag;
			if (!flag2)
			{
				BossAbilityKind kind = activeTelegraph.Kind;
				bool flag3 = ((kind == BossAbilityKind.Charge || kind - 2 <= BossAbilityKind.Shockwave) ? true : false);
				flag2 = !flag3;
			}
			if (!flag2)
			{
				num = MathF.Max(num, 0.35f + 0.65f * Ph(now, activeTelegraph.ReceivedAt, activeTelegraph.FiresAt));
			}
		}
		return num;
	}

	private static void LumieresSaiyan(Dungeon.Telegraph telegraph, MapInstance carte, long now)
	{
		if (now < telegraph.FiresAt)
		{
			Entity entity = EntiteDe(telegraph.Packet.BossId);
			if (entity != null)
			{
				float num = ForceDeLAura(entity.Id, now);
				if (num > 0.01f)
				{
					Graphics.AddLight((int)(entity.WorldPos.X + entity.WorldPos.Width / 2f), (int)(entity.WorldPos.Y + entity.WorldPos.Height * 0.6f), 52, (byte)(150f * num), 0f, Color.FromArgb(255, 180, 90, 255));
				}
			}
		}
		DungeonTelegraphShape[] array = telegraph.Packet.Shapes ?? Array.Empty<DungeonTelegraphShape>();
		if (telegraph.Kind != BossAbilityKind.EyeBeam || array.Length == 0)
		{
			return;
		}
		long num2 = now - telegraph.FiresAt;
		if (num2 < 0 || num2 > (long)array[^1].DelayMs + 650L)
		{
			return;
		}
		Vector2? vector = MainsDe(telegraph.Packet.BossId);
		if (vector.HasValue)
		{
			Vector2 valueOrDefault = vector.GetValueOrDefault();
			Vector2 value = BoutDuBalayage(carte, array, num2);
			float num3 = Vector2.Distance(valueOrDefault, value);
			for (float num4 = 24f; num4 < num3; num4 += 80f)
			{
				Vector2 vector2 = Vector2.Lerp(valueOrDefault, value, num4 / MathF.Max(1f, num3));
				Graphics.AddLight((int)vector2.X, (int)vector2.Y, 30, 110, 0f, Color.FromArgb(255, 190, 90, 255));
			}
		}
	}

	private static void LumieresDesEffetsKi(long now)
	{
		foreach (EffetKi item in EffetsKi)
		{
			long num = now - item.Ne;
			if (item.Genre == GenreKi.Explosion && num >= 0 && num < item.VieMs)
			{
				Graphics.AddLight((int)item.X, (int)item.Y, (int)(item.Taille * 1.4f), (byte)(160f * (1f - (float)num / (float)item.VieMs)), 0f, Color.FromArgb(255, 190, 90, 255));
			}
		}
	}

	private static void AnnonceSaiyan(DungeonTelegraphPacket packet, Entity boss)
	{
		float x = boss.Origin.X;
		float y = boss.Origin.Y;
		float x2 = x;
		DungeonTelegraphShape[] shapes = packet.Shapes;
		int num = ((shapes != null && shapes.Length > 0) ? shapes.Max((DungeonTelegraphShape forme) => forme.DelayMs) : 0);
		switch ((BossAbilityKind)packet.Kind)
		{
		case BossAbilityKind.EyeBeam:
			boss.PlaySpellPose(packet.DurationMs + 1600);
			Son("kamehameha_charge.wav", x2, y, -0.15f);
			break;
		case BossAbilityKind.RockFall:
			boss.PlaySpellPose(packet.DurationMs + num + 300);
			break;
		case BossAbilityKind.Shockwave:
			boss.PlaySpellPose(packet.DurationMs + 250);
			Son("Aura_intro.wav", x2, y);
			break;
		case BossAbilityKind.Fissures:
			boss.JouerLeGeste(0);
			Son("heritage_falaise_1.wav", x2, y, -0.2f);
			break;
		}
	}
}
