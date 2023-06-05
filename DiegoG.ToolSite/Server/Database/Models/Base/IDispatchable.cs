namespace DiegoG.ToolSite.Server.Database.Models.Base;

public interface IDispatchable
{
    public DateTimeOffset? ClaimedAt { get; set; }
    public ServerInfo? ClaimedBy { get; set; }
    public DateTimeOffset? DispatchedAt { get; set; }
}
