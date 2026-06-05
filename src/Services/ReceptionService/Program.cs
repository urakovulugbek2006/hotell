using HotelOS.Shared.Infrastructure;
using ReceptionService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "HotelOS Reception Service", 
        Version = "v1",
        Description = "Reception Service API for HotelOS - Handles guest check-in/check-out, room assignment, and booking management"
    });
});

// Add HotelOS Infrastructure
builder.Services.AddHotelOSInfrastructure(builder.Configuration);

// Add Reception Service dependencies
builder.Services.AddScoped<IReceptionService, global::ReceptionService.Services.ReceptionService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IRoomAssignmentAlgorithm, RoomAssignmentAlgorithm>();

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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HotelOS Reception Service v1");
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
    logger.LogInformation("HotelOS Reception Service starting up");
    logger.LogInformation("Database connection: {ConnectionString}", 
        builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=HotelOS.db");
    logger.LogInformation("Redis connection: {ConnectionString}", 
        builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379");
}

app.Run();