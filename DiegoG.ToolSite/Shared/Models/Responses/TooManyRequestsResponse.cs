using DiegoG.ToolSite.Shared.Models.Responses.Base;

namespace DiegoG.ToolSite.Shared.Models.Responses;

public class TooManyRequestsResponse : APIResponse
{
    public TooManyRequestsResponse() : base(ResponseCodeEnum.TooManyRequests)
    { }

    public static TooManyRequestsResponse Instance { get; } = new();
}
