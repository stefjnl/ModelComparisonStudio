namespace ModelComparisonStudio.Configuration
{
    public class ApiConfiguration
    {
        public NanoGPTConfiguration NanoGPT { get; set; } = new();
        public OpenRouterConfiguration OpenRouter { get; set; } = new();
        public ExecutionConfiguration Execution { get; set; } = new();
    }

    public class NanoGPTConfiguration
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://nano-gpt.com/api/v1";
        public string[] AvailableModels { get; set; } = Array.Empty<string>();
    }

    public class OpenRouterConfiguration
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
        public string[] AvailableModels { get; set; } = Array.Empty<string>();
    }

    public class ExecutionConfiguration
    {
        public int MaxConcurrentRequests { get; set; } = 2;
        public bool EnableParallelExecution { get; set; } = true;
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(60);
        public int RetryAttempts { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    }
}