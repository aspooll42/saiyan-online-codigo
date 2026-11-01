using System;
using System.IO;
using Intersect.Client.Framework.Audio;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Chat;
using Intersect.Client.Localization;
using Intersect.Core;
using Intersect.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Audio;

namespace Intersect.Client.MonoGame.Audio;

public class MonoSoundSource : GameAudioSource
{
	private readonly string mPath;

	private readonly string mRealPath;

	private readonly Func<Stream>? _createStream;

	private SoundEffect? _sound;

	private int mInstanceCount;

	public SoundEffect? Effect
	{
		get
		{
			if (_sound == null)
			{
				LoadSound();
			}
			return _sound;
		}
	}

	public override bool IsLoaded => _sound != null;

	public override int TypeVolume => Globals.Database.SoundVolume;

	public MonoSoundSource(string path, string realPath, string? name = null)
	{
		mPath = path;
		mRealPath = realPath;
		base.Name = (string.IsNullOrWhiteSpace(name) ? string.Empty : name);
	}

	public MonoSoundSource(Func<Stream> createStream, string? name = null)
	{
		_createStream = createStream;
		base.Name = (string.IsNullOrWhiteSpace(name) ? string.Empty : name);
	}

	public override GameAudioInstance CreateInstance()
	{
		mInstanceCount++;
		return new MonoSoundInstance(this);
	}

	public override void ReleaseInstance(GameAudioInstance? audioInstance)
	{
		if (mInstanceCount > 0)
		{
			mInstanceCount--;
		}
	}

	private void LoadSound()
	{
		try
		{
			if (_createStream != null)
			{
				using (Stream stream = _createStream())
				{
					_sound = SoundEffect.FromStream(stream);
					return;
				}
			}
			if (Globals.ContentManager.SoundPacks != null && Globals.ContentManager.SoundPacks.Contains(mRealPath))
			{
				using (MemoryStream stream2 = Globals.ContentManager.SoundPacks.GetAsset(mRealPath))
				{
					_sound = SoundEffect.FromStream(stream2);
					return;
				}
			}
			using Stream stream3 = Coffre.Ouvrir(mRealPath);
			_sound = SoundEffect.FromStream(stream3);
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception, "Error loading '" + mPath + "'.");
			ChatboxMsg.AddMessage(new ChatboxMsg(Strings.Errors.LoadFile.ToString(Strings.Words.LcaseSound) + " [" + mPath + "]", new Color(191, 0, 0), ChatMessageType.Error));
		}
	}
}
