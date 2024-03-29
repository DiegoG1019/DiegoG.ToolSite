using DiegoG.REST;
using DiegoG.REST.Json;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Serilog;
using DiegoG.ToolSite.Client.Types;
using DiegoG.ToolSite.Client.Workers;
using DiegoG.ToolSite.Shared.JsonConverters;
using DiegoG.ToolSite.Shared.Logging.Enrichers;
using DiegoG.ToolSite.Shared.Models.Responses.Base;
using Serilog.Events;
using DiegoG.ToolSite.Shared;

namespace DiegoG.ToolSite.Client;
public static class ClientProgram
{
    public static JsonSerializerOptions JsonOptions { get; } = SharedStatic.JsonOptions;

    public static IServiceProvider Services { get; }
    public static WebAssemblyHost Host { get; }
    public static string BaseAddress { get; }

    static ClientProgram()
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(Environment.GetCommandLineArgs());
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog();

        var services = builder.Services;

        LogHelper.DefaultFormat = p => $"[{{Timestamp:yyyy-MM-dd hh:mm:ss.fffffffzzz.fff zzz}} [{{Level:u3}}] (Area: {{Area}}) (Logger: {{LoggerName}}){(string.IsNullOrWhiteSpace(p) ? $" {p}" : "")}]{{NewLine}} > {{Message:lj}}{{NewLine}}{{Exception}}";

        LogHelper.AddConfigurator(
            (c, f, la, ln, conf) =>
                c.WriteTo.Sink(new BrowserSink(conf.Console), conf.Console)
                 .WriteTo.Sink(new ServerSink(conf.File, f), conf.File)
                 .MinimumLevel.Is(conf.Minimum)
        );

        LogHelper.Configurations["Default"] = new(LogEventLevel.Verbose, LogEventLevel.Debug, LogEventLevel.Information, LogEventLevel.Information, "");

        services.AddHostedService<BackgroundTaskSweeper>();

        services.AddSingleton<IRESTObjectSerializer<ResponseCode>>(new JsonRESTSerializer<ResponseCode>(JsonOptions));
        services.AddSingleton<RESTObjectTypeTable<ResponseCode>>(new APIResponseTypeTable());

        BaseAddress = builder.HostEnvironment.BaseAddress;

        services.AddScoped(sp =>
        {
            var client = new HttpClient(new HttpCachingHandler())
            {
                BaseAddress = new Uri(BaseAddress),
            };

            var sessions = sp.GetRequiredService<SessionManager>();
            if (sessions.CurrentUser?.Session is SessionId sid)
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {sid}");

            return client;
        });

        services.RegisterClientServices();

        Host = builder.Build();
        Services = Host.Services;
    }

    public static Task Main(string[] args)
        => Host.RunAsync();
}
