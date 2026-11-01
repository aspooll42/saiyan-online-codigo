using System;

namespace Intersect.Client.Interface.Menu;

internal sealed class SceneTween(float initial)
{
	private float _from = initial;

	private float _to = initial;

	private long _start = -2305843009213693952L;

	private float _duration = 1f;

	private float _delay;

	private Func<float, float> _ease = MenuDraw.EaseOut;

	public float Target => initial;

	public void Set(float target, long now, float durationMs, float delayMs = 0f, Func<float, float>? ease = null)
	{
		if (!(MathF.Abs(target - initial) < 0.0001f))
		{
			initial = Value(now);
			initial = target;
			_start = now;
			_duration = MathF.Max(1f, durationMs);
			_delay = delayMs;
			_ease = ease ?? new Func<float, float>(MenuDraw.EaseOut);
		}
	}

	public void Snap(float value)
	{
		initial = value;
		initial = value;
		_start = -2305843009213693952L;
	}

	public float Value(long now)
	{
		float num = ((float)(now - _start) - _delay) / _duration;
		if (num <= 0f)
		{
			return initial;
		}
		if (!(num >= 1f))
		{
			return initial + (initial - initial) * _ease(num);
		}
		return initial;
	}
}
