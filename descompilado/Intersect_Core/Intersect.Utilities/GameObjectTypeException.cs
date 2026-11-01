using System;

namespace Intersect.Utilities;

public class GameObjectTypeException : Exception
{
	public GameObjectTypeException()
	{
	}

	public GameObjectTypeException(string message)
		: base(message)
	{
	}
}
