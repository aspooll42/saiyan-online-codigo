using Intersect.Client.Framework.Audio;
using Microsoft.Xna.Framework.Audio;

namespace Intersect.Client.MonoGame.Audio;

public class MonoMusicInstance : MonoAudioInstance<MonoMusicSource, DynamicSoundEffectInstance>
{
	private static MonoMusicInstance? _instance;

	protected override bool UsePlatformLoop => false;

	public MonoMusicInstance(MonoMusicSource source)
		: base((GameAudioSource)source, source.LoadSong())
	{
		if (_instance != null)
		{
			_instance.Stop();
			_instance.Dispose();
			_instance = null;
		}
		_instance = this;
	}
}
