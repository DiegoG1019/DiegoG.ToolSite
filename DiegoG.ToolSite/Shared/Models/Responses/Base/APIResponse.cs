using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiegoG.REST;
using DiegoG.ToolSite.Shared.Models.Responses;

namespace DiegoG.ToolSite.Shared.Models.Responses.Base;

public class APIResponse : RESTObject<ResponseCode>
{
    internal APIResponse(ResponseCode code) : base(code)
    {
    }
}
