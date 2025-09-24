using Microsoft.Extensions.Logging;
using ModelComparisonStudio.Core.Interfaces;

namespace ModelComparisonStudio.Application.Services;

/// <summary>
/// Service for database initialization operations
/// </summary>
public class DatabaseInitializer
{
    private readonly IPromptTemplateRepository _repository;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        IPromptTemplateRepository repository,
        ILogger<DatabaseInitializer> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes the database with default system categories
    /// </summary>
    public async Task<bool> InitializeDatabaseAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing database with default system categories");
        return await _repository.InitializeDatabaseAsync(cancellationToken);
    }
}