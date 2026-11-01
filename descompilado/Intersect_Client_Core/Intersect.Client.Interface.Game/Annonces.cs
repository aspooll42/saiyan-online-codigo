using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Items;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Dungeons;
using Intersect.Client.Interface.Game.Enseignements;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Achievements;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.Quests;
using Intersect.GameObjects;
using Intersect.Models;

namespace Intersect.Client.Interface.Game;

public sealed class Annonces : Intersect.Client.Framework.Gwen.Control.Base
{
	private sealed class Annonce
	{
		public int Capsule;

		public int CapsulesAuTotal;

		public Genre Genre;

		public string Etiquette = string.Empty;

		public string Titre = string.Empty;

		public string SousTitre = string.Empty;

		public bool TitreSimple;

		public Color Accent;

		public TechniqueArt.Stops Degrade;

		public long HoldMs;

		public bool Decouverte;

		public string Zone = string.Empty;

		public Color ZoneCouleur;

		public string Heure = string.Empty;

		public Gain[] Gains = Array.Empty<Gain>();

		public string Filet = string.Empty;

		public bool Petite;

		public long CreeeA;
	}

	private enum Genre
	{
		Lieu,
		Evenement,
		Histoire
	}

	private enum GenreDeGain
	{
		Zeni,
		Experience,
		Objet,
		Technique,
		Reste
	}

	private sealed class Gain
	{
		public GenreDeGain Genre;

		public long Montant;

		public string Nom = string.Empty;

		public IGameTexture? Icone;

		public Guid ObjetId;

		public Color? Couleur;

		public int Largeur;
	}

	private const string ChaineDesCapsules = "variable:eb5709da217cb7e24d49782e70e18830";

	private const long ZenisParCapsule = 100L;

	private const long CapsuleHoldMs = 5000L;

	private const long CapsuleMonteMs = 740L;

	private const long CapsuleEclatMs = 600L;

	private static readonly Color LosangeEteint = new Color(255, 58, 72, 104);

	private static Guid _personnageDesCapsules;

	private static int _capsulesConnues = -1;

	private static readonly List<(int Rang, int Total, string Suite)> CapsulesTrouvees = new List<(int, int, string)>();

	private const long InMs = 240L;

	private const long OutMs = 300L;

	private const long LieuHoldMs = 2600L;

	private const long LieuPvpHoldMs = 3500L;

	private const long HistoireHoldMs = 4600L;

	private static readonly Color RougeJcJ = new Color(255, 255, 90, 76);

	private static readonly Color VertSur = new Color(255, 78, 200, 120);

	private static readonly Color Encre = new Color(255, 238, 241, 247);

	private static readonly TechniqueArt.Stops TitreLieu = new TechniqueArt.Stops(TechniqueArt.White, 0f, new Color(255, 255, 246, 224), 0.48f, new Color(255, 255, 210, 122));

	private static readonly TechniqueArt.Stops TitreJcJ = new TechniqueArt.Stops(new Color(255, 255, 242, 239), 0f, new Color(255, 255, 180, 168), 0.46f, new Color(255, 255, 74, 60));

	private static (Guid Done, Guid Next, string Giver, string Place)? _histoire;

	private readonly List<Annonce> _file = new List<Annonce>();

	private Annonce? _courante;

	private long _debut = -1L;

	private const long GainsHoldMs = 5200L;

	private const long QueteHoldMs = 2600L;

	private const long EtapeHoldMs = 3000L;

	private const long GainRetardMs = 100L;

	private const long GainEcartMs = 120L;

	private const long GainEntreeMs = 300L;

	private const long CompteMs = 700L;

	private const long HaloMs = 700L;

	private const float Chiffre = 29f;

	private const float ChiffrePetit = 22f;

	private const int GainsMax = 6;

	private static readonly Color BleuEtape = new Color(255, 120, 176, 255);

	private static readonly Color OrangeExp = new Color(255, 255, 146, 64);

	private static readonly Color EncrePastille = new Color(255, 22, 10, 2);

	private static Guid _personnage;

	private static readonly Dictionary<Guid, (Guid Tache, bool Terminee)> Connues = new Dictionary<Guid, (Guid, bool)>();

	private static readonly List<(Guid Quete, Guid Tache)> EtapesFaites = new List<(Guid, Guid)>();

	private static readonly List<Guid> QuetesFaites = new List<Guid>();

	private static readonly Dictionary<Guid, long> GainsAnnonces = new Dictionary<Guid, long>();

	private readonly HashSet<Annonce> _vues = new HashSet<Annonce>();

	private static Guid _sacDe;

	private static (Guid Id, int Quantite, ItemProperties? Jet)[] _sac = Array.Empty<(Guid, int, ItemProperties)>();

	private static readonly List<(Guid Id, ItemProperties? Jet, long A)> Recus = new List<(Guid, ItemProperties, long)>();

	public static bool EstAffichee { get; private set; }

	public bool IsShowing => _debut >= 0;

	protected override bool ShouldClip => false;

	private float Scale
	{
		get
		{
			Intersect.Client.Framework.Gwen.Control.Base parent = base.Parent;
			if (parent == null)
			{
				return 1f;
			}
			return Math.Max(0.6f, Math.Min((float)parent.Width / 1600f, (float)parent.Height / 900f));
		}
	}

	public static void ObserverSucces()
	{
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		AchievementProgress[] array = (from succes in Globals.Achievements
			where string.Equals(succes.Chain, "variable:eb5709da217cb7e24d49782e70e18830", StringComparison.OrdinalIgnoreCase)
			orderby succes.Objective
			select succes).ToArray();
		if (array.Length == 0)
		{
			return;
		}
		AchievementProgress achievementProgress = array[^1];
		int num = (int)Math.Min(achievementProgress.Value, achievementProgress.Objective);
		if (me.Id != _personnageDesCapsules || _capsulesConnues < 0)
		{
			_personnageDesCapsules = me.Id;
			_capsulesConnues = num;
			return;
		}
		if (num <= _capsulesConnues)
		{
			_capsulesConnues = num;
			return;
		}
		lock (CapsulesTrouvees)
		{
			int rang;
			for (rang = _capsulesConnues + 1; rang <= num; rang++)
			{
				AchievementProgress achievementProgress2 = array.FirstOrDefault((AchievementProgress palier) => palier.Objective == rang);
				AchievementProgress achievementProgress3 = array.FirstOrDefault((AchievementProgress palier) => palier.Objective > rang);
				string item = ((achievementProgress2 != null) ? Strings.Banners.CapsuleUnlocked.ToString(achievementProgress2.Name).ToUpperInvariant() : ((achievementProgress3 != null) ? Strings.Banners.CapsuleLeft.ToString(achievementProgress3.Objective - rang, achievementProgress3.Name) : string.Empty));
				CapsulesTrouvees.Add((rang, (int)achievementProgress.Objective, item));
			}
		}
		_capsulesConnues = num;
		CapsulePilier.Allumer();
	}

