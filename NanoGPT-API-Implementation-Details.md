# NanoGPT API Implementation Details

## API Endpoint and Base Configuration

**Base URL**: `https://nano-gpt.com/api/v1`
**Endpoint**: `/chat/completions`
**Full URL**: `https://nano-gpt.com/api/v1/chat/completions`

## HTTP Headers

The following headers are set for NanoGPT API calls:

1. **Authorization**: `Bearer {api_key}`
   - The API key is loaded from `appsettings.json` NanoGPT configuration
   - Example: `Bearer 7ad29096-8f81-45f7-a0f9-2dbed4455c3d`

2. **Accept**: `text/event-stream`
   - NanoGPT requires this accept header for their API

3. **Content-Type**: `application/json` (for request body)

## Request Body Structure

The request body follows this JSON structure:

```json
{
  "model": "mapped-model-name",
  "messages": [
    {
      "role": "user",
      "content": "user prompt here"
    }
  ],
  "stream": false,
  "temperature": 0.7,
  "max_tokens": 1000,
  "top_p": 1,
  "frequency_penalty": 0,
  "presence_penalty": 0,
  "cache_control": {
    "enabled": false
  }
}
```

## Model Name Mapping

The application maps configuration model IDs to NanoGPT API model names:

| Configuration Model ID | NanoGPT API Model Name |
|------------------------|------------------------|
| `deepseek-ai/DeepSeek-V3.1` | `deepseek-chat` |
| `moonshotai/Kimi-K2-Instruct-0905` | `kimi-chat` |
| `qwen/qwen3-next-80b-a3b-instruct` | `qwen-chat` |
| `anthropic/claude-3.5-sonnet` | `claude-chat` |
| `openai/gpt-4o-mini` | `chatgpt-4o-latest` |
| `google/gemini-2.0-flash-exp` | `gemini-chat` |
| Other models | `chatgpt-4o-latest` (fallback) |

## Implementation in AIService.cs

### Key Components

1. **Provider Assignment** ([`GetProviderInfo()`](ModelComparisonStudio/Services/AIService.cs:385)):
   - Models with `:free` suffix → OpenRouter
   - Models in NanoGPT available models list → NanoGPT
   - Fallback to OpenRouter for unknown models

2. **Request Construction** ([`AnalyzeCodeAsync()`](ModelComparisonStudio/Services/AIService.cs:94)):
   ```csharp
   string nanoGptModelName = MapModelIdToNanoGptName(modelId);
   
   var request = new
   {
       model = nanoGptModelName,
       messages = new[]
       {
           new { role = "user", content = prompt }
       },
       stream = false,
       temperature = 0.7,
       max_tokens = maxTokens,
       top_p = 1,
       frequency_penalty = 0,
       presence_penalty = 0,
       cache_control = new { enabled = false }
   };
   ```

3. **Model Mapping** ([`MapModelIdToNanoGptName()`](ModelComparisonStudio/Services/AIService.cs:430)):
   - Converts configuration model IDs to NanoGPT API model names
   - Uses pattern matching for common model families

## Example Request

**Request to**: `POST https://nano-gpt.com/api/v1/chat/completions`

**Headers**:
```
Authorization: Bearer 7ad29096-8f81-45f7-a0f9-2dbed4455c3d
Accept: text/event-stream
Content-Type: application/json
```

**Body**:
```json
{
  "model": "deepseek-chat",
  "messages": [
    {
      "role": "user",
      "content": "Hello, can you introduce yourself?"
    }
  ],
  "stream": false,
  "temperature": 0.7,
  "max_tokens": 1000,
  "top_p": 1,
  "frequency_penalty": 0,
  "presence_penalty": 0,
  "cache_control": {
    "enabled": false
  }
}
```

## Response Structure

Successful responses follow this format:

```json
{
  "id": "chatcmpl-...",
  "object": "chat.completion",
  "created": 1758426462,
  "model": "deepseek-ai/DeepSeek-V3",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "Response content here"
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 12,
    "completion_tokens": 74,
    "total_tokens": 86
  },
  "nanoGPT": {
    "cost": 0,
    "inputTokens": 12,
    "outputTokens": 74,
    "paymentSource": "USD"
  }
}
```

## Error Handling

The implementation includes comprehensive error handling:
- HTTP status code checking
- JSON deserialization with error recovery
- Timeout handling (5-minute timeout)
- Cancellation support
- Detailed logging for debugging

## Configuration

**appsettings.json**:
```json
{
  "NanoGPT": {
    "ApiKey": "your-api-key-here",
    "BaseUrl": "https://nano-gpt.com/api/v1",
    "AvailableModels": [
      "deepseek-ai/DeepSeek-V3.1",
      "moonshotai/Kimi-K2-Instruct-0905",
      "..."
    ]
  }
}
```

This implementation ensures robust communication with the NanoGPT API while maintaining compatibility with the existing OpenRouter integration.