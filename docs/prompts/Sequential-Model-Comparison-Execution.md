# Implementation Prompt for AI Coding Assistant

## Context
You are implementing a sequential model comparison feature for a .NET Core web application that compares AI language model responses. The app currently supports single-model analysis and needs to be extended for multi-model comparison.

## Current Architecture
- **Backend:** .NET Core Web API with minimal API approach
- **Frontend:** HTML/CSS (Tailwind)/JavaScript  
- **AI Provider:** OpenRouter API (existing `AIService.AnalyzeCodeAsync()` method)
- **Models:** Loaded from `appsettings.json` AvailableModels array
- **Existing Services:** `AIService`, `AIAnalysisOrchestrator`, Controllers for model and execution APIs

## Task: Implement US-002-001 Sequential Model Comparison

### Requirements Summary
Create a new API endpoint that accepts multiple selected models and a prompt, then executes API calls sequentially (one after another, not parallel) while capturing detailed performance metrics for each model.

### Implementation Specifications

#### 1. Backend API Endpoint
**Create:** `POST /api/comparison/execute`

**Request Model:**
```csharp
public class ComparisonRequest
{
    public string Prompt { get; set; }
    public List<string> SelectedModels { get; set; } // Model IDs from frontend
}
```

**Response Model:**
```csharp
public class ComparisonResponse
{
    public string ComparisonId { get; set; }
    public string Prompt { get; set; }
    public List<ModelResult> Results { get; set; }
    public DateTime ExecutedAt { get; set; }
}

public class ModelResult
{
    public string ModelId { get; set; }
    public string Response { get; set; }
    public long ResponseTimeMs { get; set; }
    public int? TokenCount { get; set; }
    public string Status { get; set; } // "success", "error", "timeout"
    public string ErrorMessage { get; set; }
}
```

#### 2. Service Layer Implementation
**Extend existing `AIService`** with new method:
```csharp
public async Task<List<ModelResult>> ExecuteSequentialComparison(
    string prompt, 
    List<string> modelIds, 
    CancellationToken cancellationToken = default)
```

**Implementation Requirements:**
- Use existing `AnalyzeCodeAsync()` method internally for each model
- Execute models sequentially using `await` (not `Task.WhenAll`)
- Wrap each call with `Stopwatch` for response time measurement
- Use try-catch for individual model failures
- Extract token count from OpenRouter API response if available
- Continue execution even if one model fails

#### 3. Controller Implementation
**Create:** `ComparisonController` or extend existing controller

**Method signature:**
```csharp
[HttpPost("execute")]
public async Task<IActionResult> ExecuteComparison([FromBody] ComparisonRequest request)
```

**Implementation Requirements:**
- Validate that 1-3 models are selected
- Validate prompt is not empty
- Generate unique `ComparisonId` using `Guid.NewGuid()`
- Call service layer for sequential execution
- Return 200 with `ComparisonResponse` on success
- Return 400 for validation errors
- Use proper async/await patterns

#### 4. Frontend Integration Requirements
**Modify existing JavaScript** to call new endpoint:

**Function to implement:**
```javascript
async function executeComparison(prompt, selectedModelIds) {
    // Disable UI during execution
    // Show progress indicators
    // Call POST /api/comparison/execute
    // Handle response and display results
    // Re-enable UI
}
```

**UI State Management:**
- Disable "Run Comparison" button during execution
- Show "Processing model X of Y..." progress indicator
- Display results as they come back (not implemented in this story)
- Handle and display error states

#### 5. Error Handling Requirements
- **Individual Model Failures:** Continue to next model, record error in `ModelResult`
- **Complete Failure:** Return 500 with error details
- **Timeout Handling:** 60-second timeout per model call
- **Rate Limiting:** Handle 429 responses gracefully

#### 6. Performance Measurement
**Capture for each model:**
- Response time (milliseconds from request start to response complete)
- Token count (extract from OpenRouter response metadata if available)
- API latency vs processing time (optional enhancement)

### Code Organization
- **Controller:** `Controllers/ComparisonController.cs` or extend existing
- **Models:** `Models/ComparisonRequest.cs` and `Models/ComparisonResponse.cs`  
- **Service:** Extend existing `AIService.cs` with comparison method
- **Frontend:** Modify existing JavaScript files for comparison execution

### Testing Validation
After implementation, the system should:
1. Accept 1-3 selected models and a prompt
2. Execute API calls one at a time (observable in network logs)
3. Return results even if one model fails
4. Show accurate response times for each model
5. Disable UI appropriately during execution

### Constraints
- **Time limit:** 30 minutes implementation
- **No database changes** required for this story
- **Reuse existing** `AIService.AnalyzeCodeAsync()` method
- **Sequential execution only** - no parallel processing
- **Simple error handling** - don't overcomplicate

Please implement this feature step by step, ensuring each component works before moving to the next. Focus on getting the core sequential execution working first, then add performance metrics and error handling.