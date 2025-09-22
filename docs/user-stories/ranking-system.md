# Story 2: Response Evaluation - Technical Implementation

## **UI/UX Design**

### **Rating Interface**
**Star Component Architecture:**
- Interactive 10-star rating system positioned below each model response panel (COMPLETED)
- Click sets permanent rating, triggers visual confirmation (brief green checkmark)
- Rating persists visually until page refresh or new comparison

**Comment System:**
- Expandable text area below star rating (collapsed by default to save space)
- Auto-resize textarea as user types (min 2 rows, max 6 rows)
- Character counter shows remaining space (500 char limit recommended)
- Save button appears when text is entered, auto-saves on blur event

### **Visual Feedback States**
**Unsaved State:**
- Subtle orange border on rating/comment components
- "Saving..." indicator with spinner during API call
- Disabled state prevents multiple simultaneous saves

**Saved State:**
- Green checkmark icon appears briefly (2 seconds)
- Border returns to normal purple accent
- Timestamp appears: "Saved 2 minutes ago"

## **Data Flow Architecture**

### **Frontend State Management**
**Evaluation Object Structure:**

evaluation = {
  promptId: "uuid-v4-generated",
  promptText: "original prompt string",
  modelId: "provider/model-name",
  rating: 1-10, // null until set
  comment: "user text", // empty string default
  responseTime: milliseconds,
  tokenCount: from_api_metadata,
  timestamp: ISO_datetime,
  saved: boolean_flag
}


**State Tracking:**
- Maintain array of evaluation objects for current comparison session
- Track dirty state per evaluation (rating/comment changed but not saved)
- Debounce comment saves (500ms delay after typing stops)
- Queue rating saves immediately on star click

### **API Persistence Layer**

**Backend Endpoint Design:**
- `POST /api/evaluations` - Create new evaluation record
- `PUT /api/evaluations/{id}` - Update existing evaluation
- Validation: rating 1-10, comment max 500 chars, required fields present
- Return evaluation ID and timestamp for frontend confirmation

**Database Persistence via SqlLite:**
```sql
CREATE TABLE evaluations (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    prompt_id TEXT NOT NULL,
    prompt_text TEXT NOT NULL,
    model_id TEXT NOT NULL,
    rating INTEGER CHECK(rating >= 1 AND rating <= 10),
    comment TEXT DEFAULT '',
    response_time_ms INTEGER,
    token_count INTEGER,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

**Indexing Strategy:**
- Index on model_id for ranking queries
- Index on created_at for chronological filtering
- Compound index on (model_id, created_at) for model history views

## **User Interaction Patterns**

### **Rating Workflow**
1. **User clicks star 7** → immediate visual feedback (stars 1-7 fill)
2. **Frontend validates** → rating in 1-10 range
3. **API call triggered** → POST/PUT to /api/evaluations
4. **Loading state** → brief spinner on star component
5. **Success response** → green checkmark, update saved timestamp
6. **Error handling** → red border, retry button, preserve user input

### **Comment Workflow**
1. **User clicks comment area** → expands textarea
2. **User types** → auto-save triggers after 500ms typing pause
3. **API call** → PUT request with comment text
4. **Success** → subtle confirmation, no intrusive UI changes
5. **Error** → Clear visual indication of unsaved evaluations


### **Data Validation**
**Frontend Validation:**
- Validate prompt and model IDs exist

**Backend Validation:**
- Sanitize comment input (prevent XSS)
- Verify model_id exists in system
- Reject duplicate evaluations for same prompt/model combination

### **User Experience Edge Cases**
- Navigate away with unsaved changes → browser confirmation dialog
- Rapid clicking on stars → debounce to prevent duplicate saves
- Empty comments → save empty string, don't block rating saves

## **Performance Considerations**

### **Database Optimization**
- Use prepared statements for SQL injection prevention

### **Frontend Optimization**
- Lazy load star rating component only when response arrives
- Virtual scrolling for comment history if viewing many evaluations
- Minimize DOM updates during typing (debounced renders)
- Cache evaluation state in sessionStorage for page refresh persistence

### **API Response Times**
- Target < 200ms for evaluation saves
- Use async/await pattern for non-blocking UI updates
- Implement request timeout (5 seconds) with user-friendly error messages
- Background sync for evaluation updates when possible

This implementation provides a smooth, reliable evaluation experience that builds user confidence in the data collection process while maintaining performance even with hundreds of stored evaluations.