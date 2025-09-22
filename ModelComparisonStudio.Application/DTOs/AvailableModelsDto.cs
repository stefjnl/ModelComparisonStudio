using System.ComponentModel.DataAnnotations;

namespace ModelComparisonStudio.Application.DTOs;

/// <summary>
/// Data Transfer Object for available models response.
/// </summary>
public class AvailableModelsDto
{
    /// <summary>
    /// Models available from NanoGPT provider.
    /// </summary>
    public ProviderModelsDto NanoGPT { get; set; } = new();

    /// <summary>
    /// Models available from OpenRouter provider.
    /// </summary>
    public ProviderModelsDto OpenRouter { get; set; } = new();

    /// <summary>
    /// Total number of models across all providers.
    /// </summary>
    public int TotalModels => NanoGPT.ModelCount + OpenRouter.ModelCount;

    /// <summary>
    /// Creates a DTO from domain available models response.
    /// </summary>
    /// <param name="domainResponse">The domain response object.</param>
    /// <returns>A new DTO instance.</returns>
    public static AvailableModelsDto FromDomainResponse(Core.Interfaces.AvailableModelsResponse domainResponse)
    {
        var nanoGptModels = domainResponse.NanoGPT.Models?.ToList() ?? new List<string>();
        var openRouterModels = domainResponse.OpenRouter.Models?.ToList() ?? new List<string>();

        return new AvailableModelsDto
        {
            NanoGPT = new ProviderModelsDto
            {
                Provider = domainResponse.NanoGPT.Provider,
                BaseUrl = domainResponse.NanoGPT.BaseUrl,
                Models = nanoGptModels
            },
            OpenRouter = new ProviderModelsDto
            {
                Provider = domainResponse.OpenRouter.Provider,
                BaseUrl = domainResponse.OpenRouter.BaseUrl,
                Models = openRouterModels
            }
        };
    }

    /// <summary>
    /// Converts this DTO to a domain available models response.
    /// </summary>
    /// <returns>A domain available models response.</returns>
    public Core.Interfaces.AvailableModelsResponse ToDomainResponse()
    {
        var nanoGptModels = NanoGPT.Models?.ToList() ?? new List<string>();
        var openRouterModels = OpenRouter.Models?.ToList() ?? new List<string>();

        return new Core.Interfaces.AvailableModelsResponse
        {
            NanoGPT = new Core.Interfaces.ProviderModels
            {
                Provider = NanoGPT.Provider,
                BaseUrl = NanoGPT.BaseUrl,
                Models = nanoGptModels
            },
            OpenRouter = new Core.Interfaces.ProviderModels
            {
                Provider = OpenRouter.Provider,
                BaseUrl = OpenRouter.BaseUrl,
                Models = openRouterModels
            }
        };
    }
}

/// <summary>
/// Data Transfer Object for provider model information.
/// </summary>
public class ProviderModelsDto
{
    /// <summary>
    /// The provider name.
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// The base URL for the provider.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// List of available model IDs.
    /// </summary>
    public IReadOnlyList<string> Models { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Number of available models.
    /// </summary>
    public int ModelCount => Models.Count;
}
