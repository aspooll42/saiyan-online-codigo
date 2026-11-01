namespace Intersect.Client.Framework.Entities;

public interface IFriendInstance
{
	string? Map { get; set; }

	string Name { get; set; }

	bool Online { get; set; }

	string? Sprite { get; set; }

	string? Face { get; set; }

	int Level { get; set; }

	int LastSeenMinutes { get; set; }
}
