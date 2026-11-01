using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Utilities;
using Intersect.Configuration;
using Intersect.Framework.Core;

namespace Intersect.Client.Interface.Game.Typewriting;

internal sealed class Typewriter
{
	public delegate void TextWrittenHandler(string text, Color color);

	private int _offset;

	private int _segmentIndex;

	private string? _lastText;

	private long _nextUpdateTime;

	private readonly TextWrittenHandler _textWrittenHandler;

	private readonly ColorizedText[] _segments;

	private static HashSet<string> FullStops => ClientConfiguration.Instance.TypewriterFullStops;

	private static long FullStopSpeed => ClientConfiguration.Instance.TypewriterFullStopDelay;

	private static HashSet<string> PartialStops => ClientConfiguration.Instance.TypewriterPauses;

	private static long PartialStopSpeed => ClientConfiguration.Instance.TypewriterPauseDelay;

	private static long TypingSpeed => ClientConfiguration.Instance.TypewriterPartDelay;

	public bool IsDone { get; private set; }

	public long DoneAtMilliseconds { get; private set; }

	public Typewriter(ColorizedText[] segments, TextWrittenHandler textWrittenHandler)
	{
		_segments = segments;
		_textWrittenHandler = textWrittenHandler;
		_nextUpdateTime = Timing.Global.MillisecondsUtc;
		_segmentIndex = 0;
		_offset = 0;
		_lastText = null;
		IsDone = false;
	}

	public void Write(string? soundName)
	{
		if (IsDone)
		{
			return;
		}
		if (_segmentIndex >= _segments.Length)
		{
			End();
		}
		else
		{
			if (Timing.Global.MillisecondsUtc < _nextUpdateTime)
			{
				return;
			}
			bool flag = false;
			while (_nextUpdateTime <= Timing.Global.MillisecondsUtc)
			{
				if (_segmentIndex >= _segments.Length)
				{
					End();
					return;
				}
				flag |= _offset % ClientConfiguration.Instance.TypewriterSoundFrequency == 0;
				ColorizedText colorizedText = _segments[_segmentIndex];
				if (_offset >= colorizedText.Text.Length)
				{
					_offset = 0;
					_segmentIndex++;
					if (_segmentIndex >= _segments.Length)
					{
						End();
						continue;
					}
					colorizedText = _segments[_segmentIndex];
				}
				string text2;
				if (char.IsSurrogatePair(colorizedText.Text, _offset))
				{
					string text = colorizedText.Text;
					int offset = _offset;
					text2 = text.Substring(offset, _offset + 2 - offset);
					_offset += 2;
				}
				else
				{
					string text3 = colorizedText.Text;
					int offset = _offset;
					text2 = text3.Substring(offset, _offset + 1 - offset);
					_offset++;
				}
				_nextUpdateTime += GetTypingDelayFor(text2, _lastText);
				_textWrittenHandler(text2, colorizedText.Color);
				_lastText = text2;
			}
			if (flag && !string.IsNullOrEmpty(soundName))
			{
				Audio.StopAllGameSoundsOf(ClientConfiguration.Instance.TypewriterSounds.ToArray());
				Audio.AddGameSound(soundName, loop: false);
			}
		}
	}

	private static long GetTypingDelayFor(string next, string? last)
	{
		if (last == null)
		{
			return TypingSpeed;
		}
		if (next == "\n")
		{
			return FullStopSpeed;
		}
		if (next == last)
		{
			return TypingSpeed;
		}
		if (FullStops.Contains(last))
		{
			return FullStopSpeed;
		}
		if (!PartialStops.Contains(last))
		{
			return TypingSpeed;
		}
		return PartialStopSpeed;
	}

	public void End()
	{
		if (IsDone)
		{
			return;
		}
		while (_segmentIndex < _segments.Length)
		{
			ColorizedText colorizedText = _segments[_segmentIndex];
			if (_offset < colorizedText.Text.Length)
			{
				TextWrittenHandler textWrittenHandler = _textWrittenHandler;
				string text = colorizedText.Text;
				int offset = _offset;
				textWrittenHandler(text.Substring(offset, text.Length - offset), colorizedText.Color);
			}
			_segmentIndex++;
			_offset = 0;
		}
		IsDone = true;
		DoneAtMilliseconds = Timing.Global.MillisecondsUtc;
	}
}
