using DiegoG.ToolSite.Shared.Models.Responses.Base;

namespace DiegoG.ToolSite.Shared.Models.Responses;

public class SessionInformationResponse : APIResponse
{
    public SessionInformationResponse() : base(ResponseCodeEnum.SessionInformationResponse) { }

    public required SessionId SessionId { get; set; }
    public required string LoggedInAs { get; set; }
    public required DateTimeOffset LoggedInSince { get; set; }
    public bool IsAnonymous { get; set; }
}