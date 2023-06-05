namespace DiegoG.ToolSite.Server.Database.Models.Base;

public class ServerInfo : IKeyed<ServerInfo>
{
    public required Id<ServerInfo> Id { get; init; }
    public required string Name { get; init; }
    public required DateTimeOffset Registered { get; init; }
    public DateTimeOffset LastHeartbeat { get; set; }
    public TimeSpan HeartbeatInterval { get; set; }
}
