using Microsoft.EntityFrameworkCore;
using ModelComparisonStudio.Application.Services;
using ModelComparisonStudio.Configuration;
using ModelComparisonStudio.Controllers;
using ModelComparisonStudio.Core.Interfaces;
using ModelComparisonStudio.Infrastructure;
using ModelComparisonStudio.Infrastructure.Repositories;
using ModelComparisonStudio.Middlewares;
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
.AddApplicationPart(typeof(Program).Assembly) // Explicitly include the current assembly for controller discovery
.AddApplicationPart(typeof(CodingAssignmentController).Assembly) // Include coding assignment controller
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
    options.UseSqlite($"Data Source={Path.Combine(builder.Environment.ContentRootPath, "evaluations.db")}"));

// Register evaluation services
builder.Services.AddScoped<IEvaluationRepository, SqliteEvaluationRepository>();
builder.Services.AddScoped<EvaluationApplicationService>();

// Register prompt template services
builder.Services.AddScoped<IPromptTemplateRepository, SqlitePromptTemplateRepository>();
builder.Services.AddScoped<DatabaseInitializer>();
builder.Services.AddScoped<PromptCategoryService>();
builder.Services.AddScoped<PromptTemplateService>();
builder.Services.AddScoped<TemplateStatisticsService>();

// Register performance monitoring services
builder.Services.AddSingleton<ModelComparisonStudio.Infrastructure.Services.QueryPerformanceMonitor>();

// Register AIService with performance monitoring
builder.Services.AddScoped<AIService>();


// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

var app = builder.Build();

// Register global exception middleware as early as possible
app.UseMiddleware<GlobalExceptionMiddleware>();

// Test logging configuration
var appLogger = app.Services.GetRequiredService<ILogger<Program>>();
appLogger.LogInformation("Application starting up - logging is working!");

// Initialize database using the application's service provider
using (var scope = app.Services.CreateScope())
{
    var dbLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var currentDir = Directory.GetCurrentDirectory();
    var contentRootPath = app.Environment.ContentRootPath;
    dbLogger.LogInformation("Current working directory: {CurrentDir}", currentDir);
    dbLogger.LogInformation("Content root path: {ContentRootPath}", contentRootPath);
    dbLogger.LogInformation("Database path will be: {DbPath}", Path.Combine(contentRootPath, "evaluations.db"));

    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var databaseInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();

    try
    {
        await dbContext.Database.EnsureCreatedAsync(); // Creates the database if it doesn't exist
        dbLogger.LogInformation("Database initialized successfully");

        // Initialize prompt template system with default categories
        await databaseInitializer.InitializeDatabaseAsync();
        dbLogger.LogInformation("Prompt template system initialized successfully");
    }
    catch (Exception ex)
    {
        dbLogger.LogError(ex, "Failed to initialize database");
        throw;
    }
}

// Add middleware to handle JSON parsing errors
app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (System.Text.Json.JsonException jsonEx)
    {
        appLogger.LogError(jsonEx, "JSON parsing error occurred");
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

app.UseRouting();

app.UseCors("AllowFrontend");

// Serve static files from wwwroot
app.UseStaticFiles();

// Serve default file (index.html) when no specific file is requested
app.UseDefaultFiles();

// Map API controllers FIRST - they should take precedence over fallback routes
app.MapControllers();

// Root endpoint to serve the frontend
app.MapGet("/", () => Results.Redirect("/index.html"));

// Add specific API route patterns to ensure they're not caught by fallback
app.MapGet("/api/{**slug}", async context =>
{
    context.Response.StatusCode = 404;
    await context.Response.WriteAsJsonAsync(new { error = "API endpoint not found" });
});

// Fallback to index.html for SPA routing - this should be LAST
app.MapFallbackToFile("index.html");

await app.RunAsync();
