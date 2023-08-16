using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace DiegoG.ToolSite.Shared.Services;

public readonly record struct LogProperty(string Name, object? Value, bool Destructure = false);

public readonly struct LogConfig
{
    public LogConfig(LogEventLevel console, LogEventLevel file, LogEventLevel database, LogEventLevel syslog, string fileLocation)
    {
        Console = console;
        File = file;
        Database = database;
        Syslog = syslog;

        ArgumentNullException.ThrowIfNull(fileLocation);
        FileLocation = LogHelper.AppDataPath is string adp
            ? fileLocation.Replace("{AppData}", adp)
            : fileLocation;
    }

    public LogEventLevel Console { get; init; }
    public LogEventLevel File { get; init; }
    public LogEventLevel Database { get; init; }
    public LogEventLevel Syslog { get; init; }
    public string FileLocation { get; init; }
    public LogEventLevel Minimum => Min(Console, Min(File, Min(Database, Syslog)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static LogEventLevel Min(LogEventLevel a, LogEventLevel b)
        => a > b ? b : a;
}

public static class LogHelper
{
    public delegate void LoggerConfiguratorDelegate(
        LoggerConfiguration configuratiom,
        string Format,
        string? LoggerArea,
        string? LoggerName,
        LogConfig Config
    );

    public static Dictionary<string, LogConfig> Configurations { get; } = new()
    {
        { "Default", new LogConfig(LogEventLevel.Information, LogEventLevel.Information, LogEventLevel.Information, LogEventLevel.Information, "logs") }
    };

    public static LogConfig DefaultConfiguration
    {
        get => Configurations["Default"];
        set => Configurations["Default"] = value;
    }

    private static readonly object Sync = new();
    private static readonly HashSet<Func<ILogEventEnricher>> Enrichers = new();
    private static readonly HashSet<LoggerConfiguratorDelegate> Configurators = new();

    private static Func<string?, string>? defaultFormat;

    [MemberNotNull(nameof(defaultFormat))]
    public static Func<string?, string> DefaultFormat
    {
        get => defaultFormat ?? throw new InvalidOperationException("DefaultFormat has not been set");
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            defaultFormat = value;
        }
    }

    public static string? AppDataPath { get; set; }

    [ThreadStatic]
    private static ILogEventEnricher[]? EnricherBuffer;

    public static ILogger CreateLogger(string? logArea = null, string? loggerName = null, string? propertyFormat = null, params LogProperty[]? properties)
    {
        EnricherBuffer ??= new ILogEventEnricher[1];
        var c = new LoggerConfiguration();

        LogConfig logConfig = logArea is null ? Configurations["Default"] : Configurations.TryGetValue(logArea, out var v) ? v : Configurations["Default"];

        foreach (var conf in Configurators)
            conf(c, DefaultFormat(propertyFormat), logArea, loggerName, logConfig);

        foreach (var enr in Enrichers)
        {
            EnricherBuffer[0] = enr();
            c.Enrich.With(EnricherBuffer);
        }

        if (logArea is not null)
            c.Enrich.WithProperty("Area", logArea);

        if (loggerName is not null)
            c.Enrich.WithProperty("LoggerName", loggerName);

        if (properties is not null)
            foreach (var prop in properties)
                c.Enrich.WithProperty(prop.Name, prop.Value!, prop.Destructure);

        return c.CreateLogger();
    }

    public static void AddConfigurator(LoggerConfiguratorDelegate configurator)
    {
        lock (Sync)
        {
            Configurators.Add(configurator);
        }
    }

    public static void AddEnricher(ILogEventEnricher enricher)
        => AddEnricher(() => enricher);

    public static void AddEnricher(Func<ILogEventEnricher> enricher)
    {
        lock (Sync)
        {
            Enrichers.Add(enricher);
        }
    }

    //private static void ThrowIfFrozen()
    //{
    //    lock (Sync)
    //        if (IsFrozen)
    //            throw new InvalidOperationException("Cannot add enrichers or configurators once the LogHelper has been frozen by being used at least once");
    //}
}
