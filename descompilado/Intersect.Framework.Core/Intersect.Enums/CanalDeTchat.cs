namespace Intersect.Enums;

public static class CanalDeTchat
{
	public const byte Local = 0;

	public const byte Global = 1;

	public const byte Groupe = 2;

	public const byte Clan = 3;

	public const byte Admin = 4;

	public const byte Prive = 5;

	public const byte Commerce = 6;

	public const byte Monde = 7;

	public static ChatMessageType? Genre(byte canal)
	{
		return canal switch
		{
			0 => ChatMessageType.Local, 
			1 => ChatMessageType.Global, 
			2 => ChatMessageType.Party, 
			3 => ChatMessageType.Guild, 
			4 => ChatMessageType.Admin, 
			6 => ChatMessageType.Trade, 
			7 => ChatMessageType.World, 
			_ => null, 
		};
	}
}
