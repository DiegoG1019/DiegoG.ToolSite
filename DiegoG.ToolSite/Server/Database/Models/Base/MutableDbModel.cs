namespace DiegoG.ToolSite.Server.Database.Models.Base;

public abstract class MutableDbModel
{
    public DateTimeOffset CreationDate { get; set; }
    public DateTimeOffset LastModifiedDate { get; set; }
}
