using System;

namespace Intersect.Client.Interface.Game.EntityPanel;

internal sealed class GaugeMotion
{
	private const long GhostHoldMs = 350L;

	private long _ghostHoldUntil;

	private float _lastTarget = -1f;

	public float Shown = -1f;

	public float Ghost = -1f;

	public float Head;

	public float Target;

	public bool IsHolding(long now)
	{
		if (Ghost > Shown)
		{
			return now < _ghostHoldUntil;
		}
		return false;
	}

	public void Snap(float ratio)
	{
		ratio = Math.Clamp(ratio, 0f, 1f);
		Target = (Shown = (Ghost = ratio));
		_lastTarget = ratio;
		Head = 0f;
	}

	public void Update(float ratio, float dt, long now)
	{
		ratio = Math.Clamp(ratio, 0f, 1f);
		Target = ratio;
		if (Shown < 0f)
		{
			Snap(ratio);
			return;
		}
		if (Math.Abs(ratio - _lastTarget) > 0.0005f)
		{
			Head = 1f;
			if (ratio < _lastTarget)
			{
				if (Ghost < Shown)
				{
					Ghost = Shown;
				}
				_ghostHoldUntil = now + 350;
			}
			_lastTarget = ratio;
		}
		Shown += (ratio - Shown) * (1f - MathF.Exp((0f - dt) * 12f));
		if (Math.Abs(ratio - Shown) < 0.001f)
		{
			Shown = ratio;
		}
		if (Ghost > Shown)
		{
			if (now >= _ghostHoldUntil)
			{
				Ghost += (Shown - Ghost) * (1f - MathF.Exp((0f - dt) * 5f));
				if (Ghost - Shown < 0.001f)
				{
					Ghost = Shown;
				}
			}
		}
		else
		{
			Ghost = Shown;
		}
		Head *= MathF.Exp((0f - dt) * 3f);
		if (Head < 0.01f)
		{
			Head = 0f;
		}
	}
}
