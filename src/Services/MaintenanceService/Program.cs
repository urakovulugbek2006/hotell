using HotelOS.Shared.Infrastructure;
using MaintenanceService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "HotelOS Maintenance Service", 
        Version = "v1",
        Description = "Maintenance Service API for HotelOS - Handles maintenance requests, priority queue management, and technician workflow"
    });
});

// Add HotelOS Infrastructure
builder.Services.AddHotelOSInfrastructure(builder.Configuration);

// Add Maintenance Service dependencies
builder.Services.AddScoped<IMaintenanceService, global::MaintenanceService.Services.MaintenanceService>();
builder.Services.AddSingleton<IPriorityQueueService, PriorityQueueService>();

// Add Background Services
builder.Services.AddHostedService<MaintenanceBackgroundService>();

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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HotelOS Maintenance Service v1");
        c.RoutePrefix = string.Empty; // Swagger UI at root
    });
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Ensure database is created (with retry - all services share one SQLite file)
app.Logger.LogInformation("HotelOS Maintenance Service starting up");
await app.Services.InitialiseDatabaseAsync(app.Logger);

app.Run();