using System;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Animations;

public class AnimationDescriptor : DatabaseObject<AnimationDescriptor>, IFolderable
{
	public AnimationLayer Lower { get; set; }

	public AnimationLayer Upper { get; set; }

	public string Sound { get; set; }

	public bool CompleteSound { get; set; }

	public string Folder { get; set; } = string.Empty;

	[JsonConstructor]
	public AnimationDescriptor(Guid id)
		: base(id)
	{
		base.Name = "New Animation";
		Lower = new AnimationLayer();
		Upper = new AnimationLayer();
	}

	public AnimationDescriptor()
	{
		base.Name = "New Animation";
		Lower = new AnimationLayer();
		Upper = new AnimationLayer();
	}
}
