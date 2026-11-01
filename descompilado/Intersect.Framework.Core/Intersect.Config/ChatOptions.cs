namespace Intersect.Config;

public class ChatOptions
{
	public int MaxChatLength { get; set; } = 120;

	public int MinIntervalBetweenChats { get; set; } = 400;

	public bool ShowAnnouncementBanners { get; set; } = true;

	public int AnnouncementDisplayDuration { get; set; } = 5000;

	public ChatTraductionOptions Traduction { get; set; } = new ChatTraductionOptions();
}
