using System;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Framework.Core;
using Intersect.GameObjects;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Interface.Game.Enseignements;

internal sealed class TrialHud : Intersect.Client.Framework.Gwen.Control.Base
{
	private const float PanelWidth = 560f;

	private readonly TrialLeaveButton _leave;

	protected override bool ShouldClip => false;

	internal static int ReservedBottom { get; private set; }

	public TrialHud(Canvas canvas)
		: base(canvas, "TrialHud")
	{
		base.MouseInputEnabled = false;
		base.KeyboardInputEnabled = false;
		base.IsHidden = true;
		SetBounds(0, 0, canvas.Width, canvas.Height);
		_leave = new TrialLeaveButton(canvas);
	}

	public void Update()
	{
		Intersect.Client.Framework.Gwen.Control.Base parent = base.Parent;
		if (parent != null && (base.Width != parent.Width || base.Height != parent.Height))
		{
			SetBounds(0, 0, parent.Width, parent.Height);
		}
		base.IsHidden = Globals.Me == null || (!MasterTrial.InTrial && !MasterTrial.ResultVisible(out var _));
		_leave.IsHidden = base.IsHidden || !MasterTrial.InTrial;
		if (!MasterTrial.InTrial)
		{
			ReservedBottom = 0;
		}
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		if (base.IsHidden)
		{
			return;
		}
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		CharteScreen.Design d = CharteScreen.Design.For(base.Width, base.Height);
		TeachingTrialPacket current = MasterTrial.Current;
		if (current != null)
		{
			Rectangle rectangle = DrawPanel(renderer, d, current);
			_leave.Place(new Rectangle(rectangle.Right + d.L(8f), rectangle.Y, d.L(118f), d.L(44f)));
			ReservedBottom = rectangle.Bottom;
		}
		if (MasterTrial.ResultVisible(out var elapsedMs))
		{
			TeachingTrialResultPacket result = MasterTrial.Result;
			if (result != null)
			{
				DrawResult(renderer, d, result, MasterTrial.ResultFade(elapsedMs));
			}
		}
	}

	private static Color MasterColor(int master)
	{
		if (!Enum.IsDefined((TeachingMaster)master) || master == 0)
		{
			return TechniqueArt.Gold;
		}
		return TeachingTexts.Of((TeachingMaster)master).Color;
	}

	private static string MasterName(int master)
	{
		if (!Enum.IsDefined((TeachingMaster)master) || master == 0)
		{
			return string.Empty;
		}
		return TeachingTexts.Of((TeachingMaster)master).Name;
	}

