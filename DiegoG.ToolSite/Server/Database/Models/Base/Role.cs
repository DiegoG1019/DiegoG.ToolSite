namespace DiegoG.ToolSite.Server.Database.Models.Base;

public class Role : IKeyed<Role>
{
    public required Id<Role> Id { get; init; }
    public required string Name { get; set; }
    public required UserPermission UserPermissions { get; set; }
    public HashSet<User> Users { get; } = new();
}