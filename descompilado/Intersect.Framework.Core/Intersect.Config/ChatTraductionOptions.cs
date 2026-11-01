namespace Intersect.Config;

public class ChatTraductionOptions
{
	public bool Lancer { get; set; } = true;

	public string Dossier { get; set; } = string.Empty;

	public bool Active { get; set; } = true;

	public int Port { get; set; } = 5490;

	public int DelaiMs { get; set; } = 1500;

	public int ReprisesMs { get; set; } = 500;

	public int MemoireReprisesS { get; set; } = 60;
}
