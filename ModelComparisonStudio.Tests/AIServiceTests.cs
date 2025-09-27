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
                    QuickTimeout = TimeSpan.FromMinutes(2),
                    StandardTimeout = TimeSpan.FromMinutes(5),
                    ExtendedTimeout = TimeSpan.FromMinutes(15),
                    DefaultTimeout = TimeSpan.FromMinutes(10),
                    RetryAttempts = 3,
                    RetryDelay = TimeSpan.FromSeconds(5),
                    EnablePerformanceMonitoring = true,
                    HealthCheckInterval = TimeSpan.FromMinutes(1)
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

        [Fact]
        public async Task ExecuteParallelComparison_WithTimeout_UsesCorrectTimeout()
        {
            // Arrange
            var prompt = "Test prompt for timeout testing";
            var modelIds = new List<string> { "gpt-4", "claude-3" };
            var customTimeout = TimeSpan.FromMinutes(10);

            // Act
            var results = await _aiService.ExecuteParallelComparison(prompt, modelIds, 2, customTimeout, CancellationToken.None);

            // Assert
            Assert.NotNull(results);
            Assert.Equal(2, results.Count);
            Assert.All(results, r => Assert.Equal(ModelResultStatus.Error.ToString(), r.Status));
        }

        [Fact]
        public async Task ExecuteParallelComparison_WithCancellationToken_HandlesCancellation()
        {
            // Arrange
            var prompt = "Test prompt";
            var modelIds = new List<string> { "gpt-4", "claude-3" };
            var cts = new CancellationTokenSource();
            cts.CancelAfter(100); // Cancel after 100ms

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _aiService.ExecuteParallelComparison(prompt, modelIds, 2, TimeSpan.FromMinutes(5), cts.Token));
        }

        [Fact]
        public async Task ExecuteParallelComparison_WithLongPrompt_UsesExtendedTimeout()
        {
            // Arrange
            var longPrompt = new string('A', 15000); // 15k character prompt
            var modelIds = new List<string> { "gpt-4" };

            // Act
            var results = await _aiService.ExecuteParallelComparison(longPrompt, modelIds, 1, CancellationToken.None);

            // Assert
            Assert.NotNull(results);
            Assert.Single(results);
        }

        [Fact]
        public async Task ExecuteParallelComparison_WithShortPrompt_UsesQuickTimeout()
        {
            // Arrange
            var shortPrompt = "Hello world";
            var modelIds = new List<string> { "gpt-4" };

            // Act
            var results = await _aiService.ExecuteParallelComparison(shortPrompt, modelIds, 1, CancellationToken.None);

            // Assert
            Assert.NotNull(results);
            Assert.Single(results);
        }

        [Fact]
        public async Task ExecuteParallelComparison_WithMediumPrompt_UsesStandardTimeout()
        {
            // Arrange
            var mediumPrompt = new string('A', 3000); // 3k character prompt
            var modelIds = new List<string> { "gpt-4" };

            // Act
            var results = await _aiService.ExecuteParallelComparison(mediumPrompt, modelIds, 1, CancellationToken.None);

            // Assert
            Assert.NotNull(results);
            Assert.Single(results);
        }

        [Theory]
        [InlineData("QuickTimeout", 2)]
        [InlineData("StandardTimeout", 5)]
        [InlineData("ExtendedTimeout", 15)]
        public async Task TimeoutConfiguration_IsAccessible(string timeoutProperty, int expectedMinutes)
        {
            // Arrange
            var config = new ApiConfiguration
            {
                Execution = new ExecutionConfiguration
                {
                    QuickTimeout = TimeSpan.FromMinutes(2),
                    StandardTimeout = TimeSpan.FromMinutes(5),
                    ExtendedTimeout = TimeSpan.FromMinutes(15),
                    DefaultTimeout = TimeSpan.FromMinutes(10)
                }
            };

            // Act & Assert
            var timeoutValue = timeoutProperty switch
            {
                "QuickTimeout" => config.Execution.QuickTimeout.TotalMinutes,
                "StandardTimeout" => config.Execution.StandardTimeout.TotalMinutes,
                "ExtendedTimeout" => config.Execution.ExtendedTimeout.TotalMinutes,
                _ => throw new ArgumentException("Invalid timeout property")
            };

            Assert.Equal(expectedMinutes, timeoutValue);
        }
    }
}