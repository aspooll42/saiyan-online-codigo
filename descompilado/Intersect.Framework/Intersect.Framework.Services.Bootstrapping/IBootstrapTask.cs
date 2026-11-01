using System.Threading;
using System.Threading.Tasks;

namespace Intersect.Framework.Services.Bootstrapping;

public interface IBootstrapTask
{
	Task ExecuteAsync(CancellationToken cancellationToken);
}
