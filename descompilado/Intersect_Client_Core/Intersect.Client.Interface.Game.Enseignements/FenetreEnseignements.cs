using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Housing;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.GameObjects;

namespace Intersect.Client.Interface.Game.Enseignements;

internal sealed class FenetreEnseignements : FenetreCharte
{
	private enum Status
	{
		Main,
		Secondary,
		Closed
	}

	private const float Largeur = 1240f;

	private const float Hauteur = 748f;

	private const float BodyTop = 58f;

	private const float BodyHeight = 676f;

	private const float ColumnsLeft = 14f;

	private const float ColumnWidth = 214f;

	private const float ColumnGap = 8f;

	private const float TreeTop = 208f;

	private const float RowPitch = 82f;

	private const float PanelLeft = 904f;

	private const float PanelWidth = 322f;

	private const string DetailsKey = "EnseignementsDetails";

	private static readonly double[] PowerWeights = new double[6] { 0.8, 0.8, 1.0, 1.0, 1.2, 2.4 };

	private const double PowerCap = 15.0;

	private TeachingMaster _main;

	private TeachingMaster _secondary;

	private Dictionary<string, int> _ranks = new Dictionary<string, int>();

	private int _seenVersion = -1;

	private bool _waiting;

	private (TeachingMaster Main, TeachingMaster Secondary, Dictionary<string, int> Ranks)? _sent;

	private string _selected = "peau";

	private string? _toast;

	private long _toastUntil;

	private bool? _details;

	private readonly List<(Rectangle Rect, string Id)> _nodeRects = new List<(Rectangle, string)>();

	private bool _rightButton;

	protected override Rectangle Place => new Rectangle(180, 76, 1240, 748);

	private bool Details
	{
		get
		{
			bool valueOrDefault = _details == true;
			if (!_details.HasValue)
			{
				valueOrDefault = Globals.Database?.LoadPreference("EnseignementsDetails") == "1";
				_details = valueOrDefault;
			}
			return _details.Value;
		}
	}

	private static int Level => Globals.Me?.Level ?? 0;

	private static TeachingMaster ServerMain => Globals.Me?.TeachingMain ?? TeachingMaster.None;

	private static TeachingMaster ServerSecondary => Globals.Me?.TeachingSecondary ?? TeachingMaster.None;

	private int Total => Teachings.PointsAt(Level);

	private int Spent => Teachings.Spent(_ranks);

	private bool Changed
	{
		get
		{
			if (_main == ServerMain && _secondary == ServerSecondary && _ranks.Count<KeyValuePair<string, int>>((KeyValuePair<string, int> pair) => pair.Value > 0) == (Globals.Me?.TeachingRanks.Count<KeyValuePair<string, int>>((KeyValuePair<string, int> pair) => pair.Value > 0) ?? 0))
			{
				return _ranks.Any<KeyValuePair<string, int>>((KeyValuePair<string, int> pair) => ServerRank(pair.Key) != pair.Value);
			}
			return true;
		}
	}

	private static bool IsAdmin
	{
		get
		{
			Player? me = Globals.Me;
			if (me == null)
			{
				return false;
			}
			return me.AccessLevel == Access.Admin;
		}
	}

	public FenetreEnseignements(Canvas gameCanvas)
		: base(gameCanvas, "FenetreEnseignements")
	{
	}

	public override void Show()
	{
		Reset();
		base.Show();
	}

	public void Toggle()
	{
		if (base.IsHidden)
		{
			Show();
		}
		else
		{
			Hide();
		}
	}

	public void Update()
	{
		if (base.IsHidden)
		{
			return;
		}
		Placer();
		Player me = Globals.Me;
		if (me == null || me.TeachingsVersion == _seenVersion)
		{
			return;
		}
		(TeachingMaster, TeachingMaster, Dictionary<string, int>)? tuple = ((TeachingMaster, TeachingMaster, Dictionary<string, int>)?)(_waiting ? ((ValueType)_sent) : ((ValueType)((TeachingMaster, TeachingMaster, Dictionary<string, int>)?)null));
		Reset();
		if (tuple.HasValue)
		{
			bool flag = me.TeachingMain == tuple.Value.Item1 && me.TeachingSecondary == tuple.Value.Item2 && me.TeachingRanks.Count<KeyValuePair<string, int>>((KeyValuePair<string, int> pair) => pair.Value > 0) == tuple.Value.Item3.Count && tuple.Value.Item3.All((KeyValuePair<string, int> pair) => Teachings.Rank(me.TeachingRanks, pair.Key) == pair.Value);
			Say(flag ? Strings.TeachingsScreen.Validated.ToString() : Strings.TeachingsScreen.RefusedByServer.ToString());
		}
	}

	internal override bool HandleKey(Keys modifier, Keys key)
	{
		if (modifier == Keys.None && key == Keys.D)
		{
			ToggleDetails();
			return true;
		}
		return base.HandleKey(modifier, key);
	}

	private void Reset()
	{
		_waiting = false;
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		_seenVersion = me.TeachingsVersion;
		_main = me.TeachingMain;
		_secondary = me.TeachingSecondary;
		_ranks = me.TeachingRanks.Where<KeyValuePair<string, int>>((KeyValuePair<string, int> pair) => pair.Value > 0).ToDictionary((KeyValuePair<string, int> pair) => pair.Key, (KeyValuePair<string, int> pair) => pair.Value);
		if (_main != TeachingMaster.None && Teachings.Find(_selected)?.Master != _main)
		{
			_selected = Teachings.All.First((TeachingNode node) => node.Master == _main).Id;
		}
	}

	private void ToggleDetails()
	{
		_details = !Details;
		Globals.Database?.SavePreference("EnseignementsDetails", Details ? "1" : "0");
		Audio.AddGameSound("menu_click.wav", loop: false);
	}

	private static int ServerRank(string id)
	{
		return Teachings.Rank(Globals.Me?.TeachingRanks, id);
	}

