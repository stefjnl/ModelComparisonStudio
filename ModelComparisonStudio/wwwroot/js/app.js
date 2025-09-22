import { escapeHtml, formatResponseContent, generatePromptId, isValidModelFormat } from './modules/utils.js';
import { saveModelsToStorage, loadModelsFromStorage } from './modules/storage.js';
import { displayErrorMessage, displaySuccessMessage } from './modules/ui.js';

// Model Comparison Studio - Enhanced JavaScript with Model Loading and Evaluation System

const ModelComparisonApp = (() => {
    // === API SECTION ===
    const api = {
        getApiBaseUrl: function() {
            // Use the same protocol and port as the current page to avoid CORS issues
            return window.location.origin;
        },

        async loadAvailableModels() {
            console.log('DEBUG: loadAvailableModels started');
            try {
                const baseUrl = this.getApiBaseUrl();
                console.log(`DEBUG: Using base URL: ${baseUrl}`);

                const response = await fetch(`${baseUrl}/api/models/available`);
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }

                const data = await response.json();
                console.log('DEBUG: API response data:', data);

                return {
                    nanoGPT: data.nanoGPT.models || [],
                    openRouter: data.openRouter.models || []
                };

            } catch (error) {
                console.error('Error loading available models from API:', error);
                throw error;
            }
        },

        async executeComparison(requestData) {
            console.log('Starting comparison with models:', requestData.selectedModels);

            // Use the correct API base URL
            const baseUrl = this.getApiBaseUrl();
            console.log(`DEBUG: Using base URL for comparison: ${baseUrl}`);

            const response = await fetch(`${baseUrl}/api/comparison/execute`, {
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

            return comparisonResult;
        },

        async saveEvaluation(evaluation) {
            const baseUrl = this.getApiBaseUrl();
            const response = await fetch(`${baseUrl}/api/evaluations/upsert`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    promptId: evaluation.promptId,
                    promptText: evaluation.promptText,
                    modelId: evaluation.modelId,
                    rating: evaluation.rating,
                    comment: evaluation.comment,
                    responseTimeMs: evaluation.responseTimeMs,
                    tokenCount: evaluation.tokenCount,
                    timestamp: evaluation.timestamp,
                    saved: evaluation.saved
                })
            });

            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}));
                let errorMessage = `HTTP error! status: ${response.status}`;

                if (errorData.detail) {
                    errorMessage = errorData.detail;
                } else if (errorData.errors) {
                    errorMessage = Object.values(errorData.errors).flat().join(', ');
                } else if (errorData.error) {
                    errorMessage = errorData.error;
                }

                throw new Error(errorMessage);
            }

            const result = await response.json();
            return result;
        },

        async loadRankingData(timeFilter = 'all', sortBy = 'rating') {
            try {
                const baseUrl = this.getApiBaseUrl();
                const endpoint = timeFilter === 'all'
                    ? `${baseUrl}/api/evaluations/statistics/all`
                    : `${baseUrl}/api/evaluations/statistics?timeframe=${timeFilter}`;

                const response = await fetch(endpoint);
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }

                const rankingData = await response.json();
                console.log('Ranking data loaded:', rankingData);
                return rankingData;

            } catch (error) {
                console.error('Error loading ranking data:', error);
                throw error;
            }
        },

        async deleteModel(modelId) {
            const baseUrl = this.getApiBaseUrl();
            const response = await fetch(`${baseUrl}/api/evaluations/model/${modelId}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json',
                }
            });

            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}));
                let errorMessage = `HTTP error! status: ${response.status}`;

                if (errorData.detail) {
                    errorMessage = errorData.detail;
                } else if (errorData.error) {
                    errorMessage = errorData.error;
                }

                throw new Error(errorMessage);
            }

            const result = await response.json();
            console.log('Model deletion result:', result);
            return result;
        }
    };

    // === MAIN APP CLASS ===
    class ModelComparisonApp {
    constructor() {
        this.selectedModels = [];
        this.availableModels = {
            nanoGPT: [],
            openRouter: []
        };
        this.currentComparison = null;
        this.evaluations = new Map(); // Store evaluations by modelId
        this.unsavedChanges = false;
        this.commentDebounceTimers = new Map();

        this.initializeEventListeners();

        // Use the organized API functions
        this.api = api;

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

        // Set up beforeunload handler for unsaved changes
        window.addEventListener('beforeunload', (e) => {
            if (this.unsavedChanges) {
                e.preventDefault();
                e.returnValue = 'You have unsaved evaluation changes. Are you sure you want to leave?';
            }
        });

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

        // Sidebar toggle functionality
        this.initializeSidebarToggle();

        // Ranking system controls
        this.setupRankingControls();
    }

    // Initialize sidebar toggle functionality
    initializeSidebarToggle() {
        const desktopToggle = document.getElementById('desktopSidebarToggle');
        const mobileToggle = document.getElementById('sidebarToggle');
        const sidebar = document.getElementById('sidebar');
        const mainContent = document.getElementById('mainContent');

        // Load saved state from localStorage
        const isCollapsed = localStorage.getItem('sidebarCollapsed') === 'true';

        if (isCollapsed) {
            this.collapseSidebar();
        }

        // Desktop toggle
        if (desktopToggle) {
            desktopToggle.addEventListener('click', () => {
                this.toggleSidebar();
            });
        }

        // Mobile toggle
        if (mobileToggle) {
            mobileToggle.addEventListener('click', () => {
                this.toggleSidebar();
            });
        }

        // Close mobile sidebar when clicking outside
        document.addEventListener('click', (e) => {
            if (window.innerWidth <= 1023 &&
                sidebar.classList.contains('sidebar-expanded') &&
                !sidebar.contains(e.target) &&
                !mobileToggle.contains(e.target)) {
                this.collapseSidebar();
            }
        });

        // Handle window resize
        window.addEventListener('resize', () => {
            if (window.innerWidth > 1023) {
                // Desktop view - ensure proper grid layout
                sidebar.classList.remove('sidebar-collapsed');
                sidebar.classList.add('sidebar-expanded');
                mainContent.classList.remove('main-content-full-width');
                mainContent.classList.add('main-content-expanded');
            }
        });
    }

    // Toggle sidebar visibility
    toggleSidebar() {
        const sidebar = document.getElementById('sidebar');
        const mainContent = document.getElementById('mainContent');

        if (sidebar.classList.contains('sidebar-expanded')) {
            this.collapseSidebar();
        } else {
            this.expandSidebar();
        }
    }

    // Collapse sidebar
    collapseSidebar() {
        const sidebar = document.getElementById('sidebar');
        const mainContent = document.getElementById('mainContent');

        sidebar.classList.remove('sidebar-expanded');
        sidebar.classList.add('sidebar-collapsed');

        mainContent.classList.remove('main-content-expanded');
        mainContent.classList.add('main-content-full-width');

        // Save state
        localStorage.setItem('sidebarCollapsed', 'true');

        // Update toggle button text
        this.updateToggleButtonText('Show');
    }

    // Expand sidebar
    expandSidebar() {
        const sidebar = document.getElementById('sidebar');
        const mainContent = document.getElementById('mainContent');

        sidebar.classList.remove('sidebar-collapsed');
        sidebar.classList.add('sidebar-expanded');

        mainContent.classList.remove('main-content-full-width');
        mainContent.classList.add('main-content-expanded');

        // Save state
        localStorage.setItem('sidebarCollapsed', 'false');

        // Update toggle button text
        this.updateToggleButtonText('Hide');
    }

    // Update toggle button text
    updateToggleButtonText(text) {
        const desktopToggle = document.getElementById('desktopSidebarToggle');
        if (desktopToggle) {
            const span = desktopToggle.querySelector('span');
            if (span) {
                span.textContent = `Toggle Sidebar (${text})`;
            }
        }
    }

    // Load available models from backend or fallback to appsettings
    async loadAvailableModels() {
        console.log('DEBUG: loadAvailableModels started');
        try {
            this.availableModels = await this.api.loadAvailableModels();
            console.log('DEBUG: Loaded models from API:', this.availableModels);
            this.displayAvailableModels();

        } catch (error) {
            console.error('Error loading available models from API:', error);
            displayErrorMessage('Failed to load available models from API. Loading from configuration.');
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
        console.log('DEBUG: displayAvailableModels called');
        console.log('DEBUG: Available models data:', this.availableModels);

        // Render both providers with correct internal names
        this.renderProviderModels('nanoGPT');
        this.renderProviderModels('openRouter');
        this.updateProviderCounts();

        console.log('DEBUG: Finished displaying available models');
    }

    // Render models for specific provider
    renderProviderModels(provider) {
        const models = this.availableModels[provider] || [];

        // Debug: Check what elements actually exist in the DOM
        console.log(`DEBUG: renderProviderModels called with provider: ${provider}`);
        console.log(`DEBUG: Available models for ${provider}:`, models);

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

            // Add click event with proper logging
            modelCard.addEventListener('click', () => {
                console.log(`DEBUG: Model card clicked: ${model} from ${provider}`);
                this.selectModel(model);
            });

            container.appendChild(modelCard);
        });

        console.log(`DEBUG: Rendered ${models.length} models for ${provider}`);
    }

    // Update provider counts
    updateProviderCounts() {
        document.getElementById('nanogpt-count').textContent = `${this.availableModels.nanoGPT?.length || 0} models`;
        document.getElementById('openrouter-count').textContent = `${this.availableModels.openRouter?.length || 0} models`;
    }

    // Toggle provider sections
    toggleProvider(provider) {
        console.log(`DEBUG: toggleProvider called with: ${provider}`);

        // The provider parameter from HTML onclick is already in the correct format
        // No conversion needed since HTML uses 'nanogpt' and 'openrouter'
        const content = document.getElementById(`${provider}-models`);
        const arrow = document.getElementById(`${provider}-arrow`);

        if (!content) {
            console.error(`DEBUG: Content element not found for ${provider}-models`);
            return;
        }

        if (!arrow) {
            console.error(`DEBUG: Arrow element not found for ${provider}-arrow`);
            return;
        }

        console.log(`DEBUG: Toggling ${provider} - current classes:`, content.className);

        if (content.classList.contains('max-h-0')) {
            // Expand
            console.log(`DEBUG: Expanding ${provider}`);
            content.classList.remove('max-h-0');
            content.classList.add('max-h-96');
            arrow.classList.add('rotate-180');
        } else {
            // Collapse
            console.log(`DEBUG: Collapsing ${provider}`);
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

    // Display error message with optional type for styling
    displayErrorMessage(message, type = 'error') {
        displayErrorMessage(message, type);
    }

    // Display success message
    displaySuccessMessage(message) {
        displaySuccessMessage(message);
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
        return isValidModelFormat(modelId);
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
        const resultsSection = document.getElementById('resultsSection');
        if (resultsSection) {
            console.log('DEBUG: Showing results section, removing hidden class');
            resultsSection.classList.remove('hidden');
            console.log('DEBUG: Results section classes after removal:', resultsSection.className);
        } else {
            console.error('DEBUG: resultsSection element not found!');
        }
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
        saveModelsToStorage(this.selectedModels);
    }

    loadModelsFromStorage() {
        return loadModelsFromStorage();
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

            const comparisonResult = await this.api.executeComparison(requestData);

            // Display results
            this.displayComparisonResults(comparisonResult);

        } catch (error) {
            console.error('Error during comparison:', error);

            // Handle validation errors specifically
            if (error.message.includes('HTTP error! status: 400')) {
                displayErrorMessage('Your request couldn\'t be processed. Please check your prompt length (keep it under 50,000 characters) and model selection.', 'validation-error');
            } else {
                displayErrorMessage(`Comparison failed: ${error.message}`);
            }

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
        console.log('DEBUG: displayComparisonResults called with:', result);
        console.log('DEBUG: Number of results:', result.results.length);

        // Store current comparison data for evaluations
        this.currentComparison = result;
        const prompt = document.getElementById('promptInput').value.trim();
        const promptId = this.generatePromptId(prompt);

        // Clear existing results
        const resultsContainer = document.getElementById('comparisonResults');
        if (!resultsContainer) {
            console.error('DEBUG: comparisonResults container not found!');
            this.displayErrorMessage('Results container not found in the UI');
            return;
        }

        resultsContainer.innerHTML = '';

        // Create vertical stack layout for each model result
        result.results.forEach((modelResult, index) => {
            const modelPanel = this.createModelPanel(modelResult, index, promptId, prompt);
            resultsContainer.appendChild(modelPanel);
        });

        // Re-enable UI
        this.setComparisonInProgress(false);

        // Show success message
        this.displaySuccessMessage(`Comparison completed! Processed ${result.results.length} models.`);
    }

    // Generate a unique ID for the prompt
    generatePromptId(prompt) {
        return generatePromptId(prompt);
    }

    // Create a single model panel for the vertical layout
    createModelPanel(modelResult, index, promptId, promptText) {
        const panel = document.createElement('div');
        panel.className = 'bg-slate-800/40 backdrop-blur-xl rounded-2xl border border-slate-700/30 p-6 shadow-modern-xl hover:shadow-modern-2xl transition-all duration-300';

        // Create star rating HTML
        const starRatingHtml = this.createStarRating(0);

        panel.innerHTML = `
            <div class="flex flex-col sm:flex-row sm:justify-between sm:items-center mb-4 gap-2">
                <h3 class="font-semibold text-purple-300 text-lg">${modelResult.modelId}</h3>
                <div class="flex items-center gap-2">
                    <div class="text-sm text-slate-400 font-mono metrics-display">-</div>
                    <div class="star-rating flex gap-1"
                         data-model-id="${modelResult.modelId}"
                         data-prompt-id="${promptId}"
                         data-prompt-text="${this.escapeHtml(promptText)}">
                        ${starRatingHtml}
                    </div>
                </div>
            </div>
            <div class="response-content bg-slate-900/50 rounded-xl p-4 min-h-32 text-slate-300 font-mono text-sm leading-relaxed mb-4">
                <div class="loading hidden">Loading...</div>
            </div>
            <div class="space-y-4">
                <div class="rating-section">
                    <textarea placeholder="Add your comments about this model's response..."
                              class="w-full p-3 bg-slate-700/50 border border-slate-600/50 rounded-xl text-white text-sm resize-none focus:outline-none focus:ring-2 focus:ring-purple-500 transition-all duration-200 comment-textarea"
                              data-model-id="${modelResult.modelId}"
                              data-prompt-id="${promptId}"
                              data-prompt-text="${this.escapeHtml(promptText)}"></textarea>
                </div>
            </div>
        `;

        // Populate the panel with actual data
        this.populateModelPanel(panel, modelResult, promptId, promptText);

        return panel;
    }

    // Helper to escape HTML for data attributes
    escapeHtml(text) {
        return escapeHtml(text);
    }

    // Populate a single model panel with data
    populateModelPanel(panel, modelResult, promptId, promptText) {
        const content = panel.querySelector('.response-content');
        const loading = panel.querySelector('.loading');
        const metrics = panel.querySelector('.metrics-display');
        const starsContainer = panel.querySelector('.star-rating');
        const commentTextarea = panel.querySelector('.comment-textarea');

        if (!content) {
            console.error('DEBUG: No .response-content found in panel');
            return;
        }

        console.log(`DEBUG: Populating panel with model:`, modelResult.modelId);

        // Hide loading indicator
        if (loading) loading.classList.add('hidden');

        // Format and set content with proper styling
        const formattedResponse = this.formatResponseContent(modelResult.response);
        content.innerHTML = formattedResponse;

        // Set metrics
        const timeText = `${(modelResult.responseTimeMs / 1000).toFixed(1)}s`;
        const tokenText = modelResult.tokenCount ? ` • ${modelResult.tokenCount} tokens` : '';
        const statusText = modelResult.status === 'success' ? '' : ` • ${modelResult.status}`;
        if (metrics) {
            metrics.textContent = `${timeText}${tokenText}${statusText}`;
        }

        // Add error styling if failed
        if (modelResult.status === 'error') {
            content.style.color = '#ef4444';
            content.style.fontStyle = 'italic';
        } else {
            // Success styling
            content.style.color = '#ffffff';
            content.style.fontStyle = 'normal';
        }

        // Set up star rating interaction with evaluation context
        if (starsContainer) {
            const modelId = starsContainer.getAttribute('data-model-id');
            const promptId = starsContainer.getAttribute('data-prompt-id');
            const promptText = starsContainer.getAttribute('data-prompt-text');

            // Check if we already have an evaluation for this model/prompt
            const evaluationKey = `${promptId}_${modelId}`;
            if (this.evaluations.has(evaluationKey)) {
                const evaluation = this.evaluations.get(evaluationKey);
                this.updateStarRatingUI(starsContainer, evaluation.rating || 0);
            }

            this.setupStarRating(starsContainer, modelId, promptId, promptText);
        }

        // Set up comment system
        if (commentTextarea) {
            const modelId = commentTextarea.getAttribute('data-model-id');
            const promptId = commentTextarea.getAttribute('data-prompt-id');
            const promptText = commentTextarea.getAttribute('data-prompt-text');

            // Load existing comment if available
            const evaluationKey = `${promptId}_${modelId}`;
            if (this.evaluations.has(evaluationKey)) {
                const evaluation = this.evaluations.get(evaluationKey);
                commentTextarea.value = evaluation.comment || '';
            }

            this.setupCommentSystem(commentTextarea, modelId, promptId, promptText);
        }
    }

    // Helper method to populate panels
    populatePanels(panels, result) {
        result.results.forEach((modelResult, index) => {
            if (index < panels.length) {
                const panel = panels[index];
                const content = panel.querySelector('.response-content');
                const loading = panel.querySelector('.loading');
                const metrics = panel.querySelector('.text-sm');
                const header = panel.querySelector('h3');

                if (!content) {
                    console.error(`DEBUG: No .response-content found in panel ${index}`);
                    return;
                }

                console.log(`DEBUG: Populating panel ${index} with model:`, modelResult.modelId);

                // Update the model name in the header
                if (header) {
                    header.textContent = modelResult.modelId;
                    console.log(`DEBUG: Updated header for panel ${index} to:`, modelResult.modelId);
                } else {
                    console.warn(`DEBUG: No h3 header found in panel ${index}`);
                }

                // Hide loading indicator
                if (loading) loading.classList.add('hidden');

                // Format and set content with proper styling
                const formattedResponse = this.formatResponseContent(modelResult.response);
                content.innerHTML = formattedResponse;

                // Set metrics
                const timeText = `${(modelResult.responseTimeMs / 1000).toFixed(1)}s`;
                const tokenText = modelResult.tokenCount ? ` • ${modelResult.tokenCount} tokens` : '';
                const statusText = modelResult.status === 'success' ? '' : ` • ${modelResult.status}`;
                if (metrics) {
                    metrics.textContent = `${timeText}${tokenText}${statusText}`;
                }

                // Add error styling if failed
                if (modelResult.status === 'error') {
                    content.style.color = '#ef4444';
                    content.style.fontStyle = 'italic';
                } else {
                    // Success styling
                    content.style.color = '#ffffff';
                    content.style.fontStyle = 'normal';
                }

                // Add star rating
                const starsContainer = panel.querySelector('.stars');
                if (starsContainer) {
                    starsContainer.innerHTML = this.createStarRating(0);
                    this.setupStarRating(starsContainer);
                }
            } else {
                console.warn(`DEBUG: No panel available for result index ${index}`);
            }
        });
    }

    // Format response content for better display
    formatResponseContent(response) {
        return formatResponseContent(response);
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

    setupStarRating(container, modelId, promptId, promptText) {
        const stars = container.querySelectorAll('.star');
        stars.forEach(star => {
            star.addEventListener('click', async (e) => {
                const rating = parseInt(e.target.getAttribute('data-rating'));
                await this.handleRatingChange(modelId, promptId, promptText, rating, container);
            });
        });
    }

    async handleRatingChange(modelId, promptId, promptText, rating, container) {
        // Update UI immediately for better UX
        this.updateStarRatingUI(container, rating);

        // Create or update evaluation
        const evaluation = this.getOrCreateEvaluation(modelId, promptId, promptText);
        evaluation.rating = rating;
        evaluation.saved = false;
        this.unsavedChanges = true;

        // Save to backend
        await this.saveEvaluation(evaluation, container);
    }

    updateStarRatingUI(container, rating) {
        const stars = container.querySelectorAll('.star');
        stars.forEach((star, index) => {
            const starRating = index + 1;
            star.classList.toggle('active', starRating <= rating);
        });
    }

    getOrCreateEvaluation(modelId, promptId, promptText) {
        const key = `${promptId}_${modelId}`;
        if (!this.evaluations.has(key)) {
            this.evaluations.set(key, {
                promptId: promptId,
                promptText: promptText,
                modelId: modelId,
                rating: null,
                comment: '',
                responseTimeMs: this.currentComparison?.results?.find(r => r.modelId === modelId)?.responseTimeMs || 1000,
                tokenCount: this.currentComparison?.results?.find(r => r.modelId === modelId)?.tokenCount || 0,
                timestamp: new Date().toISOString(),
                saved: false
            });
        }
        return this.evaluations.get(key);
    }

    async saveEvaluation(evaluation, container = null) {
        try {
            // Show saving state
            if (container) {
                this.showSavingState(container);
            }

            const result = await this.api.saveEvaluation(evaluation);

            // Update evaluation with server data
            evaluation.id = result.id;
            evaluation.timestamp = result.timestamp;
            evaluation.saved = true;
            this.unsavedChanges = false;

            // Show success state
            if (container) {
                this.showSavedState(container);
            }

            console.log('Evaluation saved successfully:', evaluation);

        } catch (error) {
            console.error('Error saving evaluation:', error);
            if (container) {
                this.showErrorState(container, error.message);
            }
            // Keep evaluation as unsaved
            evaluation.saved = false;
            this.unsavedChanges = true;
        }
    }

    showSavingState(container) {
        const stars = container.querySelectorAll('.star');
        stars.forEach(star => {
            star.style.opacity = '0.7';
            star.style.cursor = 'wait';
        });

        // Add saving indicator
        let savingIndicator = container.querySelector('.saving-indicator');
        if (!savingIndicator) {
            savingIndicator = document.createElement('div');
            savingIndicator.className = 'saving-indicator text-xs text-orange-400 mt-1';
            savingIndicator.textContent = 'Saving...';
            container.appendChild(savingIndicator);
        }
    }

    showSavedState(container) {
        const stars = container.querySelectorAll('.star');
        stars.forEach(star => {
            star.style.opacity = '1';
            star.style.cursor = 'pointer';
        });

        // Update saving indicator to saved
        const savingIndicator = container.querySelector('.saving-indicator');
        if (savingIndicator) {
            savingIndicator.textContent = 'Saved ✓';
            savingIndicator.className = 'saving-indicator text-xs text-green-400 mt-1';

            // Remove after 2 seconds
            setTimeout(() => {
                if (savingIndicator.parentNode) {
                    savingIndicator.remove();
                }
            }, 2000);
        }
    }

    showErrorState(container, errorMessage = null) {
        const stars = container.querySelectorAll('.star');
        stars.forEach(star => {
            star.style.opacity = '1';
            star.style.cursor = 'pointer';
        });

        // Show error indicator
        let errorIndicator = container.querySelector('.error-indicator');
        if (!errorIndicator) {
            errorIndicator = document.createElement('div');
            errorIndicator.className = 'error-indicator text-xs text-red-400 mt-1';
            errorIndicator.style.cursor = 'pointer';

            errorIndicator.addEventListener('click', () => {
                const modelId = container.getAttribute('data-model-id');
                const promptId = container.getAttribute('data-prompt-id');
                const promptText = container.getAttribute('data-prompt-text');
                const rating = parseInt(container.getAttribute('data-current-rating'));

                if (modelId && promptId && promptText && rating) {
                    this.handleRatingChange(modelId, promptId, promptText, rating, container);
                }
            });

            container.appendChild(errorIndicator);
        }

        // Update error message if provided
        if (errorMessage) {
            errorIndicator.textContent = `Error: ${errorMessage} - click to retry`;
        } else {
            errorIndicator.textContent = 'Error saving - click to retry';
        }
    }

    // Comment system methods
    setupCommentSystem(textarea, modelId, promptId, promptText) {
        // Store references for debouncing
        textarea.setAttribute('data-model-id', modelId);
        textarea.setAttribute('data-prompt-id', promptId);
        textarea.setAttribute('data-prompt-text', promptText);

        textarea.addEventListener('input', (e) => {
            this.handleCommentChange(e.target, modelId, promptId, promptText);
        });

        textarea.addEventListener('blur', (e) => {
            this.handleCommentBlur(e.target, modelId, promptId, promptText);
        });
    }

    handleCommentChange(textarea, modelId, promptId, promptText) {
        const comment = textarea.value.trim();

        // Clear existing timer
        const key = `${promptId}_${modelId}_comment`;
        if (this.commentDebounceTimers.has(key)) {
            clearTimeout(this.commentDebounceTimers.get(key));
        }

        // Create or update evaluation
        const evaluation = this.getOrCreateEvaluation(modelId, promptId, promptText);
        evaluation.comment = comment;
        evaluation.saved = false;
        this.unsavedChanges = true;

        // Show saving indicator
        this.showCommentSavingState(textarea);

        // Set debounce timer for auto-save
        const timer = setTimeout(async () => {
            await this.saveCommentEvaluation(evaluation, textarea);
        }, 500);

        this.commentDebounceTimers.set(key, timer);
    }

    handleCommentBlur(textarea, modelId, promptId, promptText) {
        const comment = textarea.value.trim();
        const evaluation = this.getOrCreateEvaluation(modelId, promptId, promptText);

        if (comment !== evaluation.comment) {
            evaluation.comment = comment;
            evaluation.saved = false;
            this.saveCommentEvaluation(evaluation, textarea);
        }
    }

    async saveCommentEvaluation(evaluation, textarea) {
        try {
            this.showCommentSavingState(textarea);

            const result = await this.api.saveEvaluation(evaluation);
            evaluation.id = result.id;
            evaluation.timestamp = result.timestamp;
            evaluation.saved = true;
            this.unsavedChanges = false;

            this.showCommentSavedState(textarea);

        } catch (error) {
            console.error('Error saving comment:', error);
            this.showCommentErrorState(textarea, error.message);
            evaluation.saved = false;
            this.unsavedChanges = true;
        }
    }

    showCommentSavingState(textarea) {
        let indicator = textarea.parentNode.querySelector('.comment-saving-indicator');
        if (!indicator) {
            indicator = document.createElement('div');
            indicator.className = 'comment-saving-indicator text-xs text-orange-400 mt-1';
            textarea.parentNode.appendChild(indicator);
        }
        indicator.textContent = 'Saving...';
    }

    showCommentSavedState(textarea) {
        const indicator = textarea.parentNode.querySelector('.comment-saving-indicator');
        if (indicator) {
            indicator.textContent = 'Saved ✓';
            indicator.className = 'comment-saving-indicator text-xs text-green-400 mt-1';

            setTimeout(() => {
                if (indicator.parentNode) {
                    indicator.remove();
                }
            }, 2000);
        }
    }

    showCommentErrorState(textarea, errorMessage = null) {
        const indicator = textarea.parentNode.querySelector('.comment-saving-indicator');
        if (indicator) {
            if (errorMessage) {
                indicator.textContent = `Error: ${errorMessage} - click to retry`;
            } else {
                indicator.textContent = 'Error saving - click to retry';
            }
            indicator.className = 'comment-saving-indicator text-xs text-red-400 mt-1 cursor-pointer';

            indicator.addEventListener('click', () => {
                const modelId = textarea.getAttribute('data-model-id');
                const promptId = textarea.getAttribute('data-prompt-id');
                const promptText = textarea.getAttribute('data-prompt-text');
                const comment = textarea.value.trim();

                if (modelId && promptId && promptText) {
                    const evaluation = this.getOrCreateEvaluation(modelId, promptId, promptText);
                    evaluation.comment = comment;
                    this.saveCommentEvaluation(evaluation, textarea);
                }
            });
        }
    }

    // Ranking system methods
    showRankingDashboard() {
        // Hide other sections
        document.getElementById('resultsSection').classList.add('hidden');

        // Show ranking dashboard
        const rankingDashboard = document.getElementById('rankingDashboard');
        if (rankingDashboard) {
            rankingDashboard.classList.remove('hidden');
            // Load ranking data
            this.loadRankingData('all', 'rating');
        }
    }

    // Load ranking data from the backend
    async loadRankingData(timeFilter = 'all', sortBy = 'rating') {
        try {
            const rankingData = await this.api.loadRankingData(timeFilter);
            console.log('Ranking data loaded:', rankingData);
            this.displayRankingData(rankingData, sortBy);

        } catch (error) {
            console.error('Error loading ranking data:', error);
            this.displayRankingError('Failed to load ranking data');
        }
    }

    // Display ranking data with sorting and filtering
    displayRankingData(data, sortBy) {
        const container = document.getElementById('rankingLeaderboard');

        if (!container) {
            console.error('Ranking leaderboard container not found');
            return;
        }

        // Sort data based on criteria
        const sortedData = this.sortRankingData(data, sortBy);

        // Clear existing content
        container.innerHTML = '';

        if (sortedData.length === 0) {
            container.innerHTML = '<div class="text-slate-400 text-center py-8">No ranking data available</div>';
            return;
        }

        // Create ranking cards
        sortedData.forEach((modelData, index) => {
            modelData.rank = index + 1;
            const cardHtml = this.createModelRankingCard(modelData);
            container.innerHTML += cardHtml;
        });
    }

    // Sort ranking data based on criteria
    sortRankingData(data, sortBy) {
        const sorted = [...data];

        switch (sortBy) {
            case 'rating':
                return sorted.sort((a, b) => (b.averageRating || 0) - (a.averageRating || 0));
            case 'responses':
                return sorted.sort((a, b) => b.totalEvaluations - a.totalEvaluations);
            case 'speed':
                return sorted.sort((a, b) => (a.averageSpeed || 999) - (b.averageSpeed || 999));
            default:
                return sorted;
        }
    }

    // Create a model ranking card
    createModelRankingCard(modelData) {
        // Convert average speed from milliseconds to seconds
        const averageSpeedInSeconds = modelData.averageSpeed ? (modelData.averageSpeed / 1000).toFixed(1) : '0.0';
        
        return `
            <div class="ranking-card bg-slate-800/40 backdrop-blur-xl rounded-2xl border border-slate-700/30 p-6 shadow-modern-xl hover:shadow-modern-2xl transition-all duration-300">
                <div class="flex items-center justify-between mb-4">
                    <div class="flex items-center gap-4">
                        <div class="ranking-position text-2xl font-bold text-purple-400">#${modelData.rank}</div>
                        <div>
                            <h3 class="text-lg font-semibold text-white">${modelData.modelId}</h3>
                            <div class="text-sm text-slate-400">${modelData.totalEvaluations} evaluations</div>
                        </div>
                    </div>
                    <div class="text-right">
                        <div class="text-2xl font-bold text-yellow-400">${(modelData.averageRating || 0).toFixed(1)}⭐</div>
                        <div class="text-sm text-slate-400">avg rating</div>
                    </div>
                </div>

                <!-- Performance metrics -->
                <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-4">
                    <div class="text-center">
                        <div class="text-lg font-semibold text-green-400">${averageSpeedInSeconds}s</div>
                        <div class="text-xs text-slate-400">avg response</div>
                    </div>
                    <div class="text-center">
                        <div class="text-lg font-semibold text-blue-400">${Math.round(modelData.averageTokens || 0)}</div>
                        <div class="text-xs text-slate-400">avg tokens</div>
                    </div>
                    <div class="text-center">
                        <div class="text-lg font-semibold text-purple-400">${Math.round(modelData.commentRate || 0)}%</div>
                        <div class="text-xs text-slate-400">with comments</div>
                    </div>
                    <div class="text-center">
                        <div class="text-lg font-semibold text-pink-400">${modelData.lastEvaluated || 0}</div>
                        <div class="text-xs text-slate-400">days ago</div>
                    </div>
                </div>

                <!-- Rating distribution -->
                <div class="mb-4">
                    <div class="flex items-center justify-between text-sm text-slate-400 mb-2">
                        <span>Rating Distribution</span>
                        <span>1⭐ - 10⭐</span>
                    </div>
                    <div class="flex gap-1">
                        ${this.createRatingDistributionBars(modelData.ratingDistribution || new Array(10).fill(0))}
                    </div>
                </div>

                <!-- Action buttons -->
                <div class="flex gap-2">
                    <button class="flex-1 bg-purple-600 hover:bg-purple-700 text-white font-medium py-2 px-4 rounded-xl transition-all duration-300"
                            onclick="app.selectModelForComparison('${modelData.modelId}')">
                        Select Model
                    </button>
                    <button class="flex-1 bg-slate-600 hover:bg-slate-700 text-white font-medium py-2 px-4 rounded-xl transition-all duration-300"
                            onclick="app.viewModelDetails('${modelData.modelId}')">
                        View Details
                    </button>
                    <button class="bg-red-600 hover:bg-red-700 text-white font-medium py-2 px-4 rounded-xl transition-all duration-300"
                            onclick="app.deleteModel('${modelData.modelId}')"
                            title="Delete all evaluations for this model">
                        🗑️ Delete
                    </button>
                </div>
            </div>
        `;
    }

    // Generate rating distribution for visualization (fallback method)
    generateRatingDistribution(modelData) {
        // Use backend-provided rating distribution if available
        if (modelData.ratingDistribution && Array.isArray(modelData.ratingDistribution)) {
            return modelData.ratingDistribution;
        }

        // Fallback to simulated distribution if no backend data
        const distribution = new Array(10).fill(0);
        const avgRating = modelData.averageRating || 5;
        const totalEvals = modelData.totalEvaluations || 1;

        // Create a realistic distribution around the average
        for (let i = 0; i < 10; i++) {
            const rating = i + 1;
            const distance = Math.abs(rating - avgRating);
            distribution[i] = Math.max(0, totalEvals * (0.3 - distance * 0.05));
        }

        // Normalize to total evaluations
        const sum = distribution.reduce((a, b) => a + b, 0);
        if (sum > 0) {
            distribution.forEach((_, i) => {
                distribution[i] = Math.round((distribution[i] / sum) * totalEvals);
            });
        }

        return distribution;
    }

    // Create rating distribution bars
    createRatingDistributionBars(distribution) {
        let barsHtml = '';
        for (let i = 0; i < 10; i++) {
            const height = Math.max(2, (distribution[i] || 0) * 3); // Minimum height of 2px
            barsHtml += `
                <div class="flex flex-col items-center gap-1">
                    <div class="w-3 bg-gradient-to-t from-purple-600 to-pink-600 rounded-sm"
                         style="height: ${height}px; min-height: 2px;"
                         title="${i + 1}⭐: ${distribution[i] || 0}"></div>
                    <span class="text-xs text-slate-500">${i + 1}</span>
                </div>
            `;
        }
        return barsHtml;
    }

    // Display ranking error
    displayRankingError(message) {
        const container = document.getElementById('rankingLeaderboard');
        if (container) {
            container.innerHTML = `
                <div class="text-red-400 text-center py-8">
                    <div class="text-lg font-semibold mb-2">Error Loading Rankings</div>
                    <div class="text-sm">${message}</div>
                </div>
            `;
        }
    }

    // Select model for comparison from rankings
    selectModelForComparison(modelId) {
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
        this.displaySuccessMessage(`Added ${modelId} from rankings`);

        // Switch back to main view
        this.showMainView();
    }

    // View model details (placeholder for future enhancement)
    viewModelDetails(modelId) {
        this.displaySuccessMessage(`Viewing details for ${modelId} - feature coming soon!`);
    }

    // Delete model from rankings
    async deleteModel(modelId) {
        if (!confirm(`Are you sure you want to delete all evaluations for model "${modelId}"? This action cannot be undone.`)) {
            return;
        }

        try {
            const result = await this.api.deleteModel(modelId);
            console.log('Model deletion result:', result);

            // Show success message
            this.displaySuccessMessage(`Successfully deleted ${result.deletedCount} evaluations for model ${modelId}`);

            // Reload ranking data to reflect changes
            const timeFilter = document.getElementById('rankingTimeFilter')?.value || 'all';
            const sortBy = document.getElementById('rankingSortBy')?.value || 'rating';
            await this.loadRankingData(timeFilter, sortBy);

        } catch (error) {
            console.error('Error deleting model:', error);
            this.displayErrorMessage(`Failed to delete model: ${error.message}`);
        }
    }

    // Show main view (hide rankings, show main content)
    showMainView() {
        document.getElementById('rankingDashboard').classList.add('hidden');
        // Don't show results section by default, let user navigate naturally
    }

    // Setup ranking controls event listeners
    setupRankingControls() {
        // Time filter
        const timeFilter = document.getElementById('rankingTimeFilter');
        if (timeFilter) {
            timeFilter.addEventListener('change', (e) => {
                this.loadRankingData(e.target.value, document.getElementById('rankingSortBy').value);
            });
        }

        // Sort by filter
        const sortBy = document.getElementById('rankingSortBy');
        if (sortBy) {
            sortBy.addEventListener('change', (e) => {
                this.sortCurrentRankingData(e.target.value);
            });
        }

        // Show rankings button
        const showRankingsBtn = document.getElementById('showRankingsBtn');
        if (showRankingsBtn) {
            showRankingsBtn.addEventListener('click', () => {
                this.showRankingDashboard();
            });
        }
    }

    // Sort current ranking data without reloading
    sortCurrentRankingData(sortBy) {
        const container = document.getElementById('rankingLeaderboard');
        if (!container) return;

        const cards = Array.from(container.querySelectorAll('.ranking-card'));
        const sortedCards = cards.sort((a, b) => {
            const aData = this.extractRankingDataFromCard(a);
            const bData = this.extractRankingDataFromCard(b);

            switch (sortBy) {
                case 'rating':
                    return (bData.averageRating || 0) - (aData.averageRating || 0);
                case 'responses':
                    return bData.totalEvaluations - aData.totalEvaluations;
                case 'speed':
                    return (aData.averageSpeed || 999) - (bData.averageSpeed || 999);
                default:
                    return 0;
            }
        });

        // Update ranks and re-render
        sortedCards.forEach((card, index) => {
            const rankElement = card.querySelector('.ranking-position');
            if (rankElement) {
                rankElement.textContent = `#${index + 1}`;
            }
        });

        container.innerHTML = '';
        sortedCards.forEach(card => container.appendChild(card));
    }

    // Extract ranking data from a card element
    extractRankingDataFromCard(card) {
        const modelId = card.querySelector('h3')?.textContent || '';
        const avgRating = parseFloat(card.querySelector('.text-yellow-400')?.textContent?.replace('⭐', '') || '0');
        const totalEvals = parseInt(card.querySelector('.text-slate-400')?.textContent?.match(/(\d+)/)?.[1] || '0');
        const avgSpeed = parseFloat(card.querySelector('.text-green-400')?.textContent?.replace('s', '') || '0');

        return {
            modelId,
            averageRating: avgRating,
            totalEvaluations: totalEvals,
            averageSpeed: avgSpeed
        };
    }
}

// === PUBLIC INTERFACE ===
// Create app instance and return public methods for HTML onclick handlers
const appInstance = new ModelComparisonApp();

return {
    // Essential methods for HTML onclick handlers
    toggleProvider: (provider) => appInstance.toggleProvider(provider),
    removeModel: (modelId) => appInstance.removeModel(modelId),
    selectModelForComparison: (modelId) => appInstance.selectModelForComparison(modelId),
    viewModelDetails: (modelId) => appInstance.viewModelDetails(modelId),
    deleteModel: (modelId) => appInstance.deleteModel(modelId),

    // Additional public methods that might be used by HTML
    showRankingDashboard: () => appInstance.showRankingDashboard(),
    showMainView: () => appInstance.showMainView(),

    // Expose the full app instance for advanced usage if needed
    getInstance: () => appInstance
};
})();

// Essential: Make it globally available for HTML onclick handlers
window.app = ModelComparisonApp;
