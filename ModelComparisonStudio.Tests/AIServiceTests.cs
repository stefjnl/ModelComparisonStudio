using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using ModelComparisonStudio.Configuration;
using ModelComparisonStudio.Services;
using ModelComparisonStudio.Core.ValueObjects;

namespace ModelComparisonStudio.Tests
{
    public class AIServiceTests
    {
        private readonly Mock<IOptions<ApiConfiguration>> _mockConfig;
        private readonly Mock<HttpClient> _mockHttpClient;
        private readonly Mock<ILogger<AIService>> _mockLogger;
        private readonly AIService _aiService;

        public AIServiceTests()
        {
            _mockConfig = new Mock<IOptions<ApiConfiguration>>();
            _mockHttpClient = new Mock<HttpClient>();
            _mockLogger = new Mock<ILogger<AIService>>();

            var config = new ApiConfiguration
            {
                NanoGPT = new NanoGPTConfiguration
                {
                    ApiKey = "test-key",
                    BaseUrl = "https://api.nanogpt.com",
                    AvailableModels = new[] { "gpt-4", "claude-3" }
                },
                OpenRouter = new OpenRouterConfiguration
                {
                    ApiKey = "test-key",
                    BaseUrl = "https://api.openrouter.ai",
                    AvailableModels = new[] { "gpt-4", "claude-3" }
                },
                Execution = new ExecutionConfiguration
                {
                    MaxConcurrentRequests = 2,
                    EnableParallelExecution = true,
                    DefaultTimeout = TimeSpan.FromSeconds(30)
                }
            };

            _mockConfig.Setup(c => c.Value).Returns(config);
            _aiService = new AIService(_mockConfig.Object, _mockHttpClient.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task ExecuteParallelComparison_WithValidModels_ReturnsResults()
        {
            // Arrange
            var prompt = "Test prompt";
            var modelIds = new List<string> { "gpt-4", "claude-3" };

            // Act
            var results = await _aiService.ExecuteParallelComparison(prompt, modelIds, 2, CancellationToken.None);

            // Assert
            Assert.NotNull(results);
            Assert.Equal(2, results.Count);
            Assert.All(results, r => Assert.Equal(ModelResultStatus.Error.ToString(), r.Status));
        }

        [Fact]
        public async Task ExecuteParallelComparison_WithCancellationToken_CancelsOperation()
        {
            // Arrange
            var prompt = "Test prompt";
            var modelIds = new List<string> { "gpt-4", "claude-3" };
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _aiService.ExecuteParallelComparison(prompt, modelIds, 2, cts.Token));
        }

        [Fact]
        public async Task ExecuteParallelComparison_WithConcurrencyLimit_RespectsLimit()
        {
            // Arrange
            var prompt = "Test prompt";
            var modelIds = new List<string> { "gpt-4", "claude-3", "gemini-pro" };

            // Act
            var results = await _aiService.ExecuteParallelComparison(prompt, modelIds, 1, CancellationToken.None);

            // Assert
            Assert.NotNull(results);
            Assert.Equal(3, results.Count);
        }

        [Fact]
        public async Task ExecuteSequentialComparison_WithValidModels_ReturnsResults()
        {
            // Arrange
            var prompt = "Test prompt";
            var modelIds = new List<string> { "gpt-4", "claude-3" };

            // Act
            var results = await _aiService.ExecuteSequentialComparison(prompt, modelIds, CancellationToken.None);

            // Assert
            Assert.NotNull(results);
            Assert.Equal(2, results.Count);
        }
    }
}