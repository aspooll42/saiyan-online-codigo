using System;

namespace Intersect.Client.Plugins.Interfaces;

public class TimedArgs : EventArgs
{
	public TimeSpan Delta { get; }

	public TimedArgs(TimeSpan delta)
	{
		Delta = delta;
	}
}
