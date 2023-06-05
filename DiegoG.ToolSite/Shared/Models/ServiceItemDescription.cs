using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiegoG.ToolSite.Shared.Models;

public class ServiceItemDescription
{
    public required string BigIcon { get; init; }
    public required string SmallIcon { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Uri { get; init; }
}
