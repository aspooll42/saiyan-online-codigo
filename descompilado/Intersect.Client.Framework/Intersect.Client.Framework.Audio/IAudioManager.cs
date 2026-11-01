using System;
using Intersect.Client.Framework.Core.Sounds;
using Intersect.Client.Framework.Entities;

namespace Intersect.Client.Framework.Audio;

public interface IAudioManager
{
	IMapSound PlayMapSound(string filename, int x, int y, Guid mapId, bool loop, int loopInterval, int distance, IEntity parent = null);

	ISound PlaySound(string filename, bool loop);

	void StopSound(ISound sound);

	void StopSound(IMapSound sound);

	void StopAllSounds();

	void PlayMusic(string filename, int fadeout = 0, int fadein = 0, bool loop = false);

	void StopMusic(int fadeout = 0);
}
