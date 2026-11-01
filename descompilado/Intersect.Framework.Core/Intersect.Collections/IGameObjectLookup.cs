using System;
using System.Collections;
using System.Collections.Generic;
using Intersect.Models;

namespace Intersect.Collections;

public interface IGameObjectLookup<TValue> : ILookup<Guid, TValue>, IEnumerable<KeyValuePair<Guid, TValue>>, IEnumerable where TValue : INamedObject
{
}
