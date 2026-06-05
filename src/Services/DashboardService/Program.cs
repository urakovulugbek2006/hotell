using DashboardService.Hubs;
using DashboardService.Services;
using HotelOS.Shared.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "HotelOS Dashboard Service", 
        Version = "v1",
        Description = "Real-time Dashboard Service API for HotelOS - Provides live hotel operations monitoring with WebSocket updates"
    });
});

// Add SignalR
builder.Services.AddSignalR();

// Add HotelOS Infrastructure
builder.Services.AddHotelOSInfrastructure(builder.Configuration);

// Add Dashboard Service
builder.Services.AddScoped<IDashboardService, global::DashboardService.Services.DashboardService>();

// Add Broker Event Handler (bridges Redis Pub/Sub -> SignalR)
builder.Services.AddSingleton<global::DashboardService.EventHandlers.BrokerEventHandler>();

// Add CORS for SignalR
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HotelOS Dashboard Service v1");
        c.RoutePrefix = string.Empty; // Swagger UI at root
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseRouting();
app.UseAuthorization();

// Map SignalR Hub
app.MapHub<DashboardHub>("/dashboardHub");

app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
    await context.Database.EnsureCreatedAsync();
    
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("HotelOS Dashboard Service starting up");
    logger.LogInformation("Database connection: {ConnectionString}", 
        builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=HotelOS.db");
    logger.LogInformation("Redis connection: {ConnectionString}", 
        builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379");
    logger.LogInformation("SignalR Hub available at: /dashboardHub");
}

// Subscribe the dashboard to all broker events so updates flow to clients in real time
var brokerEventHandler = app.Services.GetRequiredService<global::DashboardService.EventHandlers.BrokerEventHandler>();
await brokerEventHandler.SubscribeToAllEventsAsync();

app.Run();