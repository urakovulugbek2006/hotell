using HotelOS.Shared.Events;
using HotelOS.Shared.Infrastructure;
using HousekeepingService.EventHandlers;
using HousekeepingService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "HotelOS Housekeeping Service", 
        Version = "v1",
        Description = "Housekeeping Service API for HotelOS - Handles room cleaning, status management, and housekeeper workload tracking"
    });
});

// Add HotelOS Infrastructure
builder.Services.AddHotelOSInfrastructure(builder.Configuration);

// Add Housekeeping Service dependencies
builder.Services.AddScoped<IHousekeepingService, global::HousekeepingService.Services.HousekeepingService>();

// Add Event Handlers
builder.Services.AddScoped<IEventHandler<RoomVacatedEvent>, ReceptionEventHandler>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HotelOS Housekeeping Service v1");
        c.RoutePrefix = string.Empty; // Swagger UI at root
    });
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Set up event subscriptions (guarded so a Redis hiccup can't crash startup)
try
{
    using var scope = app.Services.CreateScope();
    var messageBroker = scope.ServiceProvider.GetRequiredService<IMessageBroker>();
    var receptionEventHandler = scope.ServiceProvider.GetRequiredService<IEventHandler<RoomVacatedEvent>>();

    await messageBroker.SubscribeAsync<RoomVacatedEvent>(EventTopics.RoomVacated, receptionEventHandler.HandleAsync);
    app.Logger.LogInformation("Subscribed to reception events: {Topic}", EventTopics.RoomVacated);
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Failed to subscribe to RoomVacated events at startup - the API will still serve requests");
}

// Ensure database is created (with retry - all services share one SQLite file)
app.Logger.LogInformation("HotelOS Housekeeping Service starting up");
await app.Services.InitialiseDatabaseAsync(app.Logger);

app.Run();