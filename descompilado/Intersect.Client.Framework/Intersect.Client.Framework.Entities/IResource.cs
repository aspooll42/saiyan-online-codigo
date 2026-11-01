using System;
using Intersect.Framework.Core.GameObjects.Resources;

namespace Intersect.Client.Framework.Entities;

public interface IResource : IEntity, IDisposable
{
	ResourceDescriptor? Descriptor { get; }
}
