/**
 * Coding Assignment Module
 * Handles single-model coding assignments with templates and progress tracking
 */

class CodingAssignmentApp {
    constructor() {
        this.currentAssignment = null;
        this.selectedModel = null;
        this.selectedTemplate = null;
        this.isExecuting = false;
        this.progressInterval = null;

        this.initializeEventListeners();
        this.loadModels();
        this.loadTemplates();
        this.initializeCharacterCounter();
    }

    /**
     * Initialize event listeners
     */
    initializeEventListeners() {
        // Model selection
        document.getElementById('modelSelect').addEventListener('change', (e) => {
            this.onModelSelected(e.target.value);
        });

        // Template selection
        document.getElementById('templateSelect').addEventListener('change', (e) => {
            this.onTemplateSelected(e.target.value);
        });

        // Assignment input
        document.getElementById('assignmentInput').addEventListener('input', (e) => {
            this.updateCharacterCount();
        });

        // Execute assignment
        document.getElementById('executeAssignmentBtn').addEventListener('click', () => {
            this.executeAssignment();
        });

        // Save as template
        document.getElementById('saveAsTemplateBtn').addEventListener('click', () => {
            this.saveAsTemplate();
        });

        // Load template
        document.getElementById('loadTemplateBtn').addEventListener('click', () => {
            this.loadTemplate();
        });

        // New assignment
        document.getElementById('newAssignmentBtn').addEventListener('click', () => {
            this.resetAssignment();
        });

        // Export results
        document.getElementById('exportResultsBtn').addEventListener('click', () => {
            this.exportResults();
        });

        // Save assignment
        document.getElementById('saveAssignmentBtn').addEventListener('click', () => {
            this.saveAssignment();
        });
    }

