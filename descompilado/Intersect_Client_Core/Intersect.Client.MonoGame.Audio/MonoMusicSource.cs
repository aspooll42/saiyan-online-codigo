using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Intersect.Client.Core;
using Intersect.Client.Framework.Audio;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Chat;
using Intersect.Client.Localization;
using Intersect.Client.Utilities;
using Intersect.Core;
using Intersect.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Audio;
using NVorbis;

namespace Intersect.Client.MonoGame.Audio;

public class MonoMusicSource : GameAudioSource
{
	private static Thread mUnderlyingThread;

	private static readonly object mInstanceLock = new object();

	private static MonoMusicSource? mActiveSource;

	private readonly string mPath;

	private readonly string mRealPath;

	private readonly Func<Stream> mCreateStream;

	private DynamicSoundEffectInstance? _instance;

	private float[]? _echantillons;

	private byte[]? _octets;

	public VorbisReader? Reader { get; set; }

	public DynamicSoundEffectInstance? Instance
	{
		get
		{
			return _instance;
		}
		set
		{
			if (value != _instance)
			{
				_instance = value;
				if (value == null)
				{
					EmitUnloaded();
				}
				else
				{
					EmitLoaded();
				}
			}
		}
	}

	public override bool IsLoaded => Instance != null;

	public override int TypeVolume => (int)((float)Globals.Database.MusicVolume * Math.Clamp(Intersect.Client.Core.Audio.Silence, 0f, 1f));

	public MonoMusicSource(string path, string realPath, string? name = null)
	{
		base.Name = (string.IsNullOrWhiteSpace(name) ? string.Empty : name);
		mPath = path;
		mRealPath = realPath;
		InitializeThread();
	}

	public MonoMusicSource(Func<Stream> createStream, string? name = null)
	{
		base.Name = (string.IsNullOrWhiteSpace(name) ? string.Empty : name);
		mCreateStream = createStream;
		InitializeThread();
	}

	private void InitializeThread()
	{
	}

	public override GameAudioInstance CreateInstance()
	{
		return new MonoMusicInstance(this);
	}

	public override void ReleaseInstance(GameAudioInstance? audioInstance)
	{
		lock (mInstanceLock)
		{
			Reader?.Dispose();
			Reader = null;
			Instance?.Dispose();
			Instance = null;
			mActiveSource = null;
		}
	}

	public DynamicSoundEffectInstance? LoadSong()
	{
		lock (mInstanceLock)
		{
			try
			{
				if (!string.IsNullOrWhiteSpace(mRealPath))
				{
					if (Reader == null)
					{
						if (Globals.ContentManager.MusicPacks != null && Globals.ContentManager.MusicPacks.Contains(Path.GetFileName(mRealPath)))
						{
							Reader = new VorbisReader(Globals.ContentManager.MusicPacks.GetAsset(Path.GetFileName(mRealPath)));
						}
						else if (mCreateStream != null)
						{
							Reader = new VorbisReader(mCreateStream());
						}
						else
						{
							Reader = new VorbisReader(Coffre.Ouvrir(mRealPath));
						}
					}
					if (Instance != null)
					{
						Instance.Dispose();
						Instance = null;
					}
					Instance = new DynamicSoundEffectInstance(Reader.SampleRate, (Reader.Channels == 1) ? AudioChannels.Mono : AudioChannels.Stereo);
					Instance.BufferNeeded += Instance_BufferNeeded;
					mActiveSource = this;
					return Instance;
				}
			}
			catch (Exception exception)
			{
				ApplicationContext.Context.Value?.Logger.LogError(exception, "Error loading '" + mPath + "'.");
				ChatboxMsg.AddMessage(new ChatboxMsg(Strings.Errors.LoadFile.ToString(Strings.Words.LcaseSound) + " [" + mPath + "]", new Color(191, 0, 0), ChatMessageType.Error));
			}
		}
		mActiveSource = this;
		return null;
	}

	private static void EnsureBuffersFilled()
	{
		int num = 3;
		int num2 = 44100;
		int val = 10;
		while (Globals.IsRunning)
		{
			Thread.Sleep(1000 / Math.Max(val, 1));
			lock (mInstanceLock)
			{
				if (mActiveSource == null)
				{
					continue;
				}
				VorbisReader reader = mActiveSource.Reader;
				DynamicSoundEffectInstance instance = mActiveSource.Instance;
				if (reader == null || instance == null || instance.IsDisposed)
				{
					continue;
				}
				float[] array = null;
				while (instance.PendingBufferCount < num)
				{
					if (array == null)
					{
						array = new float[num2];
					}
					int num3 = reader.ReadSamples(array, 0, array.Length);
					if (num3 == 0)
					{
						reader.DecodedPosition = 0L;
						continue;
					}
					byte[] array2 = new byte[num3 << 1];
					for (int i = 0; i < num3; i++)
					{
						short value = (short)MathHelper.Clamp(array[i] * 32767f, -32768f, 32767f);
						byte[] bytes = BitConverter.GetBytes(value);
						for (int j = 0; j < bytes.Length; j++)
						{
							array2[(i << 1) + j] = bytes[j];
						}
					}
					instance.SubmitBuffer(array2, 0, num3 << 1);
				}
			}
		}
	}

	private void Instance_BufferNeeded(object sender, EventArgs e)
	{
		VorbisReader reader = Reader;
		DynamicSoundEffectInstance instance = Instance;
		if (reader == null || instance == null || instance.IsDisposed)
		{
			return;
		}
		long timestamp = Stopwatch.GetTimestamp();
		int num = 0;
		while (instance.PendingBufferCount < 3)
		{
			float[] array = _echantillons ?? (_echantillons = new float[44100]);
			int num2 = LireLaSuite(reader, array);
			if (num2 == 0)
			{
				if (++num > 1)
				{
					break;
				}
				reader.DecodedPosition = 0L;
				continue;
			}
			num = 0;
			int count = num2 << 1;
			byte[] array2 = _octets ?? (_octets = new byte[88200]);
			for (int i = 0; i < num2; i++)
			{
				short num3 = (short)Math.Clamp(array[i] * 32767f, -32768f, 32767f);
				Unsafe.As<byte, short>(ref array2[i << 1]) = num3;
			}
			instance.SubmitBuffer(array2, 0, count);
		}
		ChronoImage.AjouterDepuisLeSon("musique : decodage", Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds);
	}

	internal static int LireLaSuite(VorbisReader reader, float[] tampon)
	{
		int num = tampon.Length;
		if (reader.TotalSamples > 0)
		{
			int num2 = Math.Max(1, reader.Channels);
			long val = Math.Max(0L, reader.TotalSamples - reader.SamplePosition) * num2;
			num = (int)Math.Min(val, tampon.Length);
			num -= num % num2;
		}
		if (num <= 0)
		{
			return 0;
		}
		return reader.ReadSamples(tampon, 0, num);
	}

	~MonoMusicSource()
	{
		ReleaseInstance(null);
	}
}
