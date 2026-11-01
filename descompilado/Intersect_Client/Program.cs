using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Intersect.Client.Core;
using Intersect.Framework.Reflection;

[CompilerGenerated]
internal class Program
{
	private static void Main(string[] args)
	{
		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		Console.WriteLine($"Starting {executingAssembly.GetMetadataName()} in {Environment.CurrentDirectory}...\n\t{string.Join(' ', args)}");
		Intersect.Client.Core.Program.Main(Assembly.GetExecutingAssembly(), args);
	}
}
