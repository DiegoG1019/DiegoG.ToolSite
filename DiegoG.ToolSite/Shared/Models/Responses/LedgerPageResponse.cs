using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiegoG.ToolSite.Shared.Models.Responses.Base;

namespace DiegoG.ToolSite.Shared.Models.Responses;

public class LedgerPageResponse : APIResponse
{
    public LedgerPageResponse(IEnumerable<IdentifiedItem<LedgerEntryItem>> ledgerEntries) : base(ResponseCodeEnum.LedgerPageResponse)
    {
        LedgerEntries = ledgerEntries ?? throw new ArgumentNullException(nameof(ledgerEntries));
    }

    public IEnumerable<IdentifiedItem<LedgerEntryItem>> LedgerEntries { get; }
}
