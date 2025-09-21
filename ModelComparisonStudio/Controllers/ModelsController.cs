using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ModelComparisonStudio.Configuration;

namespace ModelComparisonStudio.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModelsController : ControllerBase
    {
        private readonly ApiConfiguration _apiConfiguration;
        private readonly ILogger<ModelsController> _logger;

        public ModelsController(IOptions<ApiConfiguration> apiConfiguration, ILogger<ModelsController> logger)
        {
            _apiConfiguration = apiConfiguration.Value;
            _logger = logger;
        }

        /// <summary>
        /// Get all available models from both NanoGPT and OpenRouter providers
        /// </summary>
        /// <returns>Combined list of available models</returns>
        [HttpGet("available")]
        public ActionResult<AvailableModelsResponse> GetAvailableModels()
        {
            try
            {
                var nanoGPTModels = _apiConfiguration.NanoGPT?.AvailableModels ?? Array.Empty<string>();
                var openRouterModels = _apiConfiguration.OpenRouter?.AvailableModels ?? Array.Empty<string>();

                var response = new AvailableModelsResponse
                {
                    NanoGPT = new ProviderModels
                    {
                        Provider = "NanoGPT",
                        BaseUrl = _apiConfiguration.NanoGPT?.BaseUrl ?? string.Empty,
                        Models = nanoGPTModels,
                        ModelCount = nanoGPTModels.Length
                    },
                    OpenRouter = new ProviderModels
                    {
                        Provider = "OpenRouter",
                        BaseUrl = _apiConfiguration.OpenRouter?.BaseUrl ?? string.Empty,
                        Models = openRouterModels,
                        ModelCount = openRouterModels.Length
                    },
                    TotalModels = nanoGPTModels.Length + openRouterModels.Length
                };

                _logger.LogInformation("Retrieved available models: NanoGPT ({NanoGPTCount}), OpenRouter ({OpenRouterCount})", 
                    nanoGPTModels.Length, openRouterModels.Length);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available models from configuration");
                return StatusCode(500, new { error = "Internal server error while retrieving models" });
            }
        }

        /// <summary>
        /// Get models for a specific provider
        /// </summary>
        /// <param name="provider">Provider name (NanoGPT or OpenRouter)</param>
        /// <returns>Models for the specified provider</returns>
        [HttpGet("available/{provider}")]
        public ActionResult<ProviderModels> GetProviderModels(string provider)
        {
            try
            {
                provider = provider.ToLowerInvariant();

                ProviderModels? result = provider switch
                {
                    "nanogpt" => new ProviderModels
                    {
                        Provider = "NanoGPT",
                        BaseUrl = _apiConfiguration.NanoGPT?.BaseUrl ?? string.Empty,
                        Models = _apiConfiguration.NanoGPT?.AvailableModels ?? Array.Empty<string>(),
                        ModelCount = _apiConfiguration.NanoGPT?.AvailableModels?.Length ?? 0
                    },
                    "openrouter" => new ProviderModels
                    {
                        Provider = "OpenRouter",
                        BaseUrl = _apiConfiguration.OpenRouter?.BaseUrl ?? string.Empty,
                        Models = _apiConfiguration.OpenRouter?.AvailableModels ?? Array.Empty<string>(),
                        ModelCount = _apiConfiguration.OpenRouter?.AvailableModels?.Length ?? 0
                    },
                    _ => null
                };

                if (result == null)
                {
                    return NotFound(new { error = $"Provider '{provider}' not found. Use 'nanogpt' or 'openrouter'" });
                }

                _logger.LogInformation("Retrieved models for provider {Provider}: {ModelCount} models", 
                    result.Provider, result.ModelCount);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving models for provider {Provider}", provider);
                return StatusCode(500, new { error = "Internal server error while retrieving models" });
            }
        }
    }

    public class AvailableModelsResponse
    {
        public ProviderModels NanoGPT { get; set; } = new();
        public ProviderModels OpenRouter { get; set; } = new();
        public int TotalModels { get; set; }
    }

    public class ProviderModels
    {
        public string Provider { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string[] Models { get; set; } = Array.Empty<string>();
        public int ModelCount { get; set; }
    }
}