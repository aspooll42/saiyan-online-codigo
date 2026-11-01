using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Events;

namespace Intersect.Client.Interface.Game;

public static class NamekFinal
{
	private sealed class P
	{
		public string Type = "";

		public float X;

		public float Y;

		public float Vx;

		public float Vy;

		public float Vie;

		public float Age;

		public float G;

		public float R;

		public float X2;

		public float Y2;

		public float Largeur;

		public float Hmax;

		public float SolY;

		public Color C = Color.White;

		public string Anim = "";

		public int Col;

		public int Row;
	}

	private sealed class Onde
	{
		public float X;

		public float Y;

		public float Rmax;

		public float Duree;

		public float Age;

		public Color C = Color.White;
	}

	private sealed class Explo
	{
		public float X;

		public float Y;

		public float R;

		public float Duree;

		public float Age;
	}

	private sealed class Cratere
	{
		public float X;

		public float Y;

		public float R;

		public float Age;
	}

	private const float Duree = 35f;

	private const int Droite = 2;

	private const int Gauche = 1;

	private const float Ecart = 140f;

	private const float Recul = 48f;

	private const float CapsuleX = 260f;

	private const float CapsuleSol = 34f;

	private const float TVaincu = 19.5f;

	private const float TFuite = 21.5f;

	private const float TCapsule = 25f;

	private const float TDecollage = 28.5f;

	private const float TExplosion = 31.5f;

	private static bool _actif;

	private static float _t;

	private static float _dt;

	private static long _dernier = -1L;

	private static float _ax;

	private static float _ay;

	private static readonly Random Hasard = new Random();

	private static readonly List<P> Parts = new List<P>();

	private static readonly List<Onde> Ondes = new List<Onde>();

	private static readonly List<Explo> Explosions = new List<Explo>();

	private static readonly List<Cratere> Crateres = new List<Cratere>();

	private static readonly List<(List<(float X, float Y)> Pts, float P)> Fissures = new List<(List<(float, float)>, float)>();

	private static float _flash;

	private static List<(float X, float Y)>? _eclair;

	private static float _eclairAge;

	private static float _prochainGeyser;

	private static float _prochainMeteore;

	private static float _prochainEclair;

	private static float _prochainTir;

	private static float _prochainImpact;

	private static float _prochainHoquet;

	private static float _hoquetFin;

	private static float _telX;

	private static float _telY;

	private static readonly HashSet<string> SonsJoues = new HashSet<string>();

	private static string _gAnim = "Goku_ssj1_idle";

	private static string _fAnim = "Freezer_idle";

	private static int _gCol;

	private static int _gRow = 2;

	private static int _fCol;

	private static int _fRow = 1;

	private static float _gx;

	private static float _fx;

	private static float _gHaut;

	private static float _fHaut;

	private static float _gSol;

	private static float _gAlpha = 1f;

	private static float _auraG = 0.5f;

	private static float _auraF = 0.35f;

	private static bool _traineeG;

	private static bool _traineeF;

	private static bool _fVisible = true;

	private static bool _gDerriere;

	private static float _choc;

	private static float _chocY;

	private static float _bouleR = -1f;

	private static float _capHaut;

	private static float _capJet;

	private static float _capTremble;

	private static float _capHublot;

	private static IGameTexture? _pixel;

	private static float _ox;

	private static float _oy;

	private static float _s = 1f;

	private static readonly string[] CapsulePixels = new string[35]
	{
		"............ooooo.............", ".........ooollllmooo..........", ".......oolllllllllmmoo........", "......olwwlllllllllmmmo.......", ".....owwwwwwlllllllmmmdo......", "....owwwwwwwwllllolmmmmdo.....", "...owwwwwwwwwlooooooommddo....", "..olwwwwwwwwloohhhhvoomddeo...", "..olwwwwwwwlohhhhhvvvvoddeo...", ".ollwwwwwwloohhkkvvvvvoodeeo..",
		".ollwwwwwllohhhkvvvvvvgodeeo..", ".olllwwllllohhhvvvvvvggodeeo..", "ollllllllllohhvvvvvvgggoeeeeo.", "ollllllllllohvvvvvvggggoeeeeo.", "olllllllllloovvvvvggggooeeeeo.", "omlllllllllmovvvvgggggoeeeeeo.", "ommlllllmmmmmoovggggooeeeeeeo.", ".ommmmmmmmmmmmoooooooeeeeeeo..", ".ommmmmmmmmmmmmddoddeeeeeeeo..", ".orrrrrrrrrrrrrrrqqqqqqqqqqo..",
		"..orrrrrrrrrrrrqqqqqqqqqqqo...", "..odddmdddddddddeeeeeeeeeeo...", "...odddddddddddeeeeeeeeeeo....", "....odddddddeeeeeeeeeeeebno...", "....obeeeeeeeeeeeeeeeeeobno...", "....obneeeeeeeeeeeeeeeoobno...", "....obnooeeeeeeeeeeeoo.obno...", "....obno.oooeeeeeooo...obno...", "....obno....oobno......obno...", "....obno.....obno......obno...",
		"....obno.....obno......obno...", "...obbnno....obno.....obbnno..", "...obbnno...obbnno....obbnno..", "....oooo....obbnno.....oooo...", ".............oooo............."
	};

	private static IFont? _texte;

	public static bool Actif => _actif;

	private static FloatRect Vue => Graphics.WorldViewport;

	public static bool Commande(string texte)
	{
		string text = texte.Trim().ToLowerInvariant();
		if ((!(text == "/namek") && !(text == "/namek stop")) || 1 == 0)
		{
			return false;
		}
		Player me = Globals.Me;
		if (me == null || me.AccessLevel == Access.None)
		{
			return false;
		}
		if (text == "/namek stop")
		{
			_actif = false;
			return true;
		}
		_ax = me.Origin.X;
		_ay = me.Origin.Y - 48f;
		Recommencer();
		_actif = true;
		_dernier = -1L;
		return true;
	}

	private static void Recommencer()
	{
		_t = 0f;
		Parts.Clear();
		Ondes.Clear();
		Explosions.Clear();
		Crateres.Clear();
		Fissures.Clear();
		_flash = 0f;
		_eclair = null;
		_prochainGeyser = 0f;
		_prochainMeteore = 0.5f;
		_prochainEclair = 1.5f;
		_prochainTir = 7.8f;
		_prochainImpact = 0f;
		_prochainHoquet = 25.8f;
		_hoquetFin = 0f;
		_telX = -140f;
		_telY = 0f;
		SonsJoues.Clear();
		Random random = new Random(11);
		for (int i = 0; i < 26; i++)
		{
			float num = _ax - 360f + (float)random.Next(720);
			float num2 = _ay - 170f + (float)random.Next(420);
			List<(float, float)> list = new List<(float, float)> { (num, num2) };
			int num3 = 4 + random.Next(6);
			for (int j = 0; j < num3; j++)
			{
				num += (float)(8 + random.Next(20) * ((random.NextDouble() < 0.5) ? 1 : (-1)));
				num2 += (float)(6 + random.Next(14) * ((random.NextDouble() < 0.5) ? 1 : (-1)));
				list.Add((num, num2));
			}
			Fissures.Add((list, (float)random.NextDouble() * 6f));
		}
	}

