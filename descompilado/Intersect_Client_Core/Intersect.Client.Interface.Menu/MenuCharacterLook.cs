using System.Collections.Generic;

namespace Intersect.Client.Interface.Menu;

internal sealed record MenuCharacterLook(string Sprite, IReadOnlyList<(string Name, Color Tint)> Paperdolls);
