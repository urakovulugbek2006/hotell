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
builder.Services.AddScoped<IHousekeepingService, HousekeepingService.Services.HousekeepingService>();

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

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Set up event subscriptions
using (var scope = app.Services.CreateScope())
{
    var messageBroker = scope.ServiceProvider.GetRequiredService<IMessageBroker>();
    var receptionEventHandler = scope.ServiceProvider.GetRequiredService<IEventHandler<RoomVacatedEvent>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // Subscribe to room vacated events from reception service
    await messageBroker.SubscribeAsync<RoomVacatedEvent>(EventTopics.RoomVacated, receptionEventHandler.HandleAsync);
    
    logger.LogInformation("Subscribed to reception events: {Topic}", EventTopics.RoomVacated);
}

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
    await context.Database.EnsureCreatedAsync();
    
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("HotelOS Housekeeping Service starting up");
    logger.LogInformation("Database connection: {ConnectionString}", 
        builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=HotelOS.db");
    logger.LogInformation("Redis connection: {ConnectionString}", 
        builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379");
}

app.Run();