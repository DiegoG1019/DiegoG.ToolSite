using DiegoG.ToolSite.Shared.Models.Responses.Base;

namespace DiegoG.ToolSite.Shared.Models.Responses;

public class LedgerInsertionResponse : APIResponse
{
    public LedgerInsertionResponse() : base(ResponseCodeEnum.LedgerInsertionResponse) { }

    public required ICollection<PutResult> Modifications { get; init; }
    public required ICollection<PutResult> Additions { get; init; }
}