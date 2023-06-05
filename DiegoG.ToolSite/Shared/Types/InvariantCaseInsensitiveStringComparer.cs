using System.Diagnostics.CodeAnalysis;

namespace DiegoG.ToolSite.Shared.Types;

public class InvariantCaseInsensitiveStringComparer : IComparer<string>, IEqualityComparer<string>
{
    private InvariantCaseInsensitiveStringComparer() { }

    public static InvariantCaseInsensitiveStringComparer Instance { get; } = new();

    public int Compare(string? x, string? y)
        => string.Compare(x, y, StringComparison.CurrentCultureIgnoreCase);

    public bool Equals(string? x, string? y)
        => string.Equals(x, y, StringComparison.CurrentCultureIgnoreCase);

    public int GetHashCode([DisallowNull] string obj)
        => string.GetHashCode(obj, StringComparison.CurrentCultureIgnoreCase);
}
