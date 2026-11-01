using Intersect.Client.Framework.Audio;
using Microsoft.Xna.Framework.Audio;

namespace Intersect.Client.MonoGame.Audio;

public class MonoSoundInstance : MonoAudioInstance<MonoSoundSource, SoundEffectInstance>
{
	public MonoSoundInstance(MonoSoundSource source)
		: base((GameAudioSource)source, source.Effect?.CreateInstance())
	{
	}
}