	private static float Entre(float a, float b)
	{
		return a + (float)Hasard.NextDouble() * (b - a);
	}

	private static bool Chance(float parSeconde)
	{
		return Hasard.NextDouble() < (double)(parSeconde * _dt);
	}

	private static bool Passe(float instant)
	{
		if (_t >= instant)
		{
			return _t - _dt < instant;
		}
		return false;
	}

	private static float Lisse(float k)
	{
		return k * k * (3f - 2f * k);
	}

	private static float Bruit(float x, float y)
	{
		float num = MathF.Sin(x * 12.9898f + y * 78.233f) * 43758.547f;
		return num - MathF.Floor(num);
	}

	private static float Phase(float a, float b)
	{
		return Math.Clamp((_t - a) / (b - a), 0f, 1f);
	}

	private static Color Rgb(int r, int g, int b, float a = 1f)
	{
		return new Color((int)Math.Clamp(a * 255f, 0f, 255f), r, g, b);
	}

	private static Color Hex(uint rgb, float a = 1f)
	{
		return Rgb((int)((rgb >> 16) & 0xFF), (int)((rgb >> 8) & 0xFF), (int)(rgb & 0xFF), a);
	}

	private static void Ajouter(P p)
	{
		if (Parts.Count < 1400)
		{
			Parts.Add(p);
		}
	}

	private static (float X, float Y) SolAuHasard()
	{
		FloatRect vue = Vue;
		return (X: Entre(vue.X, vue.X + vue.Width), Y: Entre(vue.Y + vue.Height * 0.2f, vue.Y + vue.Height));
	}

	private static void Eclaboussure(float x, float y, int n, float force)
	{
		for (int i = 0; i < n; i++)
		{
			Ajouter(new P
			{
				Type = "lave",
				X = x,
				Y = y,
				SolY = y + Entre(-4f, 6f),
				Vx = Entre(-40f, 40f) * force,
				Vy = Entre(-120f, -50f) * force,
				Vie = Entre(0.6f, 1.3f),
				G = 180f
			});
		}
	}

	private static void Etincelles(float x, float y, int n, Color c)
	{
		for (int i = 0; i < n; i++)
		{
			float x2 = Entre(0f, (float)Math.PI * 2f);
			float num = Entre(40f, 150f);
			Ajouter(new P
			{
				Type = "etincelle",
				X = x,
				Y = y,
				Vx = MathF.Cos(x2) * num,
				Vy = MathF.Sin(x2) * num,
				Vie = Entre(0.15f, 0.45f),
				C = c
			});
		}
	}

	private static void Colonne(float x, float y, float hMax)
	{
		Ajouter(new P
		{
			Type = "colonne",
			X = x,
			Y = y,
			Vie = Entre(1.2f, 2.2f),
			Largeur = Entre(10f, 22f),
			Hmax = hMax
		});
		Crateres.Add(new Cratere
		{
			X = x,
			Y = y,
			R = 10f
		});
	}

	private static void Tremble(float force, long ms)
	{
		CombatFeedback.StartShake(force, ms);
	}

	private static void Explosion(float x, float y, float r, float duree)
	{
		Explosions.Add(new Explo
		{
			X = x,
			Y = y,
			R = r,
			Duree = duree
		});
		Tremble(MathF.Min(6f, r / 8f), (long)(180f + r * 4f));
	}

	private static void Son(string nom)
	{
		if (SonsJoues.Add(nom + (int)(_t * 4f)))
		{
			Player me = Globals.Me;
			if (me != null && Globals.ContentManager.GetSound(Path.GetFileNameWithoutExtension(nom)) != null)
			{
				Audio.AddMapSound(nom, me.X, me.Y, me.MapId, loop: false, 0, 30, me);
			}
		}
	}

	private static void Monde(GameRenderer renderer)
	{
		_pixel = renderer.WhitePixel;
		_ox = 0f;
		_oy = 0f;
		_s = 1f;
	}

