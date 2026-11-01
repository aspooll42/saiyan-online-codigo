using System;
using System.Threading;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Input;

namespace Intersect.Client.Framework.Gwen.Platform;

public static class Neutral
{
	private static DateTime sFirstTime = DateTime.Now;

	public static void SetCursor(Cursor cursor)
	{
		Cursor.Current = cursor;
	}

	public static string GetClipboardText()
	{
		string ret = string.Empty;
		Thread thread = new Thread((ThreadStart)delegate
		{
			try
			{
				if (GameClipboard.Instance != null && !GameClipboard.Instance.IsEmpty)
				{
					ret = GameClipboard.Instance.GetText();
				}
			}
			catch (Exception)
			{
			}
		});
		thread.Start();
		thread.Join();
		return ret;
	}

	public static bool SetClipboardText(string text)
	{
		bool ret = false;
		Thread thread = new Thread((ThreadStart)delegate
		{
			try
			{
				if (GameClipboard.Instance != null)
				{
					GameClipboard.Instance.SetText(text);
					ret = true;
				}
			}
			catch (Exception)
			{
			}
		});
		try
		{
			thread.SetApartmentState(ApartmentState.STA);
		}
		catch (PlatformNotSupportedException)
		{
		}
		thread.Start();
		thread.Join();
		return ret;
	}

	public static float GetTimeInSeconds()
	{
		return (float)(DateTime.Now - sFirstTime).TotalSeconds;
	}

	public static bool FileOpen(string title, string startPath, string extension, Action<string> callback)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Title = title,
			InitialDirectory = startPath,
			DefaultExt = "*.*",
			Filter = extension,
			CheckPathExists = true,
			Multiselect = false
		};
		if (openFileDialog.ShowDialog() == DialogResult.Ok)
		{
			callback?.Invoke(openFileDialog.FileName);
			return true;
		}
		callback?.Invoke(string.Empty);
		return false;
	}

	public static bool FileSave(string title, string startPath, string extension, Action<string> callback)
	{
		SaveFileDialog saveFileDialog = new SaveFileDialog
		{
			Title = title,
			InitialDirectory = startPath,
			DefaultExt = "*.*",
			Filter = extension,
			CheckPathExists = true,
			OverwritePrompt = true
		};
		if (saveFileDialog.ShowDialog() == DialogResult.Ok)
		{
			callback?.Invoke(saveFileDialog.FileName);
			return true;
		}
		callback?.Invoke(string.Empty);
		return false;
	}
}
