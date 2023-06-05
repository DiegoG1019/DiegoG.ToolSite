using System.ComponentModel.DataAnnotations;

namespace DiegoG.ToolSite.Server.Database.Models.Base;

[PrimaryKey(nameof(OwnerId), nameof(Label))]
public class Tag<T> where T : class, IKeyed<T>
{
    public required Id<T> OwnerId { get; init; }
    
    public required string Label { get; init; }
    
    public T Owner { get; init; }
}
