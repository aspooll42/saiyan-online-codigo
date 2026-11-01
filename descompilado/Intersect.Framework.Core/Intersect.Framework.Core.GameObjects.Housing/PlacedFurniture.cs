using System;

namespace Intersect.Framework.Core.GameObjects.Housing;

public readonly record struct PlacedFurniture(Guid Id, FurnitureDescriptor Furniture, int Variant, bool Mirror, int X, int Y);
