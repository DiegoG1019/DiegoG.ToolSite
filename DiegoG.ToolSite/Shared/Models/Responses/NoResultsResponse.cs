using DiegoG.ToolSite.Shared.Models.Responses.Base;

namespace DiegoG.ToolSite.Shared.Models.Responses;

public class NoResultsResponse : APIResponse
{
    public NoResultsResponse() : base(ResponseCodeEnum.NoResultsResponse) { }

    public static NoResultsResponse Instance { get; } = new();
}
