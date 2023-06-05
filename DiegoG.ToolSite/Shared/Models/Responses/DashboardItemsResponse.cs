using DiegoG.ToolSite.Shared.Models.Responses.Base;

namespace DiegoG.ToolSite.Shared.Models.Responses;

public class DashboardItemsResponse : APIResponse
{
    public DashboardItemsResponse() : base(ResponseCodeEnum.DashboardItemsResponse) { }

    public required IEnumerable<ServiceItemDescription> ServiceItems { get; init; }
}