	private Status StatusOf(TeachingMaster master)
	{
		if (master != _main)
		{
			if (master != _secondary)
			{
				return Status.Closed;
			}
			return Status.Secondary;
		}
		return Status.Main;
	}

	private bool CanChooseMain(TeachingMaster master)
	{
		if (IsAdmin && _main == TeachingMaster.None)
		{
			return Level >= 20;
		}
		return false;
	}

	private bool CanChooseSecondary(TeachingMaster master)
	{
		if (IsAdmin && _main != TeachingMaster.None && master != _main && _secondary == TeachingMaster.None && ServerSecondary == TeachingMaster.None)
		{
			return Level >= 40;
		}
		return false;
	}

	private TeachingTrialInfo? NextTrial(TeachingMaster master)
	{
		return TeachingTrials.ForOath(master, _main != TeachingMaster.None);
	}

	private bool PassedNextTrial(TeachingMaster master)
	{
		return Globals.Me?.HasPassedTrial(NextTrial(master)) ?? false;
	}

	private string ClosedLabel(TeachingMaster master)
	{
		if (_main == TeachingMaster.None)
		{
			if (Level >= 20)
			{
				if (!PassedNextTrial(master))
				{
					return Strings.TeachingsScreen.ChipTrialToTry.ToString();
				}
				return Strings.TeachingsScreen.ChipTrialPassed.ToString();
			}
			return Strings.TeachingsScreen.ChipLevel.ToString(20);
		}
		if (_secondary == TeachingMaster.None)
		{
			if (Level >= 40)
			{
				if (!PassedNextTrial(master))
				{
					return Strings.TeachingsScreen.ChipTrialToTry.ToString();
				}
				return Strings.TeachingsScreen.ChipTrialPassed.ToString();
			}
			return Strings.TeachingsScreen.ChipSecondaryLevel.ToString(40);
		}
		return Strings.TeachingsScreen.ChipClosed.ToString();
	}

	private string? WhyNotAdd(TeachingNode node)
	{
		TeachingTexts.MasterText masterText = TeachingTexts.Of(node.Master);
		Status status = StatusOf(node.Master);
		if (status == Status.Closed)
		{
			if (_main == TeachingMaster.None)
			{
				string text;
				if (Level >= 20)
				{
					text = TeachingTrialSteps.ToOpenPath(Globals.Me, node.Master);
					if (text == null)
					{
						return Strings.TeachingsScreen.WhyPassTrial.ToString();
					}
				}
				else
				{
					text = Strings.TeachingsScreen.WhyMastersLevel.ToString(20);
				}
				return text;
			}
			if (_secondary == TeachingMaster.None)
			{
				if (Level >= 40)
				{
					if (!PassedNextTrial(node.Master))
					{
						return Strings.TeachingsScreen.WhySecondaryTrial.ToString(40, masterText.Name);
					}
					return Strings.TeachingsScreen.SwearNextSecondary.ToString(masterText.Name);
				}
				return Strings.TeachingsScreen.WhySecondaryLevel.ToString(40);
			}
			return Strings.TeachingsScreen.WhyPathClosed.ToString();
		}
		int num = Teachings.Rank(_ranks, node.Id);
		if (num >= node.MaxRank)
		{
			return Strings.TeachingsScreen.WhyMaxRank.ToString();
		}
		if (status == Status.Secondary && node.Tier > 1)
		{
			return Strings.TeachingsScreen.WhyMainOnly.ToString();
		}
		if (status == Status.Secondary && Teachings.SpentIn(_ranks, node.Master) + node.Cost > 15)
		{
			return Strings.TeachingsScreen.WhySecondaryCap.ToString(15);
		}
		int num2 = Teachings.TierRequirement[node.Tier];
		int num3 = Teachings.SpentIn(_ranks, node.Master, node.Tier);
		if (num3 < num2)
		{
			if (node.Tier != 5)
			{
				return Strings.TeachingsScreen.WhyTierLocked.ToString(node.Tier + 1, num2, masterText.Name, num3);
			}
			return Strings.TeachingsScreen.WhyUltimateLocked.ToString(num2, masterText.Name, num3);
		}
		int num4 = Total - Spent;
		if (num4 < node.Cost)
		{
			if (node.Cost <= 1)
			{
				return Strings.TeachingsScreen.WhyNoPoints.ToString();
			}
			return Strings.TeachingsScreen.WhyUltimateCost.ToString(node.Cost, num4);
		}
		return null;
	}

	private string? WhyNotRemove(TeachingNode node)
	{
		int num = Teachings.Rank(_ranks, node.Id);
		if (num == 0)
		{
			return Strings.TeachingsScreen.WhyNothingToRemove.ToString();
		}
		if (num <= ServerRank(node.Id))
		{
			return Strings.TeachingsScreen.WhyValidatedRank.ToString();
		}
		int tier;
		for (tier = node.Tier + 1; tier <= 5; tier++)
		{
			if (Teachings.All.Any((TeachingNode other) => other.Master == node.Master && other.Tier == tier && Teachings.Rank(_ranks, other.Id) > 0) && Teachings.SpentIn(_ranks, node.Master, tier) - node.Cost < Teachings.TierRequirement[tier])
			{
				return Strings.TeachingsScreen.WhyUpperTiers.ToString();
			}
		}
		return null;
	}

	private void Add(string id)
	{
		_selected = id;
		if (_waiting)
		{
			return;
		}
		TeachingNode teachingNode = Teachings.Find(id);
		if ((object)teachingNode != null)
		{
			string text = WhyNotAdd(teachingNode);
			if (text != null)
			{
				Say(text);
			}
			else
			{
				_ranks[id] = Teachings.Rank(_ranks, id) + 1;
			}
		}
	}

