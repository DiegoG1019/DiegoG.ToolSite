using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiegoG.ToolSite.Shared.Models.Requests;
public class LedgerChangesRequest
{
    public required IdentifiedItem<LedgerEntryItem>[]? Changes { get; set; }
    public required LedgerEntryItem[]? NewEntries { get; set; }
}
