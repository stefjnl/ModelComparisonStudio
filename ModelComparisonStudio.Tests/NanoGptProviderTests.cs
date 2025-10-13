using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace ModelComparisonStudio.Tests
{
    public class NanoGptProviderTests
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly IConfiguration _configuration;

        public NanoGptProviderTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            
            // Load configuration from appsettings.json and appsettings.Development.json
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
            _configuration = builder.Build();
        }

        [Fact]
        public async Task ChatCompletions_WithValidRequest_ShouldSucceed()
        {
            // Arrange
            var baseUrl = _configuration["NanoGPT:BaseUrl"];
            var apiKey = _configuration["NanoGPT:ApiKey"];

            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(apiKey))
            {
                Assert.Fail("BaseUrl or ApiKey not found in configuration. Please check appsettings.json");
            }

            var client = new HttpClient();
            var requestUrl = $"{baseUrl}/chat/completions";

            var requestBody = new
            {
                model = "gpt-4",
                messages = new[] { new { role = "user", content = "Hello, world!" } }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Act
            var response = await client.PostAsync(requestUrl, content);

            // Log full response for debugging
            var responseContent = await response.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine($"Response Status Code: {response.StatusCode}");
            _testOutputHelper.WriteLine($"Response Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"))}");
            _testOutputHelper.WriteLine($"Response Body: {responseContent}");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }
    }
}