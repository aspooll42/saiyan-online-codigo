using Intersect.Enums;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class ChatReprisePacket : IntersectPacket
{
	[Key(0)]
	public long MessageId { get; set; }

	[Key(1)]
	public string Texte { get; set; } = string.Empty;

	[Key(2)]
	public EtatDeTraduction Etat { get; set; } = EtatDeTraduction.Traduit;

	public ChatReprisePacket()
	{
	}

	public ChatReprisePacket(long messageId, string texte, EtatDeTraduction etat = EtatDeTraduction.Traduit)
	{
		MessageId = messageId;
		Texte = texte ?? string.Empty;
		Etat = etat;
	}
}
