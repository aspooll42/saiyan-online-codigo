using System;
using System.IO;
using System.Text;
using Intersect.IO;

namespace Intersect;

public static class Console
{
	private static readonly TextWriter mDefaultError;

	private static readonly TextWriter mDefaultOut;

	public static bool InputHistoryEnabled
	{
		get
		{
			return Context.InputHistoryEnabled;
		}
		set
		{
			Context.InputHistoryEnabled = value;
		}
	}

	public static int InputHistoryLength
	{
		get
		{
			return Context.InputHistoryLength;
		}
		set
		{
			Context.InputHistoryLength = value;
		}
	}

	public static ConsoleColor BackgroundColor
	{
		get
		{
			return System.Console.BackgroundColor;
		}
		set
		{
			System.Console.BackgroundColor = value;
		}
	}

	public static ConsoleColor ForegroundColor
	{
		get
		{
			return System.Console.ForegroundColor;
		}
		set
		{
			System.Console.ForegroundColor = value;
		}
	}

	public static int BufferHeight
	{
		get
		{
			return System.Console.BufferHeight;
		}
		set
		{
			System.Console.BufferHeight = value;
		}
	}

	public static int BufferWidth
	{
		get
		{
			return System.Console.BufferWidth;
		}
		set
		{
			System.Console.BufferWidth = value;
		}
	}

	public static int WindowLeft
	{
		get
		{
			return System.Console.WindowLeft;
		}
		set
		{
			System.Console.WindowLeft = value;
		}
	}

	public static int WindowTop
	{
		get
		{
			return System.Console.WindowTop;
		}
		set
		{
			System.Console.WindowTop = value;
		}
	}

	public static int WindowHeight
	{
		get
		{
			return System.Console.WindowHeight;
		}
		set
		{
			System.Console.WindowHeight = value;
		}
	}

	public static int WindowWidth
	{
		get
		{
			return System.Console.WindowWidth;
		}
		set
		{
			System.Console.WindowWidth = value;
		}
	}

	public static int LargestWindowHeight => System.Console.LargestWindowHeight;

	public static int LargestWindowWidth => System.Console.LargestWindowWidth;

	public static int CursorLeft
	{
		get
		{
			return System.Console.CursorLeft;
		}
		set
		{
			int num = System.Console.CursorTop;
			int num2 = value;
			if (num2 < 0)
			{
				num--;
				num2 += BufferWidth;
			}
			else if (num2 > BufferWidth)
			{
				num++;
				num2 -= BufferWidth;
			}
			SetCursorPosition(num2, num);
		}
	}

	public static int CursorTop
	{
		get
		{
			return System.Console.CursorTop;
		}
		set
		{
			System.Console.CursorTop = value;
		}
	}

	public static int CursorSize
	{
		get
		{
			return System.Console.CursorSize;
		}
		set
		{
			System.Console.CursorSize = value;
		}
	}

	public static bool CursorVisible
	{
		get
		{
			return System.Console.CursorVisible;
		}
		set
		{
			System.Console.CursorVisible = value;
		}
	}

	public static string Title
	{
		get
		{
			return System.Console.Title;
		}
		set
		{
			System.Console.Title = value;
		}
	}

	public static Encoding InputEncoding
	{
		get
		{
			return System.Console.InputEncoding;
		}
		set
		{
			System.Console.InputEncoding = value;
		}
	}

	public static Encoding OutputEncoding
	{
		get
		{
			return System.Console.OutputEncoding;
		}
		set
		{
			System.Console.OutputEncoding = value;
		}
	}

	public static bool KeyAvailable => System.Console.KeyAvailable;

	public static bool NumberLock => System.Console.NumberLock;

	public static bool CapsLock => System.Console.CapsLock;

	public static bool TreatControlCAsInput
	{
		get
		{
			return System.Console.TreatControlCAsInput;
		}
		set
		{
			System.Console.TreatControlCAsInput = value;
		}
	}

	private static ConsoleContext Context { get; }

	private static ConsoleWriter ContextOut { get; }

	public static string WaitPrefix
	{
		get
		{
			return Context.WaitPrefix;
		}
		set
		{
			Context.WaitPrefix = value;
		}
	}

	public static bool IsInputRedirected => System.Console.IsInputRedirected;

	public static bool IsOutputRedirected => System.Console.IsOutputRedirected;

	public static bool IsErrorRedirected => System.Console.IsErrorRedirected;

	public static TextReader In
	{
		get
		{
			return System.Console.In;
		}
		set
		{
			SetIn(value);
		}
	}

	public static TextWriter Out
	{
		get
		{
			return System.Console.Out;
		}
		set
		{
			SetOut(value);
		}
	}

	public static TextWriter Error
	{
		get
		{
			return System.Console.Error;
		}
		set
		{
			SetError(value);
		}
	}

	public static event ConsoleCancelEventHandler CancelKeyPress;

	static Console()
	{
		if (!OperatingSystem.IsBrowser())
		{
			System.Console.CancelKeyPress += delegate(object? sender, ConsoleCancelEventArgs args)
			{
				Console.CancelKeyPress?.Invoke(sender, args);
			};
		}
		mDefaultError = Error;
		mDefaultOut = Out;
		Context = new ConsoleContext();
	}

	public static void ResetColor()
	{
		System.Console.ResetColor();
	}

	public static void SetBufferSize(int width, int height)
	{
		System.Console.SetBufferSize(width, height);
	}

