# Model Comparison Studio

A modern web application for comparing AI language models side-by-side. Supports multiple AI providers including NanoGPT and OpenRouter.

## Setup

### Prerequisites
- .NET 9 SDK
- Node.js (for frontend development)
- API keys for NanoGPT and/or OpenRouter

### Configuration

1. **Copy the example configuration:**
   ```bash
   cp appsettings.json.example ModelComparisonStudio/appsettings.json
   ```

2. **Configure your API keys:**
   Open `ModelComparisonStudio/appsettings.json` and replace the placeholder API keys with your actual keys:

   ```json
   "NanoGPT": {
     "ApiKey": "your-actual-nanogpt-api-key",
     "BaseUrl": "https://api.nano-gpt.com",
     // ... rest of configuration
   },
   "OpenRouter": {
     "ApiKey": "your-actual-openrouter-api-key",
     "BaseUrl": "https://openrouter.ai/api/v1",
     // ... rest of configuration
   }
   ```

3. **AppHost Configuration (if using Aspire):**
   The AppHost configuration is automatically ignored by Git. Create a local copy if needed:
   ```bash
   cp ModelComparisonStudio.AppHost/appsettings.json ModelComparisonStudio.AppHost/appsettings.Development.json
   ```

### Security Notes

- Never commit actual API keys to version control
- The `.gitignore` file is configured to exclude:
  - `appsettings.json` files
  - `appsettings.*.json` files (except examples)
  - Local development configuration files
  - AppHost configuration files
  - Environment-specific files

### Development

Run the application:
```bash
dotnet run
```

The application will be available at `https://localhost:7000` or `http://localhost:5000`.

## Features

- Multi-provider AI model support (NanoGPT, OpenRouter)
- Modern responsive UI with glassmorphism design
- Side-by-side model comparison
- **Parallel Model Execution** - Significantly faster comparisons with configurable concurrency
- Real-time prompt testing
- Rating and commenting system
- Local storage for model selections

## API Endpoints

- `GET /api/models/available` - Get all available models
- `GET /api/models/available/{provider}` - Get models for specific provider
- `POST /api/comparison/execute?executionMode=Parallel` - Execute model comparison (parallel by default)
- `POST /api/comparison/execute?executionMode=Sequential` - Execute model comparison (sequential mode)

## Parallel Execution

The application now supports parallel model execution to significantly reduce comparison times. This feature is enabled by default and can be configured through the `Execution` section in your configuration.

### Configuration

Add the following to your `appsettings.json`:

```json
{
  "Execution": {
    "MaxConcurrentRequests": 2,
    "EnableParallelExecution": true,
    "DefaultTimeout": "00:01:00",
    "RetryAttempts": 3,
    "RetryDelay": "00:00:01"
  }
}
```

### Performance Benefits

- **2 models**: ~50% faster than sequential execution
- **3 models**: ~67% faster than sequential execution
- **4+ models**: Significant time reduction with proper concurrency limits

### Usage

The API automatically uses parallel execution by default. To use sequential execution:

```bash
curl -X POST "https://your-app.com/api/comparison/execute?executionMode=Sequential" \
  -H "Content-Type: application/json" \
  -d '{"prompt": "Your prompt here", "selectedModels": ["gpt-4", "claude-3"]}'
```

### Monitoring

Performance metrics are logged for each parallel execution, including:
- Total execution time
- Average time per model
- Success/failure rates
- Concurrency utilization