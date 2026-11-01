using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Intersect.Collections.Slotting;

public class SlotList<TSlot> : IList<TSlot>, ICollection<TSlot>, IEnumerable<TSlot>, IEnumerable where TSlot : ISlot
{
	private readonly SortedList<int, TSlot> _slots;

	private readonly Func<int, TSlot> _factory;

	public int Capacity
	{
		get
		{
			return _slots.Capacity;
		}
		set
		{
			_slots.Capacity = value;
		}
	}

	public int Count => _slots.Count;

	public bool IsReadOnly => ((IDictionary)_slots).IsReadOnly;

	public TSlot this[int slotIndex]
	{
		get
		{
			if (_slots.TryGetValue(slotIndex, out var value))
			{
				return value;
			}
			value = _factory(slotIndex);
			_slots[slotIndex] = value;
			return value;
		}
		set
		{
			_slots[slotIndex] = value;
		}
	}

	public SlotList(int capacity, Func<int, TSlot> factory)
	{
		_slots = new SortedList<int, TSlot>(capacity);
		_factory = factory;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public IEnumerator<TSlot> GetEnumerator()
	{
		return _slots.Values.GetEnumerator();
	}

	public void Add(TSlot slot)
	{
		if (_slots.TryGetValue(slot.Slot, out var value))
		{
			if (!value.IsEmpty)
			{
				throw new ArgumentException($"Slot {slot.Slot} is already filled with a non-empty slot.", "slot");
			}
			_slots[slot.Slot] = slot;
		}
		else
		{
			_slots.Add(slot.Slot, slot);
		}
	}

	public void Clear()
	{
		_slots.Clear();
	}

	public bool Contains(TSlot slot)
	{
		return _slots.ContainsValue(slot);
	}

	public void CopyTo(TSlot[] array, int arrayIndex)
	{
		_slots.Select((KeyValuePair<int, TSlot> kvp) => kvp.Value).ToArray().CopyTo(array, arrayIndex);
	}

	public bool Remove(TSlot slot)
	{
		return _slots.Remove(slot.Slot);
	}

	public int IndexOf(TSlot slot)
	{
		return _slots.IndexOfValue(slot);
	}

	public void Insert(int slotIndex, TSlot slot)
	{
		if (slotIndex != slot.Slot)
		{
			throw new ArgumentException($"Tried to insert slot {slot.Slot} to slot index {slotIndex}", "slotIndex");
		}
		_slots.Add(slotIndex, slot);
	}

	public void RemoveAt(int slotIndex)
	{
		_slots.Remove(slotIndex);
	}

	public void Ensure(int slotIndex)
	{
		if (!_slots.ContainsKey(slotIndex))
		{
			_slots[slotIndex] = _factory(slotIndex);
		}
	}

	public void FillToCapacity()
	{
		int capacity = Capacity;
		int num = 0;
		while (Count < capacity && num < capacity)
		{
			Ensure(num);
			num++;
		}
	}

	public int FindIndex(Func<TSlot, bool> predicate)
	{
		TSlot[] array = this.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			if (predicate(array[i]))
			{
				return i;
			}
		}
		return -1;
	}
}
