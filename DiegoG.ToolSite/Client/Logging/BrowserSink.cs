using System.Text;
using Microsoft.JSInterop;
using Serilog.Core;
using Serilog.Events;

namespace DiegoG.ToolSite.Client.Logging;

public class BrowserSink : ILogEventSink
{
    private class LogMessage
    {
        public string Timestamp { get; set; }
        public string Area { get; set; }
        public string Logger { get; set; }
        public Exception? Exception { get; set; }
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
    private static LogMessage? LogMessageBuffer;

    public async void Emit(LogEvent logEvent)
    {
        if (logEvent.Level < MinimumLevel) return;

        IJSRuntime js = ClientProgram.Services.GetRequiredService<IJSRuntime>();
        LogParamBuffer ??= new object[2];
        var lmsg = LogMessageBuffer ??= new();

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

        LogParamBuffer[0] = $"{{{logEvent.Timestamp:yyyy-MM-dd hh:mm:ss}}} [{level}] > {logEvent.RenderMessage()}";

        lmsg.Area = logEvent.Properties["Area"].ToString();
        lmsg.Exception = logEvent.Exception;
        lmsg.Timestamp = logEvent.Timestamp.ToString("yyyy-MM-dd hh:mm:ss.fffffffzzz.fff zzz", null);
        lmsg.Logger = logEvent.Properties["LoggerName"].ToString();

        LogParamBuffer[1] = lmsg;
        
        if (logEvent.Level > LogEventLevel.Warning)
            await js.InvokeVoidAsync("console.error", LogParamBuffer);
        else if (logEvent.Level == LogEventLevel.Warning)
            await js.InvokeVoidAsync("console.warn", LogParamBuffer);
        else
            await js.InvokeVoidAsync("console.log", LogParamBuffer);
    }
}
