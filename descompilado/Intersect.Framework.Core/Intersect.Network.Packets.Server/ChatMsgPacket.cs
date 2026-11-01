using Intersect.Enums;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class ChatMsgPacket : IntersectPacket
{
	[Key(0)]
	public string Message { get; set; }

	[Key(1)]
	public ChatMessageType Type { get; set; }

	[Key(2)]
	public Color Color { get; set; }

	[Key(3)]
	public string Target { get; set; }

	[Key(4)]
	public long MessageId { get; set; }

	[Key(5)]
	public string Langue { get; set; } = string.Empty;

	[Key(6)]
	public string Original { get; set; } = string.Empty;

	[Key(7)]
	public EtatDeTraduction Etat { get; set; }

	[Key(8)]
	public string PhraseId { get; set; } = string.Empty;

	[Key(9)]
	public string PhraseNom { get; set; } = string.Empty;

	[Key(10)]
	public int CorpsDebut { get; set; }

	public ChatMsgPacket()
	{
	}

	public ChatMsgPacket(string message, ChatMessageType type, Color color, string target, long messageId = 0L, string langue = "", string original = "", EtatDeTraduction etat = EtatDeTraduction.Normal, string phraseId = "", string phraseNom = "", int corpsDebut = 0)
	{
		Message = message;
		Type = type;
		Color = color;
		Target = target;
		MessageId = messageId;
		Langue = langue ?? string.Empty;
		Original = original ?? string.Empty;
		Etat = etat;
		PhraseId = phraseId ?? string.Empty;
		PhraseNom = phraseNom ?? string.Empty;
		CorpsDebut = corpsDebut;
	}
}
