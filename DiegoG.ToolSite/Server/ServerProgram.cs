using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using DiegoG.ToolSite.Server.Logging.Sinks;
using DiegoG.ToolSite.Server.Middleware;
using DiegoG.ToolSite.Shared.Logging.Enrichers;
using System;
using DiegoG.REST.ASPNET;
using DiegoG.REST.Json;
using DiegoG.ToolSite.Shared.Models.Responses.Base;
using DiegoG.REST;
using System.Net;
using System.Threading.RateLimiting;
using System.Text;
using DiegoG.ToolSite.Server.Database;
using DiegoG.ToolSite.Server.Database.Models.Base;
using DiegoG.ToolSite.Shared.Services;
using DiegoG.ToolSite.Shared.JsonConverters;

namespace DiegoG.ToolSite.Server;

public static class ServerProgram
{
    private static AppSettings? settings;

    public static IConfiguration Configuration { get; }
    public static IServiceProvider Services { get; }
    public static WebApplication App { get; }
    public static ServerInfo Server { get; }
    public static JsonSerializerOptions JsonOptions { get; }

    public static AppSettings Settings
    {
        get => settings!;
        private set => settings = value ?? throw new ArgumentNullException(nameof(value));
    }

    static ServerProgram()
    {
        DatabaseSink.RegisterExitEvent();
        Helper.CreateAppDataDirectory();

        var builder = WebApplication.CreateBuilder(Environment.GetCommandLineArgs());
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog();
        builder.Host.UseSerilog();

        // Add services to the container.
        var services = builder.Services;
        var conf = builder.Configuration;

        Configuration = builder.Configuration;

        foreach (var (k, v) in Configuration.GetRequiredSection("LogSettings").Get<Dictionary<string, LogConfig>>()
            ?? throw new InvalidDataException("Could not bind LogSettings"))
        {
            if (k is null)
                continue;
            LogHelper.Configurations[k] = v;
        }

        LogHelper.DefaultFormat = p => $"[{{Timestamp:yyyy-MM-dd hh:mm:ss.fffffffzzz.fff zzz}} [{{Level:u3}}] (Area: {{Area}}) (Logger: {{LoggerName}}){(string.IsNullOrWhiteSpace(p) ? $" {p}" : "")}]{{NewLine}} > {{Message:lj}}{{NewLine}}{{Exception}}";

        var exceptionDump = Configuration.GetValue<string>("LogSettings:ExceptionDump");
        if (string.IsNullOrWhiteSpace(exceptionDump))
            throw new ArgumentException("ExceptionDump must not be null or only whitespace");
        Directory.CreateDirectory(exceptionDump);

        LogHelper.AddEnricher(new ExceptionDumper(exceptionDump));
        LogHelper.AddConfigurator(
            (c, f, la, ln, conf) =>
            {
                c.WriteTo.Console(conf.Console, f)
                 .WriteTo.File(conf.FileLocation, conf.File, f)
                 .MinimumLevel.Is(conf.Minimum);

                //if (OperatingSystem.IsWindows())
                //    c.WriteTo.EventLog(Settings.ClientName, "Application", ".", true, f, null, conf.Syslog);
                if (OperatingSystem.IsLinux())
                    c.WriteTo.LocalSyslog(Settings.ClientName, outputTemplate: f, restrictedToMinimumLevel: conf.Syslog);
            }
        );

        var log = LogHelper.CreateLogger("Init", "Program..ctor");

        builder.Configuration.AddJsonFile("appsettings.secret.json");

        log.Information("Deserializing Settings");
        var settings = builder.Configuration.GetRequiredSection("Settings").Get<AppSettings>() ?? throw new InvalidDataException("Could not bind AppSettings from Configuration");

        log.Debug("Validating settings");
        settings.Validate();

        log.Verbose("Assigning Settings to Program.Settings");
        Settings = settings;

        services.ConfigureHttpJsonOptions(x => x.SerializerOptions.Converters.Add(SessionIdJsonConverter.Instance));

        JsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };
        JsonOptions.Converters.Add(SessionIdJsonConverter.Instance);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddRateLimiter(o =>
        {
            o.OnRejected = async (c, ct) =>
            {
                using (var scope = Services!.CreateScope())
                {
                    if (!c.HttpContext.Response.HasStarted)
                    {
                        var serializer = scope.ServiceProvider.GetRequiredService<IRESTObjectSerializer<ResponseCode>>();
                        var sb = new StringBuilder(200);
                        sb.AppendJoin("; ", serializer.MIMETypes);
                        string charset = serializer.Charset;
                        if (charset != null)
                            sb.Append("; charset=").Append(charset);

                        c.HttpContext.Response.ContentType = sb.ToString();
                        await serializer.SerializeAsync(TooManyRequestsResponse.Instance, c.HttpContext.Response.Body);
                    }
                }
            };

            o.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    var userAgent = httpContext.Request.Headers.UserAgent.ToString();

                    return RateLimitPartition.GetFixedWindowLimiter
                    (userAgent, _ =>
                        new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 10,
                            Window = TimeSpan.FromSeconds(5)
                        });
                }),
                PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    var userAgent = httpContext.Request.Headers.UserAgent.ToString();

                    return RateLimitPartition.GetConcurrencyLimiter
                    (userAgent, _ =>
                        new ConcurrencyLimiterOptions
                        {
                            PermitLimit = 3,
                            QueueLimit = 3,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        });
                })
            );
        });

        log.Debug("Registering DiegoG.ToolSite Services");
        services.RegisterToolSiteServices(log);

        log.Debug("Registering DiegoG.ToolSite Workers");
        services.AddToolSiteWorkers(log);

        log.Debug("Adding Razor Page Services");
        services.AddRazorPages();

        log.Debug("Adding Controller Services");
        services.AddControllers();

        log.Debug("Adding REST Object Serializers");
        services.AddRESTObjectSerializer<ResponseCode>(new JsonRESTSerializer<ResponseCode>(JsonOptions));

        log.Debug("Adding REST Object Type Tables");
        services.AddRESTObjectTypeTable<ResponseCode>(new APIResponseTypeTable());

        log.Debug("Registering REST Object Validation Filter");
        services.UseRESTObjectValidationFilter<ResponseCode>();

        log.Debug("Registering REST InvalidModelState Response Filter");
        services.UseRESTInvalidModelStateResponse<ResponseCode>(ac => new ErrorResponse(ac.ModelState.Select(x => $"{x.Key}: {x.Value}")) { TraceId = ac.HttpContext.TraceIdentifier }.ToResult(HttpStatusCode.BadRequest));

        var dbk = builder.Configuration.GetValue<DatabaseKind>("DatabaseKind");
        services.AddDbContext<ToolSiteContext>(dbk switch
        {
            DatabaseKind.SQLite => o => o.UseSqlite(conf.GetFormattedConnectionString("ToolSiteContext")),
            DatabaseKind.SQLServer => o => o.UseSqlServer(conf.GetFormattedConnectionString("ToolSiteContext")),
            _ => throw new InvalidDataException($"Unknown database kind {dbk}")
        });
        log.Information("Registered EntityFramework for Context {context} powered by {dbk}", nameof(ToolSiteContext), dbk);

        //services.AddScoped<IStorageProvider>(x => new FileSystemStorageProvider("Dynamic"));

        log.Information("Building AppHost");
        log.Verbose("Assigning AppHost to Program.App");
        App = builder.Build();

        Configuration = App.Configuration;

        log.Debug("Registering settings change callback");
        App.Configuration.GetReloadToken().RegisterChangeCallback(x =>
        {
            var s = Settings;
            try
            {
                Log.Debug("Reloading settings");
                var settings = App.Configuration.GetRequiredSection("Settings").Get<AppSettings>() ?? throw new InvalidDataException("Could not bind AppSettings from Configuration");
                settings.Validate();
                Settings = settings;
                Log.Verbose("Succesfully reloaded settings");
            }
            catch (Exception e)
            {
                Log.Error(e, "An error ocurred while reloading AppSettings");
                Settings = s;
            }
        }, null);

        log.Debug("Assigning Service Collection to Program.Services");
        Services = App.Services;

        LogHelper.AddConfigurator(
            (c, f, la, ln, conf) => c.WriteTo.Sink(new DatabaseSink(20, conf.Database, () => Services))
        );

        log.Debug("Registering ExceptionLogger middleware");
        App.UseMiddleware<ExceptionLogger>();

        // Configure the HTTP request pipeline.
        if (App.Environment.IsDevelopment())
        {
            log.Debug("Registering DEBUG REST Exception Handler middleware");
            App.UseRESTExceptionHandler<ResponseCode>((r, e, s, c)
                => Task.FromResult(
                    new ExceptionRESTResponse<ResponseCode>(
                        e is not null
                        ? new ErrorResponse(e.Message, e.StackTrace!)
                        {
                            TraceId = c.TraceIdentifier
                        }
                        : new ErrorResponse()
                        {
                            TraceId = c.TraceIdentifier
                        },
                        HttpStatusCode.InternalServerError
                    )
                ));

            log.Debug("Configuring debug settings");
            App.UseWebAssemblyDebugging();
        }
        else
        {
            log.Debug("Registering REST Exception Handler middleware");
            App.UseRESTExceptionHandler<ResponseCode>((r, e, s, c) 
                => Task.FromResult(
                    new ExceptionRESTResponse<ResponseCode>(
                        new ErrorResponse()
                        {
                            TraceId = c.TraceIdentifier
                        },
                        HttpStatusCode.InternalServerError
                    )
                ));

            log.Debug("Configuring release settings");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            App.UseHsts();
        }

        log.Debug("Configuring routing");
        App.UseRouting();

        log.Debug("Registering Swagger");
        App.UseSwagger();
        App.UseSwaggerUI();

        log.Debug("Mapping controllers");
        App.MapControllers();

        log.Debug("Configuring the use of Blazor Framework files");
        App.UseBlazorFrameworkFiles();

        log.Debug("Configuring HTTPS redirection");
        App.UseHttpsRedirection();

        log.Debug("Configuring static files");
        App.UseStaticFiles();

        log.Debug("Mapping Razor Pages");
        App.MapRazorPages();

        log.Debug("Setting fallback to file index.html");
        App.MapFallbackToFile("index.html");

        log.Debug("Creating Server Data");
        Server = new()
        {
            Id = Id<ServerInfo>.New(),
            Name = Settings.ClientName,
            Registered = DateTimeOffset.Now
        };
        log.Debug("Created server data under: {name} ({id})", Server.Name, Server.Id);

        log.Information("Finished Initialization");
    }

    private static Task Main(string[] args)
        => App.RunAsync();
}
