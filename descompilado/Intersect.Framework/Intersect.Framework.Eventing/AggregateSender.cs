namespace Intersect.Framework.Eventing;

public sealed record AggregateSender : IAggregateSender
{
	public object CurrentSender { get; }

	public object? OriginalSender { get; }

	public AggregateSender(object currentSender, object? originalSender)
	{
		CurrentSender = currentSender;
		OriginalSender = originalSender;
	}
}
public sealed class AggregateSender<T> : IAggregateSender<T>, IAggregateSender where T : notnull
{
	public T CurrentSender { get; }

	public object? OriginalSender { get; }

	public AggregateSender(T currentSender, object? originalSender)
	{
		CurrentSender = currentSender;
		OriginalSender = originalSender;
	}
}
