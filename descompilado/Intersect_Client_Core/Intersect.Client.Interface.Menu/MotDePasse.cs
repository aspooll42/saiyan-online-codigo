namespace Intersect.Client.Interface.Menu;

internal static class MotDePasse
{
	public const int Minimum = 8;

	public static bool TropCourt(string? motDePasse)
	{
		return (motDePasse?.Length ?? 0) < 8;
	}
}
