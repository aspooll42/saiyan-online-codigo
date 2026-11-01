using System.Collections.Generic;

namespace Intersect.Core;

public interface ICommandLineOptions
{
	string WorkingDirectory { get; }

	IEnumerable<string> PluginDirectories { get; }
}
