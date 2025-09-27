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

        // Timeout configurations for different use cases
        public TimeSpan QuickTimeout { get; set; } = TimeSpan.FromMinutes(2);    // For simple prompts
        public TimeSpan StandardTimeout { get; set; } = TimeSpan.FromMinutes(5);  // For standard comparisons
        public TimeSpan ExtendedTimeout { get; set; } = TimeSpan.FromMinutes(15); // For complex coding tasks
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(10);  // Increased from 60 seconds

        public int RetryAttempts { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);      // Increased for better reliability

        // Performance monitoring
        public bool EnablePerformanceMonitoring { get; set; } = true;
        public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMinutes(1);
    }
}