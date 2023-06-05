using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodaMoney;

namespace DiegoG.ToolSite.Shared.Models;

public class LedgerEntryItem
{
    public required Money Money { get; set; }
    public required string? Message { get; set; }
    public required string? Category { get; set; }
    public required string? Recipient { get; set; }
    public required DateTimeOffset Date { get; set; }
    public required List<string> Tags { get; set; } 
}
