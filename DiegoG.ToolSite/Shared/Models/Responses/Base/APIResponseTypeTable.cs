using DiegoG.REST;
using DiegoG.ToolSite.Shared.Models.Responses;

namespace DiegoG.ToolSite.Shared.Models.Responses.Base;

public enum ResponseCodeEnum
{
    TooManyRequests = -2,
    Error = -1,

    NoResultsResponse = 1,

    DashboardItemsResponse = 10,

    LedgerPageResponse = 20,
    LedgerInsertionResponse = 21,

    SessionInformationResponse = 30,
    SuccesfullyLoggedInResponse = 31,

    UserSettingsResponse = 40
}

public class APIResponseTypeTable : RESTObjectTypeTable<ResponseCode>
{
    public override Type GetTypeFor(ResponseCode code)
        => code.Code switch
        {
            ResponseCodeEnum.TooManyRequests => typeof(TooManyRequestsResponse),
            ResponseCodeEnum.Error => typeof(ErrorResponse),
            ResponseCodeEnum.NoResultsResponse => typeof(NoResultsResponse),

            ResponseCodeEnum.DashboardItemsResponse => typeof(DashboardItemsResponse),

            ResponseCodeEnum.LedgerPageResponse => typeof(LedgerPageResponse),
            ResponseCodeEnum.LedgerInsertionResponse => typeof(LedgerInsertionResponse),

            ResponseCodeEnum.SessionInformationResponse => typeof(SessionInformationResponse),
            ResponseCodeEnum.SuccesfullyLoggedInResponse => typeof(SuccesfulLoginResponse),

            ResponseCodeEnum.UserSettingsResponse => typeof(UserSettingsResponse)
        };

    protected override void AddType(Type type, ResponseCode code)
    {
        throw new NotImplementedException();
    }
}
