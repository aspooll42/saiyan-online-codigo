using System;
using System.Threading;

namespace Intersect.Core;

public interface IThreadableApplicationService : IApplicationService, IDisposable
{
	Thread Thread { get; }
}
