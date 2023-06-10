using DiegoG.ToolSite.Shared.Models.Responses.Base;

namespace DiegoG.ToolSite.Shared.Models.Responses;

public class SessionInformationResponse : APIResponse
{
    public SessionInformationResponse() : base(ResponseCodeEnum.SessionInformationResponse) { }

    public required SessionId SessionId { get; init; }
    public required string LoggedInAs { get; init; }
    public required DateTimeOffset LoggedInSince { get; init; }
    public required bool IsAnonymous { get; init; }
    public required UserSettings? Settings { get; init; }
    public required UserPermission Permissions { get; init; }
}