	private void VerserLesCapsules(long maintenant)
	{
		(int, int, string)[] array;
		lock (CapsulesTrouvees)
		{
			if (CapsulesTrouvees.Count == 0)
			{
				return;
			}
			array = CapsulesTrouvees.ToArray();
			CapsulesTrouvees.Clear();
		}
		Guid monnaie = Globals.Me?.CurrencyItemId ?? Guid.Empty;
		(int, int, string)[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			var (num, num2, sousTitre) = array2[i];
			Inserer(new Annonce
			{
				Genre = Genre.Evenement,
				Etiquette = Strings.Banners.CapsuleTag.ToString().ToUpperInvariant(),
				Titre = Strings.Banners.CapsuleCount.ToString(num, num2),
				SousTitre = sousTitre,
				Accent = TechniqueArt.Gold,
				Degrade = new TechniqueArt.Stops(TechniqueArt.White, 0.3f, TechniqueArt.Mix(TechniqueArt.Gold, TechniqueArt.White, 0.6f), 0.75f, TechniqueArt.Gold),
				HoldMs = 5000L,
				Capsule = num,
				CapsulesAuTotal = num2,
				Filet = Strings.Banners.CapsuleGains.ToString().ToUpperInvariant(),
				Gains = new Gain[1]
				{
					new Gain
					{
						Genre = GenreDeGain.Zeni,
						Montant = 100L,
						Icone = IconeDeLaMonnaie(monnaie)
					}
				},
				CreeeA = maintenant
			});
		}
	}

	private static string TitreCapsule(Annonce a, long age)
	{
		if (age >= 740)
		{
			return a.Titre;
		}
		return Strings.Banners.CapsuleCount.ToString(a.Capsule - 1, a.CapsulesAuTotal);
	}

	private static int LargeurIconeCapsule(Annonce a, Func<float, int> L)
	{
		if (a.Capsule <= 0)
		{
			return 0;
		}
		return L(34f) + L(12f);
	}

	private static void DessinerIconeCapsule(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Annonce a, int x, int centreY, long age, float alpha, Func<float, int> L)
	{
		IGameTexture texture = Globals.ContentManager?.GetTexture(TextureType.Tileset, "capsule_doree.png");
		int num = L(72f);
		float num2 = Math.Clamp((float)(age - 100) / 800f, 0f, 1f);
		int num3 = Math.Max(2, (int)MathF.Round((float)num * MathF.Abs(MathF.Cos(num2 * (float)Math.PI * 2f))));
		int num4 = x + L(17f);
		TechniqueArt.GlowRect(renderer, new Rectangle(num4 - L(26f), centreY - L(26f), L(52f), L(52f)), TechniqueArt.Alpha(TechniqueArt.Gold, 0.3f * alpha));
		DessinerImage(renderer, texture, new Rectangle(num4 - num3 / 2, centreY - num / 2, num3, num), TechniqueArt.Alpha(TechniqueArt.White, alpha * Math.Clamp((float)age / 200f, 0f, 1f)));
	}

	private static void DessinerEclatDuCompteur(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle chiffre, long age, float alpha, Func<float, int> L)
	{
		long num = age - 740;
		if ((num >= 0 && num <= 420) || 1 == 0)
		{
			float num2 = 1f - (float)num / 420f;
			TechniqueArt.GlowRect(renderer, new Rectangle(chiffre.X - L(18f), chiffre.Y - L(8f), chiffre.Width + L(36f), chiffre.Height + L(16f)), TechniqueArt.Alpha(TechniqueArt.Gold, 0.55f * num2 * alpha));
		}
	}

	private static (int Largeur, int Hauteur) MesurerCapsule(Annonce a, Func<float, int> L)
	{
		if (a.Capsule <= 0)
		{
			return (Largeur: 0, Hauteur: 0);
		}
		int num = (a.CapsulesAuTotal + 9) / 10;
		return (Largeur: a.CapsulesAuTotal * L(10f) + Math.Max(0, num - 1) * L(12f), Hauteur: L(6f) + L(10f) + L(4f));
	}

	private static void DessinerCapsule(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Annonce a, int centreX, int y, long age, float alpha, Func<float, int> L)
	{
		int item = MesurerCapsule(a, L).Largeur;
		int num = centreX - item / 2;
		int num2 = y + L(6f) + L(5f);
		int num3 = L(10f);
		for (int i = 1; i <= a.CapsulesAuTotal; i++)
		{
			int num4 = L(18f);
			Color color = ((i < a.Capsule) ? TechniqueArt.Gold : LosangeEteint);
			if (i == a.Capsule)
			{
				long num5 = age - 740;
				if (num5 >= 0)
				{
					float num6 = Math.Clamp((float)num5 / 600f, 0f, 1f);
					float num7 = 1f - MathF.Abs(num6 * 2f - 1f);
					num4 = L(18f + 22f * num7);
					color = TechniqueArt.Mix(TechniqueArt.Gold, TechniqueArt.White, num7);
				}
			}
			int num8 = num + num3 / 2;
			TechniqueArt.Image(renderer, "lieu_point.png", new Rectangle(num8 - num4 / 2, num2 - num4 / 2, num4, num4), TechniqueArt.Alpha(color, alpha));
			num += num3 + ((i % 10 == 0) ? L(12f) : 0);
		}
	}

	public Annonces(Canvas canvas)
		: base(canvas, "Annonces")
	{
		base.MouseInputEnabled = false;
		base.IsHidden = true;
	}