	private void Remove(string id)
	{
		_selected = id;
		if (_waiting)
		{
			return;
		}
		TeachingNode teachingNode = Teachings.Find(id);
		if ((object)teachingNode == null)
		{
			return;
		}
		string text = WhyNotRemove(teachingNode);
		if (text != null)
		{
			Say(text);
			return;
		}
		int num = Teachings.Rank(_ranks, id) - 1;
		if (num <= 0)
		{
			_ranks.Remove(id);
		}
		else
		{
			_ranks[id] = num;
		}
	}

	private void Choose(TeachingMaster master)
	{
		if (_waiting)
		{
			return;
		}
		if (CanChooseMain(master))
		{
			_main = master;
			_selected = Teachings.All.First((TeachingNode node) => node.Master == master).Id;
			Say(Strings.TeachingsScreen.ChosenMain.ToString(TeachingTexts.Of(master).Name));
		}
		else if (CanChooseSecondary(master))
		{
			_secondary = master;
			_selected = Teachings.All.First((TeachingNode node) => node.Master == master).Id;
			Say(Strings.TeachingsScreen.ChosenSecondary.ToString(TeachingTexts.Of(master).Name));
		}
	}

	private void Validate()
	{
		if (!_waiting && Changed)
		{
			_waiting = true;
			_sent = (_main, _secondary, new Dictionary<string, int>(_ranks));
			PacketSender.SendSaveTeachings((int)_main, (int)_secondary, new Dictionary<string, int>(_ranks));
		}
	}

	private void Say(string text)
	{
		_toast = text;
		_toastUntil = Timing.Global.MillisecondsUtc + 2800;
	}

	private double Power()
	{
		double num = 0.0;
		foreach (KeyValuePair<string, int> rank in _ranks)
		{
			rank.Deconstruct(out var key, out var value);
			string id = key;
			int num2 = value;
			TeachingNode teachingNode = Teachings.Find(id);
			if ((object)teachingNode != null && StatusOf(teachingNode.Master) != Status.Closed)
			{
				num += PowerWeights[teachingNode.Tier] * (double)num2 / (double)teachingNode.MaxRank;
			}
		}
		return num;
	}

