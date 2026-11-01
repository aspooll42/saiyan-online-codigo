using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Framework.Core.GameObjects.Lighting;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Animations;

[Owned]
public class AnimationLayer
{
	public string Sprite { get; set; } = string.Empty;

	public int FrameCount { get; set; } = 1;

	public int XFrames { get; set; } = 1;

	public int YFrames { get; set; } = 1;

	public int FrameSpeed { get; set; } = 100;

	public int LoopCount { get; set; }

	public bool DisableRotations { get; set; }

	public bool AlternateRenderLayer { get; set; }

	[JsonIgnore]
	public string Light
	{
		get
		{
			return JsonConvert.SerializeObject(Lights);
		}
		set
		{
			Lights = JsonConvert.DeserializeObject<LightDescriptor[]>(value);
		}
	}

	[NotMapped]
	public LightDescriptor[] Lights { get; set; }

	public AnimationLayer()
	{
		Lights = new LightDescriptor[FrameCount];
		for (int i = 0; i < FrameCount; i++)
		{
			Lights[i] = new LightDescriptor();
		}
	}
}
