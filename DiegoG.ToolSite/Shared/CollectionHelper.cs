using DiegoG.ToolSite.Shared.Types;

namespace DiegoG.ToolSite.Shared;

public static class CollectionHelper
{
    public static HashSet<string> NewCaseInsensitiveHashSet() => new(InvariantCaseInsensitiveStringComparer.Instance);
}
