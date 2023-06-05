namespace DiegoG.ToolSite.Shared.Models;

public readonly record struct PutResult(int Number, bool Success, string? Error = null);
