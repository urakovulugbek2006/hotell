using HotelOS.Shared.Infrastructure;
using RoomService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "HotelOS Room Service", 
        Version = "v1",
        Description = "Room Service API for HotelOS - Handles food and beverage orders, kitchen workflow, and delivery tracking"
    });
});

// Add HotelOS Infrastructure
builder.Services.AddHotelOSInfrastructure(builder.Configuration);

// Add Room Service dependencies
builder.Services.AddScoped<IRoomService, global::RoomService.Services.RoomService>();

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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HotelOS Room Service v1");
        c.RoutePrefix = string.Empty; // Swagger UI at root
    });
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
    await context.Database.EnsureCreatedAsync();
    
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("HotelOS Room Service starting up");
    logger.LogInformation("Database connection: {ConnectionString}", 
        builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=HotelOS.db");
    logger.LogInformation("Redis connection: {ConnectionString}", 
        builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379");
}

app.Run();