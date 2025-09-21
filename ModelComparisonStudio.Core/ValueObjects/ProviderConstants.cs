namespace ModelComparisonStudio.Core.ValueObjects;

/// <summary>
/// Constants for AI provider names
/// </summary>
public static class AIProviderNames
{
    public const string OpenRouter = "OpenRouter";
    public const string NanoGPT = "NanoGPT";
}

/// <summary>
/// Constants for AI provider URLs
/// </summary>
public static class AIProviderUrls
{
    public const string NanoGPTBaseUrl = "https://nano-gpt.com/api/v1";
    public const string OpenRouterBaseUrl = "https://openrouter.ai/api/v1";
}

/// <summary>
/// Constants for common MIME types
/// </summary>
public static class MimeTypes
{
    public const string ApplicationJson = "application/json";
    public const string TextEventStream = "text/event-stream";
}