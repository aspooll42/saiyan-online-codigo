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

internal sealed class EnseignementsScreen : CharteScreen
{
	private enum Status
	{
		Main,
		Secondary,
		Closed
	}

	private const float ColumnsLeft = 36f;

	private const float ColumnWidth = 290f;

	private const float ColumnGap = 8f;

	private const float BodyTop = 96f;

	private const float BodyHeight = 768f;

	private const float TreeTop = 250f;

	private const float RowPitch = 102f;

	private const float PanelLeft = 1236f;

	private const float PanelWidth = 328f;

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

	private readonly List<(Rectangle Rect, string Id)> _nodeRects = new List<(Rectangle, string)>();

	private bool _rightButton;

	protected override string? DetailsKey => "EnseignementsDetails";

	public override bool IsBlockingInput => !base.IsHidden;

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

	public EnseignementsScreen(Canvas gameCanvas)
		: base(gameCanvas, "EnseignementsScreen")
	{
		Interface.InputBlockingComponents.Add(this);
	}

	public override void Show()
	{
		Reset();
		base.Show();
	}

	public void Toggle()
	{
		if (IsVisible())
		{
			Hide();
		}
		else
		{
			Show();
		}
	}

	public void Update()
	{
		if (base.IsHidden)
		{
			return;
		}
		FitCanvas();
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
				if (Level >= 20)
				{
					return Strings.TeachingsScreen.WhyPassTrial.ToString();
				}
				return Strings.TeachingsScreen.WhyMastersLevel.ToString(20);
			}
			if (_secondary == TeachingMaster.None)
			{
				if (Level >= 40)
				{
					return Strings.TeachingsScreen.WhySecondaryTrial.ToString(40, masterText.Name);
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

	protected override void DrawScreen(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		Color accent = ((_main != TeachingMaster.None) ? TeachingTexts.Of(_main).Color : TechniqueArt.Ki);
		DrawBackdrop(renderer, accent);
		AddHit(new Rectangle(0, 0, base.Width, base.Height), "fond", delegate
		{
		});
		int num = Total - Spent;
		DrawTopBar(renderer, Strings.TeachingsScreen.Title.ToString(), Strings.TeachingsScreen.Subtitle.ToString(me.Name, me.Level), new global::_003C_003Ez__ReadOnlyArray<Gauge>(new Gauge[2]
		{
			new Gauge(Strings.TeachingsScreen.GaugeToPlace.ToString(), $"{num}", (num > 0) ? TechniqueArt.Gold : TechniqueArt.Soft),
			new Gauge(Strings.TeachingsScreen.GaugePlaced.ToString(), $"{Spent} / {Total}", TechniqueArt.Text)
		}));
		_nodeRects.Clear();
		string text = null;
		for (int num2 = 0; num2 < TeachingTexts.Masters.Length; num2++)
		{
			string text2 = DrawColumn(renderer, TeachingTexts.Masters[num2], 36f + (float)num2 * 298f);
			if (text == null)
			{
				text = text2;
			}
		}
		DrawSheet(renderer, Teachings.Find(text ?? _selected) ?? Teachings.All[0]);
		if (_toast != null && now < _toastUntil)
		{
			int num3 = TechniqueArt.Measure(renderer, TechniqueArt.Body, Pt(14f), _toast).X + _d.L(40f);
			Rectangle rect = new Rectangle(_d.X(628f) - num3 / 2, _d.Y(806f), num3, _d.L(38f));
			TechniqueArt.Fill(renderer, rect, A(new Color(245, 12, 17, 28)));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line2));
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(3f), rect.Height), A(TechniqueArt.Gold));
			TechniqueArt.TextCenter(renderer, TechniqueArt.Body, Pt(14f), _toast, rect.X + rect.Width / 2, rect.Y + rect.Height / 2, A(TechniqueArt.Text));
		}
	}

	private string? DrawColumn(Intersect.Client.Framework.Gwen.Renderer.Base renderer, TeachingTexts.MasterText master, float x)
	{
		Status status = StatusOf(master.Master);
		Color color = master.Color;
		bool flag = status == Status.Closed;
		Rectangle rect = _d.R(x, 96f, 290f, 768f);
		Panel(renderer, rect);
		if (!flag)
		{
			TechniqueArt.FillVertical(renderer, _d.R(x + 2f, 98f, 286f, 220f), A(color, 0.1f), A(color, 0f));
		}
		float num = x + 145f;
		float num2 = (flag ? 0.45f : 1f);
		string text = (status switch
		{
			Status.Main => Strings.TeachingsScreen.ChipMain.ToString(), 
			Status.Secondary => Strings.TeachingsScreen.ChipSecondary.ToString(15), 
			_ => ClosedLabel(master.Master), 
		}).ToUpperInvariant();
		int num3 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(12f), text, _d.Em(0.12f, 12f)) + _d.L(20f);
		Rectangle rect2 = new Rectangle(_d.X(num) - num3 / 2, _d.Y(110f), num3, _d.L(22f));
		TechniqueArt.Patch(renderer, "tech_biais5.png", rect2, A(status switch
		{
			Status.Secondary => TechniqueArt.Mix(color, new Color(255, 23, 33, 54), 0.35f), 
			Status.Main => TechniqueArt.Gold, 
			_ => new Color(255, 21, 28, 43), 
		}), 6);
		TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(12f), text, rect2.X + rect2.Width / 2, rect2.Y + rect2.Height / 2, _d.Em(0.12f, 12f), A((status == Status.Main) ? HouseArt.Dark : TechniqueArt.Text));
		string text2 = master.Role.ToUpperInvariant();
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(27f), text2);
		TechniqueArt.Styled(renderer, TechniqueArt.Display, Pt(27f), text2, _d.X(num) - point.X / 2, _d.Y(140f), TechniqueArt.Stops.Solid(TechniqueArt.Mix(color, TechniqueArt.White, 0.85f)), _fade * num2);
		TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(12f), master.Name.ToUpperInvariant(), _d.X(num), _d.Y(184f), _d.Em(0.16f, 12f), A(TechniqueArt.Text, num2));
		TechniqueArt.TextCenter(renderer, TechniqueArt.Body, Pt(12.5f), master.Mission, _d.X(num), _d.Y(203f), A(TechniqueArt.Soft, num2));
		bool flag2 = flag && CanChooseMain(master.Master);
		bool flag3 = flag && CanChooseSecondary(master.Master);
		if (flag2 || flag3)
		{
			string key = $"choix-{master.Master}";
			Rectangle rect3 = _d.R(x + 26f, 218f, 238f, 34f);
			HouseArt.Hollow(renderer, _d, rect3, flag2 ? Strings.TeachingsScreen.ChooseMain.ToString() : Strings.TeachingsScreen.ChooseSecondary.ToString(), IsHot(key), _fade, color);
			AddHit(rect3, key, delegate
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
					TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(14f), Strings.TeachingsScreen.TrialName.ToString(Traduction.Contenu(teachingTrialInfo.Name)), _d.X(num), _d.Y(228f), A(flag4 ? TechniqueArt.Gold : TechniqueArt.Text, num2 + (1f - num2) * 0.6f));
					TechniqueArt.TextCenter(renderer, TechniqueArt.Body, Pt(12f), flag4 ? Strings.TeachingsScreen.SwearOath.ToString(master.Name) : Strings.TeachingsScreen.TrialAt.ToString(master.Name), _d.X(num), _d.Y(246f), A(flag4 ? TechniqueArt.Green : TechniqueArt.Soft, num2 + (1f - num2) * 0.6f));
					goto IL_0756;
				}
			}
			if (!flag)
			{
				int num4 = ((status == Status.Secondary) ? 15 : 55);
				int num5 = Teachings.SpentIn(_ranks, master.Master);
				Rectangle rect4 = _d.R(num - 100f, 232f, 150f, 6f);
				TechniqueArt.Fill(renderer, rect4, A(new Color(255, 10, 14, 24)));
				TechniqueArt.Outline(renderer, rect4, A(TechniqueArt.Line));
				Rectangle rect5 = new Rectangle(rect4.X, rect4.Y, (int)((float)rect4.Width * Math.Min(1f, (float)num5 / (float)num4)), rect4.Height);
				if (rect5.Width > 0)
				{
					TechniqueArt.FillHorizontal(renderer, rect5, A(TechniqueArt.Mix(color, TechniqueArt.Black, 0.6f)), A(color), A(TechniqueArt.Mix(color, TechniqueArt.White, 0.7f)));
				}
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(16f), $"{num5} / {num4}", rect4.Right + _d.L(10f), rect4.Y + rect4.Height / 2, A(TechniqueArt.Text));
			}
		}
		goto IL_0756;
		IL_0756:
		return DrawTree(renderer, master, x, status, num2);
	}

	private string? DrawTree(Intersect.Client.Framework.Gwen.Renderer.Base renderer, TeachingTexts.MasterText master, float x, Status status, float dim)
	{
		Color color = master.Color;
		float num = x + 145f;
		List<TeachingNode> source = Teachings.All.Where((TeachingNode node) => node.Master == master.Master).ToList();
		int num2 = Teachings.SpentIn(_ranks, master.Master, 5);
		float num3 = 276f;
		float num4 = 792f;
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
			float num7 = (float)Math.Min(num4, (double)num3 + num5 * 102.0);
			Rectangle rect = _d.R(num - 1f, num3, 2f, num7 - num3);
			TechniqueArt.Fill(renderer, rect, A(color));
			TechniqueArt.GlowRect(renderer, new Rectangle(rect.X - _d.L(4f), rect.Y, rect.Width + _d.L(8f), rect.Height), A(color, 0.25f));
		}
		string result = null;
		int tier;
		for (tier = 0; tier <= 5; tier++)
		{
			float num8 = 250f + (float)tier * 102f;
			bool flag = status != Status.Closed && Teachings.SpentIn(_ranks, master.Master, tier) >= Teachings.TierRequirement[tier] && (status != Status.Secondary || tier <= 1);
			if (tier > 0)
			{
				TechniqueArt.Write(renderer, TechniqueArt.Heavy, Pt(16f), $"{Teachings.TierRequirement[tier]}", _d.X(x + 12f), _d.Y(num8 + 2f), A(flag ? TechniqueArt.Mix(color, TechniqueArt.Soft, 0.7f) : TechniqueArt.Dim, dim));
				TechniqueArt.Write(renderer, TechniqueArt.Label, Pt(12f), Strings.TeachingsScreen.TierPoints.ToString(), _d.X(x + 12f), _d.Y(num8 + 21f), A(TechniqueArt.Dim, dim));
			}
			List<TeachingNode> list = source.Where((TeachingNode node) => node.Tier == tier).ToList();
			for (int num9 = 0; num9 < list.Count; num9++)
			{
				float centerX = ((list.Count == 1) ? num : (num + (float)((num9 == 0) ? (-56) : 56)));
				if (DrawNode(renderer, list[num9], color, centerX, num8, status, dim))
				{
					result = list[num9].Id;
				}
			}
			if (status == Status.Secondary && tier > 1)
			{
				float num10 = ((list.Count == 1) ? (num + 50f) : num);
				float num11 = 16f;
				float y = num8 + (float)((list.Count == 1) ? 33 : 32) - num11 / 2f;
				TechniqueArt.Image(renderer, "inv_cadenas.png", _d.R(num10 - num11 / 2f, y, num11, num11), A(TechniqueArt.Soft, 0.8f * dim));
			}
		}
		return result;
	}

	private bool DrawNode(Intersect.Client.Framework.Gwen.Renderer.Base renderer, TeachingNode node, Color color, float centerX, float rowTop, Status status, float dim)
	{
		TeachingTexts.NodeText nodeText = TeachingTexts.Of(node.Id);
		bool flag = node.Tier == 5;
		float num = (flag ? 66f : 52f);
		int num2 = Teachings.Rank(_ranks, node.Id);
		bool flag2 = status != Status.Closed && Teachings.SpentIn(_ranks, node.Master, node.Tier) >= Teachings.TierRequirement[node.Tier] && (status != Status.Secondary || node.Tier <= 1);
		bool flag3 = flag2 && num2 == 0 && WhyNotAdd(node) == null;
		string key = "noeud-" + node.Id;
		bool flag4 = IsHot(key);
		bool flag5 = _selected == node.Id;
		Rectangle rect = _d.R(centerX - num / 2f, rowTop + (float)((!flag) ? 6 : 0), num, num);
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
		int num5 = (flag ? 26 : 10);
		int num6 = 3;
		int num7 = node.MaxRank * num5 + (node.MaxRank - 1) * num6;
		float num8 = rowTop + (flag ? (num + 4f) : (num + 11f));
		for (int i = 0; i < node.MaxRank; i++)
		{
			Rectangle rect4 = _d.R(centerX - (float)num7 / 2f + (float)(i * (num5 + num6)), num8, num5, 5f);
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
		List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(12f), nodeText.Name, _d.L(flag ? 150 : 92), 2);
		int num9 = _d.Y(num8 + 16f);
		foreach (string item in list)
		{
			TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(12f), item, _d.X(centerX), num9, A(color4, dim));
			num9 += _d.L(14f);
		}
		Rectangle rectangle = _d.R(centerX - (float)(flag ? 76 : 48), rowTop, flag ? 152 : 96, 98f);
		_nodeRects.Add((rectangle, node.Id));
		AddHit(rectangle, key, delegate
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
		Rectangle rect = _d.R(1236f, 96f, 328f, 768f);
		Panel(renderer, rect);
		AddHit(rect, "fiche", delegate
		{
		});
		float num = 1254f;
		float num2 = 292f;
		Heading(renderer, num, 110f, num2, Strings.TeachingsScreen.SheetHeading.ToString(), null);
		DrawDetailsToggle(renderer, _d.X(num + num2), _d.Y(119.4f));
		DrawHexEmblem(renderer, _d.R(num, 136f, 62f, 62f), null, Glyph(nodeText.Icon), color, 0.58f);
		List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(23f), nodeText.Name, _d.L(num2 - 76f), 2);
		int num3 = _d.Y(96f + (float)((list.Count > 1) ? 50 : 62));
		foreach (string item in list)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(23f), item, _d.X(num + 76f), num3, A(TechniqueArt.Text));
			num3 += _d.L(24f);
		}
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), (masterText.Name + " · " + masterText.Role).ToUpperInvariant(), _d.X(num + 76f), num3 + _d.L(2f), _d.Em(0.14f, 12f), A(color));
		int num4 = Teachings.Rank(_ranks, node.Id);
		float num5 = 218f;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(17f), Strings.TeachingsScreen.Rank.ToString(num4, node.MaxRank), _d.X(num), _d.Y(num5), A(TechniqueArt.Text));
		float num6 = ((node.MaxRank == 1) ? 60f : 18f);
		for (int num7 = 0; num7 < node.MaxRank; num7++)
		{
			Rectangle rect2 = _d.R(num + num2 - (float)node.MaxRank * (num6 + 4f) + 4f + (float)num7 * (num6 + 4f), num5 - 4f, num6, 8f);
			if (num7 < num4)
			{
				TechniqueArt.Fill(renderer, rect2, A((num7 < ServerRank(node.Id)) ? color : TechniqueArt.Mix(color, TechniqueArt.White, 0.5f)));
				continue;
			}
			TechniqueArt.Fill(renderer, rect2, A(new Color(255, 10, 14, 24)));
			TechniqueArt.Outline(renderer, rect2, A(TechniqueArt.Line2));
		}
		num5 += 22f;
		if (node.Tier == 5)
		{
			num5 = Effect(renderer, num, num5, num2, Strings.TeachingsScreen.EffectLabel.ToString(), TeachingTexts.EffectAt(node, 1), strong: true);
		}
		else
		{
			num5 = Effect(renderer, num, num5, num2, Strings.TeachingsScreen.CurrentLabel.ToString(), (num4 > 0) ? TeachingTexts.EffectAt(node, num4) : Strings.TeachingsScreen.NotLearned.ToString(), num4 > 0);
			num5 = Effect(renderer, num, num5, num2, (num4 > 0) ? Strings.TeachingsScreen.NextRankLabel.ToString() : Strings.TeachingsScreen.FirstRankLabel.ToString(), (num4 < node.MaxRank) ? TeachingTexts.EffectAt(node, num4 + 1) : Strings.TeachingsScreen.MaxRankReached.ToString(), num4 < node.MaxRank);
		}
		string text = WhyNotAdd(node);
		Status status = StatusOf(node.Master);
		bool flag = status != Status.Closed && Teachings.SpentIn(_ranks, node.Master, node.Tier) >= Teachings.TierRequirement[node.Tier] && (status != Status.Secondary || node.Tier <= 1);
		string text2 = ((!flag) ? (text ?? Strings.TeachingsScreen.ConditionClosed.ToString()) : ((node.Tier == 5) ? Strings.TeachingsScreen.ConditionUltimate.ToString(masterText.Name, node.Cost) : ((status == Status.Secondary) ? Strings.TeachingsScreen.ConditionSecondary.ToString(Teachings.SpentIn(_ranks, node.Master), 15) : Strings.TeachingsScreen.ConditionOpen.ToString())));
		num5 += 8f;
		string text3 = (flag ? Strings.TeachingsScreen.SheetOpen.ToString() : Strings.TeachingsScreen.SheetClosed.ToString());
		int num8 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(12f), text3, _d.Em(0.12f, 12f)) + _d.L(14f);
		Rectangle rect3 = new Rectangle(_d.X(num), _d.Y(num5), num8, _d.L(18f));
		TechniqueArt.Patch(renderer, "tech_biais5.png", rect3, A(flag ? TechniqueArt.Green : TechniqueArt.Red), 6);
		TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(12f), text3, rect3.X + rect3.Width / 2, rect3.Y + rect3.Height / 2, _d.Em(0.12f, 12f), A(HouseArt.Dark));
		List<string> list2 = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), text2, _d.L(num2) - num8 - _d.L(10f), 3);
		int num9 = rect3.Y + rect3.Height / 2;
		foreach (string item2 in list2)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), item2, rect3.Right + _d.L(10f), num9, A(TechniqueArt.Soft));
			num9 += _d.L(16f);
		}
		num5 += (float)Math.Max(26, list2.Count * 16 + 6);
		if (!node.Implemented)
		{
			foreach (string item3 in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), Strings.TeachingsScreen.GroupEffectPending.ToString(), _d.L(num2), 2))
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), item3, _d.X(num), _d.Y(num5 + 6f), A(TechniqueArt.Gold));
				num5 += 15f;
			}
			num5 += 6f;
		}
		num5 += 6f;
		float num10 = (num2 - 8f) / 2f;
		Rectangle rect4 = _d.R(num, num5, num10, 36f);
		Rectangle rect5 = _d.R(num + num10 + 8f, num5, num10, 36f);
		bool flag2 = WhyNotRemove(node) == null && !_waiting;
		bool flag3 = text == null && !_waiting;
		HouseArt.Hollow(renderer, _d, rect4, Strings.TeachingsScreen.RemoveRank.ToString(), flag2 && IsHot("retirer"), _fade * (flag2 ? 1f : 0.45f));
		HouseArt.Hollow(renderer, _d, rect5, Strings.TeachingsScreen.Learn.ToString(), flag3 && IsHot("apprendre"), _fade * (flag3 ? 1f : 0.45f), color);
		AddHit(rect4, "retirer", delegate
		{
			Remove(node.Id);
		});
		AddHit(rect5, "apprendre", delegate
		{
			Add(node.Id);
		});
		num5 += 48f;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), Strings.TeachingsScreen.ClickHint.ToString(), _d.X(num), _d.Y(num5), A(TechniqueArt.Dim));
		if (base.Details)
		{
			num5 += 22f;
			TechniqueArt.Fill(renderer, _d.R(num, num5, num2, 1f), A(TechniqueArt.White, 0.07f));
			num5 += 16f;
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), Strings.TeachingsScreen.YourRole.ToString(), _d.X(num), _d.Y(num5), _d.Em(0.2f, 12f), A(TechniqueArt.Dim));
			num5 += 26f;
			var (text4, text5) = RoleSummary();
			TechniqueArt.Styled(renderer, TechniqueArt.Display, Pt(22f), text4.ToUpperInvariant(), _d.X(num), _d.Y(num5) - _d.L(12f), TechniqueArt.Stops.Solid(TechniqueArt.Gold), _fade);
			num5 += 22f;
			foreach (string item4 in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), text5, _d.L(num2), 3))
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), item4, _d.X(num), _d.Y(num5), A(TechniqueArt.Soft));
				num5 += 16f;
			}
			num5 += 10f;
			double num11 = Power();
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), Strings.TeachingsScreen.AddedPower.ToString(), _d.X(num), _d.Y(num5), A(TechniqueArt.Soft));
			TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(19f), "+" + TechniqueArt.Number(Math.Round(num11, 1)) + " %", _d.X(num + num2), _d.Y(num5), A((num11 > 15.0) ? TechniqueArt.Red : TechniqueArt.Text));
			num5 += 16f;
			Rectangle rect6 = _d.R(num, num5, num2, 9f);
			TechniqueArt.Fill(renderer, rect6, A(new Color(255, 10, 14, 24)));
			TechniqueArt.Outline(renderer, rect6, A(TechniqueArt.Line));
			Rectangle rect7 = new Rectangle(rect6.X, rect6.Y, (int)((double)rect6.Width * Math.Min(1.0, num11 / 30.0)), rect6.Height);
			if (rect7.Width > 0)
			{
				TechniqueArt.FillHorizontal(renderer, rect7, A(TechniqueArt.Cyan), A(TechniqueArt.Mix(TechniqueArt.Cyan, TechniqueArt.Gold, 0.5f)), A(TechniqueArt.Gold));
			}
			int num12 = rect6.X + (int)((double)rect6.Width * 15.0 / 30.0);
			TechniqueArt.Fill(renderer, new Rectangle(num12 - 1, rect6.Y - _d.L(4f), 2, rect6.Height + _d.L(8f)), A(TechniqueArt.Text));
			num5 += 22f;
			TechniqueArt.Write(renderer, TechniqueArt.Body, Pt(12f), "0", _d.X(num), _d.Y(num5) - _d.L(7f), A(TechniqueArt.Dim));
			string text6 = Strings.TeachingsScreen.PowerCap.ToString();
			string text7 = Strings.TeachingsScreen.SuperSaiyanMark.ToString();
			int x = TechniqueArt.Measure(renderer, TechniqueArt.Body, Pt(12f), text6).X;
			int x2 = TechniqueArt.Measure(renderer, TechniqueArt.Body, Pt(12f), text7).X;
			int centerX = Math.Min(num12, _d.X(num + num2) - x2 - _d.L(10f) - x / 2);
			TechniqueArt.TextCenter(renderer, TechniqueArt.Body, Pt(12f), text6, centerX, _d.Y(num5), A(TechniqueArt.Dim));
			TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(12f), text7, _d.X(num + num2), _d.Y(num5), A(TechniqueArt.Dim));
		}
		float num13 = 794f;
		Rectangle rect8 = _d.R(num, num13, 110f, 42f);
		bool changed = Changed;
		HouseArt.Hollow(renderer, _d, rect8, Strings.TeachingsScreen.Cancel.ToString(), changed && IsHot("annuler"), _fade * (changed ? 1f : 0.45f));
		AddHit(rect8, "annuler", delegate
		{
			if (Changed && !_waiting)
			{
				Reset();
				Say(Strings.TeachingsScreen.ChangesCancelled.ToString());
			}
		});
		Rectangle rect9 = _d.R(num + 118f, num13, num2 - 118f, 42f);
		HouseArt.Cta(renderer, _d, rect9, _waiting ? Strings.TeachingsScreen.Sending.ToString() : Strings.TeachingsScreen.Validate.ToString(), changed && !_waiting, changed && !_waiting && IsHot("valider"), _fade);
		AddHit(rect9, "valider", Validate);
		foreach (string item5 in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), Strings.TeachingsScreen.AfterValidation.ToString(), _d.L(num2), 2))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), item5, _d.X(num), _d.Y(num13 + 54f), A(TechniqueArt.Dim));
			num13 += 13f;
		}
	}

	private float Effect(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float left, float y, float width, string label, string text, bool strong)
	{
		TechniqueArt.Fill(renderer, _d.R(left, y, width, 1f), A(TechniqueArt.White, 0.07f));
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), label.ToUpperInvariant(), _d.X(left), _d.Y(y + 16f), _d.Em(0.16f, 12f), A(TechniqueArt.Dim));
		float num = y + 34f;
		foreach (string item in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(14f), text, _d.L(width), 3))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(14f), item, _d.X(left), _d.Y(num), A(strong ? TechniqueArt.Text : TechniqueArt.Soft));
			num += 18f;
		}
		return num + 4f;
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
