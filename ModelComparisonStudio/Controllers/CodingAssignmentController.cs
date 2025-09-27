using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ModelComparisonStudio.Configuration;
using ModelComparisonStudio.Services;
using ModelComparisonStudio.Core.ValueObjects;
using ModelComparisonStudio.Infrastructure.Services;
using static ModelComparisonStudio.Core.ValueObjects.AIProviderNames;

namespace ModelComparisonStudio.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CodingAssignmentController : BaseController
    {
        private readonly AIService _aiService;
        private readonly ApiConfiguration _apiConfiguration;
        private readonly ModelComparisonStudio.Infrastructure.Services.QueryPerformanceMonitor _performanceMonitor;

        public CodingAssignmentController(
            AIService aiService,
            IOptions<ApiConfiguration> apiConfiguration,
            ModelComparisonStudio.Infrastructure.Services.QueryPerformanceMonitor performanceMonitor,
            ILogger<CodingAssignmentController> logger) : base(logger)
        {
            _aiService = aiService;
            _apiConfiguration = apiConfiguration.Value;
            _performanceMonitor = performanceMonitor;
        }

        /// <summary>
        /// Executes a coding assignment with a single AI model
        /// </summary>
        /// <param name="request">Coding assignment request with model and task details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Coding assignment results</returns>
        [HttpPost("execute")]
        [ProducesResponseType(typeof(CodingAssignmentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExecuteCodingAssignment(
            [FromBody] CodingAssignmentRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Received coding assignment request for model {ModelId}", request.ModelId);

                // Validate the request
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).ToList();
                    _logger.LogWarning("Invalid coding assignment request: {Errors}",
                        string.Join(", ", errors.Select(e => e.ErrorMessage)));

                    var friendlyErrors = errors.Select(error => error.ErrorMessage switch
                    {
                        string msg when msg.Contains("must be between 1 and 50000 characters") =>
                            "Your assignment description is too long! Please keep it under 50,000 characters.",
                        string msg when msg.Contains("must be between 1 and") =>
                            "Your assignment description is too short! Please provide at least 1 character.",
                        string msg when msg.Contains("ModelId is required") =>
                            "Please select an AI model for your coding assignment.",
                        _ => error.ErrorMessage
                    }).ToList();

                    return BadRequest(CreateValidationErrorResponse(friendlyErrors));
                }

                // Validate that the model is available
                if (!IsModelAvailable(request.ModelId))
                {
                    _logger.LogWarning("Selected model {ModelId} is not available", request.ModelId);
                    return BadRequest(new
                    {
                        error = "Selected model is not available",
                        modelId = request.ModelId
                    });
                }

                // Determine appropriate timeout based on assignment complexity
                var timeout = DetermineOptimalTimeout(request.Assignment);
                _logger.LogInformation("Using timeout of {TimeoutSeconds} seconds for coding assignment with {CharacterCount} characters",
                    timeout.TotalSeconds, request.Assignment.Length);

                // Generate unique assignment ID
                var assignmentId = Guid.NewGuid().ToString();

                _logger.LogInformation("Starting coding assignment {AssignmentId} with model {ModelId}",
                    assignmentId, request.ModelId);

                // Execute the coding assignment
                var analysisResult = await _aiService.AnalyzeCodeAsync(
                    request.Assignment,
                    request.ModelId,
                    timeout,
                    cancellationToken);

                // Create response
                var response = new CodingAssignmentResponse
                {
                    AssignmentId = assignmentId,
                    ModelId = request.ModelId,
                    Assignment = request.Assignment,
                    Response = analysisResult.Response,
                    ResponseTimeMs = analysisResult.ResponseTimeMs,
                    TokenCount = analysisResult.TokenCount,
                    Status = analysisResult.Status,
                    ErrorMessage = analysisResult.ErrorMessage,
                    ExecutedAt = DateTime.UtcNow,
                    TimeoutUsed = timeout
                };

                _logger.LogInformation("Coding assignment {AssignmentId} completed. Status: {Status}, Time: {ResponseTime}ms",
                    assignmentId, response.Status, response.ResponseTimeMs);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during coding assignment execution");
                return StatusCode(500, CreateErrorResponse(ex));
            }
        }

        /// <summary>
        /// Gets available coding assignment templates
        /// </summary>
        /// <returns>List of available coding task templates</returns>
        [HttpGet("templates")]
        [ProducesResponseType(typeof(CodingAssignmentTemplate[]), StatusCodes.Status200OK)]
        public IActionResult GetCodingTemplates()
        {
            try
            {
                var templates = new[]
                {
                    new CodingAssignmentTemplate
                    {
                        Id = "code-review",
                        Name = "Code Review",
                        Description = "Review and improve existing code",
                        Category = "Code Review",
                        PromptTemplate = @"Please review the following code and provide:

1. **Code Quality Assessment**: Rate the code quality (1-10) and explain your reasoning
2. **Bug Detection**: Identify any bugs, security issues, or potential problems
3. **Performance Analysis**: Analyze performance bottlenecks and suggest optimizations
4. **Best Practices**: Check adherence to coding standards and best practices
5. **Improvements**: Suggest specific improvements with code examples
6. **Documentation**: Recommend documentation improvements

Code to review:
{CODE}

Please provide a comprehensive code review with specific recommendations.",
                        DefaultTimeout = TimeSpan.FromMinutes(10),
                        IsPublic = true
                    },
                    new CodingAssignmentTemplate
                    {
                        Id = "feature-implementation",
                        Name = "Feature Implementation",
                        Description = "Implement a new feature from scratch",
                        Category = "Implementation",
                        PromptTemplate = @"Please implement the following feature:

**Requirements:**
{FEATURE_REQUIREMENTS}

**Technical Specifications:**
- Language: {LANGUAGE}
- Framework: {FRAMEWORK}
- Database: {DATABASE}
- Additional constraints: {CONSTRAINTS}

**Implementation Guidelines:**
1. Follow best practices for the specified language/framework
2. Include proper error handling and validation
3. Add comprehensive documentation
4. Include unit tests if applicable
5. Consider security implications
6. Optimize for performance and maintainability

Please provide a complete, production-ready implementation with explanations.",
                        DefaultTimeout = TimeSpan.FromMinutes(15),
                        IsPublic = true
                    },
                    new CodingAssignmentTemplate
                    {
                        Id = "debugging",
                        Name = "Debug and Fix",
                        Description = "Debug and fix issues in existing code",
                        Category = "Debugging",
                        PromptTemplate = @"I have the following code that has issues. Please help me debug and fix it:

**Code:**
{CODE}

**Problem Description:**
{PROBLEM_DESCRIPTION}

**Error Messages:**
{ERROR_MESSAGES}

**Expected Behavior:**
{EXPECTED_BEHAVIOR}

Please:
1. **Analyze the Issue**: Identify what's causing the problem
2. **Root Cause**: Explain why the issue occurs
3. **Solution**: Provide the corrected code
4. **Prevention**: Suggest how to avoid similar issues in the future
5. **Testing**: Recommend tests to verify the fix",
                        DefaultTimeout = TimeSpan.FromMinutes(10),
                        IsPublic = true
                    },
                    new CodingAssignmentTemplate
                    {
                        Id = "performance-optimization",
                        Name = "Performance Optimization",
                        Description = "Optimize code for better performance",
                        Category = "Optimization",
                        PromptTemplate = @"Please optimize the following code for better performance:

**Current Code:**
{CODE}

**Performance Requirements:**
{PERFORMANCE_REQUIREMENTS}

**Current Issues:**
{CURRENT_ISSUES}

**Environment:**
- Language: {LANGUAGE}
- Runtime: {RUNTIME}
- Constraints: {CONSTRAINTS}

Please provide:
1. **Performance Analysis**: Identify bottlenecks and inefficiencies
2. **Optimization Strategy**: Explain your optimization approach
3. **Optimized Code**: Provide the improved implementation
4. **Performance Improvements**: Estimate the performance gains
5. **Trade-offs**: Discuss any trade-offs made
6. **Monitoring**: Suggest how to monitor performance improvements",
                        DefaultTimeout = TimeSpan.FromMinutes(12),
                        IsPublic = true
                    },
                    new CodingAssignmentTemplate
                    {
                        Id = "architecture-design",
                        Name = "Architecture Design",
                        Description = "Design system architecture and patterns",
                        Category = "Architecture",
                        PromptTemplate = @"Please design a software architecture for the following system:

**System Requirements:**
{SYSTEM_REQUIREMENTS}

**Technical Constraints:**
{TECHNICAL_CONSTRAINTS}

**Business Requirements:**
{BUSINESS_REQUIREMENTS}

**Scale Considerations:**
{SCALE_CONSIDERATIONS}

Please provide:
1. **High-Level Architecture**: System overview and component diagram
2. **Component Design**: Detailed design for each major component
3. **Data Flow**: How data flows through the system
4. **Technology Choices**: Recommended technologies and frameworks
5. **Design Patterns**: Applicable design patterns and principles
6. **Scalability Strategy**: How the system can scale
7. **Security Considerations**: Security design and best practices
8. **Implementation Roadmap**: Phased implementation approach",
                        DefaultTimeout = TimeSpan.FromMinutes(15),
                        IsPublic = true
                    },
                    new CodingAssignmentTemplate
                    {
                        Id = "testing-strategy",
                        Name = "Testing Strategy",
                        Description = "Create comprehensive testing strategy",
                        Category = "Testing",
                        PromptTemplate = @"Please create a comprehensive testing strategy for the following system:

**System Description:**
{SYSTEM_DESCRIPTION}

**Code to Test:**
{CODE}

**Requirements:**
{REQUIREMENTS}

Please provide:
1. **Testing Approach**: Overall testing methodology and strategy
2. **Unit Tests**: Specific unit test cases with examples
3. **Integration Tests**: Integration testing scenarios
4. **End-to-End Tests**: E2E testing approach
5. **Test Data**: Sample test data and fixtures
6. **Test Automation**: Automation strategy and tools
7. **Performance Testing**: Performance test scenarios
8. **Security Testing**: Security testing approach
9. **Testing Best Practices**: Recommendations for testing culture",
                        DefaultTimeout = TimeSpan.FromMinutes(12),
                        IsPublic = true
                    }
                };

                _logger.LogInformation("Retrieved {TemplateCount} coding assignment templates", templates.Length);
                return Ok(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving coding assignment templates");
                return StatusCode(500, CreateErrorResponse(ex));
            }
        }

        /// <summary>
        /// Gets available models for coding assignments
        /// </summary>
        /// <returns>List of models suitable for coding tasks</returns>
        [HttpGet("models")]
        [ProducesResponseType(typeof(CodingModelInfo[]), StatusCodes.Status200OK)]
        public IActionResult GetCodingModels()
        {
            try
            {
                var nanoGPTModels = _apiConfiguration.NanoGPT?.AvailableModels ?? Array.Empty<string>();
                var openRouterModels = _apiConfiguration.OpenRouter?.AvailableModels ?? Array.Empty<string>();

                var allModels = nanoGPTModels.Concat(openRouterModels).Distinct().ToArray();

                var codingModels = allModels.Select(modelId => new CodingModelInfo
                {
                    Id = modelId,
                    Name = GetModelDisplayName(modelId),
                    Provider = GetProviderFromModelId(modelId),
                    Description = GetModelDescription(modelId),
                    ContextWindow = GetModelContextWindow(modelId),
                    RecommendedForCoding = IsRecommendedForCoding(modelId)
                })
                .Where(m => m.RecommendedForCoding)
                .OrderBy(m => m.Provider)
                .ThenBy(m => m.Name)
                .ToArray();

                _logger.LogInformation("Retrieved {ModelCount} coding-suitable models", codingModels.Length);
                return Ok(codingModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving coding models");
                return StatusCode(500, CreateErrorResponse(ex));
            }
        }

        /// <summary>
        /// Determines the optimal timeout based on assignment complexity
        /// </summary>
        /// <param name="assignment">The coding assignment text</param>
        /// <returns>Appropriate timeout duration</returns>
        private TimeSpan DetermineOptimalTimeout(string assignment)
        {
            var length = assignment.Length;
            var wordCount = assignment.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

            // Use extended timeout for very long assignments
            if (length > 15000 || wordCount > 2000)
            {
                return _apiConfiguration.Execution.ExtendedTimeout;
            }

            // Use standard timeout for medium assignments
            if (length > 5000 || wordCount > 800)
            {
                return _apiConfiguration.Execution.StandardTimeout;
            }

            // Use quick timeout for short assignments
            return _apiConfiguration.Execution.QuickTimeout;
        }

        /// <summary>
        /// Check if a model is available in the configuration
        /// </summary>
        /// <param name="modelId">The model ID to check</param>
        /// <returns>True if the model is available, false otherwise</returns>
        private bool IsModelAvailable(string modelId)
        {
            var nanoGPTModels = _apiConfiguration.NanoGPT?.AvailableModels ?? Array.Empty<string>();
            var openRouterModels = _apiConfiguration.OpenRouter?.AvailableModels ?? Array.Empty<string>();

            return nanoGPTModels.Concat(openRouterModels)
                .Any(model => string.Equals(model, modelId, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the provider name from a model ID
        /// </summary>
        /// <param name="modelId">The model ID</param>
        /// <returns>Provider name</returns>
        private string GetProviderFromModelId(string modelId)
        {
            var nanoGPTModels = _apiConfiguration.NanoGPT?.AvailableModels ?? Array.Empty<string>();
            var openRouterModels = _apiConfiguration.OpenRouter?.AvailableModels ?? Array.Empty<string>();

            if (nanoGPTModels.Any(m => string.Equals(m, modelId, StringComparison.OrdinalIgnoreCase)))
                return NanoGPT;

            if (openRouterModels.Any(m => string.Equals(m, modelId, StringComparison.OrdinalIgnoreCase)))
                return OpenRouter;

            return OpenRouter; // Default fallback
        }

        /// <summary>
        /// Gets a display name for a model
        /// </summary>
        /// <param name="modelId">The model ID</param>
        /// <returns>Display name</returns>
        private string GetModelDisplayName(string modelId)
        {
            // Extract the model name from the full ID
            var parts = modelId.Split('/');
            return parts.Length > 1 ? parts[1].Replace("-", " ").ToUpperInvariant() : modelId;
        }

        /// <summary>
        /// Gets a description for a model
        /// </summary>
        /// <param name="modelId">The model ID</param>
        /// <returns>Model description</returns>
        private string GetModelDescription(string modelId)
        {
            return modelId.ToLowerInvariant() switch
            {
                var m when m.Contains("gpt-4") => "OpenAI's advanced GPT-4 model, excellent for complex coding tasks",
                var m when m.Contains("claude") => "Anthropic's Claude model, known for careful reasoning and code quality",
                var m when m.Contains("deepseek") => "DeepSeek's coding model, optimized for programming tasks",
                var m when m.Contains("qwen") => "Qwen's large language model, good for diverse coding tasks",
                var m when m.Contains("gemini") => "Google's Gemini model, strong in code generation and analysis",
                _ => $"AI model {modelId}, suitable for coding assignments"
            };
        }

        /// <summary>
        /// Gets the context window size for a model
        /// </summary>
        /// <param name="modelId">The model ID</param>
        /// <returns>Context window size in tokens</returns>
        private int GetModelContextWindow(string modelId)
        {
            return modelId.ToLowerInvariant() switch
            {
                var m when m.Contains("gpt-4") => 8192,
                var m when m.Contains("claude-3") => 200000,
                var m when m.Contains("deepseek") => 32768,
                var m when m.Contains("qwen") => 8000,
                var m when m.Contains("gemini") => 16384,
                _ => 4096 // Default context window
            };
        }

        /// <summary>
        /// Determines if a model is recommended for coding tasks
        /// </summary>
        /// <param name="modelId">The model ID</param>
        /// <returns>True if recommended for coding</returns>
        private bool IsRecommendedForCoding(string modelId)
        {
            var codingKeywords = new[] { "gpt-4", "claude", "deepseek", "qwen", "gemini", "code" };
            return codingKeywords.Any(keyword => modelId.ToLowerInvariant().Contains(keyword));
        }
    }

    /// <summary>
    /// Request model for coding assignments
    /// </summary>
    public class CodingAssignmentRequest
    {
        public string ModelId { get; set; } = string.Empty;
        public string Assignment { get; set; } = string.Empty;
        public string? TemplateId { get; set; }
        public Dictionary<string, string>? TemplateVariables { get; set; }
    }

    /// <summary>
    /// Response model for coding assignments
    /// </summary>
    public class CodingAssignmentResponse
    {
        public string AssignmentId { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        public string Assignment { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public long ResponseTimeMs { get; set; }
        public int? TokenCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime ExecutedAt { get; set; }
        public TimeSpan TimeoutUsed { get; set; }
    }

    /// <summary>
    /// Template model for coding assignments
    /// </summary>
    public class CodingAssignmentTemplate
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string PromptTemplate { get; set; } = string.Empty;
        public TimeSpan DefaultTimeout { get; set; }
        public bool IsPublic { get; set; }
    }

    /// <summary>
    /// Model information for coding assignments
    /// </summary>
    public class CodingModelInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int ContextWindow { get; set; }
        public bool RecommendedForCoding { get; set; }
    }
}