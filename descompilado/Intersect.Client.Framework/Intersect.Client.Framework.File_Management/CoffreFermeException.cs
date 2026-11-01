using System.IO;

namespace Intersect.Client.Framework.File_Management;

public sealed class CoffreFermeException : IOException
{
	public string Chemin { get; }

	public CoffreFermeException(string chemin)
		: base("« " + chemin + " » ne s'ouvre qu'après la connexion")
	{
		Chemin = chemin;
	}
}
