using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DiegoG.ToolSite.Server.Database.Models.Base;

public readonly record struct Id<TModel> : IEquatable<Id<TModel>> where TModel : class, IKeyed<TModel>
{
    public Guid Identification { get; }

    public Id(Guid identification)
    {
        Identification = identification;
    }

    public static Id<TModel> New()
        => new(Guid.NewGuid());

    public static implicit operator Guid(Id<TModel> id)
        => id.Identification;

    public static explicit operator Id<TModel>(Guid id)
        => new(id);

    public override string ToString()
        => Identification.ToString();

    public static Id<TModel> Parse(string id)
        => new(Guid.Parse(id));

    public static bool TryParse(string s, out Id<TModel> id)
    {
        if (Guid.TryParse(s, out var guid))
        {
            id = new(guid);
            return true;
        }
        id = default;
        return false;
    }

    public static ValueConverter<Id<TModel>, Guid> Converter { get; } = new IdValueConverter();

    private class IdValueConverter : ValueConverter<Id<TModel>, Guid>
    {
        public IdValueConverter()
            : base(
                  x => x.Identification,
                  x => new(x)
            )
        {
        }
    }
}