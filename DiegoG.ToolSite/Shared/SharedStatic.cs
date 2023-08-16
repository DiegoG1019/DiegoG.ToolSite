using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DiegoG.ToolSite.Shared.JsonConverters;

namespace DiegoG.ToolSite.Shared;
public static class SharedStatic
{
    static SharedStatic()
    {
        JsonOptions.Converters.Add(SessionIdJsonConverter.Instance);
    }

    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
