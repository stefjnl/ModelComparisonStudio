using System.ComponentModel.DataAnnotations;

namespace ModelComparisonStudio.Models
{
    /// <summary>
    /// Request model for comparison execution
    /// </summary>
    public class ComparisonRequest
    {
        /// <summary>
        /// The prompt to send to all models
        /// </summary>
        [Required(ErrorMessage = "Prompt is required")]
        [StringLength(10000, MinimumLength = 1, ErrorMessage = "Prompt must be between 1 and 10000 characters")]
        public string Prompt { get; set; } = string.Empty;

        /// <summary>
        /// List of model IDs to compare (1-3 models)
        /// </summary>
        [Required(ErrorMessage = "At least one model must be selected")]
        [MinLength(1, ErrorMessage = "At least one model must be selected")]
        [MaxLength(3, ErrorMessage = "Maximum of 3 models can be selected")]
        public List<string> SelectedModels { get; set; } = new List<string>();
    }
}
