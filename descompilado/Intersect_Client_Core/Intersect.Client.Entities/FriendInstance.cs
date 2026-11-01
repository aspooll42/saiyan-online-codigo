using Intersect.Client.Framework.Entities;

namespace Intersect.Client.Entities;

public class FriendInstance : IFriendInstance
{
	public string? Map { get; set; }

	public string Name { get; set; }

	public bool Online { get; set; }

	public string? Sprite { get; set; }

	public string? Face { get; set; }

	public int Level { get; set; }

	public int LastSeenMinutes { get; set; } = -1;
}
