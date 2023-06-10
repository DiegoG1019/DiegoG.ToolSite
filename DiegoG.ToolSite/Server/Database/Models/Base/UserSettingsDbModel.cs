namespace DiegoG.ToolSite.Server.Database.Models.Base;

public class UserSettingsDbModel : MutableDbModel, IKeyed<User>
{
    public Id<User> Id { get; }
    public User? User { get; }
}
