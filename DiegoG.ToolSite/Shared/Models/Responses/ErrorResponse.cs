using System.Text.Json.Serialization;
using DiegoG.ToolSite.Shared.Models.Responses.Base;

namespace DiegoG.ToolSite.Shared.Models.Responses;

public class ErrorResponse : APIResponse
{
    [JsonConstructor]
    public ErrorResponse(IEnumerable<string> errors) : base(ResponseCodeEnum.Error)
    {
        Errors = errors ?? Array.Empty<string>();
    }

    public ErrorResponse(params string[] errors) : base(ResponseCodeEnum.Error)
    {
        Errors = errors ?? Array.Empty<string>();
    }

    public IEnumerable<string> Errors { get; }

    public required string? TraceId { get; init; }
}
