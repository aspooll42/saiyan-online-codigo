using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class ChatPhrasePacket : IntersectPacket
{
	[Key(0)]
	public byte Channel { get; set; }

	[Key(1)]
	public string PhraseId { get; set; } = string.Empty;

	[Key(2)]
	public string PhraseNom { get; set; } = string.Empty;

	public ChatPhrasePacket()
	{
	}

	public ChatPhrasePacket(byte channel, string phraseId, string phraseNom = "")
	{
		Channel = channel;
		PhraseId = phraseId ?? string.Empty;
		PhraseNom = phraseNom ?? string.Empty;
	}
}
