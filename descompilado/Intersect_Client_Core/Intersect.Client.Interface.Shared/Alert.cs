namespace Intersect.Client.Interface.Shared;

public record struct Alert(string Message, string? Title = null, AlertType Type = AlertType.Unknown);
