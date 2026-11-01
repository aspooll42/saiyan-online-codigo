using System;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Housing;

public class CapsuleDescriptor : DatabaseObject<CapsuleDescriptor>, IFolderable
{
	public const int MaxSize = 8;

	public long Price { get; set; } = 20000L;

	public int MaxFurniture { get; set; } = 40;

	public string Texture { get; set; } = string.Empty;

	public int Width { get; set; } = 5;

	public int Height { get; set; } = 5;

	public int DoorX { get; set; } = 2;

	public Guid InteriorMapId { get; set; }

	public int InteriorX { get; set; }

	public int InteriorY { get; set; }

	public int RoomX { get; set; }

	public int RoomY { get; set; }

	public int RoomWidth { get; set; } = 16;

	public int RoomHeight { get; set; } = 10;

	public int WallRows { get; set; } = 2;

	public bool OwnerCanDecorate { get; set; } = true;

	public string Folder { get; set; } = string.Empty;

	[JsonConstructor]
	public CapsuleDescriptor(Guid id)
		: base(id)
	{
		base.Name = "Nouvelle capsule";
	}

	public CapsuleDescriptor()
	{
		base.Name = "Nouvelle capsule";
	}
}
