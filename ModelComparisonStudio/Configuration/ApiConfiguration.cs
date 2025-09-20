namespace ModelComparisonStudio.Configuration
{
    public class ApiConfiguration
    {
        public NanoGPTConfiguration NanoGPT { get; set; } = new();
        public OpenRouterConfiguration OpenRouter { get; set; } = new();
    }

    public class NanoGPTConfiguration
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.nano-gpt.com";
        public string[] AvailableModels { get; set; } = Array.Empty<string>();
    }

    public class OpenRouterConfiguration
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
        public string[] AvailableModels { get; set; } = Array.Empty<string>();
    }
}