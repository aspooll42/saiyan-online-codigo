using System;
using System.Collections.Generic;
using Intersect.Collections;
using Intersect.Framework.Core.GameObjects.PlayerClass;
using Intersect.Models;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class CreateCharacterPacket : IntersectPacket
{
	[Key(0)]
	public string Name { get; set; }

	[Key(1)]
	public Guid ClassId { get; set; }

	[Key(2)]
	public int Sprite { get; set; }

	public CreateCharacterPacket()
	{
	}

	public CreateCharacterPacket(string name, Guid classId, int sprite)
	{
		Name = name;
		ClassId = classId;
		Sprite = sprite;
	}

	public override Dictionary<string, SanitizedValue<object>> Sanitize()
	{
		base.Sanitize();
		Sanitizer sanitizer = default(Sanitizer);
		ClassDescriptor classDescriptor = DatabaseObject<ClassDescriptor>.Get(ClassId);
		if (classDescriptor != null)
		{
			Sprite = sanitizer.Clamp("Sprite", Sprite, 0, classDescriptor.Sprites?.Count ?? 0);
		}
		return sanitizer.Sanitized;
	}
}