	public static void SetWindowPosition(int left, int top)
	{
		System.Console.SetWindowPosition(left, top);
	}

	public static void SetWindowSize(int width, int height)
	{
		System.Console.SetWindowSize(width, height);
	}

	public static void SetCursorPosition(int left, int top)
	{
		int num = Math.Max(0, top * BufferWidth + left);
		int num2 = ((BufferWidth == 0) ? 1 : BufferWidth);
		System.Console.SetCursorPosition(num % num2, num / num2);
	}

	public static void Beep()
	{
		System.Console.Beep();
	}

	public static void Beep(int frequency, int duration)
	{
		System.Console.Beep(frequency, duration);
	}

	public static ConsoleKeyInfo ReadKey()
	{
		return System.Console.ReadKey();
	}

	private static ConsoleKeyInfo ReadKeySkipPrint()
	{
		ContextOut.SkipNextWriteChar = true;
		return ReadKey();
	}

	public static ConsoleKeyInfo ReadKey(bool intercept)
	{
		return System.Console.ReadKey(intercept);
	}

	public static int Read()
	{
		return System.Console.Read();
	}

	public static string ReadLine()
	{
		return System.Console.ReadLine();
	}

	public static ConsoleKeyInfo ReadKeyWait()
	{
		Context.Check();
		ConsoleKeyInfo result = Context.Wait(ContextOut.TextWriter.Write, ReadKey);
		Context.Clear();
		return result;
	}

	public static string ReadLine(bool withPrefix)
	{
		Context.Check();
		if (!withPrefix)
		{
			return ReadLine();
		}
		string result = Context.BufferedReadLine(ContextOut.TextWriter.Write, ContextOut.TextWriter.WriteLine, ReadKeySkipPrint);
		Context.Clear();
		return result;
	}

	public static void Clear()
	{
		System.Console.Clear();
		Context.WritePrefix(Out.Write);
	}

	public static void WriteLine()
	{
		System.Console.WriteLine();
	}

	public static void WriteLine(string format, params object[] arg)
	{
		System.Console.WriteLine(format, arg);
	}

	public static void WriteLine(char[] buffer)
	{
		System.Console.WriteLine(buffer);
	}

	public static void WriteLine(char[] buffer, int index, int count)
	{
		System.Console.WriteLine(buffer, index, count);
	}

	public static void WriteLine(bool value)
	{
		System.Console.WriteLine(value);
	}

	public static void WriteLine(char value)
	{
		System.Console.WriteLine(value);
	}

	public static void WriteLine(decimal value)
	{
		System.Console.WriteLine(value);
	}

	public static void WriteLine(double value)
	{
		System.Console.WriteLine(value);
	}

	public static void WriteLine(float value)
	{
		System.Console.WriteLine(value);
	}

	public static void WriteLine(int value)
	{
		System.Console.WriteLine(value);
	}

	public static void WriteLine(uint value)
	{
		System.Console.WriteLine(value);
	}

	public static void WriteLine(long value)
	{
		System.Console.WriteLine(value);
	}

	public static void WriteLine(ulong value)
	{
		System.Console.WriteLine(value);
	}

	public static void WriteLine(object value)
	{
		System.Console.WriteLine(value);
	}

	public static void WriteLine(string value)
	{
		System.Console.WriteLine(value);
	}

	public static void Write(string format, params object[] arg)
	{
		System.Console.Write(format, arg);
	}

	public static void Write(char[] buffer)
	{
		System.Console.Write(buffer);
	}

	public static void Write(char[] buffer, int index, int count)
	{
		System.Console.Write(buffer, index, count);
	}

	public static void Write(bool value)
	{
		System.Console.Write(value);
	}

	public static void Write(char value)
	{
		System.Console.Write(value);
	}

	public static void Write(double value)
	{
		System.Console.Write(value);
	}

	public static void Write(decimal value)
	{
		System.Console.Write(value);
	}

	public static void Write(float value)
	{
		System.Console.Write(value);
	}

	public static void Write(int value)
	{
		System.Console.Write(value);
	}

	public static void Write(uint value)
	{
		System.Console.Write(value);
	}

	public static void Write(long value)
	{
		System.Console.Write(value);
	}

	public static void Write(ulong value)
	{
		System.Console.Write(value);
	}

	public static void Write(object value)
	{
		System.Console.Write(value);
	}

	public static void Write(string value)
	{
		System.Console.Write(value);
	}

	public static Stream OpenStandardError()
	{
		return System.Console.OpenStandardError();
	}

	public static Stream OpenStandardError(int bufferSize)
	{
		return System.Console.OpenStandardError(bufferSize);
	}

	public static Stream OpenStandardInput()
	{
		return System.Console.OpenStandardInput();
	}

	public static Stream OpenStandardInput(int bufferSize)
	{
		return System.Console.OpenStandardInput(bufferSize);
	}

	public static Stream OpenStandardOutput()
	{
		return System.Console.OpenStandardOutput();
	}

	public static Stream OpenStandardOutput(int bufferSize)
	{
		return System.Console.OpenStandardOutput(bufferSize);
	}

	public static void SetIn(TextReader newIn)
	{
		System.Console.SetIn(newIn);
	}

	public static void SetOut(TextWriter newOut)
	{
		System.Console.SetOut(newOut);
	}

	public static void SetError(TextWriter newError)
	{
		System.Console.SetError(newError);
	}
}
