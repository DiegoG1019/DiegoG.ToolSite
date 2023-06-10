using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiegoG.ToolSite.Shared.Models.Requests;
public class ContactMessageRequest
{
    public required string Message { get; init; }
    public string? ResponseAddress { get; init; }
    public string? ResponseMedium { get; init; }
}
