using Microsoft.EntityFrameworkCore;
using ModelComparisonStudio.Application.Services;
using ModelComparisonStudio.Configuration;
using ModelComparisonStudio.Core.Interfaces;
using ModelComparisonStudio.Infrastructure;
using ModelComparisonStudio.Infrastructure.Repositories;
using ModelComparisonStudio.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddControllers(options =>
{
    // Configure to handle large request bodies
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(
        _ => "The field is required.");
})
.AddJsonOptions(options =>
{
    // Configure JSON options for better error handling
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.AllowTrailingCommas = true;
    options.JsonSerializerOptions.ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip;
});

// Configure form options to handle large requests
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.ValueCountLimit = 10000;
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = long.MaxValue;
});

// Add CORS to allow frontend requests
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin => true) // Allow any origin in development
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Register AIService with HttpClient configured with longer timeout
builder.Services.AddHttpClient<AIService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(5); // 300 seconds
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});
builder.Services.AddScoped<AIService>();

// Configure API settings using IOptions pattern - bind the entire configuration to ApiConfiguration
builder.Services.Configure<ApiConfiguration>(builder.Configuration);

// Register database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=evaluations.db"));

// Register evaluation services
builder.Services.AddScoped<IEvaluationRepository, SqliteEvaluationRepository>();
builder.Services.AddScoped<EvaluationApplicationService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Initialize database
using (var scope = builder.Services.BuildServiceProvider().CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated(); // Creates the database if it doesn't exist
}

var app = builder.Build();

// Test logging configuration
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application starting up - logging is working!");

// Add middleware to handle JSON parsing errors
app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (System.Text.Json.JsonException jsonEx)
    {
        logger.LogError(jsonEx, "JSON parsing error occurred");
        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            type = "json_parsing_error",
            title = "Invalid JSON",
            status = 400,
            detail = "The request body contains invalid JSON",
            traceId = context.TraceIdentifier
        });
    }
});

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowFrontend");

app.MapControllers();

// Serve static files from wwwroot
app.UseStaticFiles();

// Serve default file (index.html) when no specific file is requested
app.UseDefaultFiles();

// Root endpoint to serve the frontend
app.MapGet("/", () => Results.Redirect("/index.html"));

// Fallback to index.html for SPA routing - serve the frontend for all routes
app.MapFallbackToFile("index.html");

app.Run();
