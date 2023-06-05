using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DiegoG.ToolSite.Shared.Models;

namespace DiegoG.ToolSite.Shared.JsonConverters;

public class SessionIdJsonConverter : JsonConverter<SessionId>
{
    public override SessionId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => SessionId.Parse(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, SessionId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());

    public static SessionIdJsonConverter Instance { get; } = new();
}
