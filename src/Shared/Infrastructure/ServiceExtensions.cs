using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace HotelOS.Shared.Infrastructure;

public static class ServiceExtensions
{
    public static IServiceCollection AddHotelOSInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Entity Framework
        services.AddDbContext<HotelDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection") ?? 
                             "Data Source=HotelOS.db"));

        // Add Redis Connection
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var connectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
            return ConnectionMultiplexer.Connect(connectionString);
        });

        // Add Message Broker
        services.AddSingleton<IMessageBroker, RedisMessageBroker>();

        // Add Health Checks
        services.AddHealthChecks()
            .AddDbContextCheck<HotelDbContext>()
            .AddRedis(configuration.GetConnectionString("Redis") ?? "localhost:6379");

        return services;
    }

    public static IServiceCollection AddEventHandlers(this IServiceCollection services, Type assemblyMarkerType)
    {
        var assembly = assemblyMarkerType.Assembly;
        
        // Find all event handler implementations
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && 
                       t.GetInterfaces().Any(i => i.IsGenericType && 
                                               i.GetGenericTypeDefinition() == typeof(IEventHandler<>)))
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            var interfaceTypes = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>));

            foreach (var interfaceType in interfaceTypes)
            {
                services.AddScoped(interfaceType, handlerType);
            }
        }

        return services;
    }
}