using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Clan;
using Intersect.Client.Interface.Game.Dungeons;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Housing;
using Intersect.Client.Interface.Game.Personnage;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Localization;

namespace Intersect.Client.Interface.Game.Friends;

internal sealed class FenetreAmis : FenetreCharte
{
	private const float Largeur = 1240f;

	private const float Hauteur = 748f;

	private const float BodyTop = 58f;

	private const float BodyHeight = 676f;

	private const float RailLeft = 14f;

	private const float RailWidth = 224f;

	private const float CenterLeft = 248f;

	private const float CenterWidth = 700f;

	private const float Marge = 16f;

	private const float FicheLeft = 958f;

	private const float FicheWidth = 268f;

	private const float AddTop = 74f;

	private const float AddHeight = 36f;

	private const float ToolsTop = 124f;

	private const float ToolsHeight = 34f;

	private const float GridTop = 170f;

	private const float CardWidth = 329f;

	private const float CardHeight = 92f;

	private const float CardGap = 10f;

	private const int VisibleRows = 5;

	private static readonly Color BodyLight = new Color(255, 196, 204, 220);

	private static readonly Color GreyFrame = new Color(255, 70, 78, 96);

	private static readonly Color Rouge = new Color(255, 255, 120, 110);

	private int _filtre;

	private int _tri;

	private string _choisi = string.Empty;

	private int _scroll;

	private int _rangees;

	private Rectangle _grille;

	private TextBox _ajout;

	private TextBox _recherche;

	private string _requete = string.Empty;

	private bool _cherche;

	private const float AjoutLargeur = 532f;

	private const float ChampLargeur = 292f;

	protected override Rectangle Place => new Rectangle(180, 76, 1240, 748);

	private string NomDuTri => (_tri switch
	{
		1 => Strings.Friends.SortLevel, 
		2 => Strings.Friends.SortSeen, 
		_ => Strings.Friends.SortName, 
	}).ToString();

	public FenetreAmis(Canvas gameCanvas)
		: base(gameCanvas, "FenetreAmis")
	{
		ConstruireChamps();
	}

	public override void Hide()
	{
		_ajout.Blur();
		FermerRecherche();
		base.Hide();
	}

	public void UpdateList()
	{
	}

	public void Update()
	{
		if (!base.IsHidden)
		{
			Placer();
			PlacerChamps();
		}
	}

	protected override bool OnMouseWheeled(int delta)
	{
		Point point = LocalMouse();
		if (!base.IsHidden && _grille.Contains(point.X, point.Y))
		{
			_scroll = Math.Clamp(_scroll + ((delta <= 0) ? 1 : (-1)), 0, Math.Max(0, _rangees - 5));
			return true;
		}
		return base.OnMouseWheeled(delta);
	}

	private void ConstruireChamps()
	{
		_ajout = new TextBox(this, "Ajout")
		{
			Font = TechniqueArt.Body,
			FontSize = 11,
			ShouldDrawBackground = false,
			MaximumLength = 32
		};
		_ajout.SetTextColor(TechniqueArt.Text, ComponentState.Normal);
		_ajout.SubmitPressed += delegate
		{
			Ajouter();
		};
		Interface.FocusComponents.Add(_ajout);
		_recherche = new TextBox(this, "Recherche")
		{
			Font = TechniqueArt.Body,
			FontSize = 11,
			ShouldDrawBackground = false,
			MaximumLength = 32,
			IsHidden = true
		};
		_recherche.SetTextColor(TechniqueArt.Text, ComponentState.Normal);
		_recherche.TextChanged += delegate
		{
			_requete = PersonnageScreen.Normaliser(_recherche.Text);
			_scroll = 0;
		};
		Interface.FocusComponents.Add(_recherche);
	}

	private void PlacerChamps()
	{
		Poser(_ajout, _d.R(298f, 78f, 488f, 28f));
		if (_cherche)
		{
			Poser(_recherche, _d.R(296f, 127f, 162f, 28f));
		}
		void Poser(TextBox champ, Rectangle zone)
		{
			if (champ.FontSize != Pt(13.5f))
			{
				champ.FontSize = Pt(13.5f);
			}
			if (champ.X != zone.X || champ.Y != zone.Y || champ.Width != zone.Width || champ.Height != zone.Height)
			{
				champ.SetBounds(zone.X, zone.Y, zone.Width, zone.Height);
			}
		}
	}