	public void Lieu(string region, string titre, bool pvp, bool decouverte, string heure)
	{
		string text = region.Trim().ToUpperInvariant();
		string text2 = Strings.Banners.PlaceNew.ToString().ToUpperInvariant();
		Annonce item = new Annonce
		{
			Genre = Genre.Lieu,
			Etiquette = ((!decouverte) ? ((text.Length > 0) ? text : Strings.Banners.PlaceTag.ToString().ToUpperInvariant()) : ((text.Length > 0) ? (text2 + " · " + text) : text2)),
			Titre = titre.Trim().ToUpperInvariant(),
			Accent = (pvp ? RougeJcJ : TechniqueArt.Gold),
			Degrade = (pvp ? TitreJcJ : TitreLieu),
			HoldMs = (pvp ? 3500 : 2600),
			Decouverte = decouverte,
			Zone = (pvp ? Strings.Banners.PlacePvp : Strings.Banners.PlaceSafe).ToString().ToUpperInvariant(),
			ZoneCouleur = (pvp ? RougeJcJ : VertSur),
			Heure = (pvp ? Strings.Banners.PlacePvpWarning.ToString() : heure)
		};
		_file.RemoveAll((Annonce attente) => attente.Genre == Genre.Lieu);
		Annonce courante = _courante;
		if (courante != null && courante.Genre == Genre.Lieu)
		{
			_courante = null;
			_debut = -1L;
		}
		_file.Insert(0, item);
	}

	public void AnnulerLieu()
	{
		_file.RemoveAll((Annonce attente) => attente.Genre == Genre.Lieu);
		Annonce courante = _courante;
		if (courante != null && courante.Genre == Genre.Lieu)
		{
			_courante = null;
			_debut = -1L;
		}
	}

	public void Evenement(string etiquette, string titre, string? sousTitre, Color accent, long holdMs = 2600L)
	{
		_file.Add(new Annonce
		{
			Genre = Genre.Evenement,
			Etiquette = etiquette.Trim().ToUpperInvariant(),
			Titre = titre.Trim().ToUpperInvariant(),
			SousTitre = (sousTitre?.Trim() ?? string.Empty),
			Accent = accent,
			Degrade = new TechniqueArt.Stops(TechniqueArt.White, 0.3f, TechniqueArt.Mix(accent, TechniqueArt.White, 0.6f), 0.75f, accent),
			HoldMs = holdMs
		});
	}

	public void Serveur(string message, long dureeMs)
	{
		_file.Add(new Annonce
		{
			Genre = Genre.Evenement,
			Etiquette = Strings.Banners.ServerTag.ToString().ToUpperInvariant(),
			Titre = message.Trim(),
			TitreSimple = true,
			Accent = TechniqueArt.Ki,
			Degrade = TechniqueArt.Stops.Solid(TechniqueArt.Text),
			HoldMs = Math.Clamp(dureeMs, 2500L, 8000L)
		});
	}

	public static void Histoire(Guid done, Guid next, string giver, string place)
	{
		_histoire = (done, next, giver, place);
	}

	private static Annonce ComposerHistoire((Guid Done, Guid Next, string Giver, string Place) demande)
	{
		QuestDescriptor questDescriptor = DatabaseObject<QuestDescriptor>.Get(demande.Done);
		QuestDescriptor questDescriptor2 = ((demande.Next == Guid.Empty) ? null : DatabaseObject<QuestDescriptor>.Get(demande.Next));
		string text = questDescriptor?.Folder?.Trim() ?? string.Empty;
		bool flag = questDescriptor != null && ((questDescriptor2 == null) ? StoryThread.IsLastOfChapter(questDescriptor) : (!string.Equals(text, questDescriptor2.Folder?.Trim(), StringComparison.OrdinalIgnoreCase)));
		string text3;
		string text4;
		if (flag)
		{
			string text2 = StoryThread.ChapterTitle(text);
			text3 = (string.IsNullOrEmpty(text2) ? Strings.Story.ChapterDoneShort.ToString(text.ToUpperInvariant()) : Strings.Story.ChapterDone.ToString(text.ToUpperInvariant(), text2));
			text4 = ((questDescriptor2 == null) ? ((string)Strings.Story.ToBeContinued) : (StoryThread.ChapterTitle(questDescriptor2.Folder) ?? questDescriptor2.Name));
		}
		else
		{
			text3 = Strings.Story.QuestDoneTag;
			text4 = questDescriptor?.Name ?? string.Empty;
		}
		string sousTitre;
		if (questDescriptor2 == null)
		{
			sousTitre = $"{Strings.Story.Next} · {Strings.Story.ComingSoon}";
		}
		else
		{
			string text5 = (string.IsNullOrEmpty(demande.Giver) ? string.Empty : (string.IsNullOrEmpty(demande.Place) ? Strings.Story.Waiting.ToString(demande.Giver) : Strings.Story.WaitingAt.ToString(demande.Giver, demande.Place)));
			string value = (flag ? Traduction.Contenu(questDescriptor2.Folder?.Trim() ?? string.Empty) : questDescriptor2.Name);
			sousTitre = ((text5.Length > 0) ? $"{Strings.Story.Next} · {value} · {text5}" : $"{Strings.Story.Next} · {value}");
		}
		Color color = (flag ? TechniqueArt.Gold : TechniqueArt.Green);
		return new Annonce
		{
			Genre = Genre.Histoire,
			Etiquette = text3.ToUpperInvariant(),
			Titre = text4.ToUpperInvariant(),
			SousTitre = sousTitre,
			Accent = color,
			Degrade = new TechniqueArt.Stops(TechniqueArt.White, 0.3f, TechniqueArt.Mix(color, TechniqueArt.White, 0.6f), 0.75f, color),
			HoldMs = 4600L
		};
	}

