namespace DiegoG.ToolSite.Server.Database.Models.Base;

public interface IKeyed<TModel> where TModel : class, IKeyed<TModel>
{
    public Id<TModel> Id { get; }
}
