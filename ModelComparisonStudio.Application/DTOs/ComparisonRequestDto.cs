using System.ComponentModel.DataAnnotations;
using ModelComparisonStudio.Core.ValueObjects;

namespace ModelComparisonStudio.Application.DTOs;

/// <summary>
/// Data Transfer Object for comparison requests.
/// </summary>
public class ComparisonRequestDto
{
    /// <summary>
    /// The prompt to send to all models.
    /// </summary>
    [Required(ErrorMessage = "Prompt is required")]
    [StringLength(50000, MinimumLength = 1, ErrorMessage = "Prompt must be between 1 and 50000 characters")]
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// List of model IDs to compare (1-3 models).
    /// </summary>
    [Required(ErrorMessage = "At least one model must be selected")]
    [MinLength(1, ErrorMessage = "At least one model must be selected")]
    [MaxLength(3, ErrorMessage = "Maximum of 3 models can be selected")]
    public List<string> SelectedModels { get; set; } = new();

    /// <summary>
    /// Converts this DTO to a domain request object.
    /// </summary>
    /// <returns>A domain comparison request.</returns>
    public Core.Interfaces.ComparisonRequest ToDomainRequest()
    {
        var domainPrompt = Core.ValueObjects.Prompt.Create(Prompt);
        var domainModelIds = SelectedModels
            .Select(modelId => Core.ValueObjects.ModelId.Create(modelId))
            .ToList();

        return new Core.Interfaces.ComparisonRequest
        {
            Prompt = domainPrompt,
            SelectedModels = domainModelIds
        };
    }

    /// <summary>
    /// Creates a DTO from a domain request object.
    /// </summary>
    /// <param name="domainRequest">The domain request object.</param>
    /// <returns>A new DTO instance.</returns>
    public static ComparisonRequestDto FromDomainRequest(Core.Interfaces.ComparisonRequest domainRequest)
    {
        return new ComparisonRequestDto
        {
            Prompt = domainRequest.Prompt.Content,
            SelectedModels = domainRequest.SelectedModels
                .Select(modelId => modelId.Value)
                .ToList()
        };
    }
}
