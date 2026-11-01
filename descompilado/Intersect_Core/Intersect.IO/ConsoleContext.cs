using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Intersect.IO;

public sealed class ConsoleContext
{
	public static readonly int DefaultInputHistoryLength = 100;

	private readonly List<string> mInputHistory;

	private readonly object mLock;

	private bool mInputHistoryEnabled;

	private int mInputHistoryLength;

	private int mInputHistoryPosition;

	private int WaitCursorLeft { get; set; }

	private int WaitCursorTop { get; set; }

	public bool IsWaitingRead { get; private set; }

	public int InputHistoryPosition
	{
		get
		{
			return mInputHistoryPosition;
		}
		set
		{
			mInputHistoryPosition = Math.Max(0, Math.Min(mInputHistory.Count, value));
		}
	}

	public bool InputHistoryEnabled
	{
		get
		{
			return mInputHistoryEnabled;
		}
		set
		{
			mInputHistoryEnabled = value;
			mInputHistoryLength = (mInputHistoryEnabled ? DefaultInputHistoryLength : 0);
		}
	}

	public int InputHistoryLength
	{
		get
		{
			return mInputHistoryLength;
		}
		set
		{
			mInputHistoryLength = value;
			mInputHistoryEnabled = mInputHistoryLength != 0;
		}
	}

	public bool InputHistoryFull
	{
		get
		{
			if (mInputHistoryLength > 0)
			{
				return mInputHistoryLength <= mInputHistory.Count;
			}
			return false;
		}
	}

	public ImmutableList<string> InputHistory => mInputHistory.ToImmutableList() ?? throw new InvalidOperationException();

	public string WaitPrefix { get; set; }

	private List<char> InputBuffer { get; }

	public ConsoleContext(int inputHistoryLength = 0)
	{
		mLock = new object();
		mInputHistory = new List<string>(Math.Max(0, inputHistoryLength));
		InputHistoryLength = inputHistoryLength;
		InputBuffer = new List<char>(80);
	}

	public void Check()
	{
		if (IsWaitingRead)
		{
			throw new InvalidOperationException("Already waiting on a Read*() operation.");
		}
	}

	public TResult Wait<TResult>(Action<string> write, Func<TResult> waitForResult)
	{
		lock (mLock)
		{
			IsWaitingRead = true;
			WritePrefix(write);
		}
		return waitForResult();
	}

	public void Clear()
	{
		lock (mLock)
		{
			IsWaitingRead = false;
		}
	}

	public void ResetWaitCursor(Action<string> write, int clearBufferLength = -1)
	{
		if (IsWaitingRead)
		{
			bool flag = Console.CursorTop == WaitCursorTop;
			int left = (flag ? WaitCursorLeft : 0);
			int cursorTop = Console.CursorTop;
			Console.SetCursorPosition(left, Console.CursorTop);
			write(new string(' ', (WaitPrefix?.Length ?? 0) + Math.Max(clearBufferLength, InputBuffer.Count)));
			Console.SetCursorPosition(left, cursorTop);
			if (!flag)
			{
				Console.CursorLeft = WaitCursorLeft;
				Console.CursorTop = WaitCursorTop;
			}
		}
	}

	public async Task ResetWaitCursorAsync(Func<string, Task> writeAsync, int clearBufferLength = -1)
	{
		if (IsWaitingRead)
		{
			bool onCurrentLine = Console.CursorTop == WaitCursorTop;
			int realColumnCursor = (onCurrentLine ? WaitCursorLeft : 0);
			int currentLineCursor = Console.CursorTop;
			Console.SetCursorPosition(realColumnCursor, Console.CursorTop);
			Task task = writeAsync(new string(' ', (WaitPrefix?.Length ?? 0) + Math.Max(clearBufferLength, InputBuffer.Count)));
			if (task != null)
			{
				await task;
			}
			Console.SetCursorPosition(realColumnCursor, currentLineCursor);
			if (!onCurrentLine)
			{
				Console.CursorLeft = WaitCursorLeft;
				Console.CursorTop = WaitCursorTop;
			}
		}
	}