	private void Ajouter()
	{
		string text = _ajout.Text?.Trim();
		if (text != null && text.Length >= 3)
		{
			PacketSender.SendAddFriend(text);
			_ajout.SetText(string.Empty);
		}
	}

	private void BasculerRecherche()
	{
		if (_cherche)
		{
			FermerRecherche();
			return;
		}
		_cherche = true;
		_recherche.IsHidden = false;
		PlacerChamps();
		_recherche.Focus();
	}

	private void FermerRecherche()
	{
		if (_cherche)
		{
			_cherche = false;
			_recherche.Blur();
			_recherche.Text = string.Empty;
			_recherche.IsHidden = true;
			_requete = string.Empty;
			_scroll = 0;
		}
	}

	private static List<IFriendInstance> Amis()
	{
		return (Globals.Me?.Friends ?? new List<IFriendInstance>()).Where((IFriendInstance ami) => ami != null).ToList();
	}

	private List<IFriendInstance> Visibles(List<IFriendInstance> amis)
	{
		IEnumerable<IFriendInstance> source = amis.Where((IFriendInstance ami) => _filtre == 0 || ((_filtre != 1) ? (!ami.Online) : ami.Online));
		if (_requete.Length > 0)
		{
			string[] mots = _requete.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			source = source.Where((IFriendInstance ami) => mots.All((string mot) => PersonnageScreen.Normaliser(ami.Name + " " + (ami.Map ?? string.Empty)).Contains(mot, StringComparison.Ordinal)));
		}
		StringComparer currentCultureIgnoreCase = StringComparer.CurrentCultureIgnoreCase;
		IOrderedEnumerable<IFriendInstance> source2 = source.OrderByDescending((IFriendInstance ami) => ami.Online);
		return (_tri switch
		{
			1 => source2.ThenByDescending((IFriendInstance ami) => ami.Level).ThenBy<IFriendInstance, string>((IFriendInstance ami) => ami.Name, currentCultureIgnoreCase), 
			2 => source2.ThenBy((IFriendInstance ami) => (!ami.Online) ? ((ami.LastSeenMinutes >= 0) ? ami.LastSeenMinutes : int.MaxValue) : 0).ThenBy<IFriendInstance, string>((IFriendInstance ami) => ami.Name, currentCultureIgnoreCase), 
			_ => source2.ThenBy<IFriendInstance, string>((IFriendInstance ami) => ami.Name, currentCultureIgnoreCase), 
		}).ToList();
	}

	private IFriendInstance? Choisi(List<IFriendInstance> amis)
	{
		return amis.FirstOrDefault((IFriendInstance ami) => ami.Name == _choisi) ?? amis.OrderByDescending((IFriendInstance ami) => ami.Online).ThenBy<IFriendInstance, string>((IFriendInstance ami) => ami.Name, StringComparer.CurrentCultureIgnoreCase).FirstOrDefault();
	}

	private static string VuDepuis(IFriendInstance ami)
	{
		if (ami.Online)
		{
			return string.Empty;
		}
		int lastSeenMinutes = ami.LastSeenMinutes;
		if (lastSeenMinutes < 60)
		{
			if (lastSeenMinutes >= 0)
			{
				if (lastSeenMinutes < 2)
				{
					return Strings.Friends.SeenJustNow;
				}
				return Strings.Friends.SeenMinutes.ToString(lastSeenMinutes);
			}
			return Strings.Friends.Offline;
		}
		if (lastSeenMinutes < 2880)
		{
			if (lastSeenMinutes < 1440)
			{
				return Strings.Friends.SeenHours.ToString(lastSeenMinutes / 60);
			}
			return Strings.Friends.SeenYesterday;
		}
		if (lastSeenMinutes < 86400)
		{
			return Strings.Friends.SeenDays.ToString(lastSeenMinutes / 1440);
		}
		return Strings.Friends.SeenLongAgo;
	}

	private static string Lieu(IFriendInstance ami)
	{
		if (!string.IsNullOrWhiteSpace(ami.Map))
		{
			return Traduction.Contenu(ami.Map);
		}
		return Strings.Friends.SectionOnline.ToString();
	}

	private static void DemanderRetrait(string nom)
	{
		ConfirmCardWindow.Open(new ConfirmCardRequest
		{
			Title = Strings.ConfirmCard.RemoveFriendTitle,
			Icon = "saiyan_carte_icone_ami.png",
			Heading = nom,
			Prompt = Strings.ConfirmCard.RemoveFriendPrompt,
			ConfirmLabel = Strings.ConfirmCard.Remove,
			Danger = true,
			PlayerName = nom
		}, delegate
		{
			PacketSender.SendRemoveFriend(nom);
		});
	}

