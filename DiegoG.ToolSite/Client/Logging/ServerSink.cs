using Serilog.Core;
using Serilog.Events;

namespace DiegoG.ToolSite.Client.Logging;

public class ServerSink : ILogEventSink
{
    private readonly LogEventLevel MinimumLevel;
    private readonly string Format;

    public ServerSink(LogEventLevel minimumLevel, string format)
    {
        MinimumLevel = minimumLevel;
        Format = format ?? throw new ArgumentNullException(nameof(format));
    }

    public void Emit(LogEvent logEvent)
    {

    }
}