	public void MoveWaitCursor(bool subtractPrefix = false)
	{
		WaitCursorTop = Console.CursorTop;
		WaitCursorLeft = Console.CursorLeft;
		if (subtractPrefix)
		{
			WaitCursorLeft -= WaitPrefix?.Length ?? 0;
		}
	}

	public void WritePrefix(Action<string> write, int bufferPosition = -1)
	{
		if (!IsWaitingRead)
		{
			return;
		}
		MoveWaitCursor();
		if (!string.IsNullOrEmpty(WaitPrefix))
		{
			write(WaitPrefix + new string(InputBuffer.ToArray()));
			if (bufferPosition > -1)
			{
				Console.CursorLeft -= Math.Max(0, InputBuffer.Count - bufferPosition);
			}
		}
	}

	public async Task WritePrefixAsync(Func<string, Task> writeAsync)
	{
		if (!IsWaitingRead)
		{
			return;
		}
		MoveWaitCursor();
		if (!string.IsNullOrEmpty(WaitPrefix))
		{
			Task task = writeAsync(WaitPrefix);
			if (task != null)
			{
				await task;
			}
		}
	}

	public string BufferedReadLine(Action<string> write, Action writeLine, Func<ConsoleKeyInfo> readKey)
	{
		return Wait(write, delegate
		{
			InputBuffer.Clear();
			string text = string.Empty;
			bool flag = false;
			while (!flag)
			{
				ConsoleKeyInfo consoleKeyInfo = readKey();
				int num = Math.Max(0, Math.Min(InputBuffer.Count, Console.BufferWidth * (Console.CursorTop - WaitCursorTop) + (Console.CursorLeft - WaitCursorLeft) - (WaitPrefix?.Length ?? 0)));
				int count = InputBuffer.Count;
				switch (consoleKeyInfo.Key)
				{
				case ConsoleKey.Enter:
					text = new string(InputBuffer.ToArray());
					InputBuffer.Clear();
					if (InputHistoryFull)
					{
						mInputHistory.RemoveRange(0, mInputHistory.Count - mInputHistoryLength);
					}
					mInputHistory.Add(text);
					InputHistoryPosition = mInputHistory.Count;
					writeLine();
					MoveWaitCursor();
					flag = true;
					break;
				case ConsoleKey.Backspace:
					num--;
					if (-1 < num && num < InputBuffer.Count)
					{
						InputBuffer.RemoveAt(num);
					}
					break;
				case ConsoleKey.Delete:
					if (num < InputBuffer.Count)
					{
						InputBuffer.RemoveAt(num);
					}
					break;
				case ConsoleKey.UpArrow:
				{
					int inputHistoryPosition = InputHistoryPosition - 1;
					InputHistoryPosition = inputHistoryPosition;
					if (mInputHistory.Count > InputHistoryPosition)
					{
						InputBuffer.Clear();
						InputBuffer.AddRange(mInputHistory[InputHistoryPosition]?.ToCharArray() ?? Array.Empty<char>());
					}
					break;
				}
				case ConsoleKey.DownArrow:
				{
					int inputHistoryPosition = InputHistoryPosition + 1;
					InputHistoryPosition = inputHistoryPosition;
					if (mInputHistory.Count > InputHistoryPosition)
					{
						InputBuffer.Clear();
						InputBuffer.AddRange(mInputHistory[InputHistoryPosition]?.ToCharArray() ?? Array.Empty<char>());
					}
					else if (mInputHistory.Count == InputHistoryPosition && InputHistoryPosition > 0)
					{
						InputBuffer.Clear();
					}
					break;
				}
				case ConsoleKey.LeftArrow:
					num--;
					break;
				case ConsoleKey.RightArrow:
					num++;
					break;
				default:
					InputBuffer.Add(consoleKeyInfo.KeyChar);
					num++;
					break;
				}
				if (!flag)
				{
					ResetWaitCursor(write, count);
					WritePrefix(write, num);
				}
			}
			return text;
		});
	}
}
