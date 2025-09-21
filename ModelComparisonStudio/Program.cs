using ModelComparisonStudio.Application.Services;
using ModelComparisonStudio.Configuration;
using ModelComparisonStudio.Core.Interfaces;
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

// Register evaluation services
builder.Services.AddScoped<IEvaluationRepository, InMemoryEvaluationRepository>();
builder.Services.AddScoped<EvaluationApplicationService>();

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
