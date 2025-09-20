using ModelComparisonStudio.Services;
using ModelComparisonStudio.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddControllers();

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
