using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Intersect.Framework.Collections;

public class ConcurrentConditionalDequeue<TValue> : IReadOnlyCollection<TValue>, IEnumerable<TValue>, IEnumerable, IProducerConsumerCollection<TValue>, ICollection
{
	private static readonly Func<TValue, TValue, bool> IsEqual = (typeof(TValue).IsValueType ? new Func<TValue, TValue, bool>(StructEquals) : ((Func<TValue, TValue, bool>)((TValue a, TValue b) => (object)a == (object)b)));

	private readonly object _dequeueLock = new object();

	private readonly ConcurrentQueue<TValue> _queue = new ConcurrentQueue<TValue>();

	private ICollection QueueAsGenericCollection => _queue;

	private IProducerConsumerCollection<TValue> QueueAsProducerConsumerCollection => _queue;

	public bool IsReadOnly => false;

	bool ICollection.IsSynchronized => QueueAsProducerConsumerCollection.IsSynchronized;

	object ICollection.SyncRoot => QueueAsProducerConsumerCollection.SyncRoot;

	public int Count => _queue.Count;

	public void CopyTo(TValue[] array, int arrayIndex)
	{
		_queue.CopyTo(array, arrayIndex);
	}

	public TValue[] ToArray()
	{
		return _queue.ToArray();
	}

	bool IProducerConsumerCollection<TValue>.TryAdd(TValue item)
	{
		return QueueAsProducerConsumerCollection.TryAdd(item);
	}

	bool IProducerConsumerCollection<TValue>.TryTake([MaybeNullWhen(false)] out TValue item)
	{
		lock (_dequeueLock)
		{
			return QueueAsProducerConsumerCollection.TryTake(out item);
		}
	}

	void ICollection.CopyTo(Array array, int index)
	{
		QueueAsGenericCollection.CopyTo(array, index);
	}

	public IEnumerator<TValue> GetEnumerator()
	{
		return _queue.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public void Clear()
	{
		lock (_dequeueLock)
		{
			_queue.Clear();
		}
	}

	public void Enqueue(TValue item)
	{
		_queue.Enqueue(item);
	}

	public bool TryDequeueIf(Func<TValue, bool> consumer)
	{
		TValue value;
		return TryDequeueIf(consumer, out value);
	}

	public bool TryDequeueIf(Func<TValue, bool> consumer, [NotNullWhen(true)] out TValue? value)
	{
		ArgumentNullException.ThrowIfNull(consumer, "consumer");
		lock (_dequeueLock)
		{
			if (!_queue.TryPeek(out value))
			{
				return false;
			}
			if (value == null)
			{
				return false;
			}
			if (!consumer(value))
			{
				return false;
			}
			if (!_queue.TryDequeue(out var result))
			{
				throw new InvalidOperationException("Dequeue failed, collection may have been removed from or cleared without being locked.");
			}
			if (!IsEqual(value, result))
			{
				throw new InvalidOperationException("Unexpected dequeued value, collection may have been removed from or cleared without being locked.");
			}
			return true;
		}
	}

	public bool TryPeek([NotNullWhen(true)] out TValue? value)
	{
		lock (_dequeueLock)
		{
			return _queue.TryPeek(out value) && value != null;
		}
	}

	private static bool StructEquals(TValue a, TValue b)
	{
		return a.Equals(b);
	}
}
