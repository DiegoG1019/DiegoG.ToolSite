using System.Collections;

namespace DiegoG.ToolSite.Server.Types;

public struct ErrorList 
{
    public List<string>? Errors { get; set; }

    public IEnumerable<string> AsEnumerable()
        => Errors?.Count is > 0 ? Errors : Enumerable.Empty<string>();
}
