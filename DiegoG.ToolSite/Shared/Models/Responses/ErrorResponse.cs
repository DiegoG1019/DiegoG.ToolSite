using DiegoG.ToolSite.Shared.Models.Responses.Base;

namespace DiegoG.ToolSite.Shared.Models.Responses;

public class ErrorResponse : APIResponse
{
    public ErrorResponse(IEnumerable<string> errors) : base(ResponseCodeEnum.Error)
    {
        Errors = errors ?? Array.Empty<string>();
    }

    public ErrorResponse(params string[] errors) : base(ResponseCodeEnum.Error)
    {
        Errors = errors ?? Array.Empty<string>();
    }

    public IEnumerable<string> Errors { get; }

    public string? TraceId { get; init; }
}
