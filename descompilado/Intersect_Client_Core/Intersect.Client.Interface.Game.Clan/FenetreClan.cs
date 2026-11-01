using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.Control.EventArguments.InputSubmissionEvent;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Dungeons;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Housing;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Config.Guilds;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Localization;
using Intersect.Models;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Interface.Game.Clan;

internal sealed class FenetreClan : FenetreCharte
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

	private const float ChampTop = 74f;

	private const float ChampHeight = 36f;

	private const float TitreTop = 124f;

	private const float TitreHeight = 34f;

	private const float ListeTop = 170f;

	private const float ListeBas = 718f;

	private const float LigneWidth = 668f;

	private const float MembreHeight = 52f;

	private const float ClanHeight = 58f;

	private const float CandidatureHeight = 44f;

	private const float ObjectifHeight = 46f;

	private const float Pas = 6f;

	private const int PageAnnuaire = 20;

	private static readonly Color BodyLight = new Color(255, 196, 204, 220);

	private static readonly Color Rouge = new Color(255, 255, 120, 110);

	private static readonly Color GreyFrame = new Color(255, 70, 78, 96);

	private bool _annuaire;

	private Guid _membreChoisi;

	private string _clanChoisi = string.Empty;

	private string _page = string.Empty;

	private int _scroll;

	private int _rangees;

	private int _visibles;

	private Rectangle _liste;

	private TextBox _fondation;

	private TextBox _invitation;

	private TextBox _recherche;

	private TextBox _tag;

	private bool _identite;

	private int _emblemeChoisi;

	private int _couleurChoisie;

	private const float ChampLargeur = 532f;

	private const float LargeurPage = 1212f;

	private const float PageHeroHeight = 232f;

	private const float PageStatsTop = 300f;

	private const float PageStatsHeight = 62f;

	private const float PageBasTop = 372f;

	private const float PageGaucheWidth = 392f;

	private const float PageMeneurHeight = 190f;

	private const int PageColonnes = 4;

	private static readonly Color GrisHorsLigne = new Color(255, 196, 202, 214);

	protected override Rectangle Place => new Rectangle(180, 76, 1240, 748);

	public FenetreClan(Canvas gameCanvas)
		: base(gameCanvas, "FenetreClan")
	{
		ConstruireChamps();
	}

	public void Open()
	{
		_annuaire = false;
		_page = string.Empty;
		_scroll = 0;
		Show();
		BringToFront();
	}

	public override void Hide()
	{
		_identite = false;
		_tag.Blur();
		_fondation.Blur();
		_invitation.Blur();
		_recherche.Blur();
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
		if (!base.IsHidden && _liste.Contains(point.X, point.Y))
		{
			_scroll = Math.Clamp(_scroll + ((delta <= 0) ? 1 : (-1)), 0, Math.Max(0, _rangees - _visibles));
			return true;
		}
		return base.OnMouseWheeled(delta);
	}

	private void ConstruireChamps()
	{
		_fondation = Champ("Fondation", delegate
		{
			Fonder();
		});
		_tag = Champ("Tag", delegate
		{
			EnregistrerIdentite();
		});
		_tag.MaximumLength = 5;
		_invitation = Champ("Invitation", delegate
		{
			Inviter();
		});
		_recherche = Champ("Recherche", delegate
		{
			DemanderAnnuaire(0);
		});
		TextBox Champ(string nom, Action<TextBox> soumission)
		{
			TextBox champ = new TextBox(this, nom)
			{
				Font = TechniqueArt.Body,
				FontSize = 11,
				ShouldDrawBackground = false,
				MaximumLength = 32,
				IsHidden = true
			};
			champ.SetTextColor(TechniqueArt.Text, ComponentState.Normal);
			champ.SubmitPressed += delegate
			{
				soumission(champ);
			};
			Interface.FocusComponents.Add(champ);
			return champ;
		}
	}

	private void PlacerChamps()
	{
		bool flag = Globals.Me?.IsInGuild ?? false;
		bool flag2 = _annuaire || !flag;
		Poser(_fondation, !flag && _page.Length == 0, _d.R(32f, 124f, 180f, 28f));
		Poser(_invitation, flag && !_annuaire && PeutInviter(), _d.R(298f, 78f, 488f, 28f));
		Poser(_recherche, flag2 && _page.Length == 0, _d.R(298f, 78f, 488f, 28f));
		Poser(_tag, flag && !_annuaire && _identite, _d.R(268f, 192f, 180f, 28f));
		void Poser(TextBox champ, bool visible, Rectangle zone)
		{
			if (champ.IsHidden != !visible)
			{
				champ.IsHidden = !visible;
				if (!visible)
				{
					champ.Blur();
				}
			}
			if (visible)
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
	}

	private bool AuClavier(TextBox champ)
	{
		return InputHandler.KeyboardFocus == champ;
	}

	private static bool PeutInviter()
	{
		Player? me = Globals.Me;
		if (me == null || me.Rank != 0)
		{
			Player? me2 = Globals.Me;
			if (me2 == null)
			{
				return false;
			}
			return me2.GuildRank?.Permissions.Invite == true;
		}
		return true;
	}

	private static string TitreDuRang(int rang)
	{
		GuildRank[] ranks = Options.Instance.Guild.Ranks;
		return ranks[Math.Clamp(rang, 0, ranks.Length - 1)].Title;
	}

	private static GuildMember[] Membres()
	{
		return Globals.Me?.GuildMembers ?? Array.Empty<GuildMember>();
	}

	private static GuildApplication[] Candidatures()
	{
		if (!PeutInviter())
		{
			return Array.Empty<GuildApplication>();
		}
		return Globals.Me?.GuildApplications ?? Array.Empty<GuildApplication>();
	}

	private static GuildDirectoryEntry[] Annuaire()
	{
		return Globals.Me?.GuildDirectory ?? Array.Empty<GuildDirectoryEntry>();
	}

	private GuildMember? MembreChoisi()
	{
		return Membres().FirstOrDefault((GuildMember membre) => membre.Id == _membreChoisi) ?? Membres().FirstOrDefault();
	}

	private GuildDirectoryEntry? ClanChoisi()
	{
		return Annuaire().FirstOrDefault((GuildDirectoryEntry clan) => clan.Name == _clanChoisi) ?? Annuaire().FirstOrDefault();
	}

	private GuildProfile? Page()
	{
		if (_page.Length > 0)
		{
			GuildProfile guildProfile = Globals.Me?.GuildProfile;
			if (guildProfile != null && guildProfile.Name == _page)
			{
				return guildProfile;
			}
		}
		return null;
	}

	private void OuvrirPage(string clan)
	{
		_page = clan;
		_clanChoisi = clan;
		_scroll = 0;
		Envoyer(delegate
		{
			PacketSender.SendGuildProfile(clan);
		});
	}

	private void OuvrirIdentite()
	{
		_identite = true;
		_emblemeChoisi = Globals.Me?.GuildEmblem ?? 0;
		_couleurChoisie = Globals.Me?.GuildColor ?? 0;
		_tag.Text = Globals.Me?.GuildTag ?? string.Empty;
	}

	private void EnregistrerIdentite()
	{
		string tag = (_tag.Text ?? string.Empty).Trim().ToUpperInvariant();
		if (tag.Length >= 2 && tag.Length <= 5 && tag.All(char.IsLetterOrDigit))
		{
			int embleme = _emblemeChoisi;
			int couleur = _couleurChoisie;
			Envoyer(delegate
			{
				PacketSender.SendGuildIdentity(tag, embleme, couleur);
			});
			_identite = false;
			_tag.Blur();
		}
	}

	private void FermerPage()
	{
		_page = string.Empty;
		_scroll = 0;
	}

	private static (bool Peut, string Raison) Fondation()
	{
		int creationCost = Options.Instance.Guild.CreationCost;
		int creationLevel = Options.Instance.Guild.CreationLevel;
		if ((Globals.Me?.Level ?? 0) < creationLevel)
		{
			return (Peut: false, Raison: Strings.Guilds.FoundNeedsLevel.ToString(creationLevel));
		}
		long num = ((creationCost > 0) ? Math.Max(0L, creationCost - (Globals.Me?.Zeni ?? 0)) : 0);
		if (num > 0)
		{
			return (Peut: false, Raison: Strings.Guilds.FoundNeedsZeni.ToString(MarketWindow.FormatAmount(num), Strings.Guilds.Currency));
		}
		return (Peut: true, Raison: (creationCost > 0) ? Strings.Guilds.FoundCost.ToString(MarketWindow.FormatAmount(creationCost), Strings.Guilds.Currency, creationLevel) : Strings.Guilds.FoundCostFree.ToString(creationLevel));
	}

	private void Fonder()
	{
		string nom = _fondation.Text?.Trim();
		if (Fondation().Peut && nom != null && nom.Length >= 3)
		{
			int creationCost = Options.Instance.Guild.CreationCost;
			ConfirmCardRequest confirmCardRequest = new ConfirmCardRequest()
			{
				Title = Strings.ConfirmCard.FoundTitle,
				Icon = "saiyan_carte_icone_clan.png",
				Heading = nom,
				Prompt = Strings.ConfirmCard.FoundPrompt,
				Chip = Strings.ConfirmCard.FoundCost.ToString(creationCost, Strings.Guilds.Currency),
				ConfirmLabel = Strings.ConfirmCard.Found,
				Emblem = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_embleme_clan.png"),
			};
			ConfirmCardWindow.Open(confirmCardRequest, delegate
			{
				PacketSender.SendGuildCreate(nom);
				_fondation.SetText(string.Empty);
			});
		}
	}

	private void Inviter()
	{
		string text = _invitation.Text?.Trim();
		if (text != null && text.Length >= 3)
		{
			PacketSender.SendInviteGuild(text);
			_invitation.SetText(string.Empty);
		}
	}

	private void DemanderAnnuaire(int page)
	{
		int num = Globals.Me?.GuildDirectoryTotal ?? 0;
		int num2 = ((num > 0) ? ((num - 1) / 20) : 0);
		if (page >= 0 && page <= num2)
		{
			Envoyer(delegate
			{
				PacketSender.SendGuildDirectory(_recherche.Text?.Trim() ?? string.Empty, page);
			});
			_scroll = 0;
		}
	}

	private void OuvrirAnnuaire()
	{
		_annuaire = true;
		_scroll = 0;
		_recherche.Text = string.Empty;
		Envoyer(delegate
		{
			PacketSender.SendGuildDirectory(string.Empty, 0);
		});
	}

	private void Candidater(string clan)
	{
		Player? me = Globals.Me;
		if (me != null && !me.IsInGuild && !string.IsNullOrWhiteSpace(clan))
		{
			Envoyer(delegate
			{
				PacketSender.SendGuildApply(clan);
			});
		}
	}

	private static void Envoyer(Action envoi)
	{
		try
		{
			envoi();
		}
		catch (InvalidOperationException)
		{
		}
	}

	private static void ModifierMessage()
	{
		new InputBox(Strings.Guilds.MotdTitle, Strings.Guilds.MotdPrompt.ToString(Globals.Me?.Guild), InputType.TextInput, delegate(Intersect.Client.Framework.Gwen.Control.Base sender, InputSubmissionEventArgs args)
		{
			if (sender is InputBox && args.Value is StringSubmissionValue stringSubmissionValue)
			{
				PacketSender.SendGuildMotd(stringSubmissionValue.Value?.Trim() ?? string.Empty);
			}
		}).Focus();
	}

	private static void ConfirmerDepart()
	{
		ConfirmCardWindow.Open(new ConfirmCardRequest
		{
			Title = Strings.ConfirmCard.LeaveGuildTitle,
			Icon = "saiyan_carte_icone_clan.png",
			Heading = (Globals.Me?.Guild ?? string.Empty),
			Prompt = Strings.ConfirmCard.LeaveGuildPrompt,
			ConfirmLabel = Strings.ConfirmCard.LeaveGuild,
			Danger = true,
			Emblem = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_embleme_clan.png")
		}, PacketSender.SendLeaveGuild);
	}

	private static void ConfirmerDissolution()
	{
		new InputBox(Strings.Guilds.DisbandTitle, Strings.Guilds.DisbandPrompt.ToString(Globals.Me?.Guild), InputType.TextInput, delegate(Intersect.Client.Framework.Gwen.Control.Base sender, InputSubmissionEventArgs args)
		{
			if (sender is InputBox && args.Value is StringSubmissionValue stringSubmissionValue && stringSubmissionValue.Value?.Trim() == Globals.Me?.Guild)
			{
				PacketSender.SendGuildDisband();
			}
		}).Focus();
	}

	private static void ChangerRang(GuildMember membre, bool promotion)
	{
		GuildRank guildRank = Globals.Me?.GuildRank;
		if (guildRank == null)
		{
			return;
		}
		bool flag = Globals.Me.Rank == 0 || (promotion ? guildRank.Permissions.Promote : guildRank.Permissions.Demote);
		int nouveau = (promotion ? (membre.Rank - 1) : (membre.Rank + 1));
		if (!flag || membre.Rank <= Globals.Me.Rank || nouveau <= Globals.Me.Rank || nouveau >= Options.Instance.Guild.Ranks.Length)
		{
			return;
		}
		ConfirmCardRequest confirmCardRequest = new ConfirmCardRequest()
		{
			Title = (promotion ? Strings.ConfirmCard.PromoteTitle : Strings.ConfirmCard.DemoteTitle),
			Icon = "saiyan_carte_icone_clan.png",
			Heading = membre.Name,
			Prompt = Strings.ConfirmCard.RankPrompt.ToString(TitreDuRang(nouveau)),
			ConfirmLabel = (promotion ? Strings.ConfirmCard.Promote : Strings.ConfirmCard.Demote),
			Danger = !promotion,
			PlayerName = membre.Name,
		};
		ConfirmCardWindow.Open(confirmCardRequest, delegate
		{
			if (promotion)
			{
				PacketSender.SendPromoteGuildMember(membre.Id, nouveau);
			}
			else
			{
				PacketSender.SendDemoteGuildMember(membre.Id, nouveau);
			}
		});
	}

	private static void Exclure(GuildMember membre)
	{
		GuildRank guildRank = Globals.Me?.GuildRank;
		if (guildRank != null && (guildRank.Permissions.Kick || Globals.Me.Rank == 0) && membre.Rank > Globals.Me.Rank)
		{
			ConfirmCardWindow.Open(new ConfirmCardRequest
			{
				Title = Strings.ConfirmCard.KickTitle,
				Icon = "saiyan_carte_icone_clan.png",
				Heading = membre.Name,
				Prompt = Strings.ConfirmCard.KickPrompt,
				ConfirmLabel = Strings.ConfirmCard.Kick,
				Danger = true,
				PlayerName = membre.Name
			}, delegate
			{
				PacketSender.SendKickGuildMember(membre.Id);
			});
		}
	}

	private static void Transferer(GuildMember membre)
	{
		GuildRank guildRank = Globals.Me?.GuildRank;
		if (guildRank == null || Globals.Me.Rank != 0)
		{
			return;
		}
		new InputBox(Strings.Guilds.TransferTitle, Strings.Guilds.TransferToMemberPrompt.ToString(membre.Name, guildRank.Title, Globals.Me.Guild), InputType.TextInput, delegate(Intersect.Client.Framework.Gwen.Control.Base sender, InputSubmissionEventArgs args)
		{
			if (sender is InputBox && args.Value is StringSubmissionValue stringSubmissionValue && stringSubmissionValue.Value?.Trim() == Globals.Me?.Guild)
			{
				PacketSender.SendTransferGuild(membre.Id);
			}
		});
	}

	private static string Decrire(GuildActivityEntry entree)
	{
		return ((LocalizedString)(entree.Type switch
		{
			0 => Strings.Guilds.ActivityCreated, 
			1 => Strings.Guilds.ActivityDisbanded, 
			2 => Strings.Guilds.ActivityJoined, 
			3 => Strings.Guilds.ActivityLeft, 
			4 => Strings.Guilds.ActivityKicked, 
			5 => Strings.Guilds.ActivityPromoted, 
			6 => Strings.Guilds.ActivityDemoted, 
			7 => Strings.Guilds.ActivityTransfer, 
			8 => Strings.Guilds.ActivityRename, 
			9 => Strings.Guilds.ActivityMotd, 
			10 => Strings.Guilds.ActivityLevelUp, 
			_ => null, 
		}))?.ToString(new object[3] { entree.Actor, entree.Target, entree.Meta }) ?? string.Empty;
	}

	private static string Age(long horodatage)
	{
		double totalMinutes = (DateTime.UtcNow - DateTime.UnixEpoch.AddMilliseconds(horodatage)).TotalMinutes;
		if (!(totalMinutes < 1.0))
		{
			if (!(totalMinutes < 60.0))
			{
				if (totalMinutes < 1440.0)
				{
					return Strings.Guilds.JournalHours.ToString((int)(totalMinutes / 60.0));
				}
				return Strings.Guilds.JournalDays.ToString((int)(totalMinutes / 1440.0));
			}
			return Strings.Guilds.JournalMinutes.ToString((int)totalMinutes);
		}
		return Strings.Guilds.JournalNow.ToString();
	}

	protected override void Dessiner(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		DessinerCadre(renderer);
		bool flag = Globals.Me?.IsInGuild ?? false;
		int num = DessinerBarre(renderer, (!flag) ? ((string)Strings.Guilds.NoGuildTitle) : (Globals.Me?.Guild ?? string.Empty));
		_liste = default(Rectangle);
		_rangees = 0;
		_visibles = 1;
		if (flag)
		{
			int num2 = _d.Y(23f);
			int num3 = num - _d.L(18f);
			(string, string, Color)[] obj = new(string, string, Color)[2]
			{
				(Strings.Guilds.MembersLabel.ToString(), $"{Membres().Length}", TechniqueArt.Text),
				default((string, string, Color))
			};
			string item = Strings.Guilds.YourRank.ToString();
			string item2 = TitreDuRang(Globals.Me?.Rank ?? 0);
			Player? me = Globals.Me;
			obj[1] = (item, item2, (me != null && me.Rank == 0) ? TechniqueArt.Gold : TechniqueArt.Text);
			(string, string, Color)[] array = obj;
			for (int i = 0; i < array.Length; i++)
			{
				(string, string, Color) tuple = array[i];
				string item3 = tuple.Item1;
				string item4 = tuple.Item2;
				Color item5 = tuple.Item3;
				int num4 = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(15f), item4, num3, num2, A(item5));
				num3 -= num4 + _d.L(8f);
				string text = item3.ToUpperInvariant();
				int num5 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10f), text, _d.Em(0.16f, 10f));
				TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), text, num3 - num5, num2 + _d.L(1f), _d.Em(0.16f, 10f), A(TechniqueArt.Dim));
				num3 -= num5 + _d.L(22f);
			}
		}
		if (_page.Length > 0 && (_annuaire || !flag))
		{
			DrawPageClan(renderer, flag);
			return;
		}
		DrawRail(renderer, flag);
		if (flag && !_annuaire && _identite)
		{
			DrawIdentite(renderer);
		}
		else if (flag && !_annuaire)
		{
			DrawMonClan(renderer);
			DrawFicheMembre(renderer);
		}
		else if (_page.Length > 0)
		{
			DrawPageClan(renderer, flag);
		}
		else
		{
			DrawAnnuaire(renderer, flag);
			DrawFicheClan(renderer, flag, ClanChoisi());
		}
	}

	protected override void DessinerDessus(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		DessinerEtiquette(renderer, now);
	}

	private void Panel(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect)
	{
		TechniqueArt.Patch(renderer, "tech_panneau.png", rect, A(TechniqueArt.White), 12);
	}

	private void Champ(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle champ, TextBox boite, string icone, string vide, Color bordure)
	{
		TechniqueArt.Fill(renderer, champ, A(new Color(255, 7, 10, 18)));
		TechniqueArt.Outline(renderer, champ, A(AuClavier(boite) ? TechniqueArt.Ki : bordure));
		int num = _d.L(14f);
		Lisser(icone, new Rectangle(champ.X + _d.L(10f), champ.Y + (champ.Height - num) / 2, num, num), A(TechniqueArt.Soft));
		if (string.IsNullOrEmpty(boite.Text))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13.5f), vide, champ.X + _d.L(34f), champ.Y + champ.Height / 2, A(TechniqueArt.Dim));
		}
	}

	private void Puce(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string texte, int droite, int milieu, Color couleur, bool pleine)
	{
		int num = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10f), texte, _d.Em(0.1f, 10f)) + _d.L(12f);
		Rectangle rect = new Rectangle(droite - num, milieu - _d.L(9f), num, _d.L(18f));
		if (pleine)
		{
			TechniqueArt.Fill(renderer, rect, A(couleur));
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(10f), texte, rect.X + rect.Width / 2, rect.Y + rect.Height / 2, _d.Em(0.1f, 10f), A(HouseArt.Dark));
		}
		else
		{
			TechniqueArt.Fill(renderer, rect, A(new Color(178, 6, 8, 14)));
			TechniqueArt.Outline(renderer, rect, A(couleur, 0.6f));
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(10f), texte, rect.X + rect.Width / 2, rect.Y + rect.Height / 2, _d.Em(0.1f, 10f), A(couleur));
		}
	}

	private void Portrait(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string? sprite, Rectangle hex, Color accent, bool eteint)
	{
		Hexagone(renderer, hex, accent, null, TechniqueArt.White, vide: false);
		if (!string.IsNullOrWhiteSpace(sprite))
		{
			(IGameTexture, float, float, float, float)? tuple = DungeonEntryWindow.StandingFrame(sprite);
			if (tuple.HasValue)
			{
				(IGameTexture, float, float, float, float) valueOrDefault = tuple.GetValueOrDefault();
				int num = Math.Max(2, _d.L(4f));
				Rectangle rectangle = new Rectangle(hex.X + num * 2, hex.Y + num * 2, hex.Width - num * 4, hex.Height - num * 4);
				Color color = (eteint ? GrisHorsLigne : TechniqueArt.White);
				var (gameTexture, num2, num3, num4) = Tete(sprite, valueOrDefault);
				SaiyanHud.Draw(renderer, gameTexture, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, A(color), num2, num3, num2 + num4 / (float)gameTexture.Width, num3 + num4 / (float)gameTexture.Height);
			}
		}
	}

	internal static (IGameTexture Texture, float U, float V, float Cote) Tete(string? sprite, (IGameTexture Texture, float X, float Y, float Width, float Height) debout)
	{
		var (gameTexture, num, num2, num3, num4) = debout;
		if (!string.IsNullOrWhiteSpace(sprite))
		{
			IGameTexture gameTexture2 = Globals.ContentManager?.GetTexture(TextureType.Entity, sprite);
			if (gameTexture2 != null && gameTexture2.Width > 0 && gameTexture2.Height > 0 && SpriteSheets.ForEntity(sprite) == null)
			{
				gameTexture = gameTexture2;
				num = 0f;
				num2 = 0f;
				num3 = (float)gameTexture2.Width / 4f;
				num4 = (float)gameTexture2.Height / 4f;
			}
		}
		float item = num3 * 0.62f;
		float item2 = (num + num3 * 0.19f) / (float)gameTexture.Width;
		float item3 = (num2 + ((num4 > num3) ? (num4 * 0.37f) : (num4 * 0.12f))) / (float)gameTexture.Height;
		return (Texture: gameTexture, U: item2, V: item3, Cote: item);
	}

	private void DeboutHabille(Intersect.Client.Framework.Gwen.Renderer.Base renderer, GuildMember membre, int centreX, int piedsY, int hauteur, Color teinte)
	{
		int largeur;
		int haut;
		int x;
		int y;
		if (!string.IsNullOrWhiteSpace(membre.Sprite))
		{
			SpriteSheets.Pose? pose = SpriteSheets.StandingBody(membre.Sprite);
			if (pose.HasValue)
			{
				SpriteSheets.Pose valueOrDefault = pose.GetValueOrDefault();
				if (!valueOrDefault.IsEmpty)
				{
					int num = Math.Max(1, (int)MathF.Round((float)hauteur / (float)valueOrDefault.Frame.Height));
					largeur = valueOrDefault.Frame.Width * num;
					haut = valueOrDefault.Frame.Height * num;
					x = centreX - largeur / 2;
					y = piedsY - haut;
					Calque(valueOrDefault.Texture, valueOrDefault.Frame, teinte);
					Guid[] array = membre.Equipment ?? Array.Empty<Guid>();
					List<string> slots = Options.Instance.Equipment.Slots;
					{
						foreach (string item in Options.Instance.Equipment.Paperdoll.Directions[1])
						{
							int num2 = slots.IndexOf(item);
							if (num2 < 0 || num2 >= array.Length || array[num2] == Guid.Empty || !DatabaseObject<ItemDescriptor>.TryGet(array[num2], out var databaseObject))
							{
								continue;
							}
							string text = Paperdolls.ForClass((membre.Gender == 0) ? databaseObject.MalePaperdoll : databaseObject.FemalePaperdoll, membre.ClassId);
							if (string.IsNullOrWhiteSpace(text))
							{
								continue;
							}
							pose = SpriteSheets.StandingOutfit(membre.Sprite, text, valueOrDefault);
							if (pose.HasValue)
							{
								SpriteSheets.Pose valueOrDefault2 = pose.GetValueOrDefault();
								if (!valueOrDefault2.IsEmpty)
								{
									Calque(valueOrDefault2.Texture, valueOrDefault2.Frame, Melange(databaseObject.Color, teinte));
								}
							}
						}
						return;
					}
				}
			}
		}
		Debout(renderer, membre.Sprite, centreX, piedsY, hauteur, teinte);
		void Calque(IGameTexture texture, (int X, int Y, int Width, int Height) cadre, Color couleur)
		{
			SaiyanHud.Draw(renderer, texture, x, y, largeur, haut, A(couleur), (float)cadre.X / (float)texture.Width, (float)cadre.Y / (float)texture.Height, (float)(cadre.X + cadre.Width) / (float)texture.Width, (float)(cadre.Y + cadre.Height) / (float)texture.Height);
		}
	}

	private static Color Melange(Color a, Color b)
	{
		return new Color(a.A * b.A / 255, a.R * b.R / 255, a.G * b.G / 255, a.B * b.B / 255);
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

	private void Titre(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float top, string titre, string? complement)
	{
		int num = _d.Y(top + 17f);
		int num2 = _d.X(264f);
		DungeonHud.Diamond(renderer, num2 + _d.L(6f), num, _d.L(10f), A(TechniqueArt.Green));
		int num3 = TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(22f), titre.ToUpperInvariant(), num2 + _d.L(20f), num, A(TechniqueArt.Text));
		if (!string.IsNullOrEmpty(complement))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(13f), complement, num2 + _d.L(20f) + num3 + _d.L(12f), num + _d.L(2f), A(TechniqueArt.Dim));
		}
	}

	private void Ascenseur(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		if (_rangees > _visibles)
		{
			Rectangle rect = new Rectangle(_liste.Right + _d.L(5f), _liste.Y, _d.L(3f), _liste.Height);
			TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, 0.05f));
			int num = Math.Max(_d.L(24f), rect.Height * _visibles / _rangees);
			int y = rect.Y + (rect.Height - num) * _scroll / Math.Max(1, _rangees - _visibles);
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, y, rect.Width, num), A(TechniqueArt.Line2));
		}
	}

	private void DrawRail(Intersect.Client.Framework.Gwen.Renderer.Base renderer, bool enClan)
	{
		Panel(renderer, _d.R(14f, 58f, 224f, 676f));
		if (!enClan)
		{
			DrawFondation(renderer);
			return;
		}
		int num = _d.L(72f);
		int num2 = _d.X(126f);
		TechniqueArt.Glow(renderer, num2, _d.Y(114f), _d.L(70f), A(ClanEmbleme.Couleur(Globals.Me?.GuildColor ?? 0), 0.22f));
		ClanEmbleme.Dessiner(renderer, new Rectangle(num2 - num / 2, _d.Y(114f) - num / 2, num, num), Globals.Me?.GuildEmblem ?? 0, Globals.Me?.GuildColor ?? 0, null, _fade);
		if (!string.IsNullOrWhiteSpace(Globals.Me?.GuildTag))
		{
			Puce(renderer, "[" + Globals.Me.GuildTag + "]", num2 + _d.L(98f), _d.Y(78f), ClanEmbleme.Couleur(Globals.Me.GuildColor), pleine: false);
		}
		TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(17f), TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(17f), Globals.Me?.Guild, _d.L(200f), 1).FirstOrDefault(), num2, _d.Y(170f), A(TechniqueArt.Text));
		IFont? body = TechniqueArt.Body;
		int size = Pt(11.5f);
		string text = TitreDuRang(Globals.Me?.Rank ?? 0);
		int centerY = _d.Y(190f);
		Player? me = Globals.Me;
		TechniqueArt.TextCenter(renderer, body, size, text, num2, centerY, A((me != null && me.Rank == 0) ? TechniqueArt.Gold : TechniqueArt.Soft));
		Player? me2 = Globals.Me;
		bool flag = me2 != null && me2.Rank == 0;
		float num3 = DrawNiveau(renderer, 208f);
		if (_annuaire)
		{
			foreach (string item5 in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), Strings.Guilds.BrowseHint, _d.L(192f), 4))
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), item5, _d.X(30f), _d.Y(num3), A(BodyLight));
				num3 += 16f;
			}
		}
		else
		{
			num3 = DrawMessageDuJour(renderer, num3);
			DrawJournal(renderer, num3 + 10f, 722f - (float)(36 * (flag ? 3 : 2)) - (float)(6 * ((!flag) ? 1 : 2)) - 14f);
		}
		List<(string, string, Color, Action)> list = new List<(string, string, Color, Action)>();
		list.Add(((string, string, Color, Action))((_page.Length > 0) ? ("rail:annuaire-retour", Strings.Guilds.BackToDirectory.ToString(), null, FermerPage) : (_annuaire ? ("rail:retour", Strings.Guilds.DirectoryBack.ToString(), null, delegate
		{
			_annuaire = false;
			_scroll = 0;
		}) : ("rail:annuaire", Strings.Guilds.DirectoryButton.ToString(), null, OuvrirAnnuaire))));
		list.Add(((string, string, Color, Action))(flag ? ("rail:dissoudre", Strings.Guilds.Disband.ToString(), Rouge, ConfirmerDissolution) : ("rail:quitter", Strings.Guilds.Leave.ToString(), Rouge, ConfirmerDepart)));
		List<(string, string, Color, Action)> list2 = list;
		if (flag && !_annuaire)
		{
			list2.Insert(1, ("rail:identite", Strings.Guilds.IdentityButton.ToString(), null, OuvrirIdentite));
		}
		float num4 = 686f;
		foreach (var item6 in list2.AsEnumerable().Reverse())
		{
			string item = item6.Item1;
			string item2 = item6.Item2;
			Color item3 = item6.Item3;
			Action item4 = item6.Item4;
			Rectangle rect = _d.R(30f, num4, 192f, 36f);
			HouseArt.Hollow(renderer, _d, rect, item2, IsHot(item), _fade, item3);
			AddHit(rect, item, item4);
			num4 -= 42f;
		}
	}

	private void DrawFondation(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		int x = _d.X(30f);
		int width = _d.L(192f);
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10.5f), Strings.Guilds.Found.ToString().ToUpperInvariant(), x, _d.Y(80f), _d.Em(0.14f, 10.5f), A(TechniqueArt.Dim));
		var (flag, text) = Fondation();
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), Strings.Guilds.FoundHint, width, 1).FirstOrDefault(), x, _d.Y(102f), A(BodyLight));
		Rectangle rect = _d.R(30f, 120f, 192f, 36f);
		TechniqueArt.Fill(renderer, rect, A(new Color(255, 7, 10, 18)));
		TechniqueArt.Outline(renderer, rect, A(AuClavier(_fondation) ? TechniqueArt.Ki : TechniqueArt.Line2));
		if (string.IsNullOrEmpty(_fondation.Text))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), Strings.Guilds.FoundNamePlaceholder, rect.X + _d.L(10f), rect.Y + rect.Height / 2, A(TechniqueArt.Dim));
		}
		Rectangle rect2 = _d.R(30f, 166f, 192f, 36f);
		bool flag2 = flag && (_fondation.Text?.Trim().Length ?? 0) >= 3;
		HouseArt.Cta(renderer, _d, rect2, Strings.Guilds.FoundButton.ToString(), flag2, flag2 && IsHot("fonder"), _fade);
		AddHit(rect2, "fonder", Fonder);
		float num = 216f;
		foreach (string item in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), text, width, 3))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), item, x, _d.Y(num), A(flag ? TechniqueArt.Soft : Rouge));
			num += 15f;
		}
		List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), Strings.Guilds.ApplyHint, width, 4);
		int num2 = _d.Y(722f);
		for (int num3 = list.Count - 1; num3 >= 0; num3--)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), list[num3], x, num2 - _d.L(7f), A(TechniqueArt.Dim));
			num2 -= _d.L(14f);
		}
	}

	private float DrawNiveau(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float y)
	{
		Rectangle rect = _d.R(24f, y, 204f, 64f);
		TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, 0.03f));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
		TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(3f), rect.Height), A(TechniqueArt.Ki));
		int num = Math.Max(1, Globals.Me?.GuildLevel ?? 1);
		long num2 = Globals.Me?.GuildExperience ?? 0;
		long num3 = Globals.Me?.GuildExperienceToNext ?? 0;
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), Strings.Guilds.ClanLevel.ToString().ToUpperInvariant(), rect.X + _d.L(14f), rect.Y + _d.L(14f), _d.Em(0.12f, 10f), A(TechniqueArt.Soft));
		TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(16f), $"{num}", rect.Right - _d.L(12f), rect.Y + _d.L(14f), A(TechniqueArt.Ki));
		Rectangle rect2 = new Rectangle(rect.X + _d.L(14f), rect.Y + _d.L(27f), rect.Width - _d.L(26f), _d.L(8f));
		TechniqueArt.Fill(renderer, rect2, A(new Color(255, 7, 10, 18)));
		TechniqueArt.Outline(renderer, rect2, A(TechniqueArt.Line));
		float num4 = ((num3 <= 0) ? 1f : Math.Clamp((float)num2 / (float)num3, 0f, 1f));
		if (num4 > 0f)
		{
			TechniqueArt.FillHorizontal(renderer, new Rectangle(rect2.X + 1, rect2.Y + 1, Math.Max(2, (int)((float)(rect2.Width - 2) * num4)), rect2.Height - 2), A(TechniqueArt.Ki, 0.7f), A(TechniqueArt.Ki), A(TechniqueArt.Gold));
		}
		string text = ((num3 <= 0) ? Strings.Guilds.MaxLevel.ToString() : Strings.Guilds.ExperienceBar.ToString(MarketWindow.FormatAmount(num2), MarketWindow.FormatAmount(num3)));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(10.5f), text, rect.X + _d.L(14f), rect.Y + _d.L(44f), A(BodyLight));
		TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(10.5f), Strings.Guilds.Perks.ToString(num), rect.Right - _d.L(12f), rect.Y + _d.L(56f), A(TechniqueArt.Dim));
		if (IsHot("niveau"))
		{
			Etiqueter("niveau", rect, Strings.Guilds.PerksTip.ToString(num, (num - 1) * 5, MarketWindow.FormatAmount(Globals.Me?.GuildWeekExperience ?? 0)));
		}
		AddHit(rect, "niveau", null);
		return y + 64f + 8f;
	}

	private float DrawMessageDuJour(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float y)
	{
		Rectangle rect = _d.R(24f, y, 204f, 118f);
		TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, 0.03f));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
		TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(3f), rect.Height), A(TechniqueArt.Gold));
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), Strings.Guilds.Motd.ToString().ToUpperInvariant(), rect.X + _d.L(14f), rect.Y + _d.L(16f), _d.Em(0.12f, 10f), A(TechniqueArt.Soft));
		string text = Globals.Me?.GuildMotd ?? string.Empty;
		bool flag = string.IsNullOrWhiteSpace(text);
		int num = rect.Y + _d.L(36f);
		foreach (string item in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), flag ? Strings.Guilds.MotdEmpty.ToString() : text, rect.Width - _d.L(26f), 4))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), item, rect.X + _d.L(14f), num, A(flag ? TechniqueArt.Dim : BodyLight));
			num += _d.L(15f);
		}
		Player? me = Globals.Me;
		if (me == null || me.Rank != 0)
		{
			Player? me2 = Globals.Me;
			if (me2 == null || me2.GuildRank?.Permissions.Promote != true)
			{
				goto IL_02e7;
			}
		}
		bool flag2 = IsHot("motd");
		TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(10.5f), Strings.Guilds.MotdEdit, rect.Right - _d.L(12f), rect.Bottom - _d.L(12f), A(flag2 ? TechniqueArt.Ki : TechniqueArt.Soft));
		AddHit(rect, "motd", ModifierMessage);
		goto IL_02e7;
		IL_02e7:
		return y + 118f;
	}

	private void DrawJournal(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float haut, float bas)
	{
		int x = _d.X(30f);
		int width = _d.L(192f);
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), Strings.Guilds.Journal.ToString().ToUpperInvariant(), x, _d.Y(haut + 10f), _d.Em(0.12f, 10f), A(TechniqueArt.Soft));
		int num = _d.Y(haut + 28f);
		int num2 = _d.Y(bas);
		GuildActivityEntry[] array = Globals.Me?.GuildActivity ?? Array.Empty<GuildActivityEntry>();
		if (array.Length == 0)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), Strings.Guilds.JournalEmpty, width, 1).FirstOrDefault(), x, num, A(TechniqueArt.Dim));
			return;
		}
		GuildActivityEntry[] array2 = array;
		foreach (GuildActivityEntry guildActivityEntry in array2)
		{
			List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11f), Decrire(guildActivityEntry), width, 2);
			int num3 = _d.L(12f) + list.Count * _d.L(14f) + _d.L(6f);
			if (num + num3 > num2)
			{
				break;
			}
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(9f), Age(guildActivityEntry.TimeStamp).ToUpperInvariant(), x, num + _d.L(5f), _d.Em(0.08f, 9f), A(TechniqueArt.Dim));
			num += _d.L(14f);
			foreach (string item in list)
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11f), item, x, num + _d.L(5f), A(BodyLight));
				num += _d.L(14f);
			}
			num += _d.L(6f);
		}
	}

	private void DrawMonClan(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		Panel(renderer, _d.R(248f, 58f, 700f, 676f));
		float num = 264f;
		float num2 = 74f;
		if (PeutInviter())
		{
			Champ(renderer, _d.R(num, 74f, 532f, 36f), _invitation, "saiyan_amis_icone_ajout.png", Strings.Guilds.InvitePlaceholder, TechniqueArt.Line2);
			Rectangle rect = _d.R(num + 532f + 8f, 74f, 128f, 36f);
			bool flag = (_invitation.Text?.Trim().Length ?? 0) >= 3;
			HouseArt.Cta(renderer, _d, rect, Strings.Guilds.Invite.ToString(), flag, flag && IsHot("inviter"), _fade);
			AddHit(rect, "inviter", Inviter);
			num2 = 124f;
		}
		else
		{
			num2 = 74f;
		}
		GuildApplication[] array = Candidatures();
		if (array.Length != 0)
		{
			Titre(renderer, num2, Strings.Guilds.Applications.ToString(array.Length), Strings.Guilds.ApplicationsHint);
			float num3 = num2 + 34f + 8f;
			foreach (GuildApplication item in array.Take(3))
			{
				DrawCandidature(renderer, item, num, num3);
				num3 += 50f;
			}
			if (array.Length > 3)
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11f), Strings.Guilds.ApplicationsMore.ToString(array.Length - 3), _d.X(num + 4f), _d.Y(num3 + 8f), A(TechniqueArt.Dim));
				num3 += 20f;
			}
			num2 = num3 + 8f;
		}
		num2 = DrawObjectifs(renderer, num, num2);
		GuildMember[] array2 = Membres();
		Titre(renderer, num2, Strings.Guilds.MembersLabel, Strings.Guilds.MembersCount.ToString(array2.Length, array2.Count((GuildMember membre) => membre.Online)));
		float num4 = num2 + 34f + 12f;
		_visibles = Math.Max(1, (int)((718f - num4 + 6f) / 58f));
		_rangees = array2.Length;
		_scroll = Math.Clamp(_scroll, 0, Math.Max(0, _rangees - _visibles));
		_liste = _d.R(num, num4, 668f, (float)_visibles * 58f - 6f);
		Rectangle previous = TechniqueArt.BeginClip(renderer, _liste);
		for (int num5 = _scroll; num5 < array2.Length && num5 - _scroll < _visibles; num5++)
		{
			DrawMembre(renderer, array2[num5], num, num4 + (float)(num5 - _scroll) * 58f);
		}
		TechniqueArt.EndClip(renderer, previous);
		Ascenseur(renderer);
	}

	private float DrawObjectifs(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float gauche, float top)
	{
		GuildObjective[] array = Globals.Me?.GuildObjectives ?? Array.Empty<GuildObjective>();
		if (array.Length == 0)
		{
			return top;
		}
		long num = Math.Max(0L, (Globals.Me?.GuildObjectivesEndAt ?? 0) - Timing.Global.Milliseconds) / 1000;
		string value = Strings.Guilds.ObjectivesEnds.ToString(Strings.Guilds.ObjectiveDuration.ToString(num / 86400, num / 3600 % 24));
		Titre(renderer, top, Strings.Guilds.ObjectivesTitle, $"{value} · {Strings.Guilds.ObjectivesHint}");
		float num2 = top + 34f + 8f;
		GuildObjective[] array2 = array;
		foreach (GuildObjective guildObjective in array2)
		{
			Rectangle rect = _d.R(gauche, num2, 668f, 46f);
			bool done = guildObjective.Done;
			if (done)
			{
				TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Green, 0.14f), A(TechniqueArt.Green, 0.06f), A(TechniqueArt.Green, 0.02f));
				TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Green, 0.6f));
				TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(3f), rect.Height), A(TechniqueArt.Green));
			}
			else
			{
				TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, 0.03f));
				TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
				TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(3f), rect.Height), A(TechniqueArt.Ki, 0.7f));
			}
			int num3 = rect.Y + rect.Height / 2;
			string text = DecrireObjectif(guildObjective);
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(14f), TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(14f), text, _d.L(300f), 1).FirstOrDefault(), rect.X + _d.L(16f), num3 - _d.L(8f), A(done ? TechniqueArt.Green : TechniqueArt.Text));
			string text2 = Strings.Guilds.ObjectiveReward.ToString(MarketWindow.FormatAmount(guildObjective.RewardExperience), MarketWindow.FormatAmount(guildObjective.RewardZeni), guildObjective.Participants);
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(10.5f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(10.5f), text2, _d.L(306f), 1).FirstOrDefault(), rect.X + _d.L(16f), num3 + _d.L(9f), A(TechniqueArt.Dim));
			Rectangle rect2 = new Rectangle(rect.X + _d.L(330f), num3 - _d.L(6f), _d.L(220f), _d.L(12f));
			TechniqueArt.Fill(renderer, rect2, A(new Color(255, 7, 10, 18)));
			TechniqueArt.Outline(renderer, rect2, A(TechniqueArt.Line));
			float num4 = ((guildObjective.Goal <= 0) ? 1f : Math.Clamp((float)guildObjective.Progress / (float)guildObjective.Goal, 0f, 1f));
			if (num4 > 0f)
			{
				Color color = (done ? TechniqueArt.Green : TechniqueArt.Ki);
				TechniqueArt.FillHorizontal(renderer, new Rectangle(rect2.X + 1, rect2.Y + 1, Math.Max(2, (int)((float)(rect2.Width - 2) * num4)), rect2.Height - 2), A(color, 0.7f), A(color), A(color, 0.9f));
			}
			string text3 = (done ? Strings.Guilds.ObjectiveDone.ToString() : (MarketWindow.FormatAmount(guildObjective.Progress) + " / " + MarketWindow.FormatAmount(guildObjective.Goal)));
			TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(13f), text3, rect.Right - _d.L(14f), num3, A(done ? TechniqueArt.Green : TechniqueArt.Text));
			num2 += 52f;
		}
		return num2 + 8f;
	}

	private static string DecrireObjectif(GuildObjective objectif)
	{
		return (GuildObjectiveKind)objectif.Kind switch
		{
			GuildObjectiveKind.Monsters => Strings.Guilds.ObjectiveMonsters.ToString(objectif.Goal), 
			GuildObjectiveKind.Monster => Strings.Guilds.ObjectiveMonster.ToString(objectif.Goal, Traduction.Contenu(objectif.TargetName)), 
			GuildObjectiveKind.Quests => Strings.Guilds.ObjectiveQuests.ToString(objectif.Goal), 
			GuildObjectiveKind.Gathering => Strings.Guilds.ObjectiveGathering.ToString(objectif.Goal), 
			GuildObjectiveKind.Crafting => Strings.Guilds.ObjectiveCrafting.ToString(objectif.Goal), 
			_ => Strings.Guilds.ObjectiveClanExperience.ToString(MarketWindow.FormatAmount(objectif.Goal)), 
		};
	}

	private void DrawCandidature(Intersect.Client.Framework.Gwen.Renderer.Base renderer, GuildApplication candidature, float x, float y)
	{
		Rectangle rect = _d.R(x, y, 668f, 44f);
		TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Gold, 0.12f), A(TechniqueArt.Gold, 0.05f), A(TechniqueArt.Gold, 0.02f));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Gold, 0.5f));
		TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(3f), rect.Height), A(TechniqueArt.Gold));
		int num = rect.Y + rect.Height / 2;
		int num2 = TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(15f), candidature.Name, rect.X + _d.L(16f), num, A(TechniqueArt.Text));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), Strings.Guilds.Tooltip.ToString(candidature.Level, candidature.ClassName), rect.X + _d.L(16f) + num2 + _d.L(12f), num + _d.L(1f), A(TechniqueArt.Soft));
		int num3 = rect.Right - _d.L(10f);
		(string, string, Color, bool)[] array = new(string, string, Color, bool)[2]
		{
			("refuser", Strings.Guilds.Refuse.ToString(), Rouge, false),
			("accepter", Strings.Guilds.Accept.ToString(), TechniqueArt.Green, true)
		};
		for (int i = 0; i < array.Length; i++)
		{
			(string, string, Color, bool) tuple = array[i];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			Color item3 = tuple.Item3;
			bool accepte = tuple.Item4;
			int num4 = TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(11f), item2).X + _d.L(24f);
			Rectangle rect2 = new Rectangle(num3 - num4, rect.Y + (rect.Height - _d.L(28f)) / 2, num4, _d.L(28f));
			string cle = $"cand-{candidature.Id}:{item}";
			bool flag = IsHot(cle);
			TechniqueArt.Fill(renderer, rect2, A(item3, flag ? 0.3f : 0.12f));
			TechniqueArt.Outline(renderer, rect2, A(item3, flag ? 1f : 0.6f));
			TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(11f), item2, rect2.X + rect2.Width / 2, rect2.Y + rect2.Height / 2, A(flag ? TechniqueArt.White : item3));
			Guid id = candidature.Id;
			AddHit(rect2, cle, delegate
			{
				PacketSender.SendGuildApplicationResponse(id, accepte);
			});
			num3 -= num4 + _d.L(8f);
		}
		TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(10f), Age(candidature.When), num3 - _d.L(6f), num, A(TechniqueArt.Dim));
	}

	private void DrawMembre(Intersect.Client.Framework.Gwen.Renderer.Base renderer, GuildMember membre, float x, float y)
	{
		Rectangle rect = _d.R(x, y, 668f, 52f);
		Guid id = membre.Id;
		Guid? obj = Globals.Me?.Id;
		bool flag = id == obj;
		bool flag2 = membre.Id == _membreChoisi;
		string cle = $"membre-{membre.Id}";
		bool flag3 = IsHot(cle);
		if (flag2)
		{
			TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Ki, 0.14f), A(TechniqueArt.Ki, 0.06f), A(TechniqueArt.Ki, 0.02f));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Ki));
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(3f), rect.Height), A(TechniqueArt.Ki));
		}
		else
		{
			TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, flag3 ? 0.05f : (membre.Online ? 0.035f : 0.02f)));
			TechniqueArt.Outline(renderer, rect, A(flag3 ? TechniqueArt.Line2 : TechniqueArt.Line));
		}
		int num = rect.Y + rect.Height / 2;
		Portrait(renderer, membre.Sprite, _d.R(x + 8f, y + 6f, 40f, 40f), (membre.Rank == 0) ? TechniqueArt.Gold : (flag ? TechniqueArt.Ki : (membre.Online ? TechniqueArt.Green : GreyFrame)), !membre.Online);
		int num2 = _d.L(8f);
		TechniqueArt.Fill(renderer, new Rectangle(rect.X + _d.L(42f), rect.Bottom - _d.L(12f), num2, num2), A(membre.Online ? TechniqueArt.Green : TechniqueArt.Dim, membre.Online ? 1f : 0.6f));
		int num3 = rect.X + _d.L(60f);
		int num4 = TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(15f), membre.Name, num3, num, A(flag ? TechniqueArt.Gold : (membre.Online ? TechniqueArt.Text : BodyLight)));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11f), Strings.Guilds.Tooltip.ToString(membre.Level, membre.ClassName), num3 + num4 + _d.L(12f), num + _d.L(1f), A(TechniqueArt.Soft));
		int num5 = rect.Right - _d.L(12f);
		string text = TitreDuRang(membre.Rank).ToUpperInvariant();
		int num6 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10f), text, _d.Em(0.1f, 10f)) + _d.L(12f);
		Puce(renderer, text, num5, num, (membre.Rank == 0) ? TechniqueArt.Gold : TechniqueArt.Line2, membre.Rank == 0);
		num5 -= num6 + _d.L(14f);
		if (membre.Contribution > 0)
		{
			string text2 = Strings.Guilds.ContributionShort.ToString(MarketWindow.FormatAmount(membre.Contribution));
			TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(11f), text2, num5, num, A(TechniqueArt.Gold, 0.85f));
			num5 -= TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(11f), text2).X + _d.L(16f);
		}
		if (membre.Online)
		{
			TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(11f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11f), Traduction.Contenu(membre.MapName), _d.L(180f), 1).FirstOrDefault(), num5, num, A(TechniqueArt.Dim));
		}
		Guid id2 = membre.Id;
		AddHit(rect, cle, delegate
		{
			_membreChoisi = id2;
		});
	}

	private void DrawFicheMembre(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		Panel(renderer, _d.R(958f, 58f, 268f, 676f));
		int x = _d.X(974f);
		int width = _d.L(236f);
		GuildMember membre = MembreChoisi();
		if (membre == null)
		{
			float num = 98f;
			{
				foreach (string item5 in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), Strings.Guilds.PickMember, width, 6))
				{
					TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), item5, x, _d.Y(num), A(BodyLight));
					num += 16f;
				}
				return;
			}
		}
		_membreChoisi = membre.Id;
		Guid id = membre.Id;
		Guid? obj = Globals.Me?.Id;
		bool flag = id == obj;
		bool flag2 = membre.Rank == 0;
		Color color = (flag2 ? TechniqueArt.Gold : (flag ? TechniqueArt.Ki : (membre.Online ? TechniqueArt.Green : GreyFrame)));
		Rectangle rect = _d.R(970f, 70f, 244f, 150f);
		TechniqueArt.FillVertical(renderer, rect, A(new Color(255, 10, 18, 32)), A(new Color(255, 13, 21, 36)));
		TechniqueArt.Glow(renderer, (float)rect.X + (float)rect.Width / 2f, rect.Bottom - _d.L(40f), _d.L(80f), A(color, 0.25f));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
		if (string.IsNullOrWhiteSpace(membre.Sprite))
		{
			int num2 = _d.L(92f);
			Hexagone(renderer, new Rectangle(rect.X + (rect.Width - num2) / 2, rect.Y + (rect.Height - num2) / 2, num2, num2), color, TechniqueArt.Tex("saiyan_carte_icone_clan.png"), TechniqueArt.White, !membre.Online);
		}
		else
		{
			DeboutHabille(renderer, membre, rect.X + rect.Width / 2, rect.Bottom - _d.L(10f), _d.L(116f), membre.Online ? TechniqueArt.White : GrisHorsLigne);
		}
		string text = (membre.Online ? Strings.Guilds.Online : Strings.Guilds.Offline).ToString().ToUpperInvariant();
		Puce(renderer, text, rect.X + _d.L(8f) + TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10f), text, _d.Em(0.1f, 10f)) + _d.L(12f), rect.Y + _d.L(18f), membre.Online ? TechniqueArt.Green : TechniqueArt.Line2, membre.Online);
		float num3 = 242f;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(23f), TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(23f), membre.Name, width, 1).FirstOrDefault(), x, _d.Y(num3), A(flag ? TechniqueArt.Gold : TechniqueArt.Text));
		num3 += 22f;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), TitreDuRang(membre.Rank), x, _d.Y(num3), A(flag2 ? TechniqueArt.Gold : TechniqueArt.Soft));
		num3 += 26f;
		(string, string)[] array = new(string, string)[4]
		{
			(Strings.Guilds.LevelLabel.ToString(), $"{membre.Level}"),
			(Strings.Guilds.ClassLabel.ToString(), membre.ClassName ?? string.Empty),
			(Strings.Guilds.WhereLabel.ToString(), membre.Online ? Traduction.Contenu(membre.MapName) : Strings.Guilds.Offline.ToString()),
			(Strings.Guilds.Contribution.ToString(), Strings.Guilds.ContributionShort.ToString(MarketWindow.FormatAmount(membre.Contribution)))
		};
		for (int i = 0; i < array.Length; i++)
		{
			var (text2, text3) = array[i];
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10.5f), text2.ToUpperInvariant(), x, _d.Y(num3), _d.Em(0.14f, 10.5f), A(TechniqueArt.Dim));
			num3 += 15f;
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), text3, width, 1).FirstOrDefault(), x, _d.Y(num3), A(TechniqueArt.Text));
			num3 += 22f;
		}
		List<(string, string, Color, Action)> list = new List<(string, string, Color, Action)>();
		if (!flag)
		{
			GuildRank guildRank = Globals.Me?.GuildRank;
			if (guildRank != null)
			{
				int rank = Globals.Me.Rank;
				bool flag3 = rank == 0;
				if (membre.Online)
				{
					string nom = membre.Name;
					list.Add(("fiche:message", Strings.Friends.MessageButton.ToString(), null, delegate
					{
						Interface.GameUi?.SetChatboxText("/pm " + nom + " ");
					}));
				}
				if ((flag3 || guildRank.Permissions.Promote) && membre.Rank > rank && membre.Rank - 1 > rank)
				{
					list.Add(("fiche:promouvoir", Strings.Guilds.Promote.ToString(TitreDuRang(membre.Rank - 1)), null, delegate
					{
						ChangerRang(membre, promotion: true);
					}));
				}
				if ((flag3 || guildRank.Permissions.Demote) && membre.Rank > rank && membre.Rank + 1 < Options.Instance.Guild.Ranks.Length)
				{
					list.Add(("fiche:retrograder", Strings.Guilds.Demote.ToString(TitreDuRang(membre.Rank + 1)), null, delegate
					{
						ChangerRang(membre, promotion: false);
					}));
				}
				if (flag3)
				{
					list.Add(("fiche:transferer", Strings.Guilds.TransferButton.ToString(), null, delegate
					{
						Transferer(membre);
					}));
				}
				if ((flag3 || guildRank.Permissions.Kick) && membre.Rank > rank)
				{
					list.Add(("fiche:exclure", Strings.Guilds.KickButton.ToString(), Rouge, delegate
					{
						Exclure(membre);
					}));
				}
			}
		}
		float num4 = 686f;
		foreach (var item6 in list.AsEnumerable().Reverse())
		{
			string item = item6.Item1;
			string item2 = item6.Item2;
			Color item3 = item6.Item3;
			Action item4 = item6.Item4;
			Rectangle rect2 = _d.R(974f, num4, 236f, 36f);
			HouseArt.Hollow(renderer, _d, rect2, item2, IsHot(item), _fade, item3);
			AddHit(rect2, item, item4);
			num4 -= 42f;
		}
	}

	private void DrawIdentite(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		Panel(renderer, _d.R(248f, 58f, 700f, 676f));
		float num = 264f;
		Titre(renderer, 84f, Strings.Guilds.IdentityTitle, Strings.Guilds.IdentityHint);
		float num2 = 164f;
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10.5f), Strings.Guilds.TagLabel.ToString().ToUpperInvariant(), _d.X(num + 4f), _d.Y(num2 + 10f), _d.Em(0.14f, 10.5f), A(TechniqueArt.Dim));
		Rectangle rect = _d.R(num + 4f, num2 + 24f, 180f, 36f);
		TechniqueArt.Fill(renderer, rect, A(new Color(255, 7, 10, 18)));
		TechniqueArt.Outline(renderer, rect, A(AuClavier(_tag) ? TechniqueArt.Ki : TechniqueArt.Line2));
		if (string.IsNullOrEmpty(_tag.Text))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13.5f), Strings.Guilds.TagPlaceholder, rect.X + _d.L(10f), rect.Y + rect.Height / 2, A(TechniqueArt.Dim));
		}
		foreach (var item in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), Strings.Guilds.TagHint, _d.L(400f), 2).Select((string texte, int index) => (texte: texte, index: index)))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), item.texte, _d.X(num + 200f), _d.Y(num2 + 34f + (float)(item.index * 15)), A(BodyLight));
		}
		num2 += 82f;
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10.5f), Strings.Guilds.EmblemLabel.ToString().ToUpperInvariant(), _d.X(num + 4f), _d.Y(num2 + 10f), _d.Em(0.14f, 10.5f), A(TechniqueArt.Dim));
		num2 += 26f;
		for (int num3 = 0; num3 < 12; num3++)
		{
			Rectangle rect2 = _d.R(num + 4f + (float)(num3 % 6) * 80f, num2 + (float)(num3 / 6) * 80f, 72f, 72f);
			bool flag = num3 == _emblemeChoisi;
			string cle = $"emb:{num3}";
			if (flag)
			{
				TechniqueArt.FillHorizontal(renderer, rect2, A(TechniqueArt.Ki, 0.16f), A(TechniqueArt.Ki, 0.08f), A(TechniqueArt.Ki, 0.03f));
				TechniqueArt.Outline(renderer, rect2, A(TechniqueArt.Ki));
			}
			else
			{
				TechniqueArt.Fill(renderer, rect2, A(TechniqueArt.White, IsHot(cle) ? 0.06f : 0.03f));
				TechniqueArt.Outline(renderer, rect2, A(IsHot(cle) ? TechniqueArt.Line2 : TechniqueArt.Line));
			}
			int num4 = _d.L(52f);
			ClanEmbleme.Dessiner(renderer, new Rectangle(rect2.X + (rect2.Width - num4) / 2, rect2.Y + (rect2.Height - num4) / 2, num4, num4), num3, _couleurChoisie, null, _fade);
			int choix = num3;
			AddHit(rect2, cle, delegate
			{
				_emblemeChoisi = choix;
			});
		}
		num2 += 174f;
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10.5f), Strings.Guilds.ColorLabel.ToString().ToUpperInvariant(), _d.X(num + 4f), _d.Y(num2 + 10f), _d.Em(0.14f, 10.5f), A(TechniqueArt.Dim));
		num2 += 26f;
		string[] array = Strings.Guilds.ColorNames.ToString().Split('|');
		for (int num5 = 0; num5 < ClanEmbleme.Couleurs.Length; num5++)
		{
			Rectangle rect3 = _d.R(num + 4f + (float)(num5 * 60), num2, 52f, 40f);
			bool flag2 = num5 == _couleurChoisie;
			string cle2 = $"col:{num5}";
			TechniqueArt.Fill(renderer, rect3, A(ClanEmbleme.Couleurs[num5], flag2 ? 1f : (IsHot(cle2) ? 0.8f : 0.6f)));
			TechniqueArt.Outline(renderer, rect3, A(flag2 ? TechniqueArt.White : TechniqueArt.Line2), (!flag2) ? 1 : 2);
			if (num5 < array.Length)
			{
				TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(10f), array[num5], rect3.X + rect3.Width / 2, rect3.Bottom + _d.L(10f), A(flag2 ? TechniqueArt.Text : TechniqueArt.Dim));
			}
			int choix2 = num5;
			AddHit(rect3, cle2, delegate
			{
				_couleurChoisie = choix2;
			});
		}
		int length = (_tag.Text ?? string.Empty).Trim().Length;
		bool flag3 = length >= 2 && length <= 5 && (_tag.Text ?? string.Empty).Trim().All(char.IsLetterOrDigit);
		Rectangle rect4 = _d.R(num, 682f, 220f, 36f);
		HouseArt.Cta(renderer, _d, rect4, Strings.Guilds.Save.ToString(), flag3, flag3 && IsHot("ident:enregistrer"), _fade);
		AddHit(rect4, "ident:enregistrer", EnregistrerIdentite);
		Rectangle rect5 = _d.R(num + 232f, 682f, 160f, 36f);
		HouseArt.Hollow(renderer, _d, rect5, Strings.Guilds.Cancel.ToString(), IsHot("ident:annuler"), _fade);
		AddHit(rect5, "ident:annuler", delegate
		{
			_identite = false;
			_tag.Blur();
		});
		Panel(renderer, _d.R(958f, 58f, 268f, 676f));
		Rectangle rect6 = _d.R(970f, 70f, 244f, 196f);
		TechniqueArt.FillVertical(renderer, rect6, A(new Color(255, 10, 18, 32)), A(new Color(255, 13, 21, 36)));
		TechniqueArt.Glow(renderer, (float)rect6.X + (float)rect6.Width / 2f, (float)rect6.Y + (float)rect6.Height / 2f, _d.L(90f), A(ClanEmbleme.Couleur(_couleurChoisie), 0.25f));
		TechniqueArt.Outline(renderer, rect6, A(TechniqueArt.Line));
		int num6 = _d.L(120f);
		ClanEmbleme.Dessiner(renderer, new Rectangle(rect6.X + (rect6.Width - num6) / 2, rect6.Y + (rect6.Height - num6) / 2, num6, num6), _emblemeChoisi, _couleurChoisie, null, _fade);
		string text = (_tag.Text ?? string.Empty).Trim().ToUpperInvariant();
		float num7 = 288f;
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10.5f), Strings.Guilds.PreviewLabel.ToString().ToUpperInvariant(), _d.X(974f), _d.Y(num7), _d.Em(0.14f, 10.5f), A(TechniqueArt.Dim));
		num7 += 22f;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(16f), (text.Length != 0) ? ("[" + text + "] " + Globals.Me?.Name) : Globals.Me?.Name, _d.X(974f), _d.Y(num7), A(TechniqueArt.Text));
		num7 += 22f;
		int num8 = _d.L(16f);
		ClanEmbleme.Dessiner(renderer, new Rectangle(_d.X(974f), _d.Y(num7) - num8 / 2, num8, num8), _emblemeChoisi, _couleurChoisie, null, _fade);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), Globals.Me?.Guild, _d.X(974f) + num8 + _d.L(6f), _d.Y(num7), A(ClanEmbleme.Couleur(_couleurChoisie)));
	}

	private void DrawPageClan(Intersect.Client.Framework.Gwen.Renderer.Base renderer, bool enClan)
	{
		GuildProfile guildProfile = Page();
		GuildDirectoryEntry guildDirectoryEntry = Annuaire().FirstOrDefault((GuildDirectoryEntry clan) => clan.Name == _page);
		int num = guildProfile?.Color ?? guildDirectoryEntry?.Color ?? 0;
		Color couleur = ClanEmbleme.Couleur(num);
		DrawPageBanniere(renderer, enClan, guildProfile, guildDirectoryEntry, num, couleur);
		if (guildProfile == null)
		{
			Panel(renderer, _d.R(14f, 300f, 1212f, 434f));
			TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(15f), Strings.Guilds.PageLoading, _d.X(620f), _d.Y(460f), A(TechniqueArt.Soft));
			return;
		}
		DrawPageChiffres(renderer, guildProfile, couleur);
		DrawPageMeneur(renderer, guildProfile, couleur);
		DrawPageInvestis(renderer, guildProfile, couleur);
		DrawPageEffectif(renderer, guildProfile, couleur);
	}

	private void DrawPageBanniere(Intersect.Client.Framework.Gwen.Renderer.Base renderer, bool enClan, GuildProfile? page, GuildDirectoryEntry? entree, int couleurIndex, Color couleur)
	{
		string text = page?.Name ?? entree?.Name ?? _page;
		string text2 = page?.Tag ?? entree?.Tag ?? string.Empty;
		int embleme = page?.Emblem ?? entree?.Emblem ?? 0;
		int num = Math.Max(1, page?.Level ?? entree?.Level ?? 1);
		Rectangle rectangle = _d.R(14f, 58f, 1212f, 232f);
		Panel(renderer, rectangle);
		TechniqueArt.FillHorizontal(renderer, rectangle, A(couleur, 0.32f), A(couleur, 0.11f), A(couleur, 0.02f));
		TechniqueArt.FillVertical(renderer, new Rectangle(rectangle.X, rectangle.Y + rectangle.Height / 2, rectangle.Width, rectangle.Height / 2), A(TechniqueArt.Void, 0f), A(TechniqueArt.Void, 0.5f));
		Rectangle previous = TechniqueArt.BeginClip(renderer, rectangle);
		int num2 = _d.L(420f);
		ClanEmbleme.DessinerPicto(renderer, new Rectangle(rectangle.Right - _d.L(400f), rectangle.Y + rectangle.Height / 2 - num2 / 2 + _d.L(40f), num2, num2), embleme, A(couleur, 0.09f));
		TechniqueArt.EndClip(renderer, previous);
		TechniqueArt.FillHorizontal(renderer, new Rectangle(rectangle.X, rectangle.Bottom - _d.L(3f), rectangle.Width, _d.L(3f)), A(couleur), A(couleur, 0.6f), A(couleur, 0.1f));
		Rectangle rect = _d.R(30f, 72f, 210f, 32f);
		HouseArt.Hollow(renderer, _d, rect, Strings.Guilds.BackToDirectory.ToString(), IsHot("page:retour"), _fade);
		AddHit(rect, "page:retour", FermerPage);
		int num3 = _d.L(150f);
		Rectangle rect2 = new Rectangle(_d.X(56f), _d.Y(120f), num3, num3);
		TechniqueArt.Glow(renderer, (float)rect2.X + (float)num3 / 2f, (float)rect2.Y + (float)num3 / 2f, _d.L(130f), A(couleur, 0.38f));
		ClanEmbleme.Dessiner(renderer, rect2, embleme, couleurIndex, null, _fade);
		int num4 = _d.L(56f);
		Rectangle hex = new Rectangle(rect2.Right - num4 + _d.L(10f), rect2.Bottom - num4 + _d.L(4f), num4, num4);
		Hexagone(renderer, hex, couleur, null, TechniqueArt.White, vide: false);
		TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(22f), $"{num}", hex.X + num4 / 2, hex.Y + num4 / 2, A(TechniqueArt.Text));
		int num5 = _d.X(250f);
		int num6 = _d.Y(138f);
		int num7 = 0;
		if (!string.IsNullOrWhiteSpace(text2))
		{
			string text3 = "[" + text2 + "]";
			int num8 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(11f), text3, _d.Em(0.12f, 11f)) + _d.L(16f);
			Rectangle rect3 = new Rectangle(num5, num6 - _d.L(11f), num8, _d.L(22f));
			TechniqueArt.Fill(renderer, rect3, A(couleur));
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(11f), text3, rect3.X + rect3.Width / 2, rect3.Y + rect3.Height / 2, _d.Em(0.12f, 11f), A(HouseArt.Dark));
			num7 = num8 + _d.L(12f);
		}
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(11f), Strings.Guilds.PageKicker.ToString(num).ToUpperInvariant(), num5 + num7, num6, _d.Em(0.22f, 11f), A(TechniqueArt.Mix(couleur, TechniqueArt.White, 0.6f)));
		int size = Pt(44f);
		string text4 = TechniqueArt.Wrap(renderer, TechniqueArt.Display, size, text.ToUpperInvariant(), _d.L(560f), 1).FirstOrDefault() ?? text;
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, size, text4);
		TechniqueArt.Styled(renderer, TechniqueArt.Display, size, text4, num5, _d.Y(182f) - point.Y / 2, new TechniqueArt.Stops(TechniqueArt.White, 0.3f, TechniqueArt.Mix(couleur, TechniqueArt.White, 0.45f), 0.85f, couleur), _fade);
		string text5 = page?.Leader ?? entree?.Leader ?? string.Empty;
		long horodatage = page?.FoundedAt ?? entree?.FoundedAt ?? 0;
		int num9 = ((page != null) ? page.Members.Length : (entree?.Members ?? 0));
		string text6 = $"{Strings.Guilds.DirectoryLeader.ToString(text5)}     ·     {Strings.Guilds.FoundedLabel} {Age(horodatage)}     ·     {Strings.Guilds.DirectoryMembers.ToString(num9)}";
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), text6, num5, _d.Y(224f), A(TechniqueArt.Soft));
		if (page != null)
		{
			Rectangle rect4 = new Rectangle(num5, _d.Y(250f), _d.L(420f), _d.L(10f));
			TechniqueArt.Fill(renderer, rect4, A(new Color(255, 7, 10, 18)));
			TechniqueArt.Outline(renderer, rect4, A(TechniqueArt.Line));
			float num10 = ((page.ExperienceToNext <= 0) ? 1f : Math.Clamp((float)page.Experience / (float)page.ExperienceToNext, 0f, 1f));
			if (num10 > 0f)
			{
				TechniqueArt.FillHorizontal(renderer, new Rectangle(rect4.X + 1, rect4.Y + 1, Math.Max(2, (int)((float)(rect4.Width - 2) * num10)), rect4.Height - 2), A(couleur, 0.7f), A(couleur), A(TechniqueArt.Mix(couleur, TechniqueArt.White, 0.6f)));
			}
			string text7 = ((page.ExperienceToNext <= 0) ? Strings.Guilds.MaxLevel.ToString() : Strings.Guilds.PageNextLevel.ToString(MarketWindow.FormatAmount(page.Experience), MarketWindow.FormatAmount(page.ExperienceToNext), num + 1));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), text7, rect4.Right + _d.L(14f), rect4.Y + rect4.Height / 2, A(TechniqueArt.Dim));
			int num11 = _d.X(1198f);
			TechniqueArt.SpacedRight(renderer, TechniqueArt.Label, Pt(10.5f), Strings.Guilds.PageRank.ToString().ToUpperInvariant(), num11, _d.Y(90f), _d.Em(0.18f, 10.5f), A(TechniqueArt.Dim));
			string text8 = ((page.Rank > 0) ? $"#{page.Rank}" : "—");
			Point point2 = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(42f), text8);
			Color color = page.Rank switch
			{
				1 => TechniqueArt.Gold, 
				2 => new Color(255, 214, 222, 236), 
				3 => new Color(255, 232, 156, 96), 
				_ => TechniqueArt.Text, 
			};
			TechniqueArt.Styled(renderer, TechniqueArt.Display, Pt(42f), text8, num11 - point2.X - _d.L(6f), _d.Y(130f) - point2.Y / 2, new TechniqueArt.Stops(TechniqueArt.White, 0.25f, color, 0.9f, color), _fade);
			TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(11.5f), Strings.Rankings.GuildLevel, num11, _d.Y(164f), A(TechniqueArt.Soft));
		}
		Rectangle rect5 = _d.R(948f, 226f, 250f, 44f);
		bool flag = enClan && text == Globals.Me?.Guild;
		bool flag2 = text == Globals.Me?.GuildAppliedTo;
		if (flag)
		{
			HouseArt.Hollow(renderer, _d, rect5, Strings.Guilds.YourClan.ToString(), hot: false, _fade, TechniqueArt.Gold);
			return;
		}
		if (enClan)
		{
			TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(12f), Strings.Guilds.PageAlreadyInClan, rect5.Right, rect5.Y + rect5.Height / 2, A(TechniqueArt.Dim));
			return;
		}
		if (flag2)
		{
			HouseArt.Hollow(renderer, _d, rect5, Strings.Guilds.Applied.ToString(), hot: false, _fade, TechniqueArt.Green);
			return;
		}
		HouseArt.Cta(renderer, _d, rect5, Strings.Guilds.Apply.ToString(), enabled: true, IsHot("page:candidater"), _fade);
		string cible = text;
		AddHit(rect5, "page:candidater", delegate
		{
			Candidater(cible);
		});
	}

	private void DrawPageChiffres(Intersect.Client.Framework.Gwen.Renderer.Base renderer, GuildProfile page, Color couleur)
	{
		int num = page.Members.Count((GuildMember membre) => membre.Online);
		(string, string, Color)[] array = new(string, string, Color)[5]
		{
			(Strings.Guilds.MembersLabel.ToString(), $"{page.Members.Length}", TechniqueArt.Text),
			(Strings.Guilds.Online.ToString(), $"{num}", (num > 0) ? TechniqueArt.Green : TechniqueArt.Dim),
			(Strings.Guilds.PageWeek.ToString(), "+" + MarketWindow.FormatAmount(page.WeekExperience), TechniqueArt.Mix(couleur, TechniqueArt.White, 0.55f)),
			(Strings.Guilds.ObjectivesTitle.ToString(), $"{page.ObjectivesDone} / {page.ObjectivesTotal}", (page.ObjectivesTotal > 0 && page.ObjectivesDone >= page.ObjectivesTotal) ? TechniqueArt.Green : TechniqueArt.Text),
			(Strings.Guilds.PageAge.ToString(), Duree(page.FoundedAt), TechniqueArt.Text)
		};
		float num2 = (1212f - 10f * (float)(array.Length - 1)) / (float)array.Length;
		for (int num3 = 0; num3 < array.Length; num3++)
		{
			(string, string, Color) tuple = array[num3];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			Color item3 = tuple.Item3;
			Rectangle rect = _d.R(14f + (float)num3 * (num2 + 10f), 300f, num2, 62f);
			TechniqueArt.FillVertical(renderer, rect, A(new Color(255, 13, 18, 30)), A(new Color(255, 9, 12, 21)));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, rect.Width, _d.L(2f)), A(couleur, 0.85f));
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), item.ToUpperInvariant(), rect.X + _d.L(16f), rect.Y + _d.L(19f), _d.Em(0.16f, 10f), A(TechniqueArt.Dim));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(23f), item2, rect.X + _d.L(16f), rect.Y + _d.L(43f), A(item3));
		}
	}

	private void TitrePanneau(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect, string titre, string? complement, Color couleur)
	{
		int centerY = rect.Y + _d.L(24f);
		DungeonHud.Diamond(renderer, rect.X + _d.L(22f), centerY, _d.L(10f), A(couleur));
		int num = TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(11.5f), titre.ToUpperInvariant(), rect.X + _d.L(36f), centerY, _d.Em(0.2f, 11.5f), A(TechniqueArt.Text));
		if (!string.IsNullOrEmpty(complement))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), complement, rect.X + _d.L(36f) + num + _d.L(14f), centerY, A(TechniqueArt.Dim));
		}
	}

	private void DrawPageMeneur(Intersect.Client.Framework.Gwen.Renderer.Base renderer, GuildProfile page, Color couleur)
	{
		Rectangle rect = _d.R(14f, 372f, 392f, 190f);
		Panel(renderer, rect);
		TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Gold, 0.08f), A(TechniqueArt.Gold, 0.02f), A(TechniqueArt.Gold, 0f));
		TitrePanneau(renderer, rect, Strings.Guilds.PageLeader.ToString(), null, TechniqueArt.Gold);
		GuildMember guildMember = page.Members.FirstOrDefault((GuildMember membre) => membre.Rank == 0);
		if (guildMember != null)
		{
			int num = rect.X + _d.L(96f);
			int num2 = rect.Bottom - _d.L(20f);
			TechniqueArt.Glow(renderer, num, num2 - _d.L(10f), _d.L(80f), A(TechniqueArt.Gold, guildMember.Online ? 0.28f : 0.12f));
			TechniqueArt.FillHorizontal(renderer, new Rectangle(num - _d.L(52f), num2 - _d.L(1f), _d.L(104f), _d.L(3f)), A(TechniqueArt.Gold, 0f), A(TechniqueArt.Gold, 0.75f), A(TechniqueArt.Gold, 0f));
			DeboutHabille(renderer, guildMember, num, num2, _d.L(126f), guildMember.Online ? TechniqueArt.White : GrisHorsLigne);
			int num3 = rect.X + _d.L(188f);
			string text = Strings.Guilds.LeaderLabel.ToString().ToUpperInvariant();
			Puce(renderer, text, num3 + TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10f), text, _d.Em(0.1f, 10f)) + _d.L(12f), rect.Y + _d.L(60f), TechniqueArt.Gold, pleine: true);
			int num4 = rect.Right - _d.L(16f) - num3;
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(24f), TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(24f), guildMember.Name, num4, 1).FirstOrDefault(), num3, rect.Y + _d.L(90f), A(TechniqueArt.Text));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), Strings.Guilds.Tooltip.ToString(guildMember.Level, guildMember.ClassName), num3, rect.Y + _d.L(116f), A(TechniqueArt.Soft));
			int num5 = _d.L(8f);
			TechniqueArt.Fill(renderer, new Rectangle(num3, rect.Y + _d.L(140f) - num5 / 2, num5, num5), A(guildMember.Online ? TechniqueArt.Green : TechniqueArt.Dim));
			string text2 = (guildMember.Online ? Traduction.Contenu(guildMember.MapName) : Strings.Guilds.Offline.ToString());
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), string.IsNullOrWhiteSpace(text2) ? Strings.Guilds.Online.ToString() : text2, num4 - num5 - _d.L(8f), 1).FirstOrDefault(), num3 + num5 + _d.L(8f), rect.Y + _d.L(140f), A(guildMember.Online ? TechniqueArt.Green : TechniqueArt.Dim));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(12f), $"{Strings.Guilds.Contribution} · {Strings.Guilds.ContributionShort.ToString(MarketWindow.FormatAmount(guildMember.Contribution))}", num3, rect.Y + _d.L(164f), A(TechniqueArt.Gold, 0.9f));
		}
	}

	private void DrawPageInvestis(Intersect.Client.Framework.Gwen.Renderer.Base renderer, GuildProfile page, Color couleur)
	{
		float num = 572f;
		Rectangle rect = _d.R(14f, num, 392f, 734f - num);
		Panel(renderer, rect);
		TitrePanneau(renderer, rect, Strings.Guilds.PageTop.ToString(), Strings.Guilds.PageTopHint.ToString(), couleur);
		GuildMember[] array = (from membre in page.Members
			where membre.Contribution > 0
			orderby membre.Contribution descending
			select membre).Take(3).ToArray();
		if (array.Length == 0)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), Strings.Guilds.PageNoContribution, rect.X + _d.L(22f), rect.Y + _d.L(64f), A(TechniqueArt.Dim));
			return;
		}
		Color[] array2 = new Color[3]
		{
			TechniqueArt.Gold,
			new Color(255, 214, 222, 236),
			new Color(255, 232, 156, 96)
		};
		long num2 = Math.Max(1L, array[0].Contribution);
		for (int num3 = 0; num3 < array.Length; num3++)
		{
			GuildMember guildMember = array[num3];
			int num4 = rect.Y + _d.L(48 + num3 * 38);
			Color color = array2[num3];
			string text = $"{num3 + 1}";
			Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(20f), text);
			TechniqueArt.Styled(renderer, TechniqueArt.Display, Pt(20f), text, rect.X + _d.L(20f), num4 + _d.L(16f) - point.Y / 2, TechniqueArt.Stops.Solid(color), _fade);
			Portrait(renderer, guildMember.Sprite, new Rectangle(rect.X + _d.L(46f), num4 + _d.L(1f), _d.L(30f), _d.L(30f)), color, !guildMember.Online);
			int num5 = rect.X + _d.L(86f);
			int num6 = rect.Right - _d.L(18f);
			string text2 = Strings.Guilds.ContributionShort.ToString(MarketWindow.FormatAmount(guildMember.Contribution));
			int num7 = TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(12f), text2, num6, num4 + _d.L(11f), A(color));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(14f), TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(14f), guildMember.Name, num6 - num5 - num7 - _d.L(10f), 1).FirstOrDefault(), num5, num4 + _d.L(11f), A(TechniqueArt.Text));
			Rectangle rect2 = new Rectangle(num5, num4 + _d.L(24f), num6 - num5, _d.L(4f));
			TechniqueArt.Fill(renderer, rect2, A(TechniqueArt.White, 0.06f));
			TechniqueArt.FillHorizontal(renderer, new Rectangle(rect2.X, rect2.Y, Math.Max(2, (int)((float)rect2.Width * ((float)guildMember.Contribution / (float)num2))), rect2.Height), A(color, 0.5f), A(color), A(color, 0.8f));
		}
	}

	private void DrawPageEffectif(Intersect.Client.Framework.Gwen.Renderer.Base renderer, GuildProfile page, Color couleur)
	{
		float num = 416f;
		float num2 = 810f;
		Rectangle rect = _d.R(num, 372f, num2, 362f);
		Panel(renderer, rect);
		int num3 = page.Members.Count((GuildMember membre) => membre.Online);
		TitrePanneau(renderer, rect, Strings.Guilds.MembersLabel.ToString(), Strings.Guilds.MembersCount.ToString(page.Members.Length, num3), couleur);
		float num4 = num2 - 32f;
		float num5 = (num4 - 30f) / 4f;
		float num6 = 418f;
		float num7 = (722f - num6 - 10f) / 2f;
		_visibles = 2;
		_rangees = (page.Members.Length + 4 - 1) / 4;
		_scroll = Math.Clamp(_scroll, 0, Math.Max(0, _rangees - _visibles));
		_liste = _d.R(num + 16f, num6, num4, num7 * 2f + 10f);
		Rectangle previous = TechniqueArt.BeginClip(renderer, _liste);
		for (int num8 = _scroll * 4; num8 < page.Members.Length && num8 < (_scroll + _visibles) * 4; num8++)
		{
			int num9 = num8 - _scroll * 4;
			Rectangle carte = _d.R(num + 16f + (float)(num9 % 4) * (num5 + 10f), num6 + (float)(num9 / 4) * (num7 + 10f), num5, num7);
			DrawCartePage(renderer, page.Members[num8], carte, couleur);
		}
		TechniqueArt.EndClip(renderer, previous);
		Ascenseur(renderer);
	}

	private void DrawCartePage(Intersect.Client.Framework.Gwen.Renderer.Base renderer, GuildMember membre, Rectangle carte, Color couleur)
	{
		string cle = $"page-membre-{membre.Id}";
		bool flag = membre.Rank == 0;
		Color color = (flag ? TechniqueArt.Gold : couleur);
		bool flag2 = IsHot(cle) && membre.Online;
		TechniqueArt.FillVertical(renderer, carte, A(color, (!membre.Online) ? 0.04f : (flag2 ? 0.2f : 0.13f)), A(new Color(255, 8, 11, 19), 0.95f));
		TechniqueArt.Outline(renderer, carte, A(flag2 ? TechniqueArt.Mix(color, TechniqueArt.White, 0.6f) : (flag ? TechniqueArt.Alpha(TechniqueArt.Gold, 0.55f) : TechniqueArt.Line)));
		int num = _d.L(8f);
		TechniqueArt.Fill(renderer, new Rectangle(carte.X + _d.L(10f), carte.Y + _d.L(12f), num, num), A(membre.Online ? TechniqueArt.Green : TechniqueArt.Dim, membre.Online ? 1f : 0.6f));
		string texte = TitreDuRang(membre.Rank).ToUpperInvariant();
		Puce(renderer, texte, carte.Right - _d.L(8f), carte.Y + _d.L(16f), flag ? TechniqueArt.Gold : TechniqueArt.Line2, flag);
		int num2 = carte.X + carte.Width / 2;
		int num3 = carte.Y + _d.L(94f);
		TechniqueArt.Glow(renderer, num2, num3 - _d.L(8f), _d.L(44f), A(color, membre.Online ? 0.24f : 0.08f));
		TechniqueArt.FillHorizontal(renderer, new Rectangle(num2 - _d.L(34f), num3 - _d.L(1f), _d.L(68f), _d.L(2f)), A(color, 0f), A(color, membre.Online ? 0.7f : 0.25f), A(color, 0f));
		DeboutHabille(renderer, membre, num2, num3, _d.L(70f), membre.Online ? TechniqueArt.White : GrisHorsLigne);
		int width = carte.Width - _d.L(16f);
		TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(15f), TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(15f), membre.Name, width, 1).FirstOrDefault(), num2, carte.Y + _d.L(108f), A(membre.Online ? TechniqueArt.Text : TechniqueArt.Soft));
		TechniqueArt.TextCenter(renderer, TechniqueArt.Body, Pt(11f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11f), Strings.Guilds.Tooltip.ToString(membre.Level, membre.ClassName), width, 1).FirstOrDefault(), num2, carte.Y + _d.L(124f), A(TechniqueArt.Dim));
		if (membre.Contribution > 0)
		{
			TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(10.5f), Strings.Guilds.ContributionShort.ToString(MarketWindow.FormatAmount(membre.Contribution)), num2, carte.Y + _d.L(139f), A(TechniqueArt.Gold, 0.85f));
		}
		if (membre.Online)
		{
			if (IsHot(cle))
			{
				Etiqueter(cle, carte, Strings.Friends.MessageTip);
			}
			string nom = membre.Name;
			AddHit(carte, cle, delegate
			{
				Interface.GameUi?.SetChatboxText("/pm " + nom + " ");
			});
		}
	}

	private static string Duree(long fonde)
	{
		double num = Math.Max(0.0, (DateTime.UtcNow - DateTime.UnixEpoch.AddMilliseconds(fonde)).TotalHours);
		if (!(num < 24.0))
		{
			return Strings.Guilds.AgeDays.ToString((int)(num / 24.0));
		}
		return Strings.Guilds.AgeHours.ToString((int)num);
	}

	private void DrawAnnuaire(Intersect.Client.Framework.Gwen.Renderer.Base renderer, bool enClan)
	{
		Panel(renderer, _d.R(248f, 58f, 700f, 676f));
		float num = 264f;
		Champ(renderer, _d.R(num, 74f, 532f, 36f), _recherche, "inv_loupe.png", Strings.Guilds.SearchPlaceholder, TechniqueArt.Line2);
		Rectangle rect = _d.R(num + 532f + 8f, 74f, 128f, 36f);
		HouseArt.Cta(renderer, _d, rect, Strings.Guilds.SearchButton.ToString(), enabled: true, IsHot("chercher"), _fade);
		AddHit(rect, "chercher", delegate
		{
			DemanderAnnuaire(0);
		});
		int num2 = Globals.Me?.GuildDirectoryTotal ?? 0;
		int num3 = Globals.Me?.GuildDirectoryPage ?? 0;
		int num4 = ((num2 <= 0) ? 1 : ((num2 - 1) / 20 + 1));
		Titre(renderer, 124f, Strings.Guilds.Directory, Strings.Guilds.DirectoryPage.ToString(num2, num3 + 1, num4));
		int num5 = _d.Y(141f);
		int num6 = _d.X(932f);
		(string, bool, bool)[] array = new(string, bool, bool)[2]
		{
			("page:suivante", true, num3 < num4 - 1),
			("page:precedente", false, num3 > 0)
		};
		for (int num7 = 0; num7 < array.Length; num7++)
		{
			(string, bool, bool) tuple = array[num7];
			string item = tuple.Item1;
			bool item2 = tuple.Item2;
			bool item3 = tuple.Item3;
			Rectangle rect2 = new Rectangle(num6 - _d.L(30f), num5 - _d.L(14f), _d.L(30f), _d.L(28f));
			bool flag = item3 && IsHot(item);
			TechniqueArt.Fill(renderer, rect2, A(new Color(255, 10, 14, 24)));
			TechniqueArt.Outline(renderer, rect2, A(flag ? TechniqueArt.Line2 : TechniqueArt.Line, item3 ? 1f : 0.4f));
			TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(15f), (item2 ? Strings.Guilds.DirectoryNext : Strings.Guilds.DirectoryPrevious).ToString(), rect2.X + rect2.Width / 2, num5, A(flag ? TechniqueArt.Ki : TechniqueArt.Soft, item3 ? 1f : 0.3f));
			if (item3)
			{
				int cible = (item2 ? (num3 + 1) : (num3 - 1));
				AddHit(rect2, item, delegate
				{
					DemanderAnnuaire(cible);
				});
			}
			num6 -= _d.L(30f) + _d.L(6f);
		}
		GuildDirectoryEntry[] array2 = Annuaire();
		if (array2.Length == 0)
		{
			TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(14f), Strings.Guilds.DirectoryEmpty, _d.X(598f), _d.Y(270f), A(TechniqueArt.Soft));
			return;
		}
		_visibles = Math.Max(1, 8);
		_rangees = array2.Length;
		_scroll = Math.Clamp(_scroll, 0, Math.Max(0, _rangees - _visibles));
		_liste = _d.R(num, 170f, 668f, (float)_visibles * 64f - 6f);
		Rectangle previous = TechniqueArt.BeginClip(renderer, _liste);
		for (int num8 = _scroll; num8 < array2.Length && num8 - _scroll < _visibles; num8++)
		{
			DrawClan(renderer, array2[num8], num, 170f + (float)(num8 - _scroll) * 64f, enClan);
		}
		TechniqueArt.EndClip(renderer, previous);
		Ascenseur(renderer);
	}

	private void DrawClan(Intersect.Client.Framework.Gwen.Renderer.Base renderer, GuildDirectoryEntry clan, float x, float y, bool enClan)
	{
		Rectangle rect = _d.R(x, y, 668f, 58f);
		bool flag = clan.Name == _clanChoisi;
		string cle = "clan-" + clan.Name;
		bool flag2 = IsHot(cle);
		bool flag3 = enClan && clan.Name == Globals.Me?.Guild;
		bool flag4 = clan.Name == Globals.Me?.GuildAppliedTo;
		if (flag)
		{
			TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Ki, 0.14f), A(TechniqueArt.Ki, 0.06f), A(TechniqueArt.Ki, 0.02f));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Ki));
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(3f), rect.Height), A(TechniqueArt.Ki));
		}
		else
		{
			TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, flag2 ? 0.05f : 0.03f));
			TechniqueArt.Outline(renderer, rect, A(flag2 ? TechniqueArt.Line2 : TechniqueArt.Line));
		}
		int num = _d.L(40f);
		ClanEmbleme.Dessiner(renderer, new Rectangle(rect.X + _d.L(11f), rect.Y + (rect.Height - num) / 2, num, num), clan.Emblem, clan.Color, null, _fade);
		int num2 = rect.X + _d.L(62f);
		string text = (string.IsNullOrWhiteSpace(clan.Tag) ? clan.Name : ("[" + clan.Tag + "] " + clan.Name));
		int num3 = TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(16f), text, num2, rect.Y + _d.L(20f), A(flag3 ? TechniqueArt.Gold : TechniqueArt.Text));
		if (flag3)
		{
			Puce(renderer, Strings.Guilds.YourClan.ToString().ToUpperInvariant(), num2 + num3 + _d.L(12f) + TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10f), Strings.Guilds.YourClan.ToString().ToUpperInvariant(), _d.Em(0.1f, 10f)) + _d.L(12f), rect.Y + _d.L(20f), TechniqueArt.Gold, pleine: true);
		}
		else if (flag4)
		{
			string text2 = Strings.Guilds.Applied.ToString().ToUpperInvariant();
			Puce(renderer, text2, num2 + num3 + _d.L(12f) + TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10f), text2, _d.Em(0.1f, 10f)) + _d.L(12f), rect.Y + _d.L(20f), TechniqueArt.Green, pleine: false);
		}
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11f), Strings.Guilds.DirectoryLeader.ToString(clan.Leader), num2, rect.Y + _d.L(41f), A(TechniqueArt.Soft));
		TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(15f), Strings.Guilds.DirectoryMembers.ToString(clan.Members), rect.Right - _d.L(14f), rect.Y + _d.L(20f), A(TechniqueArt.Text));
		TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(11f), $"{Strings.Guilds.FoundedLabel} {Age(clan.FoundedAt)}", rect.Right - _d.L(14f), rect.Y + _d.L(41f), A(TechniqueArt.Dim));
		string texte = Strings.Guilds.LevelChip.ToString(Math.Max(1, clan.Level)).ToUpperInvariant();
		Puce(renderer, texte, rect.Right - _d.L(150f), rect.Y + _d.L(20f), TechniqueArt.Ki, pleine: false);
		string nom = clan.Name;
		AddHit(rect, cle, delegate
		{
			if (_clanChoisi == nom)
			{
				OuvrirPage(nom);
			}
			else
			{
				_clanChoisi = nom;
			}
		});
	}

	private void DrawFicheClan(Intersect.Client.Framework.Gwen.Renderer.Base renderer, bool enClan, GuildDirectoryEntry? entree)
	{
		Panel(renderer, _d.R(958f, 58f, 268f, 676f));
		int x = _d.X(974f);
		int width = _d.L(236f);
		if (entree == null)
		{
			float num = 98f;
			{
				foreach (string item in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), Strings.Guilds.PickClan, width, 6))
				{
					TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), item, x, _d.Y(num), A(BodyLight));
					num += 16f;
				}
				return;
			}
		}
		_clanChoisi = entree.Name;
		bool flag = enClan && entree.Name == Globals.Me?.Guild;
		bool flag2 = entree.Name == Globals.Me?.GuildAppliedTo;
		Color color = (flag ? TechniqueArt.Gold : (flag2 ? TechniqueArt.Green : TechniqueArt.Ki));
		Rectangle rect = _d.R(970f, 70f, 244f, 150f);
		TechniqueArt.FillVertical(renderer, rect, A(new Color(255, 10, 18, 32)), A(new Color(255, 13, 21, 36)));
		TechniqueArt.Glow(renderer, (float)rect.X + (float)rect.Width / 2f, (float)rect.Y + (float)rect.Height / 2f, _d.L(80f), A(color, 0.25f));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
		int num2 = _d.L(96f);
		ClanEmbleme.Dessiner(renderer, new Rectangle(rect.X + (rect.Width - num2) / 2, rect.Y + (rect.Height - num2) / 2, num2, num2), entree.Emblem, entree.Color, null, _fade);
		if (!string.IsNullOrWhiteSpace(entree.Tag))
		{
			Puce(renderer, "[" + entree.Tag + "]", rect.Right - _d.L(8f), rect.Y + _d.L(18f), ClanEmbleme.Couleur(entree.Color), pleine: false);
		}
		float num3 = 242f;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(23f), TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(23f), entree.Name, width, 1).FirstOrDefault(), x, _d.Y(num3), A(flag ? TechniqueArt.Gold : TechniqueArt.Text));
		num3 += 30f;
		(string, string)[] array = new(string, string)[4]
		{
			(Strings.Guilds.ClanLevel.ToString(), $"{Math.Max(1, entree.Level)}"),
			(Strings.Guilds.LeaderLabel.ToString(), entree.Leader),
			(Strings.Guilds.MembersLabel.ToString(), Strings.Guilds.DirectoryMembers.ToString(entree.Members)),
			(Strings.Guilds.FoundedLabel.ToString(), Age(entree.FoundedAt))
		};
		for (int i = 0; i < array.Length; i++)
		{
			var (text, text2) = array[i];
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10.5f), text.ToUpperInvariant(), x, _d.Y(num3), _d.Em(0.14f, 10.5f), A(TechniqueArt.Dim));
			num3 += 15f;
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), text2, width, 1).FirstOrDefault(), x, _d.Y(num3), A(TechniqueArt.Text));
			num3 += 22f;
		}
		float num4 = 686f;
		if (_page.Length == 0)
		{
			Rectangle rect2 = _d.R(974f, enClan ? num4 : (num4 - 42f), 236f, 36f);
			HouseArt.Hollow(renderer, _d, rect2, Strings.Guilds.ViewPage.ToString(), IsHot("voir-page"), _fade);
			string nomDeLaPage = entree.Name;
			AddHit(rect2, "voir-page", delegate
			{
				OuvrirPage(nomDeLaPage);
			});
		}
		if (enClan)
		{
			return;
		}
		num3 += 6f;
		foreach (string item2 in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), flag2 ? Strings.Guilds.AppliedHint.ToString() : Strings.Guilds.ApplyTip.ToString(), width, 4))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), item2, x, _d.Y(num3), A(flag2 ? TechniqueArt.Green : BodyLight));
			num3 += 15f;
		}
		Rectangle rect3 = _d.R(974f, 686f, 236f, 36f);
		if (flag2)
		{
			HouseArt.Hollow(renderer, _d, rect3, Strings.Guilds.Applied.ToString(), hot: false, _fade * 0.6f, TechniqueArt.Green);
			return;
		}
		string nom = entree.Name;
		HouseArt.Cta(renderer, _d, rect3, Strings.Guilds.Apply.ToString(), enabled: true, IsHot("candidater"), _fade);
		AddHit(rect3, "candidater", delegate
		{
			Candidater(nom);
		});
	}
}
