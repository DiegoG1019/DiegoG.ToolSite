using DiegoG.ToolSite.Shared.Types;

namespace DiegoG.ToolSite.Server;

public static class CollectionHelper
{
    public static HashSet<string> NewCaseInsensitiveHashSet() => new(InvariantCaseInsensitiveStringComparer.Instance);
}
