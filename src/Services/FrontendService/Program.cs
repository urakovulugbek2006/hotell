using FrontendService.Services;
using HotelOS.Shared.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "HotelOS Frontend (Guest Portal) Service", 
        Version = "v1",
        Description = "Guest-facing API for HotelOS - Handles guest registration, bookings, room availability, room service orders and maintenance requests"
    });
});

// Add HotelOS Infrastructure (DB + Redis + Message Broker)
builder.Services.AddHotelOSInfrastructure(builder.Configuration);

// Register an HttpClient so the Frontend Service can call the other microservices
builder.Services.AddHttpClient<IFrontendService, global::FrontendService.Services.FrontendService>();

// Add CORS so the React guest-portal can call this API
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HotelOS Frontend Service v1");
        c.RoutePrefix = string.Empty; // Swagger UI at root
    });
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Ensure database is created (with retry - all services share one SQLite file)
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("HotelOS Frontend (Guest Portal) Service starting up");
}
await app.Services.InitialiseDatabaseAsync(app.Logger);

app.Run();