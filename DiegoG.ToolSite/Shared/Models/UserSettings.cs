using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiegoG.ToolSite.Shared.Models.Responses.Base;

namespace DiegoG.ToolSite.Shared.Models;
public class UserSettings
{
}

public class UserSettingsResponse : APIResponse
{
    public UserSettingsResponse() : base(ResponseCodeEnum.UserSettingsResponse) { }

    public required UserSettings Settings { get; init; }
    public DateTimeOffset LastModified { get; init; }
}