	public void Update()
	{
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		ObserverLeSac(millisecondsUtc);
		VerserLesQuetes(millisecondsUtc);
		VerserLesCapsules(millisecondsUtc);
		RetenirLaFile();
		if (_debut < 0)
		{
			if (_file.Count == 0 || MasteryToast.EstAffiche)
			{
				EstAffichee = false;
				base.IsHidden = true;
				return;
			}
			_courante = _file[0];
			_file.RemoveAt(0);
			_debut = millisecondsUtc;
			if (_courante.Decouverte)
			{
				Audio.AddGameSound("lieu_decouvert.wav", loop: false);
			}
		}
		if (_courante == null || millisecondsUtc - _debut >= 240 + _courante.HoldMs + 300)
		{
			_courante = null;
			_debut = -1L;
			EstAffichee = false;
			base.IsHidden = true;
			return;
		}
		Intersect.Client.Framework.Gwen.Control.Base parent = base.Parent;
		if (parent != null)
		{
			int num = Math.Max(DungeonHud.ReservedBottom, TrialHud.ReservedBottom);
			SetBounds(0, 0, parent.Width, num + (int)(300f * Scale));
		}
		EstAffichee = true;
		base.IsHidden = false;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		if (_debut < 0)
		{
			return;
		}
		Annonce courante = _courante;
		if (courante == null)
		{
			return;
		}
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		float s = Scale;
		long num = Timing.Global.MillisecondsUtc - _debut;
		float num2 = Math.Clamp((float)num / 240f, 0f, 1f);
		num2 = 1f - (1f - num2) * (1f - num2);
		float num3 = ((num > 240 + courante.HoldMs) ? Math.Clamp((float)(num - 240 - courante.HoldMs) / 300f, 0f, 1f) : 0f);
		float alpha = num2 * (1f - num3);
		bool flag = courante.Genre == Genre.Lieu;
		IFont label = TechniqueArt.Label;
		IFont font = (courante.TitreSimple ? TechniqueArt.Heavy : TechniqueArt.Display);
		IFont font2 = TechniqueArt.Body ?? TechniqueArt.Label;
		int size = Pt(13f);
		int size2 = Pt(flag ? 40 : (courante.TitreSimple ? 24 : 34));
		int size3 = Pt(15f);
		int num4 = L(2f);
		int val = ((courante.Etiquette.Length > 0) ? TechniqueArt.SpacedWidth(renderer, label, size, courante.Etiquette, num4) : 0);
		Point point = TechniqueArt.Measure(renderer, font, size2, courante.Titre);
		int num5 = ((!courante.TitreSimple) ? ((int)MathF.Ceiling((float)point.Y * 0.2f)) : 0);
		int num6 = LargeurIconeCapsule(courante, L);
		(int, int) tuple = MesurerCapsule(courante, L);
		int num7 = point.X + num5 + num6;
		int val2 = (flag ? LargeurStatut(renderer, courante, label, font2, size3, L) : ((courante.SousTitre.Length > 0) ? TechniqueArt.Measure(renderer, font2, size3, courante.SousTitre).X : 0));
		(int, int) tuple2 = MesurerGains(renderer, courante, L, Pt);
		int num8 = L(flag ? 22 : 28);
		int val3 = Math.Max(Math.Max(Math.Max(Math.Max(val, num7), val2), tuple2.Item1), tuple.Item1) + num8 * 2;
		val3 = Math.Max(val3, L(flag ? 400 : 420));
		val3 = Math.Min(val3, base.Width - L(80f));
		int num9 = L(flag ? 9 : 12);
		int num10 = ((courante.Etiquette.Length > 0) ? L(16f) : 0);
		int num11 = ((flag || courante.SousTitre.Length > 0) ? L(20f) : 0);
		int num12 = (flag ? L(12f) : 0);
		int h = num9 + num10 + ((num10 > 0) ? L(4f) : 0) + point.Y + tuple.Item2 + tuple2.Item2 + ((num11 > 0) ? L(4f) : 0) + num11 + num12 + num9;
		int num13 = Math.Max(DungeonHud.ReservedBottom, TrialHud.ReservedBottom);
		int y = ((num13 > 0) ? (num13 + L(10f)) : L(18f)) - (int)((1f - num2) * (float)L(8f));
		Rectangle rect = new Rectangle((base.Width - val3) / 2, y, val3, h);
		int num14 = rect.X + rect.Width / 2;
		TechniqueArt.GlowRect(renderer, new Rectangle(rect.X - L(30f), rect.Y - L(20f), rect.Width + L(60f), rect.Height + L(40f)), A(courante.Accent, 0.22f), additive: false);
		TechniqueArt.Patch(renderer, "tech_panneau.png", rect, A(TechniqueArt.White), 12);
		TechniqueArt.Fill(renderer, new Rectangle(rect.X + 1, rect.Y + L(6f), Math.Max(2, L(3f)), rect.Height - L(12f)), A(courante.Accent));
		int num15 = rect.Y + num9;
		if (num10 > 0)
		{
			TechniqueArt.SpacedCenter(renderer, label, size, courante.Etiquette, num14, num15 + num10 / 2, num4, A(courante.Accent));
			num15 += num10 + L(4f);
		}
		int num16 = num14 - num7 / 2;
		string text = courante.Titre;
		if (courante.Capsule > 0)
		{
			DessinerIconeCapsule(renderer, courante, num16, num15 + point.Y / 2, num, alpha, L);
			num16 += num6;
			DessinerEclatDuCompteur(renderer, new Rectangle(num16, num15, point.X, point.Y), num, alpha, L);
			text = TitreCapsule(courante, num);
		}
		if (courante.TitreSimple)
		{
			TechniqueArt.Write(renderer, font, size2, courante.Titre, num16, num15, A(TechniqueArt.Text));
		}
		else
		{
			TechniqueArt.Styled(renderer, font, size2, text, num16, num15, courante.Degrade, alpha);
		}
		num15 += point.Y;
		if (tuple.Item2 > 0)
		{
			DessinerCapsule(renderer, courante, num14, num15, num, alpha, L);
			num15 += tuple.Item2;
		}
		if (tuple2.Item2 > 0)
		{
			DessinerGains(renderer, courante, new Rectangle(rect.X + num8, num15, rect.Width - num8 * 2, tuple2.Item2), num, alpha, Timing.Global.MillisecondsUtc, L, Pt);
			num15 += tuple2.Item2;
		}
		num15 += ((num11 > 0) ? L(4f) : 0);
		if (flag)
		{
			DessinerStatut(renderer, courante, label, font2, size3, num14, num15 + num11 / 2, alpha, L);
		}
		else if (courante.SousTitre.Length > 0)
		{
			TechniqueArt.TextCenter(renderer, font2, size3, courante.SousTitre, num14, num15 + num11 / 2, A(TechniqueArt.Soft));
		}
		num15 += num11;
		if (flag)
		{
			int num17 = num15 + L(6f);
			Rectangle rect2 = new Rectangle(num14 - L(120f), num17, L(240f), Math.Max(1, L(2f)));
			TechniqueArt.FillHorizontal(renderer, rect2, A(TechniqueArt.Ki, 0f), A(courante.Accent), A(TechniqueArt.Gold, 0f));
			if (courante.Decouverte && num >= 250 && num <= 1150)
			{
				float num18 = 1f - MathF.Pow(1f - (float)(num - 250) / 900f, 3f);
				int num19 = L(12f + num18 * 84f);
				TechniqueArt.Image(renderer, "lieu_anneau.png", new Rectangle(num14 - num19 / 2, num17 - num19 / 2, num19, num19), A(TechniqueArt.Ki, 0.85f * (1f - num18)));
			}
		}
		Color A(Color color, float factor = 1f)
		{
			return TechniqueArt.Alpha(color, factor * alpha);
		}
		int L(float value)
		{
			return (int)MathF.Round(value * s);
		}
		int Pt(float css)
		{
			return Math.Max(7, (int)MathF.Round(css * s * 0.75f));
		}
	}

