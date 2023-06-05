using System.Diagnostics.CodeAnalysis;

namespace DiegoG.ToolSite.Shared.Types;

public class DelegateEqualityComparer<T> : IEqualityComparer<T>
{
    private readonly Func<T?, T?, bool> comparer;
    private readonly Func<T?, int> hashcode;

    public DelegateEqualityComparer(Func<T?, T?, bool> comparer, Func<T?, int> hashcode)
    {
        this.comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        this.hashcode = hashcode ?? throw new ArgumentNullException(nameof(hashcode));
    }

    public bool Equals(T? x, T? y)
        => comparer(x, y);

    public int GetHashCode([DisallowNull] T obj)
        => hashcode(obj);
}