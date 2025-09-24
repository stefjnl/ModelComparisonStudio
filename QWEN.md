# Model Comparison Studio - Project Documentation

## Project Overview

Model Comparison Studio is a modern web application for comparing AI language models side-by-side. It supports multiple AI providers including NanoGPT and OpenRouter, allowing users to test and evaluate AI models with a focus on building a personal knowledge base of model performance across different prompts and use cases.

### Core Features
- Multi-provider AI model support (NanoGPT, OpenRouter)
- Modern responsive UI with glassmorphism design
- Side-by-side model comparison
- **Parallel Model Execution** - Significantly faster comparisons with configurable concurrency
- Real-time prompt testing
- Rating and commenting system
- Local storage for model selections
- Prompt template library with categories
- Model rankings and statistics

## Architecture

### Technology Stack
- **Backend**: ASP.NET Core Web API (.NET 9)
- **Frontend**: Vanilla HTML, CSS (Tailwind), and JavaScript
- **Database**: SQLite for local storage
- **API Providers**: NanoGPT, OpenRouter
- **Authentication**: API keys stored in appsettings.json (single-user application)
- **Containerization**: Docker and Docker Compose

### Project Structure
```
ModelComparisonStudio/
├── ModelComparisonStudio/          # Main web application
├── ModelComparisonStudio.AppHost/  # ASP.NET Core Aspire AppHost
├── ModelComparisonStudio.Core/     # Core domain models and interfaces
├── ModelComparisonStudio.Application/ # Application services
├── ModelComparisonStudio.Infrastructure/ # Data access and infrastructure
├── ModelComparisonStudio.ServiceDefaults/ # Shared service configurations
├── ModelComparisonStudio.Tests/    # Unit and integration tests
├── appsettings.json.example/       # Example configuration
├── Dockerfile/                     # Docker configuration
└── docker-compose.yml/             # Docker Compose configuration
```

### Key Components

#### Backend Architecture
- **ModelComparisonStudio**: Main web application with controllers, services, and configuration
- **ModelComparisonStudio.Core**: Core domain entities and interfaces
- **ModelComparisonStudio.Application**: Application services and business logic
- **ModelComparisonStudio.Infrastructure**: Data access layer with Entity Framework and repositories
- **ModelComparisonStudio.ServiceDefaults**: Shared configurations for logging, monitoring, etc.
- **ModelComparisonStudio.AppHost**: Aspire hosting for development and deployment

#### Frontend Architecture
- **wwwroot/**: Static files including HTML, CSS, and JavaScript
- **js/app.js**: Main application logic
- **js/modules/**: Modular JavaScript components for specific features

## Building and Running

### Prerequisites
- .NET 9 SDK
- Node.js (for frontend development)
- API keys for NanoGPT and/or OpenRouter

### Setup

1. **Copy the example configuration:**
   ```bash
   cp appsettings.json.example ModelComparisonStudio/appsettings.json
   ```

2. **Configure your API keys:**
   Open `ModelComparisonStudio/appsettings.json` and replace the placeholder API keys with your actual keys

3. **Run the application:**
   ```bash
   dotnet run
   ```
   
   The application will be available at `https://localhost:7071` or `http://localhost:5071`.

### Docker Setup
```bash
docker-compose up --build
```

The application will be available at `http://localhost:8085`.

### Development Conventions

- **Code Style**: Follow .NET/C# standard coding conventions and .NET Foundation coding style
- **API Endpoints**: Follow RESTful conventions with versioning in the URL
- **Async/Await**: Use async/await for all I/O operations
- **Dependency Injection**: Use built-in DI container for service registration
- **Entity Framework**: Use Code First approach with migrations
- **Configuration**: Use IOptions pattern for configuration management

### Key API Endpoints

- `GET /api/models/available` - Get all available models
- `GET /api/models/available/{provider}` - Get models for specific provider
- `POST /api/comparison/execute?executionMode=Parallel` - Execute model comparison (parallel by default)
- `POST /api/comparison/execute?executionMode=Sequential` - Execute model comparison (sequential mode)
- `GET /api/prompt-templates` - Get all prompt templates
- `POST /api/prompt-templates` - Create a new prompt template
- `GET /api/evaluations/statistics` - Get model evaluation statistics

### Parallel Execution Configuration

The application supports parallel model execution to significantly reduce comparison times:

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

### Security Notes

- Never commit actual API keys to version control
- The `.gitignore` file is configured to exclude:
  - `appsettings.json` files
  - `appsettings.*.json` files (except examples)
  - Local development configuration files
  - AppHost configuration files
  - Environment-specific files

### Testing

Run unit tests:
```bash
dotnet test
```

### Deployment

The application can be deployed as a self-contained .NET application or using the provided Docker configuration.

## Project Context

This tool directly supports a manual evaluation process by:
- Replacing handwritten notes with structured data
- Enabling quick comparison of new models against trusted ones
- Building searchable knowledge base of model performance
- Streamlining the evaluation workflow for faster decision-making

The application serves as a personal productivity tool for AI model evaluation, designed specifically for rapid testing and knowledge accumulation rather than collaborative or enterprise features.

## Key Files and Directories

- `Program.cs` - Main application entry point with service configuration
- `Controllers/` - API controllers for different features
- `Services/` - Business logic services
- `Models/` - Data transfer objects and view models
- `Configuration/` - Configuration classes and options
- `wwwroot/index.html` - Main frontend page
- `wwwroot/js/app.js` - Main frontend application logic
- `Controllers/ComparisonController.cs` - Comparison functionality
- `Controllers/ModelsController.cs` - Model management
- `Controllers/PromptTemplateController.cs` - Prompt template functionality