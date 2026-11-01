namespace Intersect.Config;

public class SmtpSettings
{
	public string FromAddress { get; set; } = string.Empty;

	public string FromName { get; set; } = string.Empty;

	public string Host { get; set; } = string.Empty;

	public int Port { get; set; } = 587;

	public bool UseSsl { get; set; } = true;

	public string Username { get; set; } = string.Empty;

	public string Password { get; set; } = string.Empty;

	public bool IsValid()
	{
		if (!string.IsNullOrEmpty(FromAddress))
		{
			return !string.IsNullOrEmpty(Host);
		}
		return false;
	}
}
