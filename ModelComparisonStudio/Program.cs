var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddControllers();

// Configure API settings
builder.Services.Configure<ModelComparisonStudio.Configuration.ApiConfiguration>(
    builder.Configuration.GetSection("ApiConfiguration"));

// Add API configuration as a singleton for direct access
builder.Services.AddSingleton<ModelComparisonStudio.Configuration.ApiConfiguration>(sp =>
{
    var configuration = builder.Configuration;
    return new ModelComparisonStudio.Configuration.ApiConfiguration
    {
        NanoGPT = new ModelComparisonStudio.Configuration.NanoGPTConfiguration
        {
            ApiKey = configuration["NanoGPT:ApiKey"] ?? string.Empty,
            BaseUrl = configuration["NanoGPT:BaseUrl"] ?? "https://api.nano-gpt.com",
            AvailableModels = configuration.GetSection("NanoGPT:AvailableModels").Get<string[]>() ?? Array.Empty<string>()
        },
        OpenRouter = new ModelComparisonStudio.Configuration.OpenRouterConfiguration
        {
            ApiKey = configuration["OpenRouter:ApiKey"] ?? string.Empty,
            BaseUrl = configuration["OpenRouter:BaseUrl"] ?? "https://openrouter.ai/api/v1",
            AvailableModels = configuration.GetSection("OpenRouter:AvailableModels").Get<string[]>() ?? Array.Empty<string>()
        }
    };
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
