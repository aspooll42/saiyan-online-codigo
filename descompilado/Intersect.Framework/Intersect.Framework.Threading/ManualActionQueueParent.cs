namespace Intersect.Framework.Threading;

public sealed class ManualActionQueueParent : IManualActionQueueParent
{
	public bool IsExecuting { get; set; }
}
