using Intersect.Client.Framework.Gwen.Control.EventArguments;

namespace Intersect.Client.Interface.Data;

public interface IDataProvider
{
	object? Value { get; }

	event DataProviderEventHandler<ValueChangedEventArgs<object?>>? ValueChanged;
}
public interface IDataProvider<TValue> : IDataProvider
{
	object? IDataProvider.Value => Value;

	new TValue Value { get; }

	new event DataProviderEventHandler<ValueChangedEventArgs<TValue>>? ValueChanged;
}
