using System;
using Intersect.Client.Core;
using Intersect.Client.Framework.Audio;
using Intersect.Client.Framework.Core.Sounds;
using Intersect.Client.Framework.Entities;

namespace Intersect.Client.Plugins.Audio;

public class AudioManager : IAudioManager
{
	public IMapSound PlayMapSound(string filename, int x, int y, Guid mapId, bool loop, int loopInterval, int distance, IEntity parent = null)
	{
		return Intersect.Client.Core.Audio.AddMapSound(filename, x, y, mapId, loop, loopInterval, distance, parent);
	}

	public ISound PlaySound(string filename, bool loop)
	{
		return Intersect.Client.Core.Audio.AddGameSound(filename, loop);
	}

	public void StopSound(ISound sound)
	{
		Intersect.Client.Core.Audio.StopSound(sound as IMapSound);
	}

	public void StopSound(IMapSound sound)
	{
		Intersect.Client.Core.Audio.StopSound(sound);
	}

	public void StopAllSounds()
	{
		Intersect.Client.Core.Audio.StopAllSounds();
	}

	public void PlayMusic(string filename, int fadeout = 0, int fadein = 0, bool loop = false)
	{
		Intersect.Client.Core.Audio.PlayMusic(filename, fadeout, fadein, loop);
	}

	public void StopMusic(int fadeout = 0)
	{
		Intersect.Client.Core.Audio.StopMusic(fadeout);
	}
}
