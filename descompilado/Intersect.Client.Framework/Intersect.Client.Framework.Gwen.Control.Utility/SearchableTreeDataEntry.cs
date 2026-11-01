namespace Intersect.Client.Framework.Gwen.Control.Utility;

public record struct SearchableTreeDataEntry(string Id, string DisplayText, Color? DisplayColor = null, string? Name = null, string? ParentId = null, object? UserData = null);
