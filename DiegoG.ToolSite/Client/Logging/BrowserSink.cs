using System.Text;
using Microsoft.JSInterop;
using Serilog.Core;
using Serilog.Events;

namespace DiegoG.ToolSite.Client.Logging;

public class BrowserSink : ILogEventSink
{
    private class LogExceptionMessage
    {
        public string Message { get; set; }
        public string? StackTrace { get; set; }
    }

    //(string Timestamp, string Area, string Logger, Exception? Exception)
    private readonly LogEventLevel MinimumLevel;

    public BrowserSink(LogEventLevel minimumLevel)
    {
        MinimumLevel = minimumLevel;
    }

    [ThreadStatic]
    private static object[]? LogParamBuffer;

    [ThreadStatic]
    private static LogExceptionMessage? LogExceptionMessageBuffer;

    public async void Emit(LogEvent logEvent)
    {
        if (logEvent.Level < MinimumLevel) return;

        IJSRuntime js = ClientProgram.Services.GetRequiredService<IJSRuntime>();
        LogParamBuffer ??= new object[2];

        var level = logEvent.Level switch
        {
            LogEventLevel.Verbose => "VRB",
            LogEventLevel.Debug => "DBG",
            LogEventLevel.Information => "INF",
            LogEventLevel.Warning => "WRN",
            LogEventLevel.Error => "ERR",
            LogEventLevel.Fatal => "FTL",
            _ => throw new ArgumentException($"Unknown LogEventLevel {logEvent.Level}", nameof(logEvent))
        };

        LogParamBuffer[0] = $"{{{logEvent.Timestamp:yyyy-MM-dd hh:mm:ss}}} ({logEvent.Properties["Area"]}) ({logEvent.Properties["LoggerName"]}) [{level}] > {logEvent.RenderMessage()}";

        LogParamBuffer[1] = null!;
        if (logEvent.Exception is Exception e)
        {
            var lemb = LogExceptionMessageBuffer ??= new();
            lemb.Message = e.Message;
            lemb.StackTrace = e.StackTrace;
            LogParamBuffer[1] = lemb;
        }
        
        if (logEvent.Level > LogEventLevel.Warning)
            await js.InvokeVoidAsync("console.error", LogParamBuffer);
        else if (logEvent.Level == LogEventLevel.Warning)
            await js.InvokeVoidAsync("console.warn", LogParamBuffer);
        else
            await js.InvokeVoidAsync("console.log", LogParamBuffer);
    }
}
