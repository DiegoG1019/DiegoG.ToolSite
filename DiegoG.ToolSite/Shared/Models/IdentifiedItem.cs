namespace DiegoG.ToolSite.Shared.Models;

public readonly record struct IdentifiedItem<TItem>(Guid TargetId, TItem Item);