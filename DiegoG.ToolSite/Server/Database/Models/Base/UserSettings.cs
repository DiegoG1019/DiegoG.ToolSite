namespace DiegoG.ToolSite.Server.Database.Models.Base;

public class UserSettings : MutableDbModel, IKeyed<User>
{
    public Id<User> Id { get; }
    public User? User { get; }
}
