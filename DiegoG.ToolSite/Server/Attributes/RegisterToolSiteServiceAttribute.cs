using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;

namespace DiegoG.ToolSite.Server.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class RegisterToolSiteServiceAttribute : Attribute
{
    public ServiceLifetime Lifetime { get; }
    public Type? ServiceType { get; }

    public RegisterToolSiteServiceAttribute(ServiceLifetime lifetime, Type? serviceType = null)
    {
        Lifetime = lifetime;
        ServiceType = serviceType;
    }
}

public static class ToolSiteServiceExtensions
{
    public static void AddToolSiteWorkers(this IServiceCollection services, ILogger? logger = null)
    {
        var log = logger ?? Log.Logger;

        log.Information("Registering DiegoG.ToolSite Workers");

        foreach (var (type, attr) in AppDomain.CurrentDomain
                            .GetAssemblies()
                            .SelectMany(x => x.GetTypes())
                            .Select(x => (Type: x, Attr: x.GetCustomAttribute<RegisterToolSiteWorkerAttribute>()))
                            .Where(x => x.Attr != null))
        {
            if (type.IsAssignableTo(typeof(IHostedService)) is false)
                throw new InvalidDataException("Classes decorated with RegisterToolSiteWorkerAttribute must implement IHostedService");

            log.Debug("Registering worker {workerType}", type);
            services.TryAddEnumerable(
                new ServiceDescriptor(
                    typeof(IHostedService),
                    type,
                    ServiceLifetime.Singleton
                )
            );
        }
    }

    public static void RegisterToolSiteServices(this IServiceCollection services, ILogger? logger = null)
    {
        var log = logger ?? Log.Logger;

        log.Information("Registering DiegoG.ToolSite Services");
        foreach (var serv in AppDomain.CurrentDomain
                            .GetAssemblies()
                            .SelectMany(x => x.GetTypes())
                            .Select(x => (Type: x, Attr: x.GetCustomAttribute<RegisterToolSiteServiceAttribute>()))
                            .Where(x => x.Attr != null))
        {
            var st = serv.Attr!.ServiceType ?? serv.Type;
            var it = serv.Type;
            var lt = serv.Attr!.Lifetime;

            log?.Debug("Registering service for {serviceType} implemented by type {implementationType} under lifetime {lifetime}", st, it, lt);
            services.Add(
                new ServiceDescriptor(
                    st,
                    it,
                    lt
                )
            );
        }
    }
}
