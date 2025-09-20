using ModelComparisonStudio.Services;
using ModelComparisonStudio.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddControllers(options =>
{
    // Configure to handle large request bodies
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(
        _ => "The field is required.");
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

// Register AIService
builder.Services.AddHttpClient<AIService>();
builder.Services.AddScoped<AIService>();

// Configure API settings - bind the entire configuration to ApiConfiguration
builder.Services.Configure<ModelComparisonStudio.Configuration.ApiConfiguration>(
    builder.Configuration);

// Add API configuration as a singleton for direct access (this will be used by AIService)
builder.Services.AddSingleton<ModelComparisonStudio.Configuration.ApiConfiguration>(sp =>
{
    var configuration = builder.Configuration;
    var apiConfig = new ModelComparisonStudio.Configuration.ApiConfiguration();
    
    // Bind NanoGPT configuration
    var nanoGptSection = configuration.GetSection("NanoGPT");
    if (nanoGptSection.Exists())
    {
        apiConfig.NanoGPT = new ModelComparisonStudio.Configuration.NanoGPTConfiguration
        {
            ApiKey = nanoGptSection["ApiKey"] ?? string.Empty,
            BaseUrl = nanoGptSection["BaseUrl"] ?? "https://api.nano-gpt.com",
            AvailableModels = nanoGptSection.GetSection("AvailableModels").Get<string[]>() ?? Array.Empty<string>()
        };
    }
    
    // Bind OpenRouter configuration
    var openRouterSection = configuration.GetSection("OpenRouter");
    if (openRouterSection.Exists())
    {
        apiConfig.OpenRouter = new ModelComparisonStudio.Configuration.OpenRouterConfiguration
        {
            ApiKey = openRouterSection["ApiKey"] ?? string.Empty,
            BaseUrl = openRouterSection["BaseUrl"] ?? "https://openrouter.ai/api/v1",
            AvailableModels = openRouterSection.GetSection("AvailableModels").Get<string[]>() ?? Array.Empty<string>()
        };
    }
    
    return apiConfig;
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Add CORS middleware
app.UseCors("AllowFrontend");

// Serve default file (index.html) when no specific file is requested
app.UseDefaultFiles();

// Serve static files from wwwroot
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

// Root endpoint to serve the frontend
app.MapGet("/", () => Results.Redirect("/index.html"));

// Fallback to index.html for SPA routing - serve the frontend for all routes
app.MapFallbackToFile("index.html");

app.Run();