	private static int LargeurStatut(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Annonce a, IFont? chipFont, IFont? extraFont, int size, Func<float, int> L)
	{
		int num = TechniqueArt.SpacedWidth(renderer, chipFont, size, a.Zone, L(2f));
		int num2 = ((a.Heure.Length > 0) ? TechniqueArt.Measure(renderer, extraFont, size, a.Heure).X : 0);
		return L(12f) + L(7f) + num + ((num2 > 0) ? (L(10f) * 2 + 1 + num2) : 0);
	}

	private static void DessinerStatut(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Annonce a, IFont? chipFont, IFont? extraFont, int size, int centreX, int centerY, float alpha, Func<float, int> L)
	{
		int num = LargeurStatut(renderer, a, chipFont, extraFont, size, L);
		int num2 = centreX - num / 2;
		int num3 = L(24f);
		TechniqueArt.Image(renderer, "lieu_point.png", new Rectangle(num2 + L(6f) - num3 / 2, centerY - num3 / 2, num3, num3), TechniqueArt.Alpha(a.ZoneCouleur, alpha));
		num2 += L(12f) + L(7f);
		num2 += TechniqueArt.Spaced(renderer, chipFont, size, a.Zone, num2, centerY, L(2f), TechniqueArt.Alpha(a.ZoneCouleur, alpha));
		if (a.Heure.Length != 0)
		{
			num2 += L(10f);
			TechniqueArt.Fill(renderer, new Rectangle(num2, centerY - L(6f), 1, L(12f)), TechniqueArt.Alpha(TechniqueArt.White, 0.28f * alpha));
			num2 += 1 + L(10f);
			TechniqueArt.TextMiddle(renderer, extraFont, size, a.Heure, num2, centerY, TechniqueArt.Alpha(Encre, alpha));
		}
	}

	public static void ObserverQuetes()
	{
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		if (me.Id != _personnage)
		{
			_personnage = me.Id;
			EtapesFaites.Clear();
			QuetesFaites.Clear();
			GainsAnnonces.Clear();
			Retenir(me.QuestProgress);
			return;
		}
		foreach (KeyValuePair<Guid, QuestProgress> item in me.QuestProgress)
		{
			item.Deconstruct(out var key, out var value);
			Guid guid = key;
			QuestProgress questProgress = value;
			(Guid, bool) value2;
			bool flag = Connues.TryGetValue(guid, out value2);
			bool flag2 = flag && value2.Item1 != Guid.Empty;
			bool flag3 = questProgress.Completed && questProgress.TaskId == Guid.Empty && (!flag || !value2.Item2 || (flag2 && EstLaDerniereTache(guid, value2.Item1)));
			if (flag2 && value2.Item1 != questProgress.TaskId && (questProgress.TaskId != Guid.Empty || (flag3 && EstLaDerniereTache(guid, value2.Item1))))
			{
				EtapesFaites.Add((guid, value2.Item1));
			}
			if (flag3)
			{
				QuetesFaites.Add(guid);
			}
		}
		Retenir(me.QuestProgress);
	}

	private static void Retenir(Dictionary<Guid, QuestProgress> progressions)
	{
		Connues.Clear();
		foreach (var (key, questProgress2) in progressions)
		{
			Connues[key] = (questProgress2.TaskId, questProgress2.Completed);
		}
	}

	private static bool EstLaDerniereTache(Guid queteId, Guid tacheId)
	{
		QuestDescriptor questDescriptor = DatabaseObject<QuestDescriptor>.Get(queteId);
		if (questDescriptor != null)
		{
			List<QuestTaskDescriptor> tasks = questDescriptor.Tasks;
			if (tasks != null && tasks.Count > 0)
			{
				List<QuestTaskDescriptor> tasks2 = questDescriptor.Tasks;
				return tasks2[tasks2.Count - 1].Id == tacheId;
			}
		}
		return false;
	}

	private void VerserLesQuetes(long maintenant)
	{
		foreach (var etapesFaite in EtapesFaites)
		{
			Guid item = etapesFaite.Quete;
			Guid item2 = etapesFaite.Tache;
			Annonce annonce = ComposerEtape(item, item2, maintenant);
			if (annonce != null)
			{
				Inserer(annonce);
			}
		}
		EtapesFaites.Clear();
		Guid guid = Guid.Empty;
		(Guid, Guid, string, string)? histoire = _histoire;
		if (histoire.HasValue)
		{
			(Guid, Guid, string, string) valueOrDefault = histoire.GetValueOrDefault();
			_histoire = null;
			guid = valueOrDefault.Item1;
			Annonce annonce2 = ComposerHistoire(valueOrDefault);
			annonce2.CreeeA = maintenant;
			long value;
			bool flag = GainsAnnonces.TryGetValue(valueOrDefault.Item1, out value) && maintenant - value < 30000;
			Inserer(flag ? annonce2 : AvecGains(annonce2, valueOrDefault.Item1, maintenant));
		}
		foreach (Guid quetesFaite in QuetesFaites)
		{
			if (quetesFaite != guid)
			{
				Annonce annonce3 = ComposerQuete(quetesFaite, maintenant);
				if (annonce3 != null)
				{
					Inserer(annonce3);
				}
			}
		}
		QuetesFaites.Clear();
	}

	private void Inserer(Annonce annonce)
	{
		int num = _file.FindIndex((Annonce attente) => attente.Genre == Genre.Evenement && !_vues.Contains(attente));
		if (num < 0)
		{
			_file.Add(annonce);
		}
		else
		{
			_file.Insert(num, annonce);
		}
		_vues.Add(annonce);
	}

	private void RetenirLaFile()
	{
		_vues.Clear();
		foreach (Annonce item in _file)
		{
			_vues.Add(item);
		}
	}

	private static Annonce? ComposerQuete(Guid queteId, long maintenant)
	{
		QuestDescriptor questDescriptor = DatabaseObject<QuestDescriptor>.Get(queteId);
		if (questDescriptor == null || string.IsNullOrWhiteSpace(questDescriptor.Name))
		{
			return null;
		}
		Annonce annonce = new Annonce
		{
			Genre = Genre.Histoire,
			Etiquette = Strings.Story.QuestDoneTag.ToString().ToUpperInvariant(),
			Titre = questDescriptor.Name.Trim().ToUpperInvariant(),
			Accent = TechniqueArt.Green,
			Degrade = new TechniqueArt.Stops(TechniqueArt.White, 0.3f, TechniqueArt.Mix(TechniqueArt.Green, TechniqueArt.White, 0.6f), 0.75f, TechniqueArt.Green),
			HoldMs = 2600L,
			CreeeA = maintenant
		};
		return AvecGains(annonce, queteId, maintenant);
	}