    /**
     * Load available models for coding assignments
     */
    async loadModels() {
        try {
            const response = await fetch('/api/coding-assignment/models');

            // Check if response is JSON
            const contentType = response.headers.get('content-type');
            if (!contentType || !contentType.includes('application/json')) {
                throw new Error(`Expected JSON response, got ${contentType || 'unknown'}. Check if the API endpoint exists.`);
            }

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(`HTTP ${response.status}: ${errorText}`);
            }

            const models = await response.json();

            const modelSelect = document.getElementById('modelSelect');
            modelSelect.innerHTML = '<option value="">Select a model...</option>';

            models.forEach(model => {
                const option = document.createElement('option');
                option.value = model.id;
                option.textContent = `${model.name} (${model.provider})`;
                option.dataset.model = JSON.stringify(model);
                modelSelect.appendChild(option);
            });

            console.log(`Loaded ${models.length} coding models`);
        } catch (error) {
            console.error('Error loading models:', error);
            this.showError(`Failed to load available models: ${error.message}`);

            // Provide fallback models for demo purposes
            this.loadFallbackModels();
        }
    }

    /**
     * Load fallback models when API is not available
     */
    loadFallbackModels() {
        const fallbackModels = [
            { id: 'gpt-4', name: 'GPT-4', provider: 'OpenRouter', description: 'OpenAI GPT-4 model', contextWindow: 8192, recommendedForCoding: true },
            { id: 'claude-3-sonnet', name: 'Claude 3 Sonnet', provider: 'OpenRouter', description: 'Anthropic Claude model', contextWindow: 200000, recommendedForCoding: true },
            { id: 'deepseek-coder', name: 'DeepSeek Coder', provider: 'OpenRouter', description: 'DeepSeek coding model', contextWindow: 32768, recommendedForCoding: true }
        ];

        const modelSelect = document.getElementById('modelSelect');
        modelSelect.innerHTML = '<option value="">Select a model...</option>';

        fallbackModels.forEach(model => {
            const option = document.createElement('option');
            option.value = model.id;
            option.textContent = `${model.name} (${model.provider})`;
            option.dataset.model = JSON.stringify(model);
            modelSelect.appendChild(option);
        });

        console.log('Loaded fallback models for demo purposes');
    }

    /**
     * Load coding assignment templates
     */
    async loadTemplates() {
        try {
            const response = await fetch('/api/coding-assignment/templates');

            // Check if response is JSON
            const contentType = response.headers.get('content-type');
            if (!contentType || !contentType.includes('application/json')) {
                throw new Error(`Expected JSON response, got ${contentType || 'unknown'}. Check if the API endpoint exists.`);
            }

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(`HTTP ${response.status}: ${errorText}`);
            }

            const templates = await response.json();

            const templateSelect = document.getElementById('templateSelect');
            templateSelect.innerHTML = '<option value="">Select a template...</option>';

            // Group templates by category
            const templatesByCategory = {};
            templates.forEach(template => {
                if (!templatesByCategory[template.category]) {
                    templatesByCategory[template.category] = [];
                }
                templatesByCategory[template.category].push(template);
            });

            // Add templates grouped by category
            Object.entries(templatesByCategory).forEach(([category, categoryTemplates]) => {
                const optgroup = document.createElement('optgroup');
                optgroup.label = category;

                categoryTemplates.forEach(template => {
                    const option = document.createElement('option');
                    option.value = template.id;
                    option.textContent = template.name;
                    option.dataset.template = JSON.stringify(template);
                    optgroup.appendChild(option);
                });

                templateSelect.appendChild(optgroup);
            });

            console.log(`Loaded ${templates.length} coding templates`);
        } catch (error) {
            console.error('Error loading templates:', error);
            this.showError(`Failed to load assignment templates: ${error.message}`);

            // Provide fallback templates for demo purposes
            this.loadFallbackTemplates();
        }
    }

    /**
     * Load fallback templates when API is not available
     */
    loadFallbackTemplates() {
        const fallbackTemplates = [
            {
                id: 'code-review',
                name: 'Code Review',
                description: 'Review and improve existing code',
                category: 'Code Review',
                promptTemplate: 'Please review the following code and provide:\n\n1. **Code Quality Assessment**: Rate the code quality (1-10) and explain your reasoning\n2. **Bug Detection**: Identify any bugs, security issues, or potential problems\n3. **Performance Analysis**: Analyze performance bottlenecks and suggest optimizations\n4. **Best Practices**: Check adherence to coding standards and best practices\n5. **Improvements**: Suggest specific improvements with code examples\n6. **Documentation**: Recommend documentation improvements\n\nCode to review:\n{CODE}',
                defaultTimeout: 600000,
                isPublic: true
            },
            {
                id: 'feature-implementation',
                name: 'Feature Implementation',
                description: 'Implement a new feature from scratch',
                category: 'Implementation',
                promptTemplate: 'Please implement the following feature:\n\n**Requirements:**\n{FEATURE_REQUIREMENTS}\n\n**Technical Specifications:**\n- Language: {LANGUAGE}\n- Framework: {FRAMEWORK}\n- Database: {DATABASE}\n- Additional constraints: {CONSTRAINTS}\n\n**Implementation Guidelines:**\n1. Follow best practices for the specified language/framework\n2. Include proper error handling and validation\n3. Add comprehensive documentation\n4. Include unit tests if applicable\n5. Consider security implications\n6. Optimize for performance and maintainability\n\nPlease provide a complete, production-ready implementation with explanations.',
                defaultTimeout: 900000,
                isPublic: true
            },
            {
                id: 'debugging',
                name: 'Debug and Fix',
                description: 'Debug and fix issues in existing code',
                category: 'Debugging',
                promptTemplate: 'I have the following code that has issues. Please help me debug and fix it:\n\n**Code:**\n{CODE}\n\n**Problem Description:**\n{PROBLEM_DESCRIPTION}\n\n**Error Messages:**\n{ERROR_MESSAGES}\n\n**Expected Behavior:**\n{EXPECTED_BEHAVIOR}\n\nPlease:\n1. **Analyze the Issue**: Identify what is causing the problem\n2. **Root Cause**: Explain why the issue occurs\n3. **Solution**: Provide the corrected code\n4. **Prevention**: Suggest how to avoid similar issues in the future\n5. **Testing**: Recommend tests to verify the fix',
                defaultTimeout: 600000,
                isPublic: true
            }
        ];

        const templateSelect = document.getElementById('templateSelect');
        templateSelect.innerHTML = '<option value="">Select a template...</option>';

        // Group fallback templates by category
        const templatesByCategory = {};
        fallbackTemplates.forEach(template => {
            if (!templatesByCategory[template.category]) {
                templatesByCategory[template.category] = [];
            }
            templatesByCategory[template.category].push(template);
        });

        // Add fallback templates grouped by category
        Object.entries(templatesByCategory).forEach(([category, categoryTemplates]) => {
            const optgroup = document.createElement('optgroup');
            optgroup.label = category;

            categoryTemplates.forEach(template => {
                const option = document.createElement('option');
                option.value = template.id;
                option.textContent = template.name;
                option.dataset.template = JSON.stringify(template);
                optgroup.appendChild(option);
            });

            templateSelect.appendChild(optgroup);
        });

        console.log('Loaded fallback templates for demo purposes');
    }

    /**
     * Handle model selection
     */
    onModelSelected(modelId) {
        if (!modelId) {
            this.selectedModel = null;
            document.getElementById('selectedModelInfo').classList.add('hidden');
            return;
        }

        const option = document.querySelector(`#modelSelect option[value="${modelId}"]`);
        const model = JSON.parse(option.dataset.model);

        this.selectedModel = model;
        this.updateModelInfo(model);
        this.validateAssignment();
    }

    /**
     * Handle template selection
     */
    onTemplateSelected(templateId) {
        if (!templateId) {
            this.selectedTemplate = null;
            document.getElementById('templatePreview').classList.add('hidden');
            return;
        }

        const option = document.querySelector(`#templateSelect option[value="${templateId}"]`);
        const template = JSON.parse(option.dataset.template);

        this.selectedTemplate = template;
        this.updateTemplatePreview(template);
        this.validateAssignment();
    }

    /**
     * Update model information display
     */
    updateModelInfo(model) {
        document.getElementById('modelName').textContent = model.name;
        document.getElementById('modelDescription').textContent = model.description;
        document.getElementById('modelContextWindow').textContent = `${model.contextWindow.toLocaleString()} tokens context window`;
        document.getElementById('selectedModelInfo').classList.remove('hidden');
    }

    /**
     * Update template preview
     */
    updateTemplatePreview(template) {
        document.getElementById('templateDescription').textContent = template.description;
        document.getElementById('templatePreview').classList.remove('hidden');
    }

    /**
     * Initialize character counter
     */
    initializeCharacterCounter() {
        this.updateCharacterCount();
    }

    /**
     * Update character and word count
     */
    updateCharacterCount() {
        const textarea = document.getElementById('assignmentInput');
        const text = textarea.value;

        const charCount = text.length;
        const wordCount = text.trim() ? text.trim().split(/\s+/).length : 0;

        document.getElementById('charCount').textContent = charCount.toLocaleString();
        document.getElementById('wordCount').textContent = wordCount.toLocaleString();

        // Change color based on length
        const counter = document.getElementById('charCount').parentElement;
        if (charCount > 45000) {
            counter.className = 'text-red-400';
        } else if (charCount > 30000) {
            counter.className = 'text-yellow-400';
        } else {
            counter.className = 'text-slate-400';
        }
    }

    /**
     * Validate assignment before execution
     */
    validateAssignment() {
        const modelSelected = !!this.selectedModel;
        const hasContent = document.getElementById('assignmentInput').value.trim().length > 0;

        const executeBtn = document.getElementById('executeAssignmentBtn');
        executeBtn.disabled = !modelSelected || !hasContent || this.isExecuting;

        return modelSelected && hasContent;
    }

    /**
     * Execute the coding assignment
     */
    async executeAssignment() {
        if (!this.validateAssignment() || this.isExecuting) {
            return;
        }

        this.isExecuting = true;
        this.showProgress();

        try {
            const assignmentData = this.prepareAssignmentData();

            // Check if we have valid data
            if (!assignmentData.modelId || !assignmentData.assignment) {
                throw new Error('Please select a model and provide assignment content');
            }

            const response = await fetch('/api/coding-assignment/execute', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(assignmentData)
            });

            // Check if response is JSON
            const contentType = response.headers.get('content-type');
            if (!contentType || !contentType.includes('application/json')) {
                const errorText = await response.text();
                throw new Error(`Expected JSON response, got ${contentType || 'unknown'}. Server response: ${errorText.substring(0, 200)}...`);
            }

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(`HTTP ${response.status}: ${errorData.error || 'Unknown error'}`);
            }

            const result = await response.json();
            this.currentAssignment = result;
            this.showResults(result);
            this.startProgressTracking(result.assignmentId);

        } catch (error) {
            console.error('Error executing assignment:', error);
            this.showError('Failed to execute coding assignment: ' + error.message);
            this.hideProgress();

            // Provide demo results for testing
            this.showDemoResults();
        } finally {
            this.isExecuting = false;
        }
    }

    /**
     * Prepare assignment data for submission
     */
    prepareAssignmentData() {
        const title = document.getElementById('assignmentTitle').value.trim() || 'Untitled Assignment';
        const assignment = document.getElementById('assignmentInput').value.trim();
        const timeoutSetting = document.getElementById('timeoutSelect').value;

        // Determine timeout based on setting
        let timeout = 'auto';
        switch (timeoutSetting) {
            case 'quick':
                timeout = 'quick';
                break;
            case 'standard':
                timeout = 'standard';
                break;
            case 'extended':
                timeout = 'extended';
                break;
            default:
                timeout = 'auto';
        }

        return {
            modelId: this.selectedModel.id,
            assignment: assignment,
            title: title,
            templateId: this.selectedTemplate?.id,
            timeoutSetting: timeout,
            enableProgressTracking: document.getElementById('enableProgressTracking').checked,
            enableStreaming: document.getElementById('enableStreaming').checked
        };
    }

    /**
     * Show progress section
     */
    showProgress() {
        document.getElementById('progressSection').classList.remove('hidden');
        document.getElementById('resultsSection').classList.add('hidden');
        document.getElementById('assignmentSection').classList.add('opacity-50', 'pointer-events-none');
    }

    /**
     * Hide progress section
     */
    hideProgress() {
        document.getElementById('progressSection').classList.add('hidden');
        document.getElementById('assignmentSection').classList.remove('opacity-50', 'pointer-events-none');
    }

    /**
     * Start progress tracking
     */
    startProgressTracking(assignmentId) {
        if (this.progressInterval) {
            clearInterval(this.progressInterval);
        }

        let progress = 0;
        const maxProgress = 90; // Don't go to 100% until actually complete

        this.progressInterval = setInterval(() => {
            if (progress < maxProgress) {
                progress += Math.random() * 3; // Random progress increment
                this.updateProgress(Math.min(progress, maxProgress));
            }
        }, 1000);

        // Check for completion every 2 seconds
        const checkCompletion = setInterval(async () => {
            try {
                const response = await fetch(`/api/coding-assignment/status/${assignmentId}`);
                if (response.ok) {
                    const status = await response.json();
                    if (status.status === 'completed' || status.status === 'failed') {
                        clearInterval(this.progressInterval);
                        clearInterval(checkCompletion);
                        this.updateProgress(100);
                        setTimeout(() => {
                            this.hideProgress();
                            this.showResults(status);
                        }, 500);
                    }
                }
            } catch (error) {
                console.error('Error checking assignment status:', error);
            }
        }, 2000);
    }

    /**
     * Update progress display
     */
    updateProgress(percentage) {
        const progressBar = document.getElementById('progressBar');
        const progressText = document.getElementById('progressText');

        progressBar.style.width = `${percentage}%`;

        if (percentage < 30) {
            progressText.textContent = 'Initializing assignment...';
        } else if (percentage < 60) {
            progressText.textContent = 'Processing requirements...';
        } else if (percentage < 90) {
            progressText.textContent = 'Generating solution...';
        } else {
            progressText.textContent = 'Finalizing results...';
        }

        // Update time remaining estimate
        const remaining = Math.max(0, 100 - percentage);
        const minutes = Math.ceil(remaining * 0.1); // Rough estimate
        document.getElementById('timeRemaining').textContent = `${minutes} min remaining`;
    }

    /**
     * Show assignment results
     */
    showResults(result) {
        // Update result info
        document.getElementById('resultModel').textContent = this.selectedModel.name;
        document.getElementById('resultTime').textContent = `${result.responseTimeMs}ms`;
        document.getElementById('resultStatus').textContent = result.status;
        document.getElementById('resultTokens').textContent = result.tokenCount?.toLocaleString() || 'N/A';

        // Show results content
        const resultsContent = document.getElementById('resultsContent');
        resultsContent.innerHTML = this.formatResults(result);

        // Show results section
        document.getElementById('resultsSection').classList.remove('hidden');
        document.getElementById('progressSection').classList.add('hidden');

        // Scroll to results
        document.getElementById('resultsSection').scrollIntoView({ behavior: 'smooth' });
    }

    /**
     * Format results for display
     */
    formatResults(result) {
        const response = result.response;

        // Try to detect if it's code and format accordingly
        if (this.isCodeResponse(response)) {
            return `
                <div class="space-y-4">
                    <div class="code-block">
                        <pre><code class="language-javascript">${this.escapeHtml(response)}</code></pre>
                    </div>
                </div>
            `;
        } else {
            // Format as markdown-style text
            return `
                <div class="prose prose-invert max-w-none">
                    <div class="whitespace-pre-wrap text-slate-300 leading-relaxed">
                        ${this.formatMarkdown(response)}
                    </div>
                </div>
            `;
        }
    }

    /**
     * Check if response appears to be code
     */
    isCodeResponse(response) {
        const codePatterns = [
            /function\s+\w+/,
            /class\s+\w+/,
            /const\s+\w+\s*=/,
            /let\s+\w+\s*=/,
            /var\s+\w+\s*=/,
            /import\s+/,
            /export\s+/,
            /public\s+class/,
            /def\s+\w+/,
            /<?php/
        ];

        return codePatterns.some(pattern => pattern.test(response));
    }

    /**
     * Basic markdown formatting
     */
    formatMarkdown(text) {
        return text
            .replace(/^### (.*$)/gm, '<h3 class="text-lg font-semibold text-white mt-4 mb-2">$1</h3>')
            .replace(/^## (.*$)/gm, '<h2 class="text-xl font-semibold text-white mt-6 mb-3">$1</h2>')
            .replace(/^# (.*$)/gm, '<h1 class="text-2xl font-bold text-white mt-8 mb-4">$1</h1>')
            .replace(/\*\*(.*?)\*\*/g, '<strong class="text-white">$1</strong>')
            .replace(/\*(.*?)\*/g, '<em class="text-slate-300">$1</em>')
            .replace(/`(.*?)`/g, '<code class="bg-slate-700 px-2 py-1 rounded text-sm text-purple-300">$1</code>')
            .replace(/\n\n/g, '</p><p class="mb-4">')
            .replace(/\n/g, '<br>');
    }

    /**
     * Escape HTML characters
     */
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    /**
     * Save assignment as template
     */
    async saveAsTemplate() {
        const title = document.getElementById('assignmentTitle').value.trim();
        const assignment = document.getElementById('assignmentInput').value.trim();

        if (!title || !assignment) {
            this.showError('Please provide both title and assignment content');
            return;
        }

        try {
            // For now, just show a message - in a real app, this would save to the backend
            this.showSuccess('Template saved successfully! (This is a demo - actual saving would require backend integration)');
        } catch (error) {
            console.error('Error saving template:', error);
            this.showError('Failed to save template');
        }
    }

    /**
     * Load template into assignment
     */
    loadTemplate() {
        if (!this.selectedTemplate) {
            this.showError('Please select a template first');
            return;
        }

        // Confirm if there's existing content
        const currentContent = document.getElementById('assignmentInput').value.trim();
        if (currentContent && !confirm('This will replace your current assignment content. Continue?')) {
            return;
        }

        document.getElementById('assignmentInput').value = this.selectedTemplate.promptTemplate;
        document.getElementById('assignmentTitle').value = this.selectedTemplate.name;
        this.updateCharacterCount();
        this.validateAssignment();

        this.showSuccess('Template loaded successfully!');
    }

    /**
     * Reset assignment form
     */
    resetAssignment() {
        if (this.isExecuting) {
            if (!confirm('Assignment is still running. Are you sure you want to start a new one?')) {
                return;
            }
        }

        // Clear form
        document.getElementById('assignmentTitle').value = '';
        document.getElementById('assignmentInput').value = '';
        document.getElementById('modelSelect').value = '';
        document.getElementById('templateSelect').value = '';

        // Reset state
        this.currentAssignment = null;
        this.selectedModel = null;
        this.selectedTemplate = null;

        // Hide sections
        document.getElementById('selectedModelInfo').classList.add('hidden');
        document.getElementById('templatePreview').classList.add('hidden');
        document.getElementById('resultsSection').classList.add('hidden');
        document.getElementById('progressSection').classList.add('hidden');
        document.getElementById('assignmentSection').classList.remove('opacity-50', 'pointer-events-none');

        // Clear intervals
        if (this.progressInterval) {
            clearInterval(this.progressInterval);
            this.progressInterval = null;
        }

        this.updateCharacterCount();
        this.validateAssignment();

        console.log('Assignment form reset');
    }

    /**
     * Export assignment results
     */
    exportResults() {
        if (!this.currentAssignment) {
            this.showError('No assignment results to export');
            return;
        }

        const exportData = {
            assignment: this.currentAssignment,
            model: this.selectedModel,
            template: this.selectedTemplate,
            exportedAt: new Date().toISOString()
        };

        const dataStr = JSON.stringify(exportData, null, 2);
        const dataBlob = new Blob([dataStr], { type: 'application/json' });

        const url = URL.createObjectURL(dataBlob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `coding-assignment-${this.currentAssignment.assignmentId}.json`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);

        this.showSuccess('Assignment results exported successfully!');
    }

    /**
     * Save assignment to local storage
     */
    saveAssignment() {
        if (!this.currentAssignment) {
            this.showError('No assignment to save');
            return;
        }

        const savedAssignments = JSON.parse(localStorage.getItem('savedAssignments') || '[]');
        savedAssignments.push({
            ...this.currentAssignment,
            savedAt: new Date().toISOString()
        });

        localStorage.setItem('savedAssignments', JSON.stringify(savedAssignments));
        this.showSuccess('Assignment saved to local storage!');
    }

    /**
     * Show error message
     */
    showError(message) {
        this.showNotification(message, 'error');
    }

    /**
     * Show success message
     */
    showSuccess(message) {
        this.showNotification(message, 'success');
    }

    /**
     * Show demo results when API is not available
     */
    showDemoResults() {
        const demoResult = {
            assignmentId: 'demo-' + Date.now(),
            modelId: this.selectedModel?.id || 'gpt-4',
            assignment: document.getElementById('assignmentInput').value,
            response: `# Demo Response

This is a demonstration of the coding assignment system. The API endpoints are not currently available, but here's what a typical response would look like:

## Analysis Complete

Your coding assignment has been processed successfully. Here's the implementation:

### 1. Project Structure
\`\`\`javascript
// Main application file
const express = require('express');
const app = express();
const port = 3000;

// Middleware
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

// Routes
app.get('/api/health', (req, res) => {
    res.json({ status: 'healthy', timestamp: new Date().toISOString() });
});

// Error handling
app.use((err, req, res, next) => {
    console.error(err.stack);
    res.status(500).json({ error: 'Something went wrong!' });
});

app.listen(port, () => {
    console.log(\`Server running on port \${port}\`);
});
\`\`\`

### 2. Key Features Implemented
- ✅ RESTful API design
- ✅ Error handling middleware
- ✅ JSON request/response handling
- ✅ Health check endpoint
- ✅ Proper HTTP status codes

### 3. Performance Considerations
- Uses efficient JSON parsing
- Implements proper error boundaries
- Follows Express.js best practices

### 4. Security Measures
- Input validation middleware
- Rate limiting (recommended)
- CORS configuration
- Helmet.js for security headers

## Next Steps
1. Add authentication middleware
2. Implement database integration
3. Add comprehensive testing
4. Set up monitoring and logging
5. Deploy to production environment

---
*This demo response shows the type of detailed, structured output you can expect from the coding assignment system.*`,
            responseTimeMs: 2500,
            tokenCount: 1247,
            status: 'completed',
            errorMessage: '',
            executedAt: new Date().toISOString(),
            timeoutUsed: 300000
        };

        this.currentAssignment = demoResult;
        this.showResults(demoResult);
        this.startProgressTracking(demoResult.assignmentId);
    }

    /**
     * Show notification
     */
    showNotification(message, type) {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `fixed top-4 right-4 z-50 p-4 rounded-xl shadow-modern-lg backdrop-blur-xl border ${
            type === 'error'
                ? 'bg-red-500/20 border-red-500/30 text-red-300'
                : 'bg-green-500/20 border-green-500/30 text-green-300'
        }`;

        notification.innerHTML = `
            <div class="flex items-center">
                <span class="flex-1">${message}</span>
                <button onclick="this.parentElement.parentElement.remove()" class="ml-4 text-current hover:opacity-70">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
                    </svg>
                </button>
            </div>
        `;

        document.body.appendChild(notification);

        // Auto-remove after 5 seconds
        setTimeout(() => {
            if (notification.parentElement) {
                notification.remove();
            }
        }, 5000);
    }
}

// Initialize the application when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.codingAssignmentApp = new CodingAssignmentApp();
    console.log('Coding Assignment Studio initialized');
});