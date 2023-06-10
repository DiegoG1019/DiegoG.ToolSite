using System.Collections;

namespace DiegoG.ToolSite.Shared.Types;

public struct ErrorList
{
    public List<string>? Errors { get; set; }

    public bool HasErrors => Errors?.Count > 0;

    public IEnumerable<string> AsEnumerable()
        => Errors?.Count is > 0 ? Errors : Enumerable.Empty<string>();
}

public static class ErrorListExtensions
{
    public static void AddError(this ref ErrorList errors, string error)
        => (errors.Errors ??= new()).Add(error);
}