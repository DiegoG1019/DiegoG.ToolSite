using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;

namespace DiegoG.ToolSite.Client.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class RegisterClientServiceAttribute : Attribute
{
    public ServiceLifetime Lifetime { get; }
    public Type? ServiceType { get; }

    public RegisterClientServiceAttribute(ServiceLifetime lifetime, Type? serviceType = null)
    {
        Lifetime = lifetime;
        ServiceType = serviceType;
    }
}

public static class ToolSiteServiceExtensions
{
    public static void RegisterClientServices(this IServiceCollection services, ILogger? logger = null)
    {
        var log = logger ?? Log.Logger;

        log.Information("Registering DiegoG.ToolSite Services");
        foreach (var serv in AppDomain.CurrentDomain
                            .GetAssemblies()
                            .SelectMany(x => x.GetTypes())
                            .Select(x => (Type: x, Attr: x.GetCustomAttribute<RegisterClientServiceAttribute>()))
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
