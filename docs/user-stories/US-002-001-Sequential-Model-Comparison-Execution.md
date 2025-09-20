# US-002-001: Sequential Model Comparison Execution

**As a** user  
**I want to** send my test prompt to all selected models one after another  
**So that** I can compare their responses while capturing accurate performance metrics

## Requirements

### Functional Requirements
- Execute API calls to selected models sequentially (not parallel)
- Capture individual response times, latency, and performance metrics per model
- Display responses in order of completion 
- Show real-time progress as each model completes
- Handle individual model failures without stopping the sequence
- Store all responses and metrics for evaluation

### Technical Requirements
- Modify existing `AIService.AnalyzeCodeAsync()` to accept multiple models
- Create new comparison endpoint separate from single-model analysis
- Implement sequential execution with performance tracking
- Return structured response containing all model results + metrics

## Acceptance Criteria

### ✅ Sequential Execution
- **Given** I have 3 models selected and enter a prompt
- **When** I click "Run Comparison" 
- **Then** API calls execute one model at a time (not simultaneously)
- **And** I see "Model 1 of 3 processing..." progress indicators

### ✅ Performance Metrics Capture
- **Given** each model API call
- **When** the call completes (success or failure)
- **Then** system records response time, token count, and API latency
- **And** metrics are displayed alongside each response

### ✅ Progressive Results Display
- **Given** sequential execution in progress
- **When** each model completes its response
- **Then** that model's result appears immediately (don't wait for all 3)
- **And** remaining models show loading state

### ✅ Failure Resilience  
- **Given** one model fails (timeout, rate limit, error)
- **When** the failure occurs
- **Then** execution continues to next model in sequence
- **And** failed model shows error message instead of response
- **And** successful models still display their results

### ✅ Response Structure
- **Given** comparison completion
- **When** all models finish (success/failure)
- **Then** frontend receives array of results with structure:
```json
{
  "comparisonId": "guid",
  "prompt": "user input text", 
  "results": [
    {
      "modelId": "qwen/qwen3-coder",
      "response": "model output text",
      "responseTime": 1250,
      "tokenCount": 89,
      "status": "success"
    }
  ]
}
```

### ✅ UI State Management
- **Given** comparison is running
- **When** user interactions occur
- **Then** "Run Comparison" button is disabled during execution
- **And** model selection is locked during execution
- **And** user can cancel mid-execution (optional enhancement)

## Implementation Notes
- **Estimated Time:** 30 minutes
- **Dependencies:** Existing `AIService`, selected models from frontend
- **API Endpoint:** `POST /api/comparison/execute`
- **Error Handling:** Individual model failures don't break the sequence

This approach gives you clean performance data for each model while avoiding rate limit issues from parallel calls.