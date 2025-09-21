using ModelComparisonStudio.Core.Entities;
using ModelComparisonStudio.Core.ValueObjects;

namespace ModelComparisonStudio.Core.Interfaces;

/// <summary>
/// Interface for AI providers that can execute model requests.
/// </summary>
public interface IAIProvider
{
    /// <summary>
    /// Gets the provider name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the base URL for the provider's API.
    /// </summary>
    string BaseUrl { get; }

    /// <summary>
    /// Gets the list of available model IDs for this provider.
    /// </summary>
    IReadOnlyList<string> AvailableModels { get; }

    /// <summary>
    /// Checks if a specific model is available for this provider.
    /// </summary>
    /// <param name="modelId">The model ID to check.</param>
    /// <returns>True if the model is available, false otherwise.</returns>
    bool IsModelAvailable(ModelId modelId);

    /// <summary>
    /// Executes a request against the specified model.
    /// </summary>
    /// <param name="modelId">The model ID to use.</param>
    /// <param name="prompt">The prompt to send to the model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A model result with the response and metadata.</returns>
    Task<ModelResult> ExecuteRequestAsync(
        ModelId modelId,
        Prompt prompt,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for managing AI providers.
/// </summary>
public interface IAIProviderManager
{
    /// <summary>
    /// Gets all available providers.
    /// </summary>
    /// <returns>List of all providers.</returns>
    Task<IReadOnlyList<IAIProvider>> GetAllProvidersAsync();

    /// <summary>
    /// Gets a provider by name.
    /// </summary>
    /// <param name="name">The provider name.</param>
    /// <returns>The provider if found, null otherwise.</returns>
    Task<IAIProvider?> GetProviderByNameAsync(string name);

    /// <summary>
    /// Gets the provider that can handle the specified model ID.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <returns>The provider if found, null otherwise.</returns>
    Task<IAIProvider?> GetProviderForModelAsync(ModelId modelId);

    /// <summary>
    /// Gets all available models across all providers.
    /// </summary>
    /// <returns>List of all available model IDs.</returns>
    Task<IReadOnlyList<string>> GetAllAvailableModelsAsync();

    /// <summary>
    /// Checks if a model is available across all providers.
    /// </summary>
    /// <param name="modelId">The model ID to check.</param>
    /// <returns>True if the model is available, false otherwise.</returns>
    Task<bool> IsModelAvailableAsync(ModelId modelId);
}

/// <summary>
/// Interface for provider configuration.
/// </summary>
public interface IProviderConfiguration
{
    /// <summary>
    /// Gets the API key for the provider.
    /// </summary>
    ApiKey ApiKey { get; }

    /// <summary>
    /// Gets the base URL for the provider's API.
    /// </summary>
    string BaseUrl { get; }

    /// <summary>
    /// Gets the list of available model IDs.
    /// </summary>
    IReadOnlyList<string> AvailableModels { get; }

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <returns>True if the configuration is valid, false otherwise.</returns>
    bool IsValid();
}
