using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Entities;
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
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Client.Networking;

namespace Intersect.Client.Interface.Game.Groupe;

internal sealed class FenetreGroupe : FenetreCharte
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

	private const float InviteTop = 74f;

	private const float InviteHeight = 36f;

	private const float TitreTop = 124f;

	private const float TitreHeight = 34f;

	private const float ListeTop = 170f;

	private const float CardWidth = 668f;

	private const float CardHeight = 96f;

	private const float CardGap = 10f;

	private const float InviteLargeur = 532f;

	private static readonly Color BodyLight = new Color(255, 196, 204, 220);

	private static readonly Color GreyFrame = new Color(255, 70, 78, 96);

	private static readonly Color Rouge = new Color(255, 255, 120, 110);

	private static readonly Color Vie = new Color(255, 98, 212, 120);

	private static readonly Color KiBleu = new Color(255, 92, 170, 255);

	private Guid _choisi;

	private TextBox _invitation;

	protected override Rectangle Place => new Rectangle(180, 76, 1240, 748);

	private static int Maximum => Math.Max(1, Options.Instance.Party.MaximumMembers);

	public FenetreGroupe(Canvas gameCanvas)
		: base(gameCanvas, "FenetreGroupe")
	{
		ConstruireChamp();
	}

	public override void Hide()
	{
		_invitation.Blur();
		base.Hide();
	}

	public void Update()
	{
		if (!base.IsHidden)
		{
			Placer();
			PlacerChamp();
		}
	}

	private void ConstruireChamp()
	{
		_invitation = new TextBox(this, "Invitation")
		{
			Font = TechniqueArt.Body,
			FontSize = 11,
			ShouldDrawBackground = false,
			MaximumLength = 32
		};
		_invitation.SetTextColor(TechniqueArt.Text, ComponentState.Normal);
		_invitation.SubmitPressed += delegate
		{
			Inviter();
		};
		Interface.FocusComponents.Add(_invitation);
	}

	private void PlacerChamp()
	{
		if (_invitation.FontSize != Pt(13.5f))
		{
			_invitation.FontSize = Pt(13.5f);
		}
		Rectangle rectangle = _d.R(298f, 78f, 488f, 28f);
		if (_invitation.X != rectangle.X || _invitation.Y != rectangle.Y || _invitation.Width != rectangle.Width || _invitation.Height != rectangle.Height)
		{
			_invitation.SetBounds(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
		}
	}

	private void Inviter()
	{
		string text = _invitation.Text?.Trim();
		if (!string.IsNullOrEmpty(text))
		{
			PacketSender.SendPartyInvite(text);
			_invitation.SetText(string.Empty);
		}
	}

	private bool ChampAuClavier()
	{
		return InputHandler.KeyboardFocus == _invitation;
	}

	private static List<IPartyMember> Membres()
	{
		return (Globals.Me?.Party ?? new List<IPartyMember>()).Where((IPartyMember membre) => membre != null).ToList();
	}

	private static bool EstMoi(IPartyMember membre)
	{
		Player me = Globals.Me;
		if (me != null)
		{
			return membre.Id == me.Id;
		}
		return false;
	}

	private static bool EstChef(List<IPartyMember> membres, IPartyMember membre)
	{
		if (membres.Count > 0)
		{
			return membres[0].Id == membre.Id;
		}
		return false;
	}

	private static bool JeMene(List<IPartyMember> membres)
	{
		Player me = Globals.Me;
		if (me != null && membres.Count > 0)
		{
			return membres[0].Id == me.Id;
		}
		return false;
	}

	private IPartyMember? Choisi(List<IPartyMember> membres)
	{
		return membres.FirstOrDefault((IPartyMember membre) => membre.Id == _choisi) ?? membres.FirstOrDefault();
	}

	private static (int Avec, int Sans) Part(int membres)
	{
		membres = Math.Max(1, membres);
		float num = Options.Instance.Party.BonusExperiencePercentPerMember / 100f;
		return (Avec: (int)Math.Round((1f + (float)membres * num) / (float)membres * 100f), Sans: (int)Math.Round(100f / (float)membres));
	}

	private static bool HorsDePortee(IPartyMember membre)
	{
		Player me = Globals.Me;
		if (me == null || membre.Id == me.Id)
		{
			return false;
		}
		if (!Globals.Entities.TryGetValue(membre.Id, out Entity value) || value == null || value.MapId != me.MapId)
		{
			return false;
		}
		return Math.Max(Math.Abs(value.X - me.X), Math.Abs(value.Y - me.Y)) > Options.Instance.Party.SharedXpRange;
	}

	private static (long Actuel, long Maximum) Vital(IPartyMember membre, int index)
	{
		if (index >= membre.Vital.Length || index >= membre.MaxVital.Length)
		{
			return (Actuel: 0L, Maximum: 1L);
		}
		long num = Math.Max(1L, membre.MaxVital[index]);
		return (Actuel: Math.Clamp(membre.Vital[index], 0L, num), Maximum: num);
	}

	private static string Chiffres(long actuel, long maximum)
	{
		return $"{actuel:N0} / {maximum:N0}".Replace(',', ' ');
	}

	protected override void Dessiner(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		DessinerCadre(renderer);
		int num = DessinerBarre(renderer, Strings.Parties.Title);
		List<IPartyMember> list = Membres();
		if (list.Count > 0)
		{
			int num2 = _d.Y(23f);
			int num3 = num - _d.L(18f);
			int num4 = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(16f), Strings.Parties.Count.ToString(list.Count, Maximum), num3, num2, A(TechniqueArt.Text));
			num3 -= num4 + _d.L(8f);
			string text = Strings.Parties.Members.ToString().ToUpperInvariant();
			int num5 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10f), text, _d.Em(0.16f, 10f));
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), text, num3 - num5, num2 + _d.L(1f), _d.Em(0.16f, 10f), A(TechniqueArt.Dim));
		}
		DrawRail(renderer, list);
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

	private void Portrait(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string? sprite, Rectangle hex, Color accent, bool eteint)
	{
		Hexagone(renderer, hex, accent, null, TechniqueArt.White, eteint);
		int num = Math.Max(2, _d.L(5f));
		Rectangle rectangle = new Rectangle(hex.X + num * 2, hex.Y + num * 2, hex.Width - num * 4, hex.Height - num * 4);
		if (!string.IsNullOrWhiteSpace(sprite))
		{
			(IGameTexture, float, float, float, float)? tuple = DungeonEntryWindow.StandingFrame(sprite);
			if (tuple.HasValue)
			{
				(IGameTexture, float, float, float, float) valueOrDefault = tuple.GetValueOrDefault();
				(IGameTexture Texture, float U, float V, float Cote) tuple2 = FenetreClan.Tete(sprite, valueOrDefault);
				IGameTexture item = tuple2.Texture;
				float item2 = tuple2.U;
				float item3 = tuple2.V;
				float item4 = tuple2.Cote;
				Color color = (eteint ? new Color(255, 196, 202, 214) : TechniqueArt.White);
				SaiyanHud.Draw(renderer, item, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, A(color), item2, item3, item2 + item4 / (float)item.Width, item3 + item4 / (float)item.Height);
			}
		}
	}

	private void Debout(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string? sprite, int centreX, int piedsY, int hauteur)
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
				SaiyanHud.Draw(renderer, item, centreX - num2 / 2, piedsY - num3, num2, num3, A(TechniqueArt.White), item2 / (float)item.Width, item3 / (float)item.Height, (item2 + item4) / (float)item.Width, (item3 + item5) / (float)item.Height);
			}
		}
	}

	private void Jauge(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rail, long actuel, long maximum, Color couleur, string? intitule, bool chiffresDedans)
	{
		TechniqueArt.Fill(renderer, rail, A(new Color(255, 7, 10, 18)));
		TechniqueArt.Outline(renderer, rail, A(TechniqueArt.Line));
		int num = (int)((float)rail.Width * ((float)actuel / (float)maximum));
		if (actuel > 0)
		{
			Rectangle rect = new Rectangle(rail.X + 1, rail.Y + 1, Math.Max(2, num - 2), rail.Height - 2);
			TechniqueArt.FillHorizontal(renderer, rect, A(couleur, 0.75f), A(couleur), A(couleur, 0.9f));
		}
		if (intitule != null)
		{
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(9.5f), intitule, rail.X + _d.L(6f), rail.Y + rail.Height / 2, _d.Em(0.1f, 9.5f), A(TechniqueArt.White, 0.92f));
		}
		if (chiffresDedans)
		{
			string text = Chiffres(actuel, maximum);
			int x = TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(10f), text).X;
			TechniqueArt.Fill(renderer, new Rectangle(rail.Right - _d.L(10f) - x, rail.Y + 1, x + _d.L(9f), rail.Height - 2), A(new Color(150, 6, 8, 14)));
			TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(10f), text, rail.Right - _d.L(6f), rail.Y + rail.Height / 2, A(TechniqueArt.Text));
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

	private void DrawRail(Intersect.Client.Framework.Gwen.Renderer.Base renderer, List<IPartyMember> membres)
	{
		Panel(renderer, _d.R(14f, 58f, 224f, 676f));
		int x = _d.X(30f);
		int width = _d.L(192f);
		float num = 80f;
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10.5f), Strings.Parties.YourParty.ToString().ToUpperInvariant(), x, _d.Y(num), _d.Em(0.14f, 10.5f), A(TechniqueArt.Dim));
		num += 20f;
		if (membres.Count == 0)
		{
			foreach (string item3 in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), Strings.Parties.AloneHint, width, 8))
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), item3, x, _d.Y(num), A(BodyLight));
				num += 16f;
			}
			return;
		}
		(int Avec, int Sans) tuple = Part(membres.Count);
		int item = tuple.Avec;
		int item2 = tuple.Sans;
		Rectangle rect = _d.R(24f, num, 204f, 104f);
		TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, 0.03f));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
		TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(3f), rect.Height), A(TechniqueArt.Gold));
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), Strings.Parties.YourShare.ToString().ToUpperInvariant(), rect.X + _d.L(14f), rect.Y + _d.L(16f), _d.Em(0.12f, 10f), A(TechniqueArt.Soft));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(30f), $"{item} %", rect.X + _d.L(14f), rect.Y + _d.L(50f), A(TechniqueArt.Gold));
		if (item > item2)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11f), Strings.Parties.WithoutBonus.ToString(item2), rect.X + _d.L(14f), rect.Y + _d.L(84f), A(TechniqueArt.Dim));
		}
		num += 116f;
		rect = _d.R(24f, num, 204f, 84f);
		TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, 0.03f));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
		TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(3f), rect.Height), A(TechniqueArt.Line2));
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), Strings.Parties.ShareRange.ToString().ToUpperInvariant(), rect.X + _d.L(14f), rect.Y + _d.L(16f), _d.Em(0.12f, 10f), A(TechniqueArt.Soft));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(17f), Strings.Parties.Tiles.ToString(Options.Instance.Party.SharedXpRange), rect.X + _d.L(14f), rect.Y + _d.L(42f), A(TechniqueArt.Text));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11f), Strings.Parties.RangeHint, rect.X + _d.L(14f), rect.Y + _d.L(66f), A(TechniqueArt.Dim));
		Rectangle rect2 = _d.R(30f, 686f, 192f, 36f);
		HouseArt.Hollow(renderer, _d, rect2, Strings.Parties.Leave.ToString(), IsHot("quitter"), _fade, Rouge);
		if (IsHot("quitter"))
		{
			Etiqueter("quitter", rect2, Strings.Parties.LeaveTip);
		}
		AddHit(rect2, "quitter", PacketSender.SendPartyLeave);
	}

	private void DrawCentre(Intersect.Client.Framework.Gwen.Renderer.Base renderer, List<IPartyMember> membres)
	{
		Panel(renderer, _d.R(248f, 58f, 700f, 676f));
		float num = 264f;
		Rectangle rect = _d.R(num, 74f, 532f, 36f);
		TechniqueArt.Fill(renderer, rect, A(new Color(255, 7, 10, 18)));
		TechniqueArt.Outline(renderer, rect, A(ChampAuClavier() ? TechniqueArt.Ki : TechniqueArt.Line2));
		int num2 = _d.L(14f);
		Lisser("saiyan_amis_icone_groupe.png", new Rectangle(rect.X + _d.L(10f), rect.Y + (rect.Height - num2) / 2, num2, num2), A(TechniqueArt.Soft));
		if (string.IsNullOrEmpty(_invitation.Text))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13.5f), Strings.Parties.InviteHint, rect.X + _d.L(34f), rect.Y + rect.Height / 2, A(TechniqueArt.Dim));
		}
		Rectangle rect2 = _d.R(num + 532f + 8f, 74f, 128f, 36f);
		bool flag = (_invitation.Text?.Trim().Length ?? 0) > 0 && membres.Count < Maximum;
		HouseArt.Cta(renderer, _d, rect2, Strings.Parties.InviteButton.ToString(), flag, flag && IsHot("inviter"), _fade);
		AddHit(rect2, "inviter", Inviter);
		int num3 = _d.Y(141f);
		DungeonHud.Diamond(renderer, _d.X(num) + _d.L(6f), num3, _d.L(10f), A(TechniqueArt.Green));
		string text = Strings.Parties.Members.ToString().ToUpperInvariant();
		int num4 = TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(22f), text, _d.X(num) + _d.L(20f), num3, A(TechniqueArt.Text));
		if (membres.Count > 0)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(13f), Strings.Parties.Count.ToString(membres.Count, Maximum), _d.X(num) + _d.L(20f) + num4 + _d.L(12f), num3 + _d.L(2f), A(TechniqueArt.Dim));
		}
		if (membres.Count == 0)
		{
			int num5 = _d.X(598f);
			float num6 = 320f;
			TechniqueArt.Glow(renderer, num5, _d.Y(num6), _d.L(120f), A(TechniqueArt.Ki, 0.12f));
			int num7 = _d.L(44f);
			Lisser("saiyan_carte_icone_groupe.png", new Rectangle(num5 - num7 / 2, _d.Y(num6) - num7 / 2, num7, num7), A(TechniqueArt.Soft, 0.8f));
			num6 += 52f;
			TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(18f), Strings.Parties.Alone, num5, _d.Y(num6), A(TechniqueArt.Text));
			num6 += 26f;
			foreach (string item in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), Strings.Parties.AloneHint, _d.L(540f), 4))
			{
				TechniqueArt.TextCenter(renderer, TechniqueArt.Body, Pt(12.5f), item, num5, _d.Y(num6), A(BodyLight));
				num6 += 17f;
			}
			num6 += 10f;
			TechniqueArt.TextCenter(renderer, TechniqueArt.Body, Pt(11.5f), Strings.Parties.FreeSlotHint, num5, _d.Y(num6), A(TechniqueArt.Dim));
			return;
		}
		float num8 = 170f;
		bool jeMene = JeMene(membres);
		foreach (IPartyMember membre in membres)
		{
			DrawCarte(renderer, membres, membre, num, num8, jeMene);
			num8 += 106f;
		}
		for (int i = membres.Count; i < Maximum; i++)
		{
			Rectangle rect3 = _d.R(num, num8, 668f, 96f);
			TechniqueArt.Fill(renderer, rect3, A(TechniqueArt.White, 0.015f));
			TechniqueArt.Outline(renderer, rect3, A(TechniqueArt.Line, 0.7f));
			Hexagone(renderer, _d.R(num + 12f, num8 + 21f, 54f, 54f), GreyFrame, null, TechniqueArt.White, vide: true);
			int num9 = _d.L(16f);
			Lisser("saiyan_amis_icone_ajout.png", new Rectangle(rect3.X + _d.L(39f) - num9 / 2, rect3.Y + _d.L(48f) - num9 / 2, num9, num9), A(TechniqueArt.Dim));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(15f), Strings.Parties.FreeSlot, rect3.X + _d.L(80f), rect3.Y + _d.L(36f), A(TechniqueArt.Dim));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), Strings.Parties.FreeSlotHint, rect3.X + _d.L(80f), rect3.Y + _d.L(60f), A(TechniqueArt.Dim, 0.8f));
			num8 += 106f;
		}
	}

	private void DrawCarte(Intersect.Client.Framework.Gwen.Renderer.Base renderer, List<IPartyMember> membres, IPartyMember membre, float x, float y, bool jeMene)
	{
		Rectangle rect = _d.R(x, y, 668f, 96f);
		bool flag = EstMoi(membre);
		bool flag2 = EstChef(membres, membre);
		bool flag3 = HorsDePortee(membre);
		string text = $"membre-{membre.Id}";
		bool flag4 = membre.Id == _choisi;
		bool flag5 = IsHot(text) || IsHot(text + ":message") || IsHot(text + ":exclure");
		if (flag4)
		{
			TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Ki, 0.14f), A(TechniqueArt.Ki, 0.06f), A(TechniqueArt.Ki, 0.02f));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Ki));
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(3f), rect.Height), A(TechniqueArt.Ki));
		}
		else
		{
			TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Green, flag5 ? 0.1f : 0.06f), A(TechniqueArt.Green, 0.025f), A(TechniqueArt.Green, 0.01f));
			TechniqueArt.Outline(renderer, rect, A(flag5 ? TechniqueArt.Line2 : TechniqueArt.Line));
		}
		Portrait(renderer, membre.Sprite, _d.R(x + 12f, y + 21f, 54f, 54f), flag2 ? TechniqueArt.Gold : (flag ? TechniqueArt.Ki : TechniqueArt.Green), flag3);
		int num = rect.X + _d.L(80f);
		int num2 = rect.Y + _d.L(22f);
		int num3 = TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(16.5f), membre.Name, num, num2, A(flag ? TechniqueArt.Gold : TechniqueArt.Text));
		int num4 = num + num3 + _d.L(10f);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), Strings.Parties.LevelShort.ToString(membre.Level), num4, num2 + _d.L(1f), A(TechniqueArt.Soft));
		num4 += TechniqueArt.Measure(renderer, TechniqueArt.Body, Pt(11.5f), Strings.Parties.LevelShort.ToString(membre.Level)).X + _d.L(14f);
		if (flag2)
		{
			string text2 = Strings.Parties.Leader.ToString().ToUpperInvariant();
			int num5 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10f), text2, _d.Em(0.1f, 10f)) + _d.L(12f);
			Puce(renderer, text2, num4 + num5, num2, TechniqueArt.Gold, pleine: true);
			num4 += num5 + _d.L(8f);
		}
		if (flag)
		{
			string text3 = Strings.Parties.You.ToString().ToUpperInvariant();
			int num6 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10f), text3, _d.Em(0.1f, 10f)) + _d.L(12f);
			Puce(renderer, text3, num4 + num6, num2, TechniqueArt.Ki, pleine: false);
		}
		if (flag3)
		{
			TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(11f), Strings.Parties.OutOfRange, rect.Right - _d.L(14f), num2, A(Rouge));
		}
		int num7 = (rect.Right - _d.L(14f) - num - _d.L(10f)) / 2;
		var (actuel, maximum) = Vital(membre, 0);
		var (actuel2, maximum2) = Vital(membre, 1);
		Jauge(renderer, new Rectangle(num, rect.Y + _d.L(44f), num7, _d.L(18f)), actuel, maximum, Vie, Strings.Parties.Vital0.ToString().TrimEnd(' ', ':').ToUpperInvariant(), chiffresDedans: true);
		Jauge(renderer, new Rectangle(num + num7 + _d.L(10f), rect.Y + _d.L(44f), num7, _d.L(18f)), actuel2, maximum2, KiBleu, Strings.Parties.Vital1.ToString().TrimEnd(' ', ':').ToUpperInvariant(), chiffresDedans: true);
		List<(string, string, string, Color, Action)> list = new List<(string, string, string, Color, Action)>();
		string nom = membre.Name;
		Guid id = membre.Id;
		if (!flag)
		{
			list.Add((text + ":message", "saiyan_amis_icone_message.png", Strings.Friends.MessageTip, TechniqueArt.Gold, delegate
			{
				Interface.GameUi?.SetChatboxText("/pm " + nom + " ");
			}));
			if (jeMene)
			{
				list.Add((text + ":exclure", "saiyan_amis_icone_retirer.png", Strings.Parties.Kick.ToString(nom), Rouge, delegate
				{
					PacketSender.SendPartyKick(id);
				}));
			}
		}
		int num8 = rect.Right - _d.L(12f);
		foreach (var item5 in list.AsEnumerable().Reverse())
		{
			string item = item5.Item1;
			string item2 = item5.Item2;
			string item3 = item5.Item3;
			Color item4 = item5.Item4;
			Action action = item5.Item5;
			Rectangle rect2 = new Rectangle(num8 - _d.L(26f), rect.Bottom - _d.L(8f) - _d.L(26f), _d.L(26f), _d.L(26f));
			bool flag6 = IsHot(item);
			if (flag5)
			{
				TechniqueArt.Fill(renderer, rect2, A(new Color(255, 10, 14, 24)));
				TechniqueArt.Outline(renderer, rect2, A(flag6 ? item4 : TechniqueArt.Line2));
			}
			int num9 = _d.L(14f);
			Lisser(item2, new Rectangle(rect2.X + (rect2.Width - num9) / 2, rect2.Y + (rect2.Height - num9) / 2, num9, num9), A(flag6 ? item4 : (flag5 ? TechniqueArt.Soft : TechniqueArt.Dim), flag5 ? 1f : 0.6f));
			if (flag6)
			{
				Etiqueter(item, rect2, item3);
			}
			AddHit(rect2, item, delegate
			{
				_choisi = id;
				action();
			});
			num8 -= _d.L(26f) + _d.L(6f);
		}
		AddHit(rect, text, delegate
		{
			_choisi = id;
		});
	}

	private void DrawFiche(Intersect.Client.Framework.Gwen.Renderer.Base renderer, List<IPartyMember> membres)
	{
		Panel(renderer, _d.R(958f, 58f, 268f, 676f));
		int num = _d.X(974f);
		int num2 = _d.L(236f);
		IPartyMember partyMember = Choisi(membres);
		if (partyMember == null)
		{
			float num3 = 98f;
			{
				foreach (string item5 in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), Strings.Parties.Alone, num2, 6))
				{
					TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), item5, num, _d.Y(num3), A(BodyLight));
					num3 += 16f;
				}
				return;
			}
		}
		_choisi = partyMember.Id;
		bool flag = EstMoi(partyMember);
		bool flag2 = EstChef(membres, partyMember);
		bool flag3 = HorsDePortee(partyMember);
		Color color = (flag2 ? TechniqueArt.Gold : (flag ? TechniqueArt.Ki : TechniqueArt.Green));
		Rectangle rect = _d.R(970f, 70f, 244f, 196f);
		TechniqueArt.FillVertical(renderer, rect, A(new Color(255, 10, 18, 32)), A(new Color(255, 13, 21, 36)));
		TechniqueArt.Glow(renderer, (float)rect.X + (float)rect.Width / 2f, rect.Bottom - _d.L(50f), _d.L(96f), A(color, 0.3f));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
		Debout(renderer, partyMember.Sprite, rect.X + rect.Width / 2, rect.Bottom - _d.L(12f), _d.L(150f));
		string text = (flag2 ? Strings.Parties.Leader : (flag ? Strings.Parties.You : Strings.Parties.Member)).ToString().ToUpperInvariant();
		int num4 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10f), text, _d.Em(0.14f, 10f)) + _d.L(12f);
		Puce(renderer, text, rect.X + _d.L(8f) + num4, rect.Y + _d.L(18f), color, flag2);
		float num5 = 288f;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(23f), TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(23f), partyMember.Name, num2, 1).FirstOrDefault(), num, _d.Y(num5), A(flag ? TechniqueArt.Gold : TechniqueArt.Text));
		num5 += 22f;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), Strings.Parties.LevelShort.ToString(partyMember.Level), num, _d.Y(num5), A(TechniqueArt.Gold));
		num5 += 26f;
		(int, Color, string)[] array = new(int, Color, string)[2]
		{
			(0, Vie, Strings.Parties.Vital0.ToString()),
			(1, KiBleu, Strings.Parties.Vital1.ToString())
		};
		for (int i = 0; i < array.Length; i++)
		{
			var (index, couleur, text2) = array[i];
			var (actuel, maximum) = Vital(partyMember, index);
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10.5f), text2.TrimEnd(' ', ':').ToUpperInvariant(), num, _d.Y(num5), _d.Em(0.14f, 10.5f), A(TechniqueArt.Dim));
			TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(11f), Chiffres(actuel, maximum), num + num2, _d.Y(num5), A(TechniqueArt.Text));
			num5 += 13f;
			Jauge(renderer, new Rectangle(num, _d.Y(num5), num2, _d.L(12f)), actuel, maximum, couleur, null, chiffresDedans: false);
			num5 += 24f;
		}
		if (flag3)
		{
			num5 += 4f;
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(12f), Strings.Parties.OutOfRange, num, _d.Y(num5), A(Rouge));
			num5 += 16f;
			foreach (string item6 in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), Strings.Parties.RangeHint, num2, 2))
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), item6, num, _d.Y(num5), A(TechniqueArt.Dim));
				num5 += 15f;
			}
		}
		string nom = partyMember.Name;
		Guid id = partyMember.Id;
		List<(string, string, Color, Action)> list = new List<(string, string, Color, Action)>();
		if (!flag)
		{
			list.Add(("fiche:message", Strings.Friends.MessageButton.ToString(), null, delegate
			{
				Interface.GameUi?.SetChatboxText("/pm " + nom + " ");
			}));
			if (JeMene(membres))
			{
				list.Add(("fiche:exclure", Strings.Parties.KickButton.ToString(), Rouge, delegate
				{
					PacketSender.SendPartyKick(id);
				}));
			}
		}
		float num6 = 686f;
		foreach (var item7 in list.AsEnumerable().Reverse())
		{
			string item = item7.Item1;
			string item2 = item7.Item2;
			Color item3 = item7.Item3;
			Action item4 = item7.Item4;
			Rectangle rect2 = _d.R(974f, num6, 236f, 36f);
			HouseArt.Hollow(renderer, _d, rect2, item2, IsHot(item), _fade, item3);
			AddHit(rect2, item, item4);
			num6 -= 42f;
		}
	}
}
