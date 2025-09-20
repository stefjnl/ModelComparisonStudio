// Model Comparison Studio - Enhanced JavaScript with Model Loading

class ModelComparisonApp {
    constructor() {
        this.selectedModels = [];
        this.availableModels = {
            nanoGPT: [],
            openRouter: []
        };
        this.currentComparison = null;

        this.initializeEventListeners();

        // Ensure DOM is ready before loading models
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => {
                console.log('DEBUG: DOM ready, loading available models');
                this.loadAvailableModels();
            });
        } else {
            // DOM is already ready
            console.log('DEBUG: DOM already ready, loading available models immediately');
            setTimeout(() => {
                this.loadAvailableModels();
            }, 100); // Small delay for any remaining async operations
        }

        this.updateUI();
    }

    // Initialize event listeners
    initializeEventListeners() {
        // Model management
        document.getElementById('addModelBtn').addEventListener('click', () => this.showModelInput());
        document.getElementById('cancelAddModel').addEventListener('click', () => this.hideModelInput());
        document.getElementById('addModelConfirm').addEventListener('click', () => this.addModel());
        document.getElementById('modelIdInput').addEventListener('keypress', (e) => {
            if (e.key === 'Enter') this.addModel();
        });

        // Comparison
        document.getElementById('runComparisonBtn').addEventListener('click', () => this.runComparison());
        document.getElementById('promptInput').addEventListener('input', () => this.updateRunButtonState());
    }

    // Load available models from backend or fallback to appsettings
    async loadAvailableModels() {
        try {
            const response = await fetch('/api/models/available');
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data = await response.json();
            this.availableModels = {
                nanoGPT: data.nanoGPT.models || [],
                openRouter: data.openRouter.models || []
            };

            console.log('Loaded models from API:', this.availableModels);
            this.displayAvailableModels();

        } catch (error) {
            console.error('Error loading available models from API:', error);
            this.displayErrorMessage('Failed to load available models from API. Loading from configuration.');
            this.loadModelsFromConfiguration();
        }
    }

    // Load models directly from appsettings.json configuration
    loadModelsFromConfiguration() {
        // Fallback models from appsettings.json structure
        this.availableModels = {
            nanoGPT: [
                "deepseek/deepseek-chat-v3.1",
                "qwen/qwen3-coder",
                "moonshotai/kimi-k2-0905",
                "qwen/qwen3-next-80b-a3b-instruct",
                "anthropic/claude-3.5-sonnet",
                "openai/gpt-4o-mini",
                "google/gemini-2.0-flash-exp"
            ],
            openRouter: [
                "deepseek/deepseek-chat-v3.1",
                "qwen/qwen3-coder",
                "moonshotai/kimi-k2-0905",
                "qwen/qwen3-next-80b-a3b-instruct",
                "anthropic/claude-3.5-sonnet",
                "openai/gpt-4o-mini",
                "google/gemini-2.0-flash-exp"
            ]
        };

        console.log('Loaded models from configuration:', this.availableModels);
        this.displayAvailableModels();

        // Show success message for fallback
        this.displaySuccessMessage('Loaded models from configuration');
    }

    // Display available models in separate collapsible sections
    displayAvailableModels() {
        this.renderProviderModels('nanoGPT');
        this.renderProviderModels('openRouter');
        this.updateProviderCounts();
    }

    // Render models for specific provider
    renderProviderModels(provider) {
        const models = this.availableModels[provider] || [];

        // Debug: Check what elements actually exist in the DOM
        console.log(`DEBUG: Looking for ${provider}-models element`);
        console.log(`DEBUG: All elements with ID containing '${provider}':`,
            Array.from(document.querySelectorAll(`[id*="${provider}"]`)).map(el => el.id)
        );

        // The provider parameter should match the exact IDs in the HTML
        // Available models are stored as 'nanoGPT' and 'openRouter' but HTML uses 'nanogpt' and 'openrouter'
        const htmlProviderId = provider === 'nanoGPT' ? 'nanogpt' : 'openrouter';

        const container = document.getElementById(`${htmlProviderId}-models`);
        const countElement = document.getElementById(`${htmlProviderId}-count`);

        // Add defensive programming - check if elements exist
        if (!container) {
            console.error(`Container element not found: ${htmlProviderId}-models`);
            console.error(`DEBUG: Available elements in DOM:`,
                Array.from(document.querySelectorAll('[id]')).map(el => el.id)
            );
            return;
        }

        if (!countElement) {
            console.error(`Count element not found: ${htmlProviderId}-count`);
        } else {
            countElement.textContent = `${models.length} models`;
        }

        if (models.length === 0) {
            container.innerHTML = `<div class="text-slate-400 text-sm">No ${provider} models available</div>`;
            return;
        }

        container.innerHTML = '';

        models.forEach(model => {
            const modelCard = document.createElement('div');
            modelCard.className = 'model-card bg-slate-700/50 border border-slate-600 rounded-lg p-3 cursor-pointer hover:bg-slate-600/50 transition-all duration-200';
            modelCard.innerHTML = `
                <div class="font-medium text-white">${model}</div>
                <div class="text-xs text-slate-400 mt-1">${provider}</div>
            `;
            modelCard.addEventListener('click', () => this.selectModel(model));
            container.appendChild(modelCard);
        });
    }

    // Update provider counts
    updateProviderCounts() {
        document.getElementById('nanogpt-count').textContent = `${this.availableModels.nanoGPT?.length || 0} models`;
        document.getElementById('openrouter-count').textContent = `${this.availableModels.openRouter?.length || 0} models`;
    }

    // Toggle provider sections
    toggleProvider(provider) {
        // Convert to HTML format for element IDs
        const htmlProviderId = provider === 'nanoGPT' ? 'nanogpt' : 'openrouter';

        const content = document.getElementById(`${htmlProviderId}-models`);
        const arrow = document.getElementById(`${htmlProviderId}-arrow`);

        if (content.classList.contains('max-h-0')) {
            // Expand
            content.classList.remove('max-h-0');
            content.classList.add('max-h-96');
            arrow.classList.add('rotate-180');
        } else {
            // Collapse
            content.classList.remove('max-h-96');
            content.classList.add('max-h-0');
            arrow.classList.remove('rotate-180');
        }
    }

    // Render available models as interactive cards
    renderAvailableModels(models) {
        // This method is now replaced by renderProviderModels
        // Kept for backward compatibility
    }

    // Select model from available models
    selectModel(modelId) {
        if (this.selectedModels.includes(modelId)) {
            this.displayErrorMessage('This model is already selected');
            return;
        }

        if (this.selectedModels.length >= 3) {
            this.displayErrorMessage('Maximum of 3 models can be selected');
            return;
        }

        this.selectedModels.push(modelId);
        this.saveModelsToStorage();
        this.updateUI();

        // Show success message
        this.displaySuccessMessage(`Added ${modelId}`);
    }

    // Create model suggestions for the input
    createModelSuggestions(models) {
        const input = document.getElementById('modelIdInput');
        const container = document.createElement('div');
        container.id = 'modelSuggestions';
        container.className = 'model-suggestions hidden absolute z-10 w-full mt-1 bg-slate-700 border border-slate-600 rounded-xl shadow-modern-lg';

        input.parentNode.style.position = 'relative';
        if (input.nextElementSibling?.id === 'modelSuggestions') {
            input.nextElementSibling.remove();
        }

        models.forEach(item => {
            const suggestion = document.createElement('div');
            suggestion.className = 'p-3 hover:bg-slate-600 cursor-pointer transition-colors duration-200';
            suggestion.innerHTML = `
                <div class="font-medium text-white">${item.model}</div>
                <div class="text-sm text-slate-400">${item.provider}</div>
            `;
            suggestion.addEventListener('click', () => {
                input.value = item.model;
                container.classList.add('hidden');
            });
            container.appendChild(suggestion);
        });

        input.parentNode.appendChild(container);

        // Show/hide suggestions based on input
        input.addEventListener('input', (e) => {
            const query = e.target.value.toLowerCase();
            const filtered = models.filter(item =>
                item.model.toLowerCase().includes(query) ||
                item.provider.toLowerCase().includes(query)
            );

            if (filtered.length > 0 && query.length > 0) {
                container.innerHTML = '';
                filtered.forEach(item => {
                    const suggestion = document.createElement('div');
                    suggestion.className = 'p-3 hover:bg-slate-600 cursor-pointer transition-colors duration-200';
                    suggestion.innerHTML = `
                        <div class="font-medium text-white">${item.model}</div>
                        <div class="text-sm text-slate-400">${item.provider}</div>
                    `;
                    suggestion.addEventListener('click', () => {
                        input.value = item.model;
                        container.classList.add('hidden');
                    });
                    container.appendChild(suggestion);
                });
                container.classList.remove('hidden');
            } else {
                container.classList.add('hidden');
            }
        });

        // Hide suggestions when clicking outside
        document.addEventListener('click', (e) => {
            if (!container.contains(e.target) && e.target !== input) {
                container.classList.add('hidden');
            }
        });
    }

    // Load default models if API fails
    loadDefaultModels() {
        this.availableModels = {
            nanoGPT: [
                "gpt-4o-mini",
                "claude-3-5-sonnet",
                "gemini-1.5-flash"
            ],
            openRouter: [
                "deepseek/deepseek-chat-v3.1",
                "qwen/qwen3-coder",
                "moonshotai/kimi-k2-0905"
            ]
        };
    }

    // Display error message
    displayErrorMessage(message) {
        const container = document.getElementById('selectedModels');
        const errorDiv = document.createElement('div');
        errorDiv.className = 'error-message';
        errorDiv.textContent = message;
        container.appendChild(errorDiv);

        setTimeout(() => {
            errorDiv.remove();
        }, 3000);
    }

    // Display success message
    displaySuccessMessage(message) {
        const container = document.getElementById('selectedModels');
        const successDiv = document.createElement('div');
        successDiv.className = 'success-message';
        successDiv.textContent = message;
        container.appendChild(successDiv);

        setTimeout(() => {
            successDiv.remove();
        }, 3000);
    }

    // Model management methods
    showModelInput() {
        document.getElementById('modelInputForm').classList.remove('hidden');
        document.getElementById('modelIdInput').focus();
    }

    hideModelInput() {
        document.getElementById('modelInputForm').classList.add('hidden');
        document.getElementById('modelIdInput').value = '';
        const suggestions = document.getElementById('modelSuggestions');
        if (suggestions) suggestions.classList.add('hidden');
    }

    async addModel() {
        const modelId = document.getElementById('modelIdInput').value.trim();

        if (!modelId) {
            this.displayErrorMessage('Please enter a model ID');
            return;
        }

        if (!this.isValidModelFormat(modelId)) {
            this.displayErrorMessage('Invalid model format. Use: provider/model-name');
            return;
        }

        if (this.selectedModels.includes(modelId)) {
            this.displayErrorMessage('This model is already selected');
            return;
        }

        if (this.selectedModels.length >= 3) {
            this.displayErrorMessage('Maximum of 3 models can be selected');
            return;
        }

        this.selectedModels.push(modelId);
        this.saveModelsToStorage();
        this.updateUI();
        this.hideModelInput();
    }

    // Check if model exists in available models
    isValidModelFormat(modelId) {
        return /^[a-zA-Z0-9_-]+\/[a-zA-Z0-9_-]+$/.test(modelId);
    }

    removeModel(modelId) {
        this.selectedModels = this.selectedModels.filter(m => m !== modelId);
        this.saveModelsToStorage();
        this.updateUI();
    }

    // Comparison methods
    updateRunButtonState() {
        const prompt = document.getElementById('promptInput').value.trim();
        const hasModels = this.selectedModels.length > 0;
        const hasPrompt = prompt.length > 0;

        document.getElementById('runComparisonBtn').disabled = !(hasModels && hasPrompt);
    }

    async runComparison() {
        const prompt = document.getElementById('promptInput').value.trim();

        if (!prompt || this.selectedModels.length === 0) {
            return;
        }

        this.showResultsSection();
        this.prepareResponsePanels();

        // Execute real comparison
        await this.executeRealComparison(prompt);
    }

    showResultsSection() {
        document.getElementById('resultsSection').classList.remove('hidden');
    }

    prepareResponsePanels() {
        const panels = document.querySelectorAll('.bg-gray-800.rounded-lg');

        panels.forEach((panel, index) => {
            const modelName = this.selectedModels[index] || 'No model selected';
            const header = panel.querySelector('h3');
            const metrics = panel.querySelector('.text-sm');
            const content = panel.querySelector('.response-content');
            const loading = panel.querySelector('.loading');

            header.textContent = modelName;
            metrics.textContent = '-';
            content.innerHTML = '';
            loading.classList.remove('hidden');

            // Clear previous ratings and comments
            const starsContainer = panel.querySelector('.stars');
            const commentArea = panel.querySelector('textarea');
            starsContainer.innerHTML = this.createStarRating(0);
            commentArea.value = '';
        });
    }

    // Enhanced model display
    updateUI() {
        this.updateSelectedModelsDisplay();
        this.updateRunButtonState();
    }

    updateSelectedModelsDisplay() {
        const container = document.getElementById('selectedModels');

        if (this.selectedModels.length === 0) {
            container.innerHTML = '<div class="text-slate-400 text-sm">No models selected</div>';
            return;
        }

        container.innerHTML = '';
        this.selectedModels.forEach(modelId => {
            const pill = document.createElement('div');
            pill.className = 'model-pill bg-gradient-to-r from-purple-600 to-pink-600 text-white px-3 py-1 rounded-full text-sm font-medium flex items-center gap-2';
            pill.innerHTML = `
                ${modelId}
                <span class="remove-btn cursor-pointer hover:text-red-300 transition-colors duration-200" onclick="app.removeModel('${modelId}')">×</span>
            `;
            container.appendChild(pill);
        });
    }

    // Enhanced model suggestions
    createModelSuggestions(models) {
        const input = document.getElementById('modelIdInput');
        let container = document.getElementById('modelSuggestions');

        if (!container) {
            container = document.createElement('div');
            container.id = 'modelSuggestions';
            container.className = 'hidden absolute z-10 w-full mt-1 bg-slate-700/90 backdrop-blur-sm border border-slate-600 rounded-xl shadow-modern-lg max-h-60 overflow-y-auto';
            input.parentNode.style.position = 'relative';
            input.parentNode.appendChild(container);
        }

        container.innerHTML = '';

        models.forEach(item => {
            const suggestion = document.createElement('div');
            suggestion.className = 'p-3 hover:bg-slate-600/50 cursor-pointer transition-colors duration-200 border-b border-slate-600/30 last:border-b-0';
            suggestion.innerHTML = `
                <div class="font-medium text-white">${item.model}</div>
                <div class="text-xs text-slate-400">${item.provider}</div>
            `;
            suggestion.addEventListener('click', () => {
                input.value = item.model;
                container.classList.add('hidden');
            });
            container.appendChild(suggestion);
        });

        // Show suggestions
        input.addEventListener('input', (e) => {
            const query = e.target.value.toLowerCase();
            const filtered = models.filter(item =>
                item.model.toLowerCase().includes(query) ||
                item.provider.toLowerCase().includes(query)
            );

            if (filtered.length > 0 && query.length > 0) {
                container.innerHTML = '';
                filtered.forEach(item => {
                    const suggestion = document.createElement('div');
                    suggestion.className = 'p-3 hover:bg-slate-600/50 cursor-pointer transition-colors duration-200 border-b border-slate-600/30 last:border-b-0';
                    suggestion.innerHTML = `
                        <div class="font-medium text-white">${item.model}</div>
                        <div class="text-xs text-slate-400">${item.provider}</div>
                    `;
                    suggestion.addEventListener('click', () => {
                        input.value = item.model;
                        container.classList.add('hidden');
                    });
                    container.appendChild(suggestion);
                });
                container.classList.remove('hidden');
            } else {
                container.classList.add('hidden');
            }
        });

        // Hide suggestions when clicking outside
        document.addEventListener('click', (e) => {
            if (!container.contains(e.target) && e.target !== input) {
                container.classList.add('hidden');
            }
        });
    }

    // Storage methods
    saveModelsToStorage() {
        localStorage.setItem('modelComparisonStudio_models', JSON.stringify(this.selectedModels));
    }

    loadModelsFromStorage() {
        const stored = localStorage.getItem('modelComparisonStudio_models');
        return stored ? JSON.parse(stored) : [];
    }

    // Comparison methods
    async executeRealComparison(prompt) {
        try {
            // Disable UI during execution
            this.setComparisonInProgress(true);

            const requestData = {
                prompt: prompt,
                selectedModels: this.selectedModels
            };

            console.log('Starting comparison with models:', this.selectedModels);

            const response = await fetch('/api/comparison/execute', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(requestData)
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.error || `HTTP error! status: ${response.status}`);
            }

            const comparisonResult = await response.json();
            console.log('Comparison completed:', comparisonResult);

            // Display results
            this.displayComparisonResults(comparisonResult);

        } catch (error) {
            console.error('Error during comparison:', error);
            this.displayErrorMessage(`Comparison failed: ${error.message}`);
            this.setComparisonInProgress(false);
        }
    }

    // Set comparison in progress state
    setComparisonInProgress(inProgress) {
        const runButton = document.getElementById('runComparisonBtn');
        const modelSelectors = document.querySelectorAll('.model-card');

        if (inProgress) {
            runButton.disabled = true;
            runButton.textContent = 'Running Comparison...';
            // Disable model selection during execution
            modelSelectors.forEach(card => {
                card.style.pointerEvents = 'none';
                card.style.opacity = '0.5';
            });
        } else {
            runButton.disabled = false;
            runButton.textContent = 'Run Comparison';
            // Re-enable model selection
            modelSelectors.forEach(card => {
                card.style.pointerEvents = 'auto';
                card.style.opacity = '1';
            });
        }
    }

    // Display comparison results
    displayComparisonResults(result) {
        const panels = document.querySelectorAll('.bg-gray-800.rounded-lg');

        result.results.forEach((modelResult, index) => {
            if (index < panels.length) {
                const panel = panels[index];
                const content = panel.querySelector('.response-content');
                const loading = panel.querySelector('.loading');
                const metrics = panel.querySelector('.text-sm');

                // Hide loading indicator
                loading.classList.add('hidden');

                // Set content
                content.textContent = modelResult.response;

                // Set metrics
                const timeText = `${(modelResult.responseTimeMs / 1000).toFixed(1)}s`;
                const tokenText = modelResult.tokenCount ? ` • ${modelResult.tokenCount} tokens` : '';
                const statusText = modelResult.status === 'success' ? '' : ` • ${modelResult.status}`;
                metrics.textContent = `${timeText}${tokenText}${statusText}`;

                // Add error styling if failed
                if (modelResult.status === 'error') {
                    content.style.color = '#ef4444';
                    content.style.fontStyle = 'italic';
                }

                // Add star rating
                const starsContainer = panel.querySelector('.stars');
                starsContainer.innerHTML = this.createStarRating(0);
                this.setupStarRating(starsContainer);
            }
        });

        // Re-enable UI
        this.setComparisonInProgress(false);

        // Show success message
        this.displaySuccessMessage(`Comparison completed! Processed ${result.results.length} models.`);
    }

    // Comparison methods
    simulateComparison(prompt) {
        const panels = document.querySelectorAll('.bg-slate-800\\/40');

        panels.forEach((panel, index) => {
            if (index < this.selectedModels.length) {
                setTimeout(() => {
                    this.simulateResponse(panel, prompt, index);
                }, 1000 + (index * 500));
            }
        });
    }

    simulateResponse(panel, prompt, index) {
        const modelName = this.selectedModels[index];
        const content = panel.querySelector('.response-content');
        const loading = panel.querySelector('.loading');
        const metrics = panel.querySelector('.text-sm');

        loading.classList.add('hidden');

        const responseText = `This is a simulated response from ${modelName} for the prompt: "${prompt.substring(0, 50)}..."\n\nThis demonstrates how the comparison will work once the API integration is complete. The real implementation will fetch actual responses from both NanoGPT and OpenRouter APIs.`;

        content.textContent = responseText;
        metrics.textContent = '2.1s • 128 tokens';

        // Add star rating
        const starsContainer = panel.querySelector('.stars');
        starsContainer.innerHTML = this.createStarRating(0);
        this.setupStarRating(starsContainer);
    }

    createStarRating(rating = 0) {
        let starsHtml = '';
        for (let i = 1; i <= 10; i++) {
            const isActive = i <= rating;
            starsHtml += `<span class="star ${isActive ? 'active' : ''}" data-rating="${i}" title="${i}/10">⭐</span>`;
        }
        return starsHtml;
    }

    setupStarRating(container) {
        const stars = container.querySelectorAll('.star');
        stars.forEach(star => {
            star.addEventListener('click', (e) => {
                const rating = parseInt(e.target.getAttribute('data-rating'));
                this.updateStarRating(container, rating);
            });
        });
    }

    updateStarRating(container, rating) {
        const stars = container.querySelectorAll('.star');
        stars.forEach((star, index) => {
            const starRating = index + 1;
            star.classList.toggle('active', starRating <= rating);
        });
    }
}

// Initialize the application
const app = new ModelComparisonApp();

// Export for global access
window.ModelComparisonApp = ModelComparisonApp;