	private static Rectangle DrawPanel(Intersect.Client.Framework.Gwen.Renderer.Base renderer, CharteScreen.Design d, TeachingTrialPacket trial)
	{
		Color color = MasterColor(trial.Master);
		TeachingTrialObjective[] array = trial.Objectives ?? Array.Empty<TeachingTrialObjective>();
		int num = d.L(70f);
		TeachingTrialObjective[] array2 = array;
		foreach (TeachingTrialObjective line in array2)
		{
			num += d.L(IsBar(line) ? 38 : 28);
		}
		num += d.L(12f);
		int num2 = d.L(560f);
		Rectangle rectangle = new Rectangle(d.X(800f) - num2 / 2, d.Y(18f), num2, num);
		TechniqueArt.FillVertical(renderer, rectangle, new Color(236, 6, 8, 14), new Color(200, 6, 8, 14));
		TechniqueArt.Outline(renderer, rectangle, TechniqueArt.Line2);
		TechniqueArt.Fill(renderer, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, Math.Max(2, d.L(3f))), color);
		TechniqueArt.FillVertical(renderer, new Rectangle(rectangle.X + 1, rectangle.Y + d.L(3f), rectangle.Width - 2, d.L(48f)), TechniqueArt.Alpha(color, 0.12f), TechniqueArt.Alpha(color, 0f));
		int num3 = rectangle.X + d.L(18f);
		string text = Strings.TrialHud.Eyebrow.ToString(MasterName(trial.Master)).ToUpperInvariant();
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, d.Pt(12f), text, num3, rectangle.Y + d.L(18f), d.Em(0.18f, 12f), color);
		string text2 = trial.Name.ToUpperInvariant();
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, d.Pt(24f), text2);
		TechniqueArt.Styled(renderer, TechniqueArt.Display, d.Pt(24f), text2, num3, rectangle.Y + d.L(47f) - point.Y / 2, new TechniqueArt.Stops(TechniqueArt.White, 0.4f, TechniqueArt.Mix(color, TechniqueArt.White, 0.55f), 1f, color));
		long remainingMs = MasterTrial.RemainingMs;
		bool flag = remainingMs <= 10000;
		float alpha = (flag ? (0.6f + 0.4f * MathF.Sin((float)Timing.Global.MillisecondsUtc / 120f)) : 1f);
		Color color2 = (flag ? TechniqueArt.Alpha(TechniqueArt.Red, alpha) : TechniqueArt.Gold);
		int right = rectangle.Right - d.L(18f);
		TechniqueArt.TextRight(renderer, TechniqueArt.Label, d.Pt(12f), Strings.TrialHud.Hourglass.ToString(), right, rectangle.Y + d.L(18f), TechniqueArt.Dim);
		TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, d.Pt(24f), MasterTrial.Clock(remainingMs), right, rectangle.Y + d.L(45f), color2);
		TechniqueArt.Fill(renderer, new Rectangle(rectangle.X + d.L(14f), rectangle.Y + d.L(66f), rectangle.Width - d.L(28f), 1), TechniqueArt.Line);
		int top = rectangle.Y + d.L(70f);
		TeachingTrialObjective[] array3 = array;
		foreach (TeachingTrialObjective line2 in array3)
		{
			top = DrawObjective(renderer, d, line2, num3, rectangle.Right - d.L(18f), top, color);
		}
		return rectangle;
	}

	private static bool IsBar(TeachingTrialObjective line)
	{
		TrialObjectiveKind kind = (TrialObjectiveKind)line.Kind;
		if ((uint)(kind - 2) > 1u)
		{
			if (line.Kind == 1)
			{
				return line.Max > 0;
			}
			return false;
		}
		return true;
	}

	private static int DrawObjective(Intersect.Client.Framework.Gwen.Renderer.Base renderer, CharteScreen.Design d, TeachingTrialObjective line, int left, int right, int top, Color color)
	{
		int num = top + d.L(14f);
		int num2 = d.L(12f);
		Rectangle rect = new Rectangle(left, num - num2 / 2, num2, num2);
		Color color2 = (line.Bonus ? TechniqueArt.Gold : TechniqueArt.Line2);
		if (line.Done)
		{
			TechniqueArt.Fill(renderer, rect, TechniqueArt.Green);
			TechniqueArt.Glow(renderer, (float)rect.X + (float)num2 / 2f, num, (float)num2 * 1.2f, TechniqueArt.Alpha(TechniqueArt.Green, 0.35f));
		}
		else
		{
			TechniqueArt.Fill(renderer, rect, new Color(255, 10, 14, 24));
			TechniqueArt.Outline(renderer, rect, color2);
		}
		int num3 = left + num2 + d.L(10f);
		int num4 = num3;
		if (line.Bonus)
		{
			num4 += TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, d.Pt(12.5f), Strings.TrialHud.BonusPrefix.ToString(), num4, num, TechniqueArt.Gold);
		}
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, d.Pt(13f), line.Label, num4, num, line.Done ? TechniqueArt.Soft : TechniqueArt.Text);
		TrialObjectiveKind kind = (TrialObjectiveKind)line.Kind;
		string text;
		switch (kind)
		{
		case TrialObjectiveKind.Bar:
		case TrialObjectiveKind.EnemyBar:
			text = $"{line.Current} %";
			break;
		case TrialObjectiveKind.Count:
			text = ((line.Max > 0) ? $"{line.Current}/{line.Max}" : line.Current.ToString());
			break;
		default:
			text = string.Empty;
			break;
		}
		string text2 = text;
		if (text2.Length > 0)
		{
			TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, d.Pt(15f), text2, right, num, TechniqueArt.Text);
		}
		if (!IsBar(line))
		{
			return top + d.L(28f);
		}
		float num5 = ((kind == TrialObjectiveKind.Count) ? Math.Clamp((float)line.Current / (float)Math.Max(1, line.Max), 0f, 1f) : Math.Clamp((float)line.Current / 100f, 0f, 1f));
		Rectangle rect2 = new Rectangle(num3, top + d.L(27f), right - num3, Math.Max(3, d.L(5f)));
		TechniqueArt.Fill(renderer, rect2, new Color(255, 10, 14, 24));
		TechniqueArt.Outline(renderer, rect2, TechniqueArt.Line);
		Rectangle rect3 = new Rectangle(rect2.X + 1, rect2.Y + 1, (int)((float)(rect2.Width - 2) * num5), rect2.Height - 2);
		if (rect3.Width > 0)
		{
			Color color3 = ((kind == TrialObjectiveKind.EnemyBar) ? TechniqueArt.Red : color);
			TechniqueArt.FillHorizontal(renderer, rect3, TechniqueArt.Mix(color3, TechniqueArt.Black, 0.6f), color3, TechniqueArt.Mix(color3, TechniqueArt.White, 0.7f));
		}
		return top + d.L(38f);
	}

	private static void DrawResult(Intersect.Client.Framework.Gwen.Renderer.Base renderer, CharteScreen.Design d, TeachingTrialResultPacket result, float fade)
	{
		Color color = (result.Success ? TechniqueArt.Gold : TechniqueArt.Red);
		string text = (result.Success ? Strings.TrialHud.Success.ToString() : Strings.TrialHud.Failure.ToString());
		int num = d.X(800f);
		int num2 = d.Y(250f);
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, d.Pt(46f), text);
		TechniqueArt.Glow(renderer, num, (float)num2 + (float)point.Y / 2f, (float)point.X * 0.75f, TechniqueArt.Alpha(color, 0.3f * fade));
		TechniqueArt.Styled(renderer, TechniqueArt.Display, d.Pt(46f), text, num - point.X / 2, num2, result.Success ? new TechniqueArt.Stops(TechniqueArt.White, 0.35f, new Color(255, 255, 226, 150), 0.8f, TechniqueArt.Ki) : new TechniqueArt.Stops(TechniqueArt.White, 0.4f, new Color(255, 255, 196, 204), 1f, TechniqueArt.Red), fade);
		int num3 = num2 + point.Y + d.L(16f);
		string text2 = (MasterName(result.Master) + " · " + result.Name).ToUpperInvariant();
		TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, d.Pt(12f), text2, num, num3, d.Em(0.18f, 12f), TechniqueArt.Alpha(MasterColor(result.Master), fade));
		num3 += d.L(26f);
		if (!string.IsNullOrWhiteSpace(result.Message))
		{
			TechniqueArt.TextCenter(renderer, TechniqueArt.Body, d.Pt(15f), result.Message, num, num3, TechniqueArt.Alpha(TechniqueArt.Text, fade));
			num3 += d.L(26f);
		}
		if (result.Bonus)
		{
			TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, d.Pt(14f), Strings.TrialHud.BonusSuccess.ToString(), num, num3, TechniqueArt.Alpha(TechniqueArt.Gold, fade));
			num3 += d.L(26f);
		}
		string nextStep = MasterTrial.NextStep;
		if (nextStep == null)
		{
			return;
		}
		num3 += d.L(6f);
		foreach (string item in TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, d.Pt(14f), nextStep, d.L(760f), 2))
		{
			TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, d.Pt(14f), item, num, num3, TechniqueArt.Alpha(TechniqueArt.Green, fade));
			num3 += d.L(22f);
		}
	}
}