	private static Annonce AvecGains(Annonce annonce, Guid queteId, long maintenant)
	{
		QuestDescriptor questDescriptor = DatabaseObject<QuestDescriptor>.Get(queteId);
		if (questDescriptor == null)
		{
			return annonce;
		}
		Gain[] array = GainsDe(questDescriptor.Rewards);
		if (array.Length == 0)
		{
			return annonce;
		}
		string text = (questDescriptor.Name ?? string.Empty).Trim();
		bool flag = string.Equals(annonce.Titre, text.ToUpperInvariant(), StringComparison.Ordinal);
		annonce.Gains = array;
		annonce.Filet = (flag ? Strings.Banners.QuestGains.ToString() : Strings.Banners.QuestGainsOf.ToString(text)).ToUpperInvariant();
		annonce.HoldMs = Math.Max(annonce.HoldMs, 5200L);
		GainsAnnonces[queteId] = maintenant;
		return annonce;
	}

	private static Annonce? ComposerEtape(Guid queteId, Guid tacheId, long maintenant)
	{
		QuestTaskDescriptor questTaskDescriptor = DatabaseObject<QuestDescriptor>.Get(queteId)?.FindTask(tacheId);
		if (questTaskDescriptor == null)
		{
			return null;
		}
		Gain[] array = GainsDe(questTaskDescriptor.Rewards);
		if (array.Length == 0)
		{
			return null;
		}
		return new Annonce
		{
			Genre = Genre.Histoire,
			Etiquette = Strings.Banners.TaskDoneTag.ToString().ToUpperInvariant(),
			Titre = QuestTrackerPanel.ObjectiveOf(questTaskDescriptor),
			TitreSimple = true,
			Accent = BleuEtape,
			Degrade = TechniqueArt.Stops.Solid(TechniqueArt.Text),
			HoldMs = 3000L,
			Gains = array,
			Filet = Strings.Banners.TaskGains.ToString().ToUpperInvariant(),
			Petite = true,
			CreeeA = maintenant
		};
	}

	private static Gain[] GainsDe(QuestReward? recompense)
	{
		if (recompense == null || recompense.IsEmpty)
		{
			return Array.Empty<Gain>();
		}
		List<Gain> list = new List<Gain>();
		Guid guid = Globals.Me?.CurrencyItemId ?? Guid.Empty;
		long num = QuestRewardText.ZeniOf(recompense, guid);
		if (num > 0)
		{
			list.Add(new Gain
			{
				Genre = GenreDeGain.Zeni,
				Montant = num,
				Icone = IconeDeLaMonnaie(guid)
			});
		}
		if (recompense.Experience > 0)
		{
			list.Add(new Gain
			{
				Genre = GenreDeGain.Experience,
				Montant = recompense.Experience
			});
		}
		foreach (QuestRewardItem item in recompense.Items)
		{
			if (item.Quantity > 0 && !QuestRewardText.IsCurrency(item.ItemId, guid) && DatabaseObject<ItemDescriptor>.TryGet(item.ItemId, out var databaseObject))
			{
				list.Add(new Gain
				{
					Genre = GenreDeGain.Objet,
					Montant = item.Quantity,
					Nom = (databaseObject.Name ?? string.Empty),
					ObjetId = databaseObject.Id,
					Icone = (string.IsNullOrWhiteSpace(databaseObject.Icon) ? null : Globals.ContentManager?.GetTexture(TextureType.Item, databaseObject.Icon)),
					Couleur = ((JetDObjet.Plafond(databaseObject) == 0) ? CouleurDeRarete(databaseObject.Rarity) : null)
				});
			}
		}
		foreach (Guid spell in recompense.Spells)
		{
			if (DatabaseObject<SpellDescriptor>.TryGet(spell, out var databaseObject2))
			{
				list.Add(new Gain
				{
					Genre = GenreDeGain.Technique,
					Nom = (databaseObject2.Name ?? string.Empty),
					Icone = (string.IsNullOrWhiteSpace(databaseObject2.Icon) ? null : Globals.ContentManager?.GetTexture(TextureType.Spell, databaseObject2.Icon)),
					Couleur = TechniqueArt.Ki
				});
			}
		}
		if (list.Count > 6)
		{
			int num2 = list.Count - 5;
			list.RemoveRange(5, num2);
			list.Add(new Gain
			{
				Genre = GenreDeGain.Reste,
				Montant = num2
			});
		}
		return list.ToArray();
	}

