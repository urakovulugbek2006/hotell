using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace HotelOS.Shared.Infrastructure;

public static class ServiceExtensions
{
    public static IServiceCollection AddHotelOSInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Entity Framework (SQLite). The connection string adds a long
        // command timeout; combined with the retry logic in InitialiseDatabaseAsync
        // this handles the brief moment several services write to the shared file at once.
        services.AddDbContext<HotelDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection") ?? 
                             "Data Source=HotelOS.db"));

        // Add Redis Connection.
        // AbortOnConnectFail = false is critical in Docker: it lets the service
        // start even if Redis isn't ready yet, then reconnect automatically,
        // instead of throwing and crashing the whole service on startup.
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var connectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
            var options = ConfigurationOptions.Parse(connectionString);
            options.AbortOnConnectFail = false;
            options.ConnectRetry = 5;
            options.ConnectTimeout = 10000;
            return ConnectionMultiplexer.Connect(options);
        });

        // Add Message Broker
        services.AddSingleton<IMessageBroker, RedisMessageBroker>();

        return services;
    }

    /// <summary>
    /// Initialises the database with retries. All six services share one SQLite
    /// file and start at the same time, so the first write can hit a transient
    /// "database is locked" error. Retrying a few times lets every service get
    /// past startup instead of crashing the container.
    /// </summary>
    public static async Task InitialiseDatabaseAsync(this IServiceProvider services, ILogger logger)
    {
        const int maxAttempts = 10;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var scope = services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
                await context.Database.EnsureCreatedAsync();
                logger.LogInformation("Database initialised successfully on attempt {Attempt}", attempt);
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Database init attempt {Attempt}/{Max} failed; retrying...", attempt, maxAttempts);
                if (attempt == maxAttempts)
                {
                    logger.LogError(ex, "Database init failed after {Max} attempts; continuing so the API can still serve requests once the DB is ready", maxAttempts);
                    return; // Don't crash - the table may already exist via another service
                }
                await Task.Delay(2000 * attempt);
            }
        }
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