	protected override void Dessiner(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		DessinerCadre(renderer);
		int num = DessinerBarre(renderer, Strings.Friends.Title);
		List<IFriendInstance> list = Amis();
		int num2 = list.Count((IFriendInstance ami) => ami.Online);
		int num3 = _d.Y(23f);
		int num4 = num - _d.L(18f);
		(string, string, Color)[] array = new(string, string, Color)[2]
		{
			(Strings.Friends.Title.ToString(), $"{list.Count}", TechniqueArt.Text),
			(Strings.Friends.SectionOnline.ToString(), $"{num2}", (num2 > 0) ? TechniqueArt.Green : TechniqueArt.Soft)
		};
		for (int num5 = 0; num5 < array.Length; num5++)
		{
			(string, string, Color) tuple = array[num5];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			Color item3 = tuple.Item3;
			int num6 = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(16f), item2, num4, num3, A(item3));
			num4 -= num6 + _d.L(8f);
			int num7 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10f), item.ToUpperInvariant(), _d.Em(0.16f, 10f));
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), item.ToUpperInvariant(), num4 - num7, num3 + _d.L(1f), _d.Em(0.16f, 10f), A(TechniqueArt.Dim));
			num4 -= num7 + _d.L(22f);
		}
		DrawRail(renderer, list, num2);
		DrawCentre(renderer, list);
		DrawFiche(renderer, list);
	}

	protected override void DessinerDessus(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		DessinerEtiquette(renderer, now);
	}

	private void Panel(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect)
	{
		TechniqueArt.Patch(renderer, "tech_panneau.png", rect, A(TechniqueArt.White), 12);
	}

	private void Portrait(Intersect.Client.Framework.Gwen.Renderer.Base renderer, IFriendInstance ami, Rectangle hex, Color accent)
	{
		Hexagone(renderer, hex, accent, null, TechniqueArt.White, !ami.Online);
		int num = Math.Max(2, _d.L(5f));
		Rectangle rect = new Rectangle(hex.X + num * 2, hex.Y + num * 2, hex.Width - num * 4, hex.Height - num * 4);
		Color color = (ami.Online ? TechniqueArt.White : new Color(255, 196, 202, 214));
		if (!string.IsNullOrWhiteSpace(ami.Face))
		{
			IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Face, ami.Face);
			if (gameTexture != null)
			{
				Lisser(gameTexture, rect, A(color));
				return;
			}
		}
		if (!string.IsNullOrWhiteSpace(ami.Sprite))
		{
			(IGameTexture, float, float, float, float)? tuple = DungeonEntryWindow.StandingFrame(ami.Sprite);
			if (tuple.HasValue)
			{
				(IGameTexture, float, float, float, float) valueOrDefault = tuple.GetValueOrDefault();
				var (gameTexture2, num2, num3, num4) = FenetreClan.Tete(ami.Sprite, valueOrDefault);
				SaiyanHud.Draw(renderer, gameTexture2, rect.X, rect.Y, rect.Width, rect.Height, A(color), num2, num3, num2 + num4 / (float)gameTexture2.Width, num3 + num4 / (float)gameTexture2.Height);
			}
		}
	}

	private void Debout(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string? sprite, int centreX, int piedsY, int hauteur, Color teinte)
	{
		if (!string.IsNullOrWhiteSpace(sprite))
		{
			(IGameTexture, float, float, float, float)? tuple = DungeonEntryWindow.StandingFrame(sprite);
			if (tuple.HasValue)
			{
				(IGameTexture, float, float, float, float) valueOrDefault = tuple.GetValueOrDefault();
				(IGameTexture, float, float, float, float) tuple2 = valueOrDefault;
				IGameTexture item = tuple2.Item1;
				float item2 = tuple2.Item2;
				float item3 = tuple2.Item3;
				float item4 = tuple2.Item4;
				float item5 = tuple2.Item5;
				int num = Math.Max(1, (int)MathF.Round((float)hauteur / item5));
				int num2 = (int)(item4 * (float)num);
				int num3 = (int)(item5 * (float)num);
				SaiyanHud.Draw(renderer, item, centreX - num2 / 2, piedsY - num3, num2, num3, A(teinte), item2 / (float)item.Width, item3 / (float)item.Height, (item2 + item4) / (float)item.Width, (item3 + item5) / (float)item.Height);
			}
		}
	}

	private void DrawRail(Intersect.Client.Framework.Gwen.Renderer.Base renderer, List<IFriendInstance> amis, int enLigne)
	{
		Panel(renderer, _d.R(14f, 58f, 224f, 676f));
		float num = 70f;
		(string, int, Color, int)[] array = new(string, int, Color, int)[3]
		{
			(Strings.Friends.FilterAll.ToString(), amis.Count, TechniqueArt.Text, 0),
			(Strings.Friends.FilterOnline.ToString(), enLigne, TechniqueArt.Green, 1),
			(Strings.Friends.FilterOffline.ToString(), amis.Count - enLigne, TechniqueArt.Dim, 2)
		};
		(string, int, Color, int)[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			(string, int, Color, int) tuple = array2[i];
			string item = tuple.Item1;
			int item2 = tuple.Item2;
			Color item3 = tuple.Item3;
			int item4 = tuple.Item4;
			Rectangle rect = _d.R(22f, num, 208f, 36f);
			bool flag = _filtre == item4;
			string cle = $"rail-{item4}";
			if (flag)
			{
				TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Ki, 0.18f), A(TechniqueArt.Ki, 0.08f), A(TechniqueArt.Ki, 0.02f));
				TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Ki, 0.45f));
				TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(3f), rect.Height), A(TechniqueArt.Ki));
			}
			else if (IsHot(cle))
			{
				TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, 0.04f));
				TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
			}
			int centerY = rect.Y + rect.Height / 2;
			DungeonHud.Diamond(renderer, rect.X + _d.L(12f), centerY, _d.L(7f), A(item3, 0.9f));
			int num2 = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(14f), $"{item2}", rect.Right - _d.L(10f), centerY, A(flag ? TechniqueArt.Text : TechniqueArt.Dim));
			string text = TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(13f), item, rect.Right - _d.L(18f) - num2 - (rect.X + _d.L(26f)), 1).FirstOrDefault();
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(13f), text, rect.X + _d.L(26f), centerY, A(flag ? TechniqueArt.Text : BodyLight));
			int choix = item4;
			AddHit(rect, cle, delegate
			{
				_filtre = choix;
				_scroll = 0;
			});
			num += 38f;
		}
		List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), Strings.Friends.Tip, _d.L(200f), 3);
		int num3 = _d.Y(722f);
		for (int num4 = list.Count - 1; num4 >= 0; num4--)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), list[num4], _d.X(26f), num3 - _d.L(7f), A(TechniqueArt.Dim));
			num3 -= _d.L(14f);
		}
	}

	private void DrawCentre(Intersect.Client.Framework.Gwen.Renderer.Base renderer, List<IFriendInstance> amis)
	{
		Panel(renderer, _d.R(248f, 58f, 700f, 676f));
		float num = 264f;
		_grille = default(Rectangle);
		_rangees = 0;
		Rectangle rect = _d.R(num, 74f, 532f, 36f);
		TechniqueArt.Fill(renderer, rect, A(new Color(255, 7, 10, 18)));
		TechniqueArt.Outline(renderer, rect, A(InputHandlerAjout() ? TechniqueArt.Ki : TechniqueArt.Line2));
		int num2 = _d.L(14f);
		Lisser("saiyan_amis_icone_ajout.png", new Rectangle(rect.X + _d.L(10f), rect.Y + (rect.Height - num2) / 2, num2, num2), A(TechniqueArt.Soft));
		if (string.IsNullOrEmpty(_ajout.Text))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13.5f), Strings.Friends.AddHint, rect.X + _d.L(34f), rect.Y + rect.Height / 2, A(TechniqueArt.Dim));
		}
		Rectangle rect2 = _d.R(num + 532f + 8f, 74f, 128f, 36f);
		bool flag = (_ajout.Text?.Trim().Length ?? 0) >= 3;
		HouseArt.Cta(renderer, _d, rect2, Strings.Friends.AddButton.ToString(), flag, flag && IsHot("ajouter"), _fade);
		AddHit(rect2, "ajouter", Ajouter);
		List<IFriendInstance> list = Visibles(amis);
		string titre = ((_requete.Length > 0) ? Strings.Friends.Results.ToString(list.Count) : Strings.Friends.Title.ToString());
		DessinerOutils(renderer, titre, (amis.Count == 1) ? Strings.Friends.FooterOne.ToString(amis.Count((IFriendInstance ami) => ami.Online)) : Strings.Friends.FooterMany.ToString(amis.Count, amis.Count((IFriendInstance ami) => ami.Online)));
		if (amis.Count == 0)
		{
			TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(14f), Strings.Friends.Empty, _d.X(598f), _d.Y(270f), A(TechniqueArt.Soft));
			return;
		}
		if (list.Count == 0)
		{
			LocalizedString localizedString = ((_requete.Length > 0) ? Strings.Friends.NoResult : ((_filtre == 1) ? Strings.Friends.NoneOnline : Strings.Friends.NothingHere));
			TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(14f), localizedString, _d.X(598f), _d.Y(270f), A(TechniqueArt.Soft));
			return;
		}
		_rangees = (list.Count + 1) / 2;
		_scroll = Math.Clamp(_scroll, 0, Math.Max(0, _rangees - 5));
		_grille = _d.R(num, 170f, 668f, 500f);
		Rectangle previous = TechniqueArt.BeginClip(renderer, _grille);
		for (int num3 = _scroll * 2; num3 < list.Count && num3 - _scroll * 2 < 10; num3++)
		{
			int num4 = num3 - _scroll * 2;
			DrawCarte(renderer, list[num3], num + (float)(num4 % 2) * 339f, 170f + (float)(num4 / 2) * 102f);
		}
		TechniqueArt.EndClip(renderer, previous);
		if (_rangees > 5)
		{
			Rectangle rect3 = new Rectangle(_grille.Right + _d.L(5f), _grille.Y, _d.L(3f), _grille.Height);
			TechniqueArt.Fill(renderer, rect3, A(TechniqueArt.White, 0.05f));
			int num5 = Math.Max(_d.L(24f), rect3.Height * 5 / _rangees);
			int y = rect3.Y + (rect3.Height - num5) * _scroll / Math.Max(1, _rangees - 5);
			TechniqueArt.Fill(renderer, new Rectangle(rect3.X, y, rect3.Width, num5), A(TechniqueArt.Line2));
		}
	}

	private bool InputHandlerAjout()
	{
		return InputHandler.KeyboardFocus == _ajout;
	}

	private void DessinerOutils(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string titre, string compte)
	{
		int num = _d.Y(141f);
		int num2 = _d.X(264f);
		int num3 = _d.X(932f);
		int num4 = _d.L(34f);
		int num5 = _d.Y(124f);
		string text = $"{Strings.Friends.SortTool} : {NomDuTri}";
		int num6 = TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(12f), text).X + _d.L(34f);
		Rectangle rect = new Rectangle(num3 - num6, num5 + (num4 - _d.L(28f)) / 2, num6, _d.L(28f));
		bool flag = IsHot("outil:tri");
		TechniqueArt.Fill(renderer, rect, A(new Color(255, 10, 14, 24)));
		TechniqueArt.Outline(renderer, rect, A(flag ? TechniqueArt.Line2 : TechniqueArt.Line));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(12f), text, rect.X + _d.L(10f), num, A(flag ? TechniqueArt.Text : TechniqueArt.Soft));
		FenetreCharte.Triangle(renderer, rect.Right - _d.L(18f), num, _d.L(9f), _d.L(5f), versLeHaut: false, A(flag ? TechniqueArt.Ki : TechniqueArt.Soft));
		if (flag)
		{
			Etiqueter("outil:tri", rect, Strings.Friends.SortHint);
		}
		AddHit(rect, "outil:tri", delegate
		{
			_tri = (_tri + 1) % 3;
			_scroll = 0;
		});
		num3 -= num6 + _d.L(7f);
		Rectangle rect2 = new Rectangle(num3 - _d.L(30f), num5 + (num4 - _d.L(30f)) / 2, _d.L(30f), _d.L(30f));
		bool flag2 = IsHot("outil:loupe");
		if (_cherche)
		{
			TechniqueArt.FillVertical(renderer, rect2, A(new Color(255, 255, 194, 122)), A(new Color(255, 224, 117, 26)));
			TechniqueArt.Outline(renderer, rect2, A(new Color(255, 255, 194, 122)));
		}
		else
		{
			TechniqueArt.Fill(renderer, rect2, A(new Color(255, 10, 14, 24)));
			TechniqueArt.Outline(renderer, rect2, A(flag2 ? TechniqueArt.Line2 : TechniqueArt.Line));
		}
		Color couleur = A(_cherche ? new Color(255, 23, 11, 2) : (flag2 ? TechniqueArt.White : TechniqueArt.Soft));
		int num7 = _d.L(14f);
		Lisser(_cherche ? "tech_croix.png" : "inv_loupe.png", new Rectangle(rect2.X + (rect2.Width - num7) / 2, rect2.Y + (rect2.Height - num7) / 2, num7, num7), couleur);
		if (flag2)
		{
			Etiqueter("outil:loupe", rect2, _cherche ? Strings.Inventaire.CloseSearch : Strings.Friends.SearchTool);
		}
		AddHit(rect2, "outil:loupe", BasculerRecherche);
		num3 -= _d.L(30f) + _d.L(10f);
		if (_cherche)
		{
			Rectangle rect3 = new Rectangle(num2, num5, Math.Min(_d.L(292f), num3 - num2), num4);
			TechniqueArt.Fill(renderer, rect3, A(new Color(255, 7, 10, 18)));
			TechniqueArt.Outline(renderer, rect3, A(TechniqueArt.Ki));
			Lisser("inv_loupe.png", new Rectangle(rect3.X + _d.L(10f), rect3.Y + (rect3.Height - num7) / 2, num7, num7), A(TechniqueArt.Soft));
			if (string.IsNullOrEmpty(_recherche.Text))
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13.5f), Strings.Friends.SearchPlaceholder, rect3.X + _d.L(32f), num, A(TechniqueArt.Dim));
			}
			if (_requete.Length > 0)
			{
				TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(12f), titre, rect3.Right - _d.L(10f), num, A(TechniqueArt.Ki));
			}
		}
		else
		{
			DungeonHud.Diamond(renderer, num2 + _d.L(6f), num, _d.L(10f), A(TechniqueArt.Green));
			int num8 = TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(22f), titre.ToUpperInvariant(), num2 + _d.L(20f), num, A(TechniqueArt.Text));
			string text2 = TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(13f), compte, num3 - (num2 + _d.L(20f) + num8 + _d.L(12f)), 1).FirstOrDefault();
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(13f), text2, num2 + _d.L(20f) + num8 + _d.L(12f), num + _d.L(2f), A(TechniqueArt.Dim));
		}
	}

	private void DrawCarte(Intersect.Client.Framework.Gwen.Renderer.Base renderer, IFriendInstance ami, float x, float y)
	{
		Rectangle rect = _d.R(x, y, 329f, 92f);
		bool flag = ami.Name == _choisi;
		string text = "carte-" + ami.Name;
		bool flag2 = IsHot(text) || IsHot(text + ":message") || IsHot(text + ":groupe") || IsHot(text + ":retirer");
		if (flag)
		{
			TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Ki, 0.14f), A(TechniqueArt.Ki, 0.06f), A(TechniqueArt.Ki, 0.02f));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Ki));
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(3f), rect.Height), A(TechniqueArt.Ki));
		}
		else if (ami.Online)
		{
			TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Green, flag2 ? 0.12f : 0.07f), A(TechniqueArt.Green, 0.03f), A(TechniqueArt.Green, 0.01f));
			TechniqueArt.Outline(renderer, rect, A(flag2 ? TechniqueArt.Line2 : TechniqueArt.Line));
		}
		else
		{
			TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, flag2 ? 0.05f : 0.025f));
			TechniqueArt.Outline(renderer, rect, A(flag2 ? TechniqueArt.Line2 : TechniqueArt.Line));
		}
		Portrait(renderer, ami, _d.R(x + 10f, y + 19f, 54f, 54f), ami.Online ? TechniqueArt.Ki : GreyFrame);
		int num = _d.L(10f);
		Rectangle rect2 = new Rectangle(rect.X + _d.L(52f), rect.Y + _d.L(60f), num, num);
		TechniqueArt.Fill(renderer, rect2, A(new Color(255, 10, 14, 24)));
		TechniqueArt.Fill(renderer, new Rectangle(rect2.X + 2, rect2.Y + 2, num - 4, num - 4), A(ami.Online ? TechniqueArt.Green : TechniqueArt.Dim));
		int num2 = rect.X + _d.L(74f);
		int num3 = rect.Y + _d.L(22f);
		int num4 = 0;
		if (ami.Level > 0)
		{
			string text2 = Strings.Friends.Level.ToString(ami.Level).ToUpperInvariant();
			num4 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10f), text2, _d.Em(0.1f, 10f)) + _d.L(12f);
			Rectangle rect3 = new Rectangle(rect.Right - _d.L(12f) - num4, num3 - _d.L(9f), num4, _d.L(18f));
			TechniqueArt.Fill(renderer, rect3, A(new Color(178, 6, 8, 14)));
			TechniqueArt.Outline(renderer, rect3, A(ami.Online ? TechniqueArt.Gold : TechniqueArt.Line2, 0.6f));
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(10f), text2, rect3.X + rect3.Width / 2, rect3.Y + rect3.Height / 2, _d.Em(0.1f, 10f), A(ami.Online ? TechniqueArt.Gold : TechniqueArt.Dim));
			num4 += _d.L(10f);
		}
		int width = rect.Right - _d.L(12f) - num4 - num2;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(16.5f), TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(16.5f), ami.Name, width, 1).FirstOrDefault(), num2, num3, A(ami.Online ? TechniqueArt.Text : BodyLight));
		int num5 = rect.Y + _d.L(45f);
		if (ami.Online)
		{
			int num6 = _d.L(11f);
			Lisser("saiyan_amis_icone_lieu.png", new Rectangle(num2, num5 - num6 / 2, num6, num6), A(TechniqueArt.Green));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), Lieu(ami), rect.Right - _d.L(12f) - num2 - _d.L(16f), 1).FirstOrDefault(), num2 + num6 + _d.L(5f), num5, A(TechniqueArt.Soft));
		}
		else
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), VuDepuis(ami), rect.Right - _d.L(12f) - num2, 1).FirstOrDefault(), num2, num5, A(TechniqueArt.Dim));
		}
		List<(string, string, string, Color, Action)> list = new List<(string, string, string, Color, Action)>();
		string nom = ami.Name;
		if (ami.Online)
		{
			list.Add((text + ":message", "saiyan_amis_icone_message.png", Strings.Friends.MessageTip, TechniqueArt.Gold, delegate
			{
				Interface.GameUi?.SetChatboxText("/pm " + nom + " ");
			}));
			list.Add((text + ":groupe", "saiyan_amis_icone_groupe.png", Strings.Friends.PartyTip, TechniqueArt.Gold, delegate
			{
				PacketSender.SendPartyInvite(nom);
			}));
		}
		list.Add((text + ":retirer", "saiyan_amis_icone_retirer.png", Strings.Friends.RemoveFriend, Rouge, delegate
		{
			DemanderRetrait(nom);
		}));
		int num7 = rect.Right - _d.L(12f);
		foreach (var item5 in list.AsEnumerable().Reverse())
		{
			string item = item5.Item1;
			string item2 = item5.Item2;
			string item3 = item5.Item3;
			Color item4 = item5.Item4;
			Action action = item5.Item5;
			Rectangle rect4 = new Rectangle(num7 - _d.L(26f), rect.Bottom - _d.L(12f) - _d.L(26f), _d.L(26f), _d.L(26f));
			bool flag3 = IsHot(item);
			if (flag2)
			{
				TechniqueArt.Fill(renderer, rect4, A(new Color(255, 10, 14, 24)));
				TechniqueArt.Outline(renderer, rect4, A(flag3 ? item4 : TechniqueArt.Line2));
			}
			int num8 = _d.L(14f);
			Lisser(item2, new Rectangle(rect4.X + (rect4.Width - num8) / 2, rect4.Y + (rect4.Height - num8) / 2, num8, num8), A(flag3 ? item4 : (flag2 ? TechniqueArt.Soft : TechniqueArt.Dim), flag2 ? 1f : 0.6f));
			if (flag3)
			{
				Etiqueter(item, rect4, item3);
			}
			AddHit(rect4, item, delegate
			{
				_choisi = nom;
				action();
			});
			num7 -= _d.L(26f) + _d.L(6f);
		}
		AddHit(rect, text, delegate
		{
			_choisi = nom;
		});
	}

	private void DrawFiche(Intersect.Client.Framework.Gwen.Renderer.Base renderer, List<IFriendInstance> amis)
	{
		Panel(renderer, _d.R(958f, 58f, 268f, 676f));
		int x = _d.X(974f);
		int width = _d.L(236f);
		IFriendInstance friendInstance = Choisi(amis);
		if (friendInstance == null)
		{
			float num = 98f;
			{
				foreach (string item5 in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), (amis.Count == 0) ? Strings.Friends.Empty.ToString() : Strings.Friends.PickHint.ToString(), width, 6))
				{
					TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), item5, x, _d.Y(num), A(BodyLight));
					num += 16f;
				}
				return;
			}
		}
		_choisi = friendInstance.Name;
		Color color = (friendInstance.Online ? TechniqueArt.Green : GreyFrame);
		Rectangle rect = _d.R(970f, 70f, 244f, 196f);
		TechniqueArt.FillVertical(renderer, rect, A(new Color(255, 10, 18, 32)), A(new Color(255, 13, 21, 36)));
		TechniqueArt.Glow(renderer, (float)rect.X + (float)rect.Width / 2f, rect.Bottom - _d.L(50f), _d.L(96f), A(color, 0.3f));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
		Debout(renderer, friendInstance.Sprite, rect.X + rect.Width / 2, rect.Bottom - _d.L(12f), _d.L(150f), friendInstance.Online ? TechniqueArt.White : new Color(255, 150, 156, 168));
		if (string.IsNullOrWhiteSpace(friendInstance.Sprite))
		{
			Portrait(renderer, friendInstance, new Rectangle(rect.X + (rect.Width - _d.L(92f)) / 2, rect.Y + (rect.Height - _d.L(92f)) / 2, _d.L(92f), _d.L(92f)), color);
		}
		string text = (friendInstance.Online ? Strings.Friends.SectionOnline : Strings.Friends.SectionOffline).ToString().ToUpperInvariant();
		int w = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10f), text, _d.Em(0.14f, 10f)) + _d.L(12f);
		Rectangle rect2 = new Rectangle(rect.X + _d.L(8f), rect.Y + _d.L(8f), w, _d.L(20f));
		if (friendInstance.Online)
		{
			TechniqueArt.Fill(renderer, rect2, A(TechniqueArt.Green));
		}
		else
		{
			TechniqueArt.Fill(renderer, rect2, A(new Color(217, 6, 8, 14)));
			TechniqueArt.Outline(renderer, rect2, A(TechniqueArt.Line2));
		}
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), text, rect2.X + _d.L(6f), rect2.Y + rect2.Height / 2, _d.Em(0.14f, 10f), A(friendInstance.Online ? HouseArt.Dark : TechniqueArt.Text));
		float num2 = 288f;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(23f), TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(23f), friendInstance.Name, width, 1).FirstOrDefault(), x, _d.Y(num2), A(TechniqueArt.Text));
		num2 += 22f;
		if (friendInstance.Level > 0)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), Strings.Friends.Level.ToString(friendInstance.Level), x, _d.Y(num2), A(TechniqueArt.Gold));
			num2 += 17f;
		}
		num2 += 8f;
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10.5f), (friendInstance.Online ? Strings.Friends.Where : Strings.Friends.LastSeenLabel).ToString().ToUpperInvariant(), x, _d.Y(num2), _d.Em(0.14f, 10.5f), A(TechniqueArt.Dim));
		num2 += 16f;
		foreach (string item6 in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), friendInstance.Online ? Lieu(friendInstance) : VuDepuis(friendInstance), width, 2))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), item6, x, _d.Y(num2), A(friendInstance.Online ? TechniqueArt.Text : BodyLight));
			num2 += 16f;
		}
		string nom = friendInstance.Name;
		List<(string, string, Color, Action)> list = new List<(string, string, Color, Action)>();
		if (friendInstance.Online)
		{
			list.Add(("fiche:message", Strings.Friends.MessageButton.ToString(), null, delegate
			{
				Interface.GameUi?.SetChatboxText("/pm " + nom + " ");
			}));
			list.Add(("fiche:groupe", Strings.Friends.InviteButton.ToString(), null, delegate
			{
				PacketSender.SendPartyInvite(nom);
			}));
		}
		list.Add(("fiche:retirer", Strings.Friends.RemoveButton.ToString(), Rouge, delegate
		{
			DemanderRetrait(nom);
		}));
		float num3 = 686f;
		foreach (var item7 in list.AsEnumerable().Reverse())
		{
			string item = item7.Item1;
			string item2 = item7.Item2;
			Color item3 = item7.Item3;
			Action item4 = item7.Item4;
			Rectangle rect3 = _d.R(974f, num3, 236f, 36f);
			HouseArt.Hollow(renderer, _d, rect3, item2, IsHot(item), _fade, item3);
			AddHit(rect3, item, item4);
			num3 -= 42f;
		}
	}
}