	protected override void Dessiner(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		DessinerCadre(renderer);
		int num = DessinerBarre(renderer, Strings.TeachingsScreen.Title);
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		int num2 = _d.Y(23f);
		int num3 = Total - Spent;
		int num4 = num - _d.L(18f);
		(string, string, Color)[] array = new(string, string, Color)[2]
		{
			(Strings.TeachingsScreen.GaugePlaced.ToString(), $"{Spent} / {Total}", TechniqueArt.Text),
			(Strings.TeachingsScreen.GaugeToPlace.ToString(), $"{num3}", (num3 > 0) ? TechniqueArt.Gold : TechniqueArt.Soft)
		};
		for (int i = 0; i < array.Length; i++)
		{
			(string, string, Color) tuple = array[i];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			Color item3 = tuple.Item3;
			int num5 = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(16f), item2, num4, num2, A(item3));
			num4 -= num5 + _d.L(8f);
			int num6 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10f), item.ToUpperInvariant(), _d.Em(0.16f, 10f));
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), item.ToUpperInvariant(), num4 - num6, num2 + _d.L(1f), _d.Em(0.16f, 10f), A(TechniqueArt.Dim));
			num4 -= num6 + _d.L(22f);
		}
		_nodeRects.Clear();
		string text = null;
		for (int j = 0; j < TeachingTexts.Masters.Length; j++)
		{
			string text2 = DrawColumn(renderer, TeachingTexts.Masters[j], 14f + (float)j * 222f);
			if (text == null)
			{
				text = text2;
			}
		}
		DrawSheet(renderer, Teachings.Find(text ?? _selected) ?? Teachings.All[0]);
		if (_toast != null && now < _toastUntil)
		{
			int num7 = Math.Min(_d.L(860f), TechniqueArt.Measure(renderer, TechniqueArt.Body, Pt(13.5f), _toast).X + _d.L(40f));
			Rectangle rect = new Rectangle(_d.X(454f) - num7 / 2, _d.Y(682f), num7, _d.L(36f));
			TechniqueArt.Fill(renderer, rect, A(new Color(245, 12, 17, 28)));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line2));
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(3f), rect.Height), A(TechniqueArt.Gold));
			TechniqueArt.TextCenter(renderer, TechniqueArt.Body, Pt(13.5f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(13.5f), _toast, num7 - _d.L(20f), 1).FirstOrDefault(), rect.X + rect.Width / 2, rect.Y + rect.Height / 2, A(TechniqueArt.Text));
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

	private void Embleme(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle outer, int glyph, Color color, float iconRatio)
	{
		TechniqueArt.Image(renderer, "tech_hexa_bord.png", outer, A(TechniqueArt.Mix(color, TechniqueArt.White, 0.95f)));
		int num = Math.Max(1, (int)MathF.Round((float)outer.Width * 2f / 62f));
		Rectangle rect = new Rectangle(outer.X + num, outer.Y + num, outer.Width - num * 2, outer.Height - num * 2);
		TechniqueArt.Image(renderer, "tech_hexa.png", rect, A(new Color(255, 8, 11, 19)));
		TechniqueArt.Image(renderer, "tech_hexa_radial.png", rect, A(TechniqueArt.Mix(color, new Color(255, 22, 28, 44), 0.3f)));
		int num2 = (int)MathF.Round((float)rect.Width * iconRatio);
		Rectangle rect2 = new Rectangle(rect.X + (rect.Width - num2) / 2, rect.Y + (rect.Height - num2) / 2, num2, num2);
		TechniqueArt.Glow(renderer, (float)rect2.X + (float)num2 / 2f, (float)rect2.Y + (float)num2 / 2f, (float)num2 * 0.75f, A(color, 0.45f));
		TechniqueArt.Cell(renderer, "tech_familles.png", glyph, 40, rect2, A(color));
	}

	private string? DrawColumn(Intersect.Client.Framework.Gwen.Renderer.Base renderer, TeachingTexts.MasterText master, float x)
	{
		Status status = StatusOf(master.Master);
		Color color = master.Color;
		bool flag = status == Status.Closed;
		Rectangle rect = _d.R(x, 58f, 214f, 676f);
		Panel(renderer, rect);
		if (!flag)
		{
			TechniqueArt.FillVertical(renderer, _d.R(x + 2f, 60f, 210f, 160f), A(color, 0.1f), A(color, 0f));
		}
		float num = x + 107f;
		float num2 = (flag ? 0.45f : 1f);
		string text = (status switch
		{
			Status.Main => Strings.TeachingsScreen.ChipMain.ToString(), 
			Status.Secondary => Strings.TeachingsScreen.ChipSecondary.ToString(15), 
			_ => ClosedLabel(master.Master), 
		}).ToUpperInvariant();
		int num3 = Math.Min(_d.L(198f), TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(11f), text, _d.Em(0.1f, 11f)) + _d.L(18f));
		Rectangle rect2 = new Rectangle(_d.X(num) - num3 / 2, _d.Y(68f), num3, _d.L(20f));
		TechniqueArt.Patch(renderer, "tech_biais5.png", rect2, A(status switch
		{
			Status.Secondary => TechniqueArt.Mix(color, new Color(255, 23, 33, 54), 0.35f), 
			Status.Main => TechniqueArt.Gold, 
			_ => new Color(255, 21, 28, 43), 
		}), 6);
		TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(11f), text, rect2.X + rect2.Width / 2, rect2.Y + rect2.Height / 2, _d.Em(0.1f, 11f), A((status == Status.Main) ? HouseArt.Dark : TechniqueArt.Text));
		string text2 = master.Role.ToUpperInvariant();
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(22f), text2);
		TechniqueArt.Styled(renderer, TechniqueArt.Display, Pt(22f), text2, _d.X(num) - point.X / 2, _d.Y(94f), TechniqueArt.Stops.Solid(TechniqueArt.Mix(color, TechniqueArt.White, 0.85f)), _fade * num2);
		TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(11f), master.Name.ToUpperInvariant(), _d.X(num), _d.Y(130f), _d.Em(0.16f, 11f), A(TechniqueArt.Text, num2));
		TechniqueArt.TextCenter(renderer, TechniqueArt.Body, Pt(11.5f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), master.Mission, _d.L(198f), 1).FirstOrDefault(), _d.X(num), _d.Y(147f), A(TechniqueArt.Soft, num2));
		bool flag2 = flag && CanChooseMain(master.Master);
		bool flag3 = flag && CanChooseSecondary(master.Master);
		if (flag2 || flag3)
		{
			string cle = $"choix-{master.Master}";
			Rectangle rect3 = _d.R(x + 18f, 164f, 178f, 30f);
			HouseArt.Hollow(renderer, _d, rect3, flag2 ? Strings.TeachingsScreen.ChooseMain.ToString() : Strings.TeachingsScreen.ChooseSecondary.ToString(), IsHot(cle), _fade, color);
			AddHit(rect3, cle, delegate
			{
				Choose(master.Master);
			});
		}
		else
		{
			if (flag)
			{
				TeachingTrialInfo teachingTrialInfo = NextTrial(master.Master);
				if ((object)teachingTrialInfo != null && Level >= teachingTrialInfo.Level && (_main == TeachingMaster.None || _secondary == TeachingMaster.None))
				{
					bool flag4 = PassedNextTrial(master.Master);
					TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(12.5f), TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(12.5f), Strings.TeachingsScreen.TrialName.ToString(Traduction.Contenu(teachingTrialInfo.Name)), _d.L(198f), 1).FirstOrDefault(), _d.X(num), _d.Y(170f), A(flag4 ? TechniqueArt.Gold : TechniqueArt.Text, num2 + (1f - num2) * 0.6f));
					TechniqueArt.TextCenter(renderer, TechniqueArt.Body, Pt(11f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11f), flag4 ? Strings.TeachingsScreen.SwearOath.ToString(master.Name) : Strings.TeachingsScreen.TrialAt.ToString(master.Name), _d.L(198f), 1).FirstOrDefault(), _d.X(num), _d.Y(187f), A(flag4 ? TechniqueArt.Green : TechniqueArt.Soft, num2 + (1f - num2) * 0.6f));
					goto IL_07ef;
				}
			}
			if (!flag)
			{
				int num4 = ((status == Status.Secondary) ? 15 : 55);
				int num5 = Teachings.SpentIn(_ranks, master.Master);
				Rectangle rect4 = _d.R(num - 80f, 176f, 110f, 6f);
				TechniqueArt.Fill(renderer, rect4, A(new Color(255, 10, 14, 24)));
				TechniqueArt.Outline(renderer, rect4, A(TechniqueArt.Line));
				Rectangle rect5 = new Rectangle(rect4.X, rect4.Y, (int)((float)rect4.Width * Math.Min(1f, (float)num5 / (float)num4)), rect4.Height);
				if (rect5.Width > 0)
				{
					TechniqueArt.FillHorizontal(renderer, rect5, A(TechniqueArt.Mix(color, TechniqueArt.Black, 0.6f)), A(color), A(TechniqueArt.Mix(color, TechniqueArt.White, 0.7f)));
				}
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(14f), $"{num5} / {num4}", rect4.Right + _d.L(8f), rect4.Y + rect4.Height / 2, A(TechniqueArt.Text));
			}
		}
		goto IL_07ef;
		IL_07ef:
		return DrawTree(renderer, master, x, status, num2);
	}

	private string? DrawTree(Intersect.Client.Framework.Gwen.Renderer.Base renderer, TeachingTexts.MasterText master, float x, Status status, float dim)
	{
		Color color = master.Color;
		float num = x + 107f;
		List<TeachingNode> source = Teachings.All.Where((TeachingNode node) => node.Master == master.Master).ToList();
		int num2 = Teachings.SpentIn(_ranks, master.Master, 5);
		float num3 = 230f;
		float num4 = 645f;
		TechniqueArt.Fill(renderer, _d.R(num - 1f, num3, 2f, num4 - num3), A(TechniqueArt.Line, dim));
		double num5 = 5.0;
		for (int num6 = 0; num6 < 5; num6++)
		{
			if (num2 < Teachings.TierRequirement[num6 + 1])
			{
				num5 = (double)num6 + (double)(num2 - Teachings.TierRequirement[num6]) / (double)(Teachings.TierRequirement[num6 + 1] - Teachings.TierRequirement[num6]);
				break;
			}
		}
		if (num2 > 0 && status != Status.Closed)
		{
			float num7 = (float)Math.Min(num4, (double)num3 + num5 * 82.0);
			Rectangle rect = _d.R(num - 1f, num3, 2f, num7 - num3);
			TechniqueArt.Fill(renderer, rect, A(color));
			TechniqueArt.GlowRect(renderer, new Rectangle(rect.X - _d.L(4f), rect.Y, rect.Width + _d.L(8f), rect.Height), A(color, 0.25f));
		}
		string result = null;
		int tier;
		for (tier = 0; tier <= 5; tier++)
		{
			float num8 = 208f + (float)tier * 82f;
			bool flag = status != Status.Closed && Teachings.SpentIn(_ranks, master.Master, tier) >= Teachings.TierRequirement[tier] && (status != Status.Secondary || tier <= 1);
			if (tier > 0)
			{
				TechniqueArt.Write(renderer, TechniqueArt.Heavy, Pt(13f), $"{Teachings.TierRequirement[tier]}", _d.X(x + 8f), _d.Y(num8 + 2f), A(flag ? TechniqueArt.Mix(color, TechniqueArt.Soft, 0.7f) : TechniqueArt.Dim, dim));
				TechniqueArt.Write(renderer, TechniqueArt.Label, Pt(10f), Strings.TeachingsScreen.TierPoints.ToString(), _d.X(x + 8f), _d.Y(num8 + 18f), A(TechniqueArt.Dim, dim));
			}
			List<TeachingNode> list = source.Where((TeachingNode node) => node.Tier == tier).ToList();
			for (int num9 = 0; num9 < list.Count; num9++)
			{
				float centerX = ((list.Count == 1) ? num : (num + (float)((num9 == 0) ? (-44) : 44)));
				if (DrawNode(renderer, list[num9], color, centerX, num8, status, dim))
				{
					result = list[num9].Id;
				}
			}
			if (status == Status.Secondary && tier > 1)
			{
				float num10 = ((list.Count == 1) ? (num + 42f) : num);
				float num11 = 14f;
				float y = num8 + 24f - num11 / 2f;
				TechniqueArt.Image(renderer, "inv_cadenas.png", _d.R(num10 - num11 / 2f, y, num11, num11), A(TechniqueArt.Soft, 0.8f * dim));
			}
		}
		return result;
	}

	private bool DrawNode(Intersect.Client.Framework.Gwen.Renderer.Base renderer, TeachingNode node, Color color, float centerX, float rowTop, Status status, float dim)
	{
		TeachingTexts.NodeText nodeText = TeachingTexts.Of(node.Id);
		bool flag = node.Tier == 5;
		float num = (flag ? 54f : 42f);
		int num2 = Teachings.Rank(_ranks, node.Id);
		bool flag2 = status != Status.Closed && Teachings.SpentIn(_ranks, node.Master, node.Tier) >= Teachings.TierRequirement[node.Tier] && (status != Status.Secondary || node.Tier <= 1);
		bool flag3 = flag2 && num2 == 0 && WhyNotAdd(node) == null;
		string cle = "noeud-" + node.Id;
		bool flag4 = IsHot(cle);
		bool flag5 = _selected == node.Id;
		Rectangle rect = _d.R(centerX - num / 2f, rowTop + (float)((!flag) ? 4 : 0), num, num);
		if (num2 > 0 || flag5 || flag4)
		{
			TechniqueArt.Glow(renderer, (float)rect.X + (float)rect.Width / 2f, (float)rect.Y + (float)rect.Height / 2f, (float)rect.Width * ((num2 >= node.MaxRank) ? 0.95f : 0.7f), A(color, ((num2 >= node.MaxRank) ? 0.45f : 0.25f) * dim));
		}
		Color color2 = ((num2 > 0) ? color : (flag3 ? TechniqueArt.Mix(color, TechniqueArt.Line2, 0.55f) : TechniqueArt.Line));
		if (flag5 || flag4)
		{
			color2 = TechniqueArt.Mix(color2, TechniqueArt.White, 0.6f);
		}
		TechniqueArt.Image(renderer, "tech_hexa_bord.png", rect, A(color2, dim));
		int num3 = Math.Max(1, (int)MathF.Round((float)rect.Width * 2.5f / 52f));
		Rectangle rect2 = new Rectangle(rect.X + num3, rect.Y + num3, rect.Width - num3 * 2, rect.Height - num3 * 2);
		TechniqueArt.Image(renderer, "tech_hexa.png", rect2, A(new Color(255, 8, 11, 19), dim));
		if (flag2 || num2 > 0)
		{
			TechniqueArt.Image(renderer, "tech_hexa_radial.png", rect2, A(TechniqueArt.Mix(color, new Color(255, 22, 28, 44), (num2 > 0) ? 0.45f : 0.2f), dim));
		}
		int num4 = (int)MathF.Round((float)rect2.Width * 0.56f);
		Rectangle rect3 = new Rectangle(rect2.X + (rect2.Width - num4) / 2, rect2.Y + (rect2.Height - num4) / 2, num4, num4);
		Color color3 = ((num2 > 0) ? TechniqueArt.Mix(color, TechniqueArt.White, 0.6f) : (flag3 ? color : new Color(255, 75, 85, 107)));
		TechniqueArt.Cell(renderer, "tech_familles.png", Glyph(nodeText.Icon), 40, rect3, A(color3, dim));
		int num5 = (flag ? 22 : 8);
		int num6 = 3;
		int num7 = node.MaxRank * num5 + (node.MaxRank - 1) * num6;
		float num8 = rowTop + (flag ? (num + 3f) : (num + 8f));
		for (int i = 0; i < node.MaxRank; i++)
		{
			Rectangle rect4 = _d.R(centerX - (float)num7 / 2f + (float)(i * (num5 + num6)), num8, num5, 4f);
			bool flag6 = i < ServerRank(node.Id);
			if (i < num2)
			{
				TechniqueArt.Fill(renderer, rect4, A(flag6 ? color : TechniqueArt.Mix(color, TechniqueArt.White, 0.5f), dim));
				continue;
			}
			TechniqueArt.Fill(renderer, rect4, A(new Color(255, 10, 14, 24), dim));
			TechniqueArt.Outline(renderer, rect4, A(TechniqueArt.Line2, dim));
		}
		Color color4 = ((num2 > 0 || flag5) ? TechniqueArt.Text : (flag3 ? TechniqueArt.Soft : TechniqueArt.Dim));
		List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(11f), nodeText.Name, _d.L(flag ? 130 : 84), 2);
		int num9 = _d.Y(num8 + 12f);
		foreach (string item in list)
		{
			TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(11f), item, _d.X(centerX), num9, A(color4, dim));
			num9 += _d.L(12f);
		}
		Rectangle rectangle = _d.R(centerX - (float)(flag ? 66 : 42), rowTop, flag ? 132 : 84, 80f);
		_nodeRects.Add((rectangle, node.Id));
		AddHit(rectangle, cle, delegate
		{
			Add(node.Id);
		});
		return flag4;
	}

	private void DrawSheet(Intersect.Client.Framework.Gwen.Renderer.Base renderer, TeachingNode node)
	{
		TeachingTexts.MasterText masterText = TeachingTexts.Of(node.Master);
		TeachingTexts.NodeText nodeText = TeachingTexts.Of(node.Id);
		Color color = masterText.Color;
		Rectangle rect = _d.R(904f, 58f, 322f, 676f);
		Panel(renderer, rect);
		AddHit(rect, "fiche", null);
		float num = 920f;
		float num2 = 290f;
		int num3 = _d.Y(79f);
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), Strings.TeachingsScreen.SheetHeading.ToString().ToUpperInvariant(), _d.X(num), num3, _d.Em(0.16f, 12f), A(TechniqueArt.Soft));
		string text = (Details ? Strings.Techniques.Simple : Strings.Techniques.Details).ToString().ToUpperInvariant();
		int num4 = _d.L(10f) + TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(12f), text, _d.Em(0.08f, 12f)) + _d.L(8f) + LargeurTouche(renderer, "D") + _d.L(10f);
		Rectangle rect2 = new Rectangle(_d.X(num + num2) - num4, num3 - _d.L(13f), num4, _d.L(26f));
		bool flag = IsHot("details");
		TechniqueArt.Patch(renderer, "tech_chanfrein8.png", rect2, A(new Color(204, 12, 17, 28)), 10);
		TechniqueArt.Patch(renderer, "tech_chanfrein8_bord.png", rect2, A(flag ? TechniqueArt.Gold : TechniqueArt.Line2), 10);
		int num5 = TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), text, rect2.X + _d.L(10f), num3, _d.Em(0.08f, 12f), A(flag ? TechniqueArt.Text : TechniqueArt.Soft));
		Touche(renderer, "D", rect2.X + _d.L(10f) + num5 + _d.L(8f), num3);
		AddHit(rect2, "details", ToggleDetails);
		Embleme(renderer, _d.R(num, 98f, 56f, 56f), Glyph(nodeText.Icon), color, 0.58f);
		List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(20f), nodeText.Name, _d.L(num2 - 68f), 2);
		int num6 = _d.Y(58f + (float)((list.Count > 1) ? 50 : 60));
		foreach (string item in list)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(20f), item, _d.X(num + 68f), num6, A(TechniqueArt.Text));
			num6 += _d.L(21f);
		}
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(11f), TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(11f), (masterText.Name + " · " + masterText.Role).ToUpperInvariant(), _d.L(num2 - 68f), 1).FirstOrDefault(), _d.X(num + 68f), num6 + _d.L(2f), _d.Em(0.12f, 11f), A(color));
		int num7 = Teachings.Rank(_ranks, node.Id);
		float num8 = 172f;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(15f), Strings.TeachingsScreen.Rank.ToString(num7, node.MaxRank), _d.X(num), _d.Y(num8), A(TechniqueArt.Text));
		float num9 = ((node.MaxRank == 1) ? 54f : 16f);
		for (int i = 0; i < node.MaxRank; i++)
		{
			Rectangle rect3 = _d.R(num + num2 - (float)node.MaxRank * (num9 + 4f) + 4f + (float)i * (num9 + 4f), num8 - 3f, num9, 7f);
			if (i < num7)
			{
				TechniqueArt.Fill(renderer, rect3, A((i < ServerRank(node.Id)) ? color : TechniqueArt.Mix(color, TechniqueArt.White, 0.5f)));
				continue;
			}
			TechniqueArt.Fill(renderer, rect3, A(new Color(255, 10, 14, 24)));
			TechniqueArt.Outline(renderer, rect3, A(TechniqueArt.Line2));
		}
		num8 += 18f;
		if (node.Tier == 5)
		{
			num8 = Effect(renderer, num, num8, num2, Strings.TeachingsScreen.EffectLabel.ToString(), TeachingTexts.EffectAt(node, 1), strong: true);
		}
		else
		{
			num8 = Effect(renderer, num, num8, num2, Strings.TeachingsScreen.CurrentLabel.ToString(), (num7 > 0) ? TeachingTexts.EffectAt(node, num7) : Strings.TeachingsScreen.NotLearned.ToString(), num7 > 0);
			num8 = Effect(renderer, num, num8, num2, (num7 > 0) ? Strings.TeachingsScreen.NextRankLabel.ToString() : Strings.TeachingsScreen.FirstRankLabel.ToString(), (num7 < node.MaxRank) ? TeachingTexts.EffectAt(node, num7 + 1) : Strings.TeachingsScreen.MaxRankReached.ToString(), num7 < node.MaxRank);
		}
		string text2 = WhyNotAdd(node);
		Status status = StatusOf(node.Master);
		bool flag2 = status != Status.Closed && Teachings.SpentIn(_ranks, node.Master, node.Tier) >= Teachings.TierRequirement[node.Tier] && (status != Status.Secondary || node.Tier <= 1);
		string text3 = ((!flag2) ? (text2 ?? Strings.TeachingsScreen.ConditionClosed.ToString()) : ((node.Tier == 5) ? Strings.TeachingsScreen.ConditionUltimate.ToString(masterText.Name, node.Cost) : ((status == Status.Secondary) ? Strings.TeachingsScreen.ConditionSecondary.ToString(Teachings.SpentIn(_ranks, node.Master), 15) : Strings.TeachingsScreen.ConditionOpen.ToString())));
		num8 += 6f;
		string text4 = (flag2 ? Strings.TeachingsScreen.SheetOpen.ToString() : Strings.TeachingsScreen.SheetClosed.ToString());
		int num10 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(11f), text4, _d.Em(0.12f, 11f)) + _d.L(12f);
		Rectangle rect4 = new Rectangle(_d.X(num), _d.Y(num8), num10, _d.L(17f));
		TechniqueArt.Patch(renderer, "tech_biais5.png", rect4, A(flag2 ? TechniqueArt.Green : TechniqueArt.Red), 6);
		TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(11f), text4, rect4.X + rect4.Width / 2, rect4.Y + rect4.Height / 2, _d.Em(0.12f, 11f), A(HouseArt.Dark));
		List<string> list2 = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), text3, _d.L(num2) - num10 - _d.L(8f), 3);
		int num11 = rect4.Y + rect4.Height / 2;
		foreach (string item2 in list2)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), item2, rect4.Right + _d.L(8f), num11, A(TechniqueArt.Soft));
			num11 += _d.L(15f);
		}
		num8 += (float)Math.Max(24, list2.Count * 15 + 6);
		if (!node.Implemented)
		{
			foreach (string item3 in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), Strings.TeachingsScreen.GroupEffectPending.ToString(), _d.L(num2), 2))
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), item3, _d.X(num), _d.Y(num8 + 5f), A(TechniqueArt.Gold));
				num8 += 14f;
			}
			num8 += 5f;
		}
		num8 += 4f;
		float num12 = (num2 - 8f) / 2f;
		Rectangle rect5 = _d.R(num, num8, num12, 32f);
		Rectangle rect6 = _d.R(num + num12 + 8f, num8, num12, 32f);
		bool flag3 = WhyNotRemove(node) == null && !_waiting;
		bool flag4 = text2 == null && !_waiting;
		HouseArt.Hollow(renderer, _d, rect5, Strings.TeachingsScreen.RemoveRank.ToString(), flag3 && IsHot("retirer"), _fade * (flag3 ? 1f : 0.45f));
		HouseArt.Hollow(renderer, _d, rect6, Strings.TeachingsScreen.Learn.ToString(), flag4 && IsHot("apprendre"), _fade * (flag4 ? 1f : 0.45f), color);
		AddHit(rect5, "retirer", delegate
		{
			Remove(node.Id);
		});
		AddHit(rect6, "apprendre", delegate
		{
			Add(node.Id);
		});
		num8 += 42f;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), Strings.TeachingsScreen.ClickHint.ToString(), _d.L(num2), 1).FirstOrDefault(), _d.X(num), _d.Y(num8), A(TechniqueArt.Dim));
		if (Details)
		{
			num8 += 18f;
			TechniqueArt.Fill(renderer, _d.R(num, num8, num2, 1f), A(TechniqueArt.White, 0.07f));
			num8 += 14f;
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(11f), Strings.TeachingsScreen.YourRole.ToString(), _d.X(num), _d.Y(num8), _d.Em(0.2f, 11f), A(TechniqueArt.Dim));
			num8 += 22f;
			var (text5, text6) = RoleSummary();
			TechniqueArt.Styled(renderer, TechniqueArt.Display, Pt(19f), text5.ToUpperInvariant(), _d.X(num), _d.Y(num8) - _d.L(10f), TechniqueArt.Stops.Solid(TechniqueArt.Gold), _fade);
			num8 += 20f;
			foreach (string item4 in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), text6, _d.L(num2), 3))
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), item4, _d.X(num), _d.Y(num8), A(TechniqueArt.Soft));
				num8 += 15f;
			}
			num8 += 8f;
			double num13 = Power();
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), Strings.TeachingsScreen.AddedPower.ToString(), _d.X(num), _d.Y(num8), A(TechniqueArt.Soft));
			TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(17f), "+" + TechniqueArt.Number(Math.Round(num13, 1)) + " %", _d.X(num + num2), _d.Y(num8), A((num13 > 15.0) ? TechniqueArt.Red : TechniqueArt.Text));
			num8 += 14f;
			Rectangle rect7 = _d.R(num, num8, num2, 8f);
			TechniqueArt.Fill(renderer, rect7, A(new Color(255, 10, 14, 24)));
			TechniqueArt.Outline(renderer, rect7, A(TechniqueArt.Line));
			Rectangle rect8 = new Rectangle(rect7.X, rect7.Y, (int)((double)rect7.Width * Math.Min(1.0, num13 / 30.0)), rect7.Height);
			if (rect8.Width > 0)
			{
				TechniqueArt.FillHorizontal(renderer, rect8, A(TechniqueArt.Cyan), A(TechniqueArt.Mix(TechniqueArt.Cyan, TechniqueArt.Gold, 0.5f)), A(TechniqueArt.Gold));
			}
			int num14 = rect7.X + (int)((double)rect7.Width * 15.0 / 30.0);
			TechniqueArt.Fill(renderer, new Rectangle(num14 - 1, rect7.Y - _d.L(4f), 2, rect7.Height + _d.L(8f)), A(TechniqueArt.Text));
			num8 += 20f;
			TechniqueArt.Write(renderer, TechniqueArt.Body, Pt(11f), "0", _d.X(num), _d.Y(num8) - _d.L(6f), A(TechniqueArt.Dim));
			string text7 = Strings.TeachingsScreen.PowerCap.ToString();
			string text8 = Strings.TeachingsScreen.SuperSaiyanMark.ToString();
			int x = TechniqueArt.Measure(renderer, TechniqueArt.Body, Pt(11f), text7).X;
			int x2 = TechniqueArt.Measure(renderer, TechniqueArt.Body, Pt(11f), text8).X;
			int centerX = Math.Min(num14, _d.X(num + num2) - x2 - _d.L(10f) - x / 2);
			TechniqueArt.TextCenter(renderer, TechniqueArt.Body, Pt(11f), text7, centerX, _d.Y(num8), A(TechniqueArt.Dim));
			TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(11f), text8, _d.X(num + num2), _d.Y(num8), A(TechniqueArt.Dim));
		}
		float num15 = 668f;
		Rectangle rect9 = _d.R(num, num15, 100f, 38f);
		bool changed = Changed;
		HouseArt.Hollow(renderer, _d, rect9, Strings.TeachingsScreen.Cancel.ToString(), changed && IsHot("annuler"), _fade * (changed ? 1f : 0.45f));
		AddHit(rect9, "annuler", delegate
		{
			if (Changed && !_waiting)
			{
				Reset();
				Say(Strings.TeachingsScreen.ChangesCancelled.ToString());
			}
		});
		Rectangle rect10 = _d.R(num + 108f, num15, num2 - 108f, 38f);
		HouseArt.Cta(renderer, _d, rect10, _waiting ? Strings.TeachingsScreen.Sending.ToString() : Strings.TeachingsScreen.Validate.ToString(), changed && !_waiting, changed && !_waiting && IsHot("valider"), _fade);
		AddHit(rect10, "valider", Validate);
		foreach (string item5 in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11f), Strings.TeachingsScreen.AfterValidation.ToString(), _d.L(num2), 2))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11f), item5, _d.X(num), _d.Y(num15 + 50f), A(TechniqueArt.Dim));
			num15 += 12f;
		}
	}

	private float Effect(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float left, float y, float width, string label, string text, bool strong)
	{
		TechniqueArt.Fill(renderer, _d.R(left, y, width, 1f), A(TechniqueArt.White, 0.07f));
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(11f), label.ToUpperInvariant(), _d.X(left), _d.Y(y + 14f), _d.Em(0.16f, 11f), A(TechniqueArt.Dim));
		float num = y + 30f;
		foreach (string item in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(13f), text, _d.L(width), 3))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), item, _d.X(left), _d.Y(num), A(strong ? TechniqueArt.Text : TechniqueArt.Soft));
			num += 16f;
		}
		return num + 2f;
	}

	private (string Title, string Text) RoleSummary()
	{
		if (_main == TeachingMaster.None)
		{
			return (Title: Strings.TeachingsScreen.RoleNone.ToString(), Text: (Level < 20) ? Strings.TeachingsScreen.RoleNoneBeforeLevel.ToString(20) : Strings.TeachingsScreen.RoleNoneAfterLevel.ToString());
		}
		TeachingTexts.MasterText masterText = TeachingTexts.Of(_main);
		int num = Teachings.SpentIn(_ranks, _main);
		string text = masterText.Mission.ToLowerInvariant();
		if (_secondary == TeachingMaster.None)
		{
			return (Title: masterText.Role, Text: (Level < 40) ? Strings.TeachingsScreen.RoleMainSecondaryLater.ToString(masterText.Name, num, 55, text, 40) : Strings.TeachingsScreen.RoleMain.ToString(masterText.Name, num, 55, text));
		}
		TeachingTexts.MasterText masterText2 = TeachingTexts.Of(_secondary);
		int num2 = Teachings.SpentIn(_ranks, _secondary);
		string item = ((num2 > 0) ? Strings.TeachingsScreen.RoleWithTouch.ToString(masterText.Role, masterText2.Role) : masterText.Role);
		return (Title: item, Text: Strings.TeachingsScreen.RoleMainAndSecondary.ToString(masterText.Name, num, 55, masterText2.Name, num2, 15, text));
	}

	private static int Glyph(string icon)
	{
		switch (icon)
		{
		case "goutte":
		case "flamme":
		case "souffle":
		case "haricot":
		case "ki":
			return 0;
		case "onde":
		case "perce":
			return 1;
		case "poing":
		case "poingleve":
			return 2;
		case "coeur":
		case "lotus":
		case "plume":
			return 3;
		case "etoile":
			return 4;
		case "impact":
		case "eclair":
		case "baton":
			return 5;
		case "groupe":
		case "rempart":
		case "brise":
		case "bouclier":
		case "carapace":
			return 6;
		case "planete":
		case "oeil":
		case "aile":
		case "cible":
			return 7;
		default:
			return 8;
		}
	}

	protected override void OnMouseDown(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseDown(mouseButton, mousePosition, userAction);
		_rightButton = mouseButton == MouseButton.Right || (InputHandler.IsRightMouseDown && !InputHandler.IsLeftMouseDown);
	}

	protected override void OnMouseClicked(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		if (!_rightButton && mouseButton != MouseButton.Right)
		{
			base.OnMouseClicked(mouseButton, mousePosition, userAction);
			return;
		}
		_rightButton = false;
		Point point = CanvasPosToLocal(mousePosition);
		foreach (var (rectangle, id) in _nodeRects)
		{
			if (rectangle.Contains(point.X, point.Y))
			{
				Audio.AddGameSound("menu_click.wav", loop: false);
				Remove(id);
				break;
			}
		}
	}
}
