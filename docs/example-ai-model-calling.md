Here's a general description of how the application could call the correct AI model(s) based on the dropdown selection and appsettings.json configuration:

## Model Selection and Call Flow Architecture

### 1. Configuration Setup (appsettings.json)
The application reads model configuration from appsettings.json

### 2. Frontend Model Selection (JavaScript)
The frontend loads available models via [`ModelApiController.GetAvailableModels() which reads from the `AvailableModels` configuration. The JavaScript code in [`model-selector.js`] handles dropdown population and stores the selected model ID.

### 3. Request Flow to Backend
When analysis starts, the frontend sends a [`RunComparisonRequest`](AICodeReviewer.Web/Models/RunAnalysisRequest.cs:15) containing the selected model IDs to [`ExecutionApiController.StartAnalysis()`].

### 4. Backend Processing Pipeline
The request flows through:
1. **ExecutionApiController** - Validates model selection
2. **IComparisonService** - Orchestrates analysis
3. **ComparisonOrchestrator**  - Handles timeout and fallback logic
4. **AIService** - Makes the actual AI calls

### 5. External API Call
The [`AIService.AnalyzeCodeAsync()`](AICodeReviewer.Web/Infrastructure/Services/AIService.cs:66) method:
- Receives the selected `model` parameter directly from frontend
- Builds prompt using language-specific templates
- Constructs HTTP request to OpenRouter or NanoGPT API (based on the provided configuration)
- Sets headers including API key from configuration
- Sends POST request to `https://api.openrouter.ai/api/v1/chat/completions` with the selected model ID