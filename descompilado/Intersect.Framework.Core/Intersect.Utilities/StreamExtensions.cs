using System;
using System.IO;

namespace Intersect.Utilities;

public static class StreamExtensions
{
	public static void Pipe(this Stream input, Stream output)
	{
		if (input == null)
		{
			throw new ArgumentNullException("input");
		}
		if (output == null)
		{
			throw new ArgumentNullException("output");
		}
		byte[] array = new byte[8192];
		int count;
		while ((count = input.Read(array, 0, array.Length)) > 0)
		{
			output.Write(array, 0, count);
		}
	}
}
