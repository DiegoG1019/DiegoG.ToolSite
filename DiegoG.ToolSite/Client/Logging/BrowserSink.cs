using Serilog.Core;
using Serilog.Events;

namespace DiegoG.ToolSite.Client.Logging;

public class BrowserSink : ILogEventSink
{
    private readonly LogEventLevel MinimumLevel;
    private readonly string Format;

    public BrowserSink(LogEventLevel minimumLevel, string format)
    {
        MinimumLevel = minimumLevel;
        Format = format ?? throw new ArgumentNullException(nameof(format));
    }

    public void Emit(LogEvent logEvent)
    {

    }
}