	private static IGameTexture? IconeDeLaMonnaie(Guid monnaie)
	{
		if (monnaie != Guid.Empty && DatabaseObject<ItemDescriptor>.TryGet(monnaie, out var databaseObject) && !string.IsNullOrWhiteSpace(databaseObject.Icon))
		{
			IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Item, databaseObject.Icon);
			if (gameTexture != null)
			{
				return gameTexture;
			}
		}
		return TechniqueArt.Tex("hud_zeni.png");
	}

	private static Color CouleurDeRarete(int rarete)
	{
		if (!CustomColors.Items.Rarities.TryGetValue(rarete, out var value) || !(value != null))
		{
			return TechniqueArt.Text;
		}
		return value;
	}

	private static void ObserverLeSac(long maintenant)
	{
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		IItem[] inventory = me.Inventory;
		if (me.Id != _sacDe || _sac.Length != inventory.Length)
		{
			_sacDe = me.Id;
			_sac = new(Guid, int, ItemProperties)[inventory.Length];
			Recus.Clear();
			for (int i = 0; i < inventory.Length; i++)
			{
				_sac[i] = (Id: inventory[i]?.ItemId ?? Guid.Empty, Quantite: inventory[i]?.Quantity ?? 0, Jet: inventory[i]?.ItemProperties);
			}
			return;
		}
		for (int j = 0; j < inventory.Length; j++)
		{
			Guid guid = inventory[j]?.ItemId ?? Guid.Empty;
			int num = inventory[j]?.Quantity ?? 0;
			ItemProperties itemProperties = inventory[j]?.ItemProperties;
			(Guid, int, ItemProperties) tuple = _sac[j];
			if (guid != Guid.Empty && (guid != tuple.Item1 || num > tuple.Item2 || itemProperties != tuple.Item3))
			{
				Recus.Add((guid, itemProperties, maintenant));
			}
			_sac[j] = (Id: guid, Quantite: num, Jet: itemProperties);
		}
		Recus.RemoveAll(((Guid Id, ItemProperties? Jet, long A) recu) => maintenant - recu.A > 15000);
	}

	private static Color CouleurDe(Gain gain, long creeeA, long maintenant)
	{
		Color couleur = gain.Couleur;
		if ((object)couleur != null)
		{
			return couleur;
		}
		DatabaseObject<ItemDescriptor>.TryGet(gain.ObjetId, out var databaseObject);
		for (int num = Recus.Count - 1; num >= 0; num--)
		{
			if (Recus[num].Id == gain.ObjetId && Recus[num].A >= creeeA - 3000)
			{
				return gain.Couleur = CouleurDeRarete(JetDObjet.Rarete(databaseObject, Recus[num].Jet));
			}
		}
		Color color = CouleurDeRarete(databaseObject?.Rarity ?? 0);
		if (maintenant - creeeA > 2500)
		{
			gain.Couleur = color;
		}
		return color;
	}

	private static string Compte(long montant)
	{
		return "+" + SaiyanHud.Amount(montant);
	}

	private static string Quantite(long quantite)
	{
		return "×" + SaiyanHud.Amount(quantite);
	}

	private static int CoteDeLIcone(Gain gain, Func<float, int> L)
	{
		IGameTexture icone = gain.Icone;
		int num = ((icone != null && icone.Width > 0) ? icone.Width : 32);
		return num * Math.Max(1, (int)MathF.Round((float)L(32f) / (float)num));
	}

	private static int CoteDeLaCase(Gain gain, Func<float, int> L)
	{
		return Math.Max(L(42f), CoteDeLIcone(gain, L) + L(5f) * 2);
	}

	private static int CoteDeLaPiece(Gain gain, bool petite, Func<float, int> L)
	{
		int num = L(petite ? 24 : 30);
		IGameTexture icone = gain.Icone;
		int num2 = ((icone != null && icone.Width > 0) ? icone.Width : num);
		int num3 = Math.Max(1, (int)MathF.Round((float)num / (float)num2));
		if (!(MathF.Abs((float)num / (float)num2 - (float)num3) <= 0.2f))
		{
			return num;
		}
		return num2 * num3;
	}

	private static int HauteurDuRang(Annonce a, Func<float, int> L)
	{
		int num = L(a.Petite ? 34 : 44);
		Gain[] gains = a.Gains;
		foreach (Gain gain in gains)
		{
			GenreDeGain genre = gain.Genre;
			if ((uint)(genre - 2) <= 1u)
			{
				num = Math.Max(num, Math.Max(L(44f), CoteDeLaCase(gain, L) + L(2f)));
			}
		}
		return num;
	}

	private static void DessinerImage(Intersect.Client.Framework.Gwen.Renderer.Base renderer, IGameTexture? texture, Rectangle zone, Color couleur)
	{
		if (texture == null || texture.Width <= 0)
		{
			return;
		}
		GameRenderer gameRenderer = ((zone.Width % texture.Width == 0) ? null : Graphics.Renderer);
		bool linearFiltering = gameRenderer?.LinearFiltering ?? false;
		if (gameRenderer != null)
		{
			gameRenderer.LinearFiltering = true;
		}
		try
		{
			SaiyanHud.Draw(renderer, texture, zone.X, zone.Y, zone.Width, zone.Height, couleur);
		}
		finally
		{
			if (gameRenderer != null)
			{
				gameRenderer.LinearFiltering = linearFiltering;
			}
		}
	}

	private static (int Largeur, int Hauteur) MesurerGains(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Annonce a, Func<float, int> L, Func<float, int> Pt)
	{
		if (a.Gains.Length == 0)
		{
			return (Largeur: 0, Hauteur: 0);
		}
		int num = L(a.Petite ? 22 : 26);
		int num2 = num * (a.Gains.Length - 1);
		Gain[] gains = a.Gains;
		foreach (Gain gain in gains)
		{
			gain.Largeur = LargeurDuGain(renderer, gain, a.Petite, L, Pt);
			num2 += gain.Largeur;
		}
		int val = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(11f), a.Filet, L(2f)) + (L(12f) + L(40f)) * 2;
		int item = L(6f) + L(14f) + L(a.Petite ? 6 : 8) + HauteurDuRang(a, L) + L(4f);
		return (Largeur: Math.Max(num2, val), Hauteur: item);
	}

	private static int LargeurDuGain(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Gain gain, bool petite, Func<float, int> L, Func<float, int> Pt)
	{
		int size = Pt(petite ? 22f : 29f);
		return gain.Genre switch
		{
			GenreDeGain.Zeni => CoteDeLaPiece(gain, petite, L) + L(8f) + TechniqueArt.Measure(renderer, TechniqueArt.Heavy, size, Compte(gain.Montant)).X + L(8f) + TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(11f), Strings.Banners.GainZeni.ToString().ToUpperInvariant(), L(2f)), 
			GenreDeGain.Experience => LargeurDeLaPastille(renderer, L, Pt) + L(8f) + TechniqueArt.Measure(renderer, TechniqueArt.Heavy, size, Compte(gain.Montant)).X, 
			GenreDeGain.Objet => CoteDeLaCase(gain, L) + L(9f) + TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(15f), gain.Nom).X + L(9f) + TechniqueArt.Measure(renderer, TechniqueArt.Heavy, Pt(20f), Quantite(gain.Montant)).X, 
			GenreDeGain.Technique => CoteDeLaCase(gain, L) + L(9f) + TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(15f), gain.Nom).X, 
			_ => TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(15f), Strings.Banners.GainMore.ToString(gain.Montant)).X, 
		};
	}

	private static int LargeurDeLaPastille(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Func<float, int> L, Func<float, int> Pt)
	{
		return TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(11f), Strings.Banners.GainExp.ToString().ToUpperInvariant(), L(1.5f)) + L(6f) * 2;
	}

	private static void DessinerGains(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Annonce a, Rectangle zone, long age, float alpha, long maintenant, Func<float, int> L, Func<float, int> Pt)
	{
		int num = zone.X + zone.Width / 2;
		int num2 = zone.Y + L(6f);
		int num3 = num2 + L(7f);
		int num4 = TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(11f), a.Filet, num, num3, L(2f), A(TechniqueArt.Dim));
		int num5 = (zone.Width - num4) / 2 - L(12f);
		if (num5 > 0)
		{
			TechniqueArt.FillHorizontal(renderer, new Rectangle(zone.X, num3, num5, 1), A(a.Accent, 0f), A(a.Accent, 0.35f), A(a.Accent, 0.7f));
			TechniqueArt.FillHorizontal(renderer, new Rectangle(zone.Right - num5, num3, num5, 1), A(a.Accent, 0.7f), A(a.Accent, 0.35f), A(a.Accent, 0f));
		}
		num2 += L(14f) + L(a.Petite ? 6 : 8);
		int centreY = num2 + HauteurDuRang(a, L) / 2;
		int num6 = L(a.Petite ? 22 : 26);
		int num7 = num6 * (a.Gains.Length - 1);
		Gain[] gains = a.Gains;
		foreach (Gain gain in gains)
		{
			num7 += gain.Largeur;
		}
		int num8 = num - num7 / 2;
		for (int j = 0; j < a.Gains.Length; j++)
		{
			Gain gain2 = a.Gains[j];
			long num9 = age - (340 + (long)j * 120L);
			if (num9 >= 0)
			{
				DessinerGain(renderer, a, gain2, num8, centreY, num9, alpha, maintenant, L, Pt);
			}
			num8 += gain2.Largeur + num6;
		}
		Color A(Color color, float factor = 1f)
		{
			return TechniqueArt.Alpha(color, factor * alpha);
		}
	}

	private static void DessinerGain(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Annonce a, Gain gain, int x, int centreY, long depuis, float alpha, long maintenant, Func<float, int> L, Func<float, int> Pt)
	{
		float num = Math.Clamp((float)depuis / 300f, 0f, 1f);
		float num2 = 1f - MathF.Pow(1f - num, 3f);
		float num3 = Math.Clamp(num2 / 0.6f, 0f, 1f);
		float num4 = ((num2 < 0.6f) ? (0.9f + 0.15f * num3) : (1.05f - 0.05f * ((num2 - 0.6f) / 0.4f)));
		alpha *= num3;
		centreY += (int)MathF.Round((float)L(7f) * (1f - num3));
		float num5 = Math.Clamp((float)(depuis - 80) / 700f, 0f, 1f);
		long montant = (long)Math.Round((double)gain.Montant * (1.0 - Math.Pow(1f - num5, 3.0)));
		int size = Pt(a.Petite ? 22f : 29f);
		switch (gain.Genre)
		{
		case GenreDeGain.Zeni:
		{
			int num11 = CoteDeLaPiece(gain, a.Petite, L);
			DessinerImage(renderer, gain.Icone, new Rectangle(x, centreY - num11 / 2, num11, num11), A(TechniqueArt.White));
			x += num11 + L(8f);
			Point point = TechniqueArt.Measure(renderer, TechniqueArt.Heavy, size, Compte(gain.Montant));
			TechniqueArt.GlowRect(renderer, new Rectangle(x - L(10f), centreY - point.Y / 2 - L(4f), point.X + L(20f), point.Y + L(8f)), A(TechniqueArt.Gold, 0.16f));
			x += TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, size, Compte(montant), x, centreY, A(TechniqueArt.Gold)) + L(8f);
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(11f), Strings.Banners.GainZeni.ToString().ToUpperInvariant(), x, centreY + L(5f), L(2f), A(TechniqueArt.Dim));
			break;
		}
		case GenreDeGain.Experience:
		{
			Rectangle rect2 = new Rectangle(x, centreY - L(9f), LargeurDeLaPastille(renderer, L, Pt), L(18f));
			TechniqueArt.Patch(renderer, "tech_biais4.png", rect2, A(TechniqueArt.Ki), 5);
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(11f), Strings.Banners.GainExp.ToString().ToUpperInvariant(), rect2.X + rect2.Width / 2, centreY, L(1.5f), A(EncrePastille));
			x += rect2.Width + L(8f);
			Point point2 = TechniqueArt.Measure(renderer, TechniqueArt.Heavy, size, Compte(gain.Montant));
			TechniqueArt.GlowRect(renderer, new Rectangle(x - L(10f), centreY - point2.Y / 2 - L(4f), point2.X + L(20f), point2.Y + L(8f)), A(TechniqueArt.Ki, 0.16f));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, size, Compte(montant), x, centreY, A(OrangeExp));
			break;
		}
		case GenreDeGain.Objet:
		case GenreDeGain.Technique:
		{
			Color color = ((gain.Genre == GenreDeGain.Objet) ? CouleurDe(gain, a.CreeeA, maintenant) : (gain.Couleur ?? TechniqueArt.Ki));
			int num6 = CoteDeLaCase(gain, L);
			int num7 = (int)MathF.Round((float)num6 * num4);
			Rectangle rect = new Rectangle(x + (num6 - num7) / 2, centreY - num7 / 2, num7, num7);
			float num8 = ((depuis < 700) ? ((float)depuis / 700f) : 1f);
			float num9 = ((num8 < 0.3f) ? (num8 / 0.3f) : (1f - (num8 - 0.3f) / 0.7f));
			TechniqueArt.GlowRect(renderer, new Rectangle(rect.X - L(9f), rect.Y - L(9f), rect.Width + L(18f), rect.Height + L(18f)), A(color, 0.55f * num9), additive: false);
			TechniqueArt.Patch(renderer, "saiyan_slot.png", rect, A(TechniqueArt.White), 10);
			int num10 = CoteDeLIcone(gain, L);
			DessinerImage(renderer, gain.Icone, new Rectangle(x + (num6 - num10) / 2, centreY - num10 / 2, num10, num10), A(TechniqueArt.White));
			TechniqueArt.Fill(renderer, new Rectangle(rect.X + L(3f), rect.Bottom - L(3f), rect.Width - L(6f), Math.Max(1, L(2f))), A(color));
			x += num6 + L(9f);
			x += TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(15f), gain.Nom, x, centreY, A(color));
			if (gain.Genre == GenreDeGain.Objet)
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(20f), Quantite(gain.Montant), x + L(9f), centreY, A(TechniqueArt.Text));
			}
			break;
		}
		default:
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(15f), Strings.Banners.GainMore.ToString(gain.Montant), x, centreY, A(TechniqueArt.Dim));
			break;
		}
		Color A(Color color2, float factor = 1f)
		{
			return TechniqueArt.Alpha(color2, factor * alpha);
		}
	}
}
