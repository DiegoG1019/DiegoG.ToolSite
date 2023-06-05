using DiegoG.ToolSite.Shared.Models.Responses.Base;

namespace DiegoG.ToolSite.Shared.Models.Responses;

public class SuccesfulLoginResponse : APIResponse
{
    public SuccesfulLoginResponse() : base(ResponseCodeEnum.SuccesfullyLoggedInResponse) { }

    public required SessionId SessionId { get; init; }
    public required string LoggedInAs { get; init; }
}