	private static void Fill(float x, float y, float w, float h, Color c, bool add = false)
	{
		if (_pixel != null && !(w <= 0f) && !(h <= 0f) && c.A > 0)
		{
			Graphics.DrawGameTexture(_pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(_ox + x * _s), MathF.Round(_oy + y * _s), MathF.Ceiling(w * _s), MathF.Ceiling(h * _s)), c, null, (!add) ? GameBlendModes.None : GameBlendModes.Add);
		}
	}

	private static void Triangle(float xa, float y, float xp, float yp, float xb, Color c, bool add)
	{
		float num = y - yp;
		if (!(num < 1f))
		{
			for (float num2 = 0f; num2 < num; num2 += 2f)
			{
				float num3 = num2 / num;
				float num4 = xp + (xa - xp) * num3;
				float num5 = xp + (xb - xp) * num3;
				Fill(MathF.Min(num4, num5), yp + num2, MathF.Max(1f, MathF.Abs(num5 - num4)), 2f, c, add);
			}
		}
	}

	private static void Disque(float cx, float cy, float r, Color c, bool add, float aplatir = 1f)
	{
		float num = MathF.Max(1f, r * aplatir);
		for (float num2 = 0f - num; num2 <= num; num2 += 2f)
		{
			float num3 = r * MathF.Sqrt(MathF.Max(0f, 1f - num2 * num2 / (num * num)));
			Fill(cx - num3, cy + num2, num3 * 2f, 2f, c, add);
		}
	}

	private static void Trait(float x1, float y1, float x2, float y2, float epaisseur, Color c, bool add)
	{
		int num = (int)MathF.Max(MathF.Abs(x2 - x1), MathF.Abs(y2 - y1));
		for (int i = 0; i <= num; i += 2)
		{
			float num2 = ((num == 0) ? 0f : ((float)i / (float)num));
			Fill(x1 + (x2 - x1) * num2 - epaisseur / 2f, y1 + (y2 - y1) * num2 - epaisseur / 2f, MathF.Max(2f, epaisseur), MathF.Max(2f, epaisseur), c, add);
		}
	}

	private static void Sprite(string planche, int col, int row, float x, float pieds, float alpha = 1f)
	{
		if (!(alpha <= 0f))
		{
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Entity, planche);
			if (texture != null)
			{
				Graphics.DrawGameTexture(texture, new FloatRect(col * 32, row * 48, 32f, 48f), new FloatRect(MathF.Round(x - 16f), MathF.Round(pieds - 46f), 32f, 48f), Rgb(255, 255, 255, alpha));
			}
		}
	}

	private static void Aura(float x, float pieds, uint[] couleurs, float force, float alpha = 1f)
	{
		for (int i = 0; i < 13; i++)
		{
			float num = (float)i / 12f - 0.5f;
			float num2 = x + num * 20f;
			float num3 = (22f + 16f * force) * MathF.Max(0.25f, 1f - MathF.Abs(num) * 1.3f) * (0.55f + 0.45f * MathF.Abs(MathF.Sin(_t * 11f + (float)i * 2.3f)));
			float xp = num2 + num * 7f + MathF.Sin(_t * 9f + (float)i) * 2f;
			float a = MathF.Min(0.85f, (0.3f + 0.2f * force) * (1.1f - MathF.Abs(num))) * alpha;
			Triangle(num2 - 1.6f, pieds + 1f, xp, pieds - num3, num2 + 1.6f, Hex(couleurs[i % couleurs.Length], a), add: true);
		}
	}

	private static void Flammes(float x0, float x1, float y0, float pas, float hMax, float alpha)
	{
		if (!(hMax < 1f))
		{
			for (float num = x0 - pas; num < x1 + pas; num += pas)
			{
				float num2 = hMax * (0.3f + 0.7f * MathF.Abs(MathF.Sin(_t * 6f + num * 0.37f) * MathF.Cos(_t * 4.3f + num * 0.11f)));
				float xp = num + MathF.Sin(_t * 8f + num) * 2f;
				Triangle(num - pas * 0.75f, y0, xp, y0 - num2, num + pas * 0.75f, Hex(16730640u, alpha), add: true);
				Triangle(num - pas * 0.3f, y0, xp, y0 - num2 * 0.55f, num + pas * 0.3f, Hex(16761402u, alpha * 0.9f), add: true);
			}
		}
	}

	private static void Boule(float x, float y, float r, uint[] couleurs)
	{
		for (int i = 0; i < couleurs.Length; i++)
		{
			float y2 = r * (1f - (float)i / (float)couleurs.Length) + MathF.Sin(_t * 20f + (float)i) * 0.8f;
			Disque(x, y, MathF.Max(1f, y2), Hex(couleurs[i], 0.5f + (float)i * 0.12f), add: true);
		}
	}

	private static uint CouleurCapsule(char c)
	{
		return c switch
		{
			'o' => 2301998u, 
			'e' => 4869730u, 
			'd' => 7765138u, 
			'm' => 11449542u, 
			'l' => 14475498u, 
			'w' => 16448511u, 
			'q' => 5902360u, 
			'r' => 10100264u, 
			'g' => 2755646u, 
			'v' => 6168192u, 
			'h' => 10111168u, 
			'k' => 15915263u, 
			'b' => 3816012u, 
			_ => 6974592u, 
		};
	}

	private static void Capsule(float x, float pieds, float hublot)
	{
		float num = MathF.Round(x - 15f);
		float num2 = MathF.Round(pieds - 34f);
		for (int i = 0; i < CapsulePixels.Length; i++)
		{
			string text = CapsulePixels[i];
			int num3 = 0;
			while (num3 < text.Length)
			{
				char c = text[num3];
				int j;
				for (j = num3 + 1; j < text.Length && text[j] == c; j++)
				{
				}
				if (c != '.')
				{
					Fill(num + (float)num3, num2 + (float)i, j - num3, 1f, Hex(CouleurCapsule(c)));
					bool flag = hublot > 0f;
					bool flag2 = flag;
					if (flag2)
					{
						bool flag3 = ((c == 'g' || c == 'h' || c == 'v') ? true : false);
						flag2 = flag3;
					}
					if (flag2)
					{
						Fill(num + (float)num3, num2 + (float)i, j - num3, 1f, Hex(6340863u, 0.45f * hublot), add: true);
					}
				}
				num3 = j;
			}
		}
	}

	private static void Jet(float x, float y, float longueur)
	{
		if (longueur < 2f)
		{
			return;
		}
		for (float num = 0f; num < longueur; num += 2f)
		{
			float num2 = num / longueur;
			float num3 = (12f - 9f * num2) * (0.8f + 0.2f * MathF.Sin(_t * 50f + num));
			Fill(x - num3 / 2f, y + num, num3, 2f, Hex(16734740u, 0.7f * (1f - num2)), add: true);
			Fill(x - num3 / 4f, y + num, num3 / 2f, 2f, Hex(16761418u, 0.9f * (1f - num2)), add: true);
			if (num2 < 0.45f)
			{
				Fill(x - num3 / 8f, y + num, MathF.Max(2f, num3 / 4f), 2f, Hex(16774864u, 1f - num2 * 2f), add: true);
			}
		}
	}

	private static void Avancer()
	{
		long milliseconds = Timing.Global.Milliseconds;
		if (_dernier == milliseconds)
		{
			return;
		}
		float num = ((_dernier < 0) ? 0f : Math.Clamp((float)(milliseconds - _dernier) / 1000f, 0f, 0.05f));
		_dernier = milliseconds;
		_dt = num;
		_t += num;
		if (_t >= 35f)
		{
			_actif = false;
			return;
		}
		float num2 = MathF.Min(1f, 0.35f + _t / 35f);
		FloatRect vue = Vue;
		if (_t >= _prochainGeyser)
		{
			var (x, y) = SolAuHasard();
			Eclaboussure(x, y, (int)(10f + 20f * num2), 0.8f + num2 * 0.7f);
			_prochainGeyser = _t + Entre(0.15f, 0.6f) * (1.3f - num2);
		}
		if (_t >= _prochainMeteore)
		{
			(float X, float Y) tuple2 = SolAuHasard();
			float item = tuple2.X;
			float item2 = tuple2.Y;
			float num3 = Entre(40f, 90f);
			float num4 = Entre(160f, 240f);
			float num5 = (item2 - (vue.Y - 30f)) / num4;
			Ajouter(new P
			{
				Type = "meteore",
				X = item - num3 * num5,
				Y = vue.Y - 30f,
				Vx = num3,
				Vy = num4,
				SolY = item2,
				Vie = num5 + 0.1f
			});
			_prochainMeteore = _t + Entre(0.3f, 1f) * (1.4f - num2);
		}
		if (_t >= _prochainEclair)
		{
			(float X, float Y) tuple3 = SolAuHasard();
			float item3 = tuple3.X;
			float item4 = tuple3.Y;
			List<(float, float)> list = new List<(float, float)>();
			float num6 = item3 + Entre(-60f, 60f);
			for (float num7 = vue.Y - 10f; num7 < item4; num7 += Entre(10f, 22f))
			{
				list.Add((num6, num7));
				num6 += Entre(-14f, 14f) + (item3 - num6) * 0.15f;
			}
			list.Add((item3, item4));
			_eclair = list;
			_eclairAge = 0f;
			_flash = MathF.Max(_flash, 0.22f);
			Crateres.Add(new Cratere
			{
				X = item3,
				Y = item4,
				R = 6f
			});
			Son((Hasard.NextDouble() < 0.5) ? "meteo_tonnerre_proche_1.wav" : "eclair.wav");
			_prochainEclair = _t + Entre(1.2f, 3.2f);
		}
		float parSeconde = ((_t >= 31.5f) ? 6f : ((_t >= 25f) ? 3.5f : ((_t >= 19.5f) ? 2.5f : ((_t >= 15f) ? 0.8f : ((_t >= 7f) ? 0.25f : 0f)))));
		if (Chance(parSeconde))
		{
			var (x2, y2) = SolAuHasard();
			Colonne(x2, y2, Entre(60f, (_t >= 19.5f) ? 220 : 130));
			Tremble(2f, 260L);
		}
		for (int num8 = (int)((double)(num * 70f) + Hasard.NextDouble()); num8 > 0; num8--)
		{
			Ajouter(new P
			{
				Type = "braise",
				X = Entre(vue.X, vue.X + vue.Width),
				Y = vue.Y + vue.Height + 2f,
				Vx = Entre(-8f, 8f),
				Vy = Entre(-60f, -20f),
				Vie = Entre(3f, 6f)
			});
		}
		if (Chance(20f))
		{
			var (x3, y3) = SolAuHasard();
			Ajouter(new P
			{
				Type = "fumee",
				X = x3,
				Y = y3,
				Vx = Entre(-6f, 6f),
				Vy = Entre(-16f, -6f),
				Vie = Entre(3f, 5f),
				R = Entre(10f, 22f)
			});
		}
		int num9 = (int)(_t * 8f) % 16;
		float num10 = 10f + MathF.Sin(_t * 2.2f) * 3f;
		_gx = -140f;
		_fx = 140f;
		_gHaut = num10;
		_fHaut = num10 + 2f;
		_gSol = 0f;
		_gAlpha = 1f;
		_gDerriere = false;
		_traineeG = false;
		_traineeF = false;
		_auraG = 0.5f;
		_auraF = 0.35f;
		_fVisible = true;
		_bouleR = -1f;
		_capHaut = 0f;
		_capJet = 0f;
		_capTremble = 0f;
		_capHublot = 0f;
		_gAnim = "Goku_ssj1_idle";
		_gCol = num9;
		_gRow = 2;
		_fAnim = "Freezer_idle";
		_fCol = num9;
		_fRow = 1;
		if (_t >= 3f && _t < 4.2f)
		{
			float num11 = Lisse(Phase(3f, 4.2f));
			_gx = -140f + 126f * num11;
			_gHaut = num10 + MathF.Sin(num11 * (float)Math.PI) * 16f;
			int gCol = 4 + (int)(_t * 12f) % 4;
			_gAnim = "Goku_ssj1_vol";
			_gCol = gCol;
			_gRow = 2;
			_traineeG = true;
			_fx = 140f - 126f * num11;
			_fHaut = num10 + MathF.Sin(num11 * (float)Math.PI) * 12f;
			_fAnim = "Freezer_attack1";
			_fCol = 0;
			_fRow = 1;
			_traineeF = true;
			_auraG = 0.8f;
			Son("ki_depart.wav");
		}
		else if (_t >= 4.2f && _t < 7f)
		{
			_gx = -14f + MathF.Sin(_t * 11f) * 2f;
			_fx = 14f + MathF.Sin(_t * 11f + 1f) * 2f;
			_gHaut = 18f;
			_fHaut = 18f;
			int num12 = (int)((_t - 4.2f) / 0.11f);
			string gAnim = ((num12 % 8 < 4) ? "Goku_ssj1_attack1" : "Goku_ssj1_attack2");
			int gCol2 = num12 % 4;
			_gAnim = gAnim;
			_gCol = gCol2;
			_gRow = 2;
			int fCol = (num12 + 2) % 4;
			_fAnim = "Freezer_attack2";
			_fCol = fCol;
			_fRow = 1;
			_auraG = 0.9f;
			_auraF = 0.6f;
			if (_t >= _prochainImpact)
			{
				float y4 = _ay - 18f - 26f + Entre(-6f, 6f);
				Etincelles(_ax, y4, 12, (Hasard.NextDouble() < 0.5) ? Hex(16774064u) : Hex(14722303u));
				if (Hasard.NextDouble() < 0.35)
				{
					Ondes.Add(new Onde
					{
						X = _ax,
						Y = _ay,
						Rmax = 40f,
						Duree = 0.35f,
						C = Hex(16773328u)
					});
				}
				Tremble(1.5f, 120L);
				_prochainImpact = _t + 0.11f;
				Son("ki_impact.wav");
			}
		}
		else if (_t >= 7f && _t < 7.6f)
		{
			float num13 = Lisse(Phase(7f, 7.6f));
			if (Passe(7f))
			{
				Ondes.Add(new Onde
				{
					X = _ax,
					Y = _ay,
					Rmax = 180f,
					Duree = 0.7f,
					C = Color.White
				});
				Explosion(_ax, _ay - 44f, 40f, 0.5f);
				_flash = MathF.Max(_flash, 0.55f);
				Etincelles(_ax, _ay - 44f, 50, Hex(16774064u));
				Crateres.Add(new Cratere
				{
					X = _ax,
					Y = _ay,
					R = 26f
				});
				Son("ki_fatal.wav");
			}
			_gx = -14f - 126f * num13;
			_fx = 14f + 126f * num13;
			_gHaut = 18f - 8f * num13;
			_fHaut = 18f - 6f * num13;
			_gAnim = "Goku_ssj1_block";
			_gCol = 0;
			_gRow = 2;
			_fAnim = "Freezer_block";
			_fCol = 0;
			_fRow = 1;
		}
		else if (_t >= 7.6f && _t < 11f)
		{
			_fAnim = "Freezer_spell";
			_fCol = 0;
			_fRow = 1;
			_auraF = 0.55f;
			_gx = _telX;
			_gHaut = num10 + _telY;
			_auraG = 0.6f;
			if (_t >= _prochainTir)
			{
				float num14 = _ax + _gx;
				float num15 = _ay - _gHaut - 26f;
				Ajouter(new P
				{
					Type = "rayonMortel",
					X = _ax + _fx - 14f,
					Y = _ay - _fHaut - 28f,
					X2 = num14,
					Y2 = num15,
					Vie = 0.14f
				});
				Explosion(num14, _ay - 4f, 18f, 0.45f);
				Eclaboussure(num14, _ay, 16, 1f);
				Crateres.Add(new Cratere
				{
					X = num14,
					Y = _ay,
					R = 12f
				});
				Ajouter(new P
				{
					Type = "fantome",
					X = num14,
					Y = num15 + 26f,
					Vie = 0.25f,
					Anim = _gAnim,
					Col = _gCol,
					Row = _gRow
				});
				_telX = -140f + (new float[4] { -60f, 0f, 40f, -30f })[Hasard.Next(4)];
				_telY = (new float[4] { 18f, 0f, 10f, -4f })[Hasard.Next(4)];
				_prochainTir = _t + 0.42f;
				Son("ki_impact.wav");
			}
		}
		else if (_t >= 11f && _t < 15f)
		{
			float num16 = Phase(11f, 15f);
			int gCol3 = (int)(_t * 10f) % 4;
			_gAnim = "Goku_ssj1_ki";
			_gCol = gCol3;
			_gRow = 2;
			_auraG = 0.8f + num16 * 1.6f;
			_gHaut = 6f;
			int fCol2 = (int)(_t * 8f) % 4;
			_fAnim = "Freezer_ki";
			_fCol = fCol2;
			_fRow = 1;
			_auraF = 0.4f + num16 * 0.7f;
			_bouleR = 3f + num16 * 20f;
			if (Chance(24f))
			{
				Ajouter(new P
				{
					Type = "debris",
					X = _ax + _gx + Entre(-40f, 40f),
					Y = _ay + Entre(-10f, 10f),
					Vx = Entre(-4f, 4f),
					Vy = Entre(-50f, -25f),
					Vie = 2.5f
				});
			}
			Tremble(0.8f + num16 * 2f, 200L);
			Son("ssj_montee.wav");
			if (_t > 12.5f)
			{
				Son("kamehameha_charge.wav");
			}
		}
		else if (_t >= 15f && _t < 19.5f)
		{
			float num17 = Phase(15f, 19.5f);
			_gAnim = "Goku_ssj1_spell";
			_gCol = 0;
			_gRow = 2;
			_auraG = 2.3f;
			_gHaut = 8f;
			_fAnim = "Freezer_spell";
			_fCol = 0;
			_fRow = 1;
			_auraF = 1.1f;
			_fHaut = 8f;
			float num18 = 20f + MathF.Sin(_t * 5f) * 16f - MathF.Sin(num17 * (float)Math.PI) * 30f;
			if (num17 > 0.8f)
			{
				num18 += (_fx - 10f - num18) * Lisse((num17 - 0.8f) / 0.2f);
			}
			_choc = num18;
			_chocY = _ay - _gHaut - 27f;
			if (Chance(40f))
			{
				Etincelles(_ax + num18, _chocY, 4, (Hasard.NextDouble() < 0.5) ? Hex(12577023u) : Hex(16756848u));
			}
			Tremble(1.5f + num17 * 3.5f, 160L);
			if (Chance(15f))
			{
				var (x4, y5) = SolAuHasard();
				Eclaboussure(x4, y5, 14, 1.4f);
			}
			Son("kamehameha_ha.wav");
		}
		else if (_t >= 19.5f && _t < 21.5f)
		{
			float num19 = Phase(19.5f, 21.5f);
			if (Passe(19.5f))
			{
				Explosion(_ax + _fx, _ay - _fHaut - 26f, 110f, 2.4f);
				_flash = MathF.Max(_flash, 1f);
				Ondes.Add(new Onde
				{
					X = _ax + _fx,
					Y = _ay,
					Rmax = 320f,
					Duree = 1.4f,
					C = Color.White
				});
				Crateres.Add(new Cratere
				{
					X = _ax + _fx,
					Y = _ay,
					R = 44f
				});
				Son("ki_fatal.wav");
			}
			int gCol4 = (int)(_t * 6f) % 16;
			_gAnim = "Goku_ssj1_idle";
			_gCol = gCol4;
			_gRow = 2;
			_auraG = 1.2f - num19;
			_gHaut = num10 * (1f - Lisse(num19));
			_fVisible = _t < 19.9f;
			_fAnim = "Freezer_block";
			_fCol = 0;
			_fRow = 1;
			Tremble(4f * (1f - num19) + 1.5f, 200L);
			if (Chance(36f))
			{
				var (x5, y6) = SolAuHasard();
				Eclaboussure(x5, y6, 22, 1.8f);
			}
		}
		else if (_t >= 21.5f && _t < 25f)
		{
			float num20 = Phase(21.5f, 25f);
			float num21 = 238f;
			_fVisible = false;
			_gx = -140f + (num21 + 140f) * Lisse(num20);
			_gSol = 36f * Lisse(num20);
			_gHaut = 14f + 14f * MathF.Abs(MathF.Sin(num20 * (float)Math.PI * 3f)) * (1f - Lisse(MathF.Max(0f, (num20 - 0.85f) / 0.15f)));
			int gCol5 = 4 + (int)(_t * 12f) % 4;
			_gAnim = "Goku_ssj1_vol";
			_gCol = gCol5;
			_gRow = 2;
			_traineeG = num20 < 0.9f;
			_auraG = 0.9f;
			float[] array = new float[3] { 22.3f, 23.2f, 24.1f };
			foreach (float instant in array)
			{
				if (Passe(instant))
				{
					Colonne(_ax + _gx + 70f, _ay + _gSol + Entre(-6f, 10f), Entre(90f, 150f));
					Tremble(3f, 280L);
				}
			}
			Tremble(2f, 200L);
			if (Chance(30f))
			{
				var (x6, y7) = SolAuHasard();
				Eclaboussure(x6, y7, 22, 1.8f);
			}
			if (Passe(21.5f))
			{
				Son("ki_depart.wav");
			}
		}
		else if (_t >= 25f && _t < 28.5f)
		{
			float num22 = Phase(25f, 25.8f);
			_fVisible = false;
			_gDerriere = true;
			_gx = 238f + 22f * Lisse(num22);
			_gSol = 36f - 12f * Lisse(num22);
			_gHaut = 4f * (1f - num22);
			_gAlpha = 1f - Lisse(num22);
			_auraG = 0f;
			_gAnim = "Goku_ssj1_idle";
			_gCol = 0;
			_gRow = 2;
			_capHublot = ((_t < 25.8f) ? 0f : ((MathF.Sin(_t * 14f) > -0.2f) ? 1f : 0.35f));
			_capTremble = ((_t > 26f) ? ((MathF.Sin(_t * 70f) > 0f) ? 1 : (-1)) : 0);
			if (_t >= _prochainHoquet)
			{
				_hoquetFin = _t + 0.12f;
				for (int j = 0; j < 4; j++)
				{
					Ajouter(new P
					{
						Type = "vapeur",
						X = _ax + 260f + Entre(-10f, 10f),
						Y = _ay + 34f - 4f,
						Vx = Entre(-30f, 30f),
						Vy = Entre(-20f, -5f),
						Vie = Entre(0.6f, 1.1f),
						R = Entre(3f, 6f)
					});
				}
				Etincelles(_ax + 260f, _ay + 34f - 6f, 6, Hex(16765040u));
				Tremble(1.5f, 120L);
				_prochainHoquet = _t + Entre(0.35f, 0.7f);
			}
			_capJet = ((_t < _hoquetFin) ? 9 : 0);
			_capJet = MathF.Max(_capJet, (_t > 27.9f) ? (6f + 8f * Phase(27.9f, 28.5f)) : 0f);
			if (Chance(30f))
			{
				var (x7, y8) = SolAuHasard();
				Eclaboussure(x7, y8, 22, 2f);
			}
		}
		else if (_t >= 28.5f && _t < 31.5f)
		{
			float num23 = Phase(28.5f, 31.5f);
			_fVisible = false;
			_gAlpha = 0f;
			_auraG = 0f;
			_capHublot = 1f;
			_capHaut = 480f * MathF.Pow(num23, 2.2f);
			_capJet = 22f + 30f * MathF.Min(1f, num23 * 3f);
			_capTremble = ((num23 < 0.2f) ? ((MathF.Sin(_t * 70f) > 0f) ? 1 : (-1)) : 0);
			if (Passe(28.5f))
			{
				Ondes.Add(new Onde
				{
					X = _ax + 260f,
					Y = _ay + 34f,
					Rmax = 90f,
					Duree = 0.8f,
					C = Hex(16769200u)
				});
				for (int k = 0; k < 14; k++)
				{
					Ajouter(new P
					{
						Type = "vapeur",
						X = _ax + 260f + Entre(-14f, 14f),
						Y = _ay + 34f - 2f,
						Vx = Entre(-70f, 70f),
						Vy = Entre(-25f, 5f),
						Vie = Entre(0.8f, 1.5f),
						R = Entre(4f, 8f)
					});
				}
				_flash = MathF.Max(_flash, 0.35f);
				Son("ki_depart.wav");
				Tremble(5f, 400L);
			}
			Tremble(3.5f * (1f - num23) + 1.5f, 200L);
			if (Chance(40f))
			{
				var (x8, y9) = SolAuHasard();
				Eclaboussure(x8, y9, 24, 2.2f);
			}
		}
		else if (_t >= 31.5f)
		{
			float num24 = Phase(31.5f, 35f);
			_fVisible = false;
			_gAlpha = 0f;
			_auraG = 0f;
			_capHaut = 9999f;
			Tremble(3.5f + num24 * 3f, 200L);
			if (Chance(60f))
			{
				var (x9, y10) = SolAuHasard();
				Eclaboussure(x9, y10, 28, 2.4f);
			}
			if (Chance(9f))
			{
				var (x10, y11) = SolAuHasard();
				Explosion(x10, y11, Entre(30f, 90f), 1.1f);
				Crateres.Add(new Cratere
				{
					X = x10,
					Y = y11,
					R = 24f
				});
				_flash = MathF.Max(_flash, 0.25f);
			}
			if (Passe(31.5f))
			{
				Son("ki_fatal.wav");
			}
			Son("ssj_cri.wav");
		}
		for (int num25 = Parts.Count - 1; num25 >= 0; num25--)
		{
			P p = Parts[num25];
			p.Age += num;
			p.X += p.Vx * num;
			p.Y += p.Vy * num;
			p.Vy += p.G * num;
			if (p.Type == "meteore" && p.Y >= p.SolY)
			{
				p.Age = p.Vie;
				Explosion(p.X, p.Y, 16f, 0.55f);
				Eclaboussure(p.X, p.Y, 14, 1.1f);
				Crateres.Add(new Cratere
				{
					X = p.X,
					Y = p.Y,
					R = 10f
				});
			}
			if (p.Type == "lave" && p.Vy > 0f && p.Y > p.SolY)
			{
				p.Age = p.Vie;
			}
			if (p.Type == "colonne" && p.Age < p.Vie * 0.6f && Chance(36f))
			{
				float num26 = p.Hmax * MathF.Pow(MathF.Sin((float)Math.PI * p.Age / p.Vie), 0.7f);
				Ajouter(new P
				{
					Type = "lave",
					X = p.X + Entre(-5f, 5f),
					Y = p.Y - num26,
					SolY = p.Y + Entre(-6f, 10f),
					Vx = Entre(-60f, 60f),
					Vy = Entre(-60f, 10f),
					Vie = Entre(0.8f, 1.4f),
					G = 180f
				});
			}
			if (p.Age >= p.Vie)
			{
				Parts.RemoveAt(num25);
			}
		}
		foreach (Onde onde in Ondes)
		{
			onde.Age += num;
		}
		Ondes.RemoveAll((Onde o) => o.Age >= o.Duree);
		foreach (Explo explosion in Explosions)
		{
			explosion.Age += num;
		}
		Explosions.RemoveAll((Explo e) => e.Age >= e.Duree);
		foreach (Cratere cratere in Crateres)
		{
			cratere.Age += num;
		}
		if (Crateres.Count > 90)
		{
			Crateres.RemoveRange(0, Crateres.Count - 90);
		}
		_flash *= MathF.Pow(0.01f, num);
		if (_eclair != null)
		{
			_eclairAge += num;
			if (_eclairAge > 0.2f)
			{
				_eclair = null;
			}
		}
	}

	private static bool PretMonde()
	{
		if (_actif)
		{
			GameRenderer renderer = Graphics.Renderer;
			if (renderer != null && Globals.Me != null)
			{
				Avancer();
				if (!_actif)
				{
					return false;
				}
				Monde(renderer);
				return true;
			}
		}
		return false;
	}

	public static void DrawSol()
	{
		if (!PretMonde())
		{
			return;
		}
		float num = MathF.Min(1f, 0.35f + _t / 35f);
		float num2 = ((_t >= 19.5f) ? Phase(19.5f, 35f) : 0f);
		float num3 = Lisse(Phase(0f, 2.5f));
		foreach (var fissure in Fissures)
		{
			float num4 = (0.55f + 0.45f * MathF.Sin(_t * 3f + fissure.P) * num) * num3;
			for (int i = 1; i < fissure.Pts.Count; i++)
			{
				var (x, y) = fissure.Pts[i - 1];
				var (x2, y2) = fissure.Pts[i];
				Trait(x, y, x2, y2, 4f + num2 * 10f, Hex(3803652u, 0.55f * num3), add: false);
				Trait(x, y, x2, y2, 3f + num2 * 7f, Hex(16726538u, 0.35f * num4 + num2 * 0.3f), add: true);
				Trait(x, y, x2, y2, 1f + num2 * 3f, Hex(16756778u, 0.9f * num4), add: true);
			}
		}
		foreach (Cratere cratere in Crateres)
		{
			float num5 = MathF.Max(0.25f, 1f - cratere.Age / 8f);
			Disque(cratere.X, cratere.Y, cratere.R, Hex(1705990u, 0.7f), add: false, 0.5f);
			Disque(cratere.X, cratere.Y, cratere.R * 0.7f, Hex(16730640u, 0.55f * num5), add: true, 0.5f);
			Disque(cratere.X, cratere.Y, cratere.R * 0.35f, Hex(16765514u, 0.6f * num5 * (0.7f + 0.3f * MathF.Sin(_t * 9f + cratere.X))), add: true, 0.5f);
		}
		if (_gAlpha > 0f)
		{
			Disque(_ax + _gx, _ay + _gSol + 2f, MathF.Max(6f, 12f - _gHaut * 0.15f), Rgb(0, 0, 0, 0.35f * _gAlpha), add: false, 0.35f);
		}
		if (_fVisible)
		{
			Disque(_ax + _fx, _ay + 2f, MathF.Max(6f, 12f - _fHaut * 0.15f), Rgb(0, 0, 0, 0.35f), add: false, 0.35f);
		}
		if (_capHaut < 200f)
		{
			Disque(_ax + 260f, _ay + 34f, MathF.Max(4f, 16f - _capHaut * 0.06f), Rgb(0, 0, 0, 0.4f * (1f - _capHaut / 200f)), add: false, 0.3f);
		}
	}

	public static void DrawVoile()
	{
		if (PretMonde())
		{
			FloatRect vue = Vue;
			float num = Lisse(Phase(0f, 3f));
			float num2 = MathF.Min(1f, 0.35f + _t / 35f);
			Fill(vue.X, vue.Y, vue.Width, vue.Height, Rgb(60, 6, 0, (0.18f + 0.22f * num2) * num));
			Fill(vue.X, vue.Y, vue.Width, vue.Height, Rgb(255, 80, 12, (0.06f + 0.08f * num2 + 0.03f * MathF.Sin(_t * 2.5f)) * num), add: true);
			for (int i = 0; i < 8; i++)
			{
				float a = 0.08f * (float)(8 - i) / 8f * num;
				Fill(vue.X, vue.Y + (float)(i * 6), vue.Width, 6f, Rgb(10, 0, 0, a));
				Fill(vue.X, vue.Y + vue.Height - (float)((i + 1) * 6), vue.Width, 6f, Rgb(40, 4, 0, a));
			}
		}
	}

	public static void DrawMonde()
	{
		if (!PretMonde())
		{
			return;
		}
		FloatRect vue = Vue;
		foreach (P part in Parts)
		{
			if (part.Type == "fumee")
			{
				float num = part.Age / part.Vie;
				Disque(part.X, part.Y, part.R * (1f + num), Hex(1312776u, 0.3f * (1f - num)), add: false);
			}
		}
		foreach (P part2 in Parts)
		{
			if (!(part2.Type != "colonne"))
			{
				float y = part2.Age / part2.Vie;
				float num2 = part2.Hmax * MathF.Pow(MathF.Sin((float)Math.PI * MathF.Min(1f, y)), 0.7f);
				for (float num3 = 0f; num3 < num2; num3 += 3f)
				{
					float num4 = part2.Largeur * (0.55f + 0.45f * Bruit(MathF.Floor(num3 / 3f), MathF.Floor(part2.Age * 18f) + part2.X)) * (1f - 0.35f * num3 / MathF.Max(1f, num2));
					Fill(part2.X - num4 / 2f, part2.Y - num3 - 3f, num4, 3f, (num3 > num2 - 10f) ? Hex(16771226u, 0.75f) : Hex(16734740u, 0.75f), add: true);
					Fill(part2.X - num4 / 5f, part2.Y - num3 - 3f, MathF.Max(2f, num4 * 0.4f), 3f, Hex(16765514u, 0.9f), add: true);
				}
			}
		}
		float num5 = _ax + _fx;
		float num6 = _ay - _fHaut;
		if (_fVisible)
		{
			Aura(num5, num6, new uint[3] { 11758847u, 8006872u, 14725375u }, _auraF);
			if (_traineeF)
			{
				for (int num7 = 3; num7 >= 1; num7--)
				{
					Sprite(_fAnim + ".png", _fCol, _fRow, num5 + (float)(num7 * 8), num6, 0.12f * (float)(4 - num7));
				}
			}
			Sprite(_fAnim + ".png", _fCol, _fRow, num5, num6);
			if (_bouleR > 0f)
			{
				Boule(num5, num6 - 62f - _bouleR * 0.4f, _bouleR, new uint[4] { 7997976u, 14692378u, 16747050u, 16769184u });
			}
		}
		if (_gDerriere)
		{
			DessinerGoku();
		}
		if (_capHaut < 9000f)
		{
			float x = _ax + 260f + _capTremble;
			float num8 = _ay + 34f - _capHaut;
			Jet(x, num8 - 7f, _capJet * (0.8f + 0.2f * MathF.Sin(_t * 40f)));
			Capsule(x, num8, _capHublot);
		}
		if (!_gDerriere)
		{
			DessinerGoku();
		}
		if (_t >= 15f && _t < 19.5f)
		{
			float num9 = Phase(15f, 19.5f);
			float num10 = _ax + _gx;
			float num11 = _ax + _choc;
			for (float num12 = num10 + 14f; num12 < num11; num12 += 2f)
			{
				float num13 = 5f + num9 * 4f + MathF.Sin(num12 * 0.35f - _t * 40f) * 1.6f + (float)(Hasard.NextDouble() - 0.5) * 1.6f;
				Fill(num12, _chocY - num13, 2f, num13 * 2f, Hex(4765951u, 0.55f), add: true);
				Fill(num12, _chocY - num13 * 0.45f, 2f, num13 * 0.9f, Hex(16056319u, 0.9f), add: true);
			}
			if (num11 + 28f < num5 - 14f)
			{
				Trait(num11 + 26f, _chocY, num5 - 14f, num6 - 28f, 3f, Hex(14692378u, 0.5f), add: true);
			}
			Boule(num11 + 12f, _chocY, 16f + MathF.Sin(_t * 30f) * 1.5f, new uint[4] { 5900832u, 14692378u, 16747050u, 16773312u });
		}
		foreach (P part3 in Parts)
		{
			float num14 = part3.Age / part3.Vie;
			switch (part3.Type)
			{
			case "lave":
				Fill(part3.X, part3.Y, 2f, 2f, Hex((num14 < 0.3f) ? 16773280u : ((num14 < 0.6f) ? 16752666u : ((num14 < 0.85f) ? 14696460u : 4853256u))));
				break;
			case "braise":
				if (Hasard.NextDouble() < 0.85)
				{
					Fill(part3.X + MathF.Sin(part3.Age * 5f) * 3f, part3.Y, 2f, 2f, Hex((num14 < 0.5f) ? 16764778u : 16734740u));
				}
				break;
			case "vapeur":
				Disque(part3.X, part3.Y, part3.R * (1f + num14 * 1.5f), Hex(12104880u, 0.45f * (1f - num14)), add: false);
				break;
			case "etincelle":
				Trait(part3.X, part3.Y, part3.X - part3.Vx * 0.03f, part3.Y - part3.Vy * 0.03f, 1f, Rgb(part3.C.R, part3.C.G, part3.C.B, 1f - num14), add: false);
				break;
			case "debris":
				Fill(part3.X, part3.Y, 3f, 2f, Hex(2758164u));
				Fill(part3.X, part3.Y + 2f, 3f, 1f, Hex(16742954u));
				break;
			case "meteore":
				Trait(part3.X, part3.Y, part3.X - part3.Vx * 0.2f, part3.Y - part3.Vy * 0.2f, 4f, Hex(16738842u, 0.5f), add: true);
				Trait(part3.X, part3.Y, part3.X - part3.Vx * 0.09f, part3.Y - part3.Vy * 0.09f, 2f, Hex(16769146u), add: true);
				Fill(part3.X - 2f, part3.Y - 2f, 4f, 4f, Hex(16774864u));
				break;
			case "rayonMortel":
				Trait(part3.X, part3.Y, part3.X2, part3.Y2, 4f, Hex(16734936u, 0.55f), add: true);
				Trait(part3.X, part3.Y, part3.X2, part3.Y2, 2f, Hex(16773375u), add: true);
				break;
			case "fantome":
				Sprite(part3.Anim + ".png", part3.Col, part3.Row, part3.X, part3.Y, 0.5f * (1f - num14));
				break;
			}
		}
		foreach (Onde onde in Ondes)
		{
			float num15 = onde.Age / onde.Duree;
			float num16 = onde.Rmax * Lisse(num15);
			for (float num17 = 0f; num17 < (float)Math.PI * 2f; num17 += 0.05f)
			{
				Fill(onde.X + MathF.Cos(num17) * num16, onde.Y + MathF.Sin(num17) * num16 * 0.45f, 2f, 2f, Rgb(onde.C.R, onde.C.G, onde.C.B, (1f - num15) * 0.8f));
			}
		}
		foreach (Explo explosion in Explosions)
		{
			float num18 = explosion.Age / explosion.Duree;
			float num19 = explosion.R * (0.3f + 0.7f * Lisse(MathF.Min(1f, num18 * 2.2f)));
			Disque(explosion.X, explosion.Y, num19, Hex(9048070u, 0.5f * (1f - num18)), add: true, 0.8f);
			Disque(explosion.X, explosion.Y, num19 * 0.78f, Hex(16734740u, 0.6f * (1f - num18)), add: true, 0.8f);
			Disque(explosion.X, explosion.Y, num19 * 0.55f, Hex(16761418u, 0.8f * (1f - num18)), add: true, 0.8f);
			Disque(explosion.X, explosion.Y, num19 * 0.3f, Hex(16775392u, 0.9f * (1f - num18)), add: true, 0.8f);
		}
		if (_eclair != null)
		{
			for (int i = 1; i < _eclair.Count; i++)
			{
				Trait(_eclair[i - 1].X, _eclair[i - 1].Y, _eclair[i].X, _eclair[i].Y, 3f, Hex(11563263u, 0.5f), add: true);
				Trait(_eclair[i - 1].X, _eclair[i - 1].Y, _eclair[i].X, _eclair[i].Y, 1f, Hex(16773375u), add: true);
			}
		}
		if (_t >= 19.5f)
		{
			float num20 = Phase(19.5f, 35f);
			Flammes(vue.X, vue.X + vue.Width, vue.Y + vue.Height + 2f, 10f, 20f + 90f * num20, 0.6f);
		}
	}

	private static void DessinerGoku()
	{
		if (_gAlpha <= 0f)
		{
			return;
		}
		float num = _ax + _gx;
		float pieds = _ay + _gSol - _gHaut;
		if (_auraG > 0f)
		{
			Aura(num, pieds, new uint[3] { 16769658u, 16756778u, 16774848u }, _auraG, _gAlpha);
		}
		if (_traineeG)
		{
			for (int num2 = 3; num2 >= 1; num2--)
			{
				Sprite(_gAnim + ".png", _gCol, _gRow, num - (float)(num2 * 8), pieds, 0.12f * (float)(4 - num2) * _gAlpha);
			}
		}
		Sprite(_gAnim + ".png", _gCol, _gRow, num, pieds, _gAlpha);
	}

	public static void Draw()
	{
		if (!_actif)
		{
			return;
		}
		GameRenderer renderer = Graphics.Renderer;
		if (renderer == null)
		{
			return;
		}
		Avancer();
		if (_actif)
		{
			_pixel = renderer.WhitePixel;
			FloatRect currentView = Graphics.CurrentView;
			float num = MathF.Max(0.1f, renderer.Scale);
			float num2 = (float)renderer.ScreenWidth / num;
			float num3 = (float)renderer.ScreenHeight / num;
			_s = num2 / 320f;
			_ox = currentView.X;
			_oy = currentView.Y;
			float num4 = num3 / _s;
			if (_flash > 0.01f)
			{
				Fill(0f, 0f, 320f, num4, Rgb(255, 248, 230, MathF.Min(1f, _flash)));
			}
			float num5 = 32.4f;
			if (_t > num5)
			{
				float num6 = Lisse(MathF.Min(1f, (_t - num5) / 2.2f));
				float num7 = num4 - num6 * (num4 + 30f);
				Fill(0f, num7, 320f, num4 - num7, Rgb(200, 48, 10, 0.85f));
				Flammes(0f, 320f, num7 + 1f, 10f, 36f, 0.9f);
				Flammes(0f, 320f, num7 + 1f, 5f, 18f, 0.9f);
			}
			float num8 = 24f * Lisse(Phase(0f, 0.8f));
			Fill(0f, 0f, 320f, num8, Rgb(0, 0, 0, 0.92f));
			Fill(0f, num4 - num8, 320f, num8, Rgb(0, 0, 0, 0.92f));
			if (_t > 34.2f)
			{
				Fill(0f, 0f, 320f, num4, Rgb(255, 244, 228, MathF.Min(1f, (_t - 34.2f) / 0.8f)));
			}
			if (_texte == null)
			{
				_texte = Globals.ContentManager.GetFont("chakrapetchmedium") ?? Globals.ContentManager.GetFont("chakrapetch") ?? Globals.ContentManager.GetFont("rajdhanibold");
			}
			if (_texte != null)
			{
				string text = ((_t >= 31.5f) ? Strings.Cinematique.NamekExplosion : ((_t >= 28.5f) ? Strings.Cinematique.NamekDecollage : ((_t >= 25f) ? Strings.Cinematique.NamekCapsule : ((_t >= 21.5f) ? Strings.Cinematique.NamekFuite : ((_t >= 19.5f) ? Strings.Cinematique.NamekVaincu : ((_t >= 15f) ? Strings.Cinematique.NamekLegende5 : ((_t >= 11f) ? Strings.Cinematique.NamekLegende4 : ((_t >= 7.6f) ? Strings.Cinematique.NamekLegende3 : ((_t >= 3f) ? Strings.Cinematique.NamekLegende2 : Strings.Cinematique.NamekLegende1)))))))));
				FloatRect clipRect = new FloatRect(0f, 0f, renderer.ScreenWidth, renderer.ScreenHeight);
				int size = (int)MathF.Max(9f, 10f * _s / 4f * 1.6f);
				Vector2 vector = renderer.MeasureText(text, _texte, size, 1f);
				float num9 = MathF.Round(160f * _s - vector.X / 2f);
				float num10 = MathF.Round((num4 - 17f) * _s);
				renderer.DrawString(text, _texte, size, num9 + 1f, num10 + 1f, 1f, new Color(200, 0, 0, 0), worldPos: true, null, clipRect);
				renderer.DrawString(text, _texte, size, num9, num10, 1f, Color.White, worldPos: true, null, clipRect);
			}
		}
	}
}
