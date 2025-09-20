// Model Comparison Studio - JavaScript Foundation

class ModelComparisonApp {
    constructor() {
        this.selectedModels = [];
        this.availableModels = this.loadModelsFromStorage();
        this.currentComparison = null;
        
        this.initializeEventListeners();
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

    // Model management methods
    showModelInput() {
        document.getElementById('modelInputForm').classList.remove('hidden');
        document.getElementById('modelIdInput').focus();
    }

    hideModelInput() {
        document.getElementById('modelInputForm').classList.add('hidden');
        document.getElementById('modelIdInput').value = '';
    }

    async addModel() {
        const modelId = document.getElementById('modelIdInput').value.trim();
        
        if (!modelId) {
            alert('Please enter a model ID in the format: provider/model-name');
            return;
        }

        if (!this.isValidModelFormat(modelId)) {
            alert('Invalid model format. Please use: provider/model-name');
            return;
        }

        if (this.selectedModels.includes(modelId)) {
            alert('This model is already selected');
            return;
        }

        if (this.selectedModels.length >= 3) {
            alert('Maximum of 3 models can be selected');
            return;
        }

        // Simulate metadata fetch (will be implemented with actual API call)
        try {
            const metadata = await this.fetchModelMetadata(modelId);
            this.selectedModels.push(modelId);
            this.saveModelsToStorage();
            this.updateUI();
            this.hideModelInput();
        } catch (error) {
            alert('Error fetching model metadata: ' + error.message);
        }
    }

    removeModel(modelId) {
        this.selectedModels = this.selectedModels.filter(m => m !== modelId);
        this.saveModelsToStorage();
        this.updateUI();
    }

    isValidModelFormat(modelId) {
        return /^[a-zA-Z0-9_-]+\/[a-zA-Z0-9_-]+$/.test(modelId);
    }

    async fetchModelMetadata(modelId) {
        // Placeholder for actual API call to nano-gpt
        // Will be implemented when backend API is ready
        return {
            id: modelId,
            name: modelId.split('/')[1],
            contextWindow: 4096, // Default value
            provider: modelId.split('/')[0]
        };
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
        
        // Placeholder for actual API calls
        // This will be replaced with real fetch calls to the backend
        this.simulateComparison(prompt);
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

    simulateComparison(prompt) {
        const panels = document.querySelectorAll('.bg-gray-800.rounded-lg');
        
        panels.forEach((panel, index) => {
            if (index < this.selectedModels.length) {
                setTimeout(() => {
                    this.simulateResponse(panel, prompt, index);
                }, 1000 + (index * 500)); // Stagger responses
            }
        });
    }

    simulateResponse(panel, prompt, index) {
        const content = panel.querySelector('.response-content');
        const loading = panel.querySelector('.loading');
        const metrics = panel.querySelector('.text-sm');
        
        loading.classList.add('hidden');
        
        // Simulated response
        const responseText = `This is a simulated response from ${this.selectedModels[index]} for the prompt: "${prompt.substring(0, 50)}..."\n\nLorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.`;
        
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
            starsHtml += `<span class="star ${isActive ? 'active' : ''}" data-rating="${i}">⭐</span>`;
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
            if (starRating <= rating) {
                star.classList.add('active');
            } else {
                star.classList.remove('active');
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

    // UI update methods
    updateUI() {
        this.updateSelectedModelsDisplay();
        this.updateRunButtonState();
    }

    updateSelectedModelsDisplay() {
        const container = document.getElementById('selectedModels');
        
        if (this.selectedModels.length === 0) {
            container.innerHTML = '<div class="text-gray-500">No models selected</div>';
            return;
        }

        container.innerHTML = '';
        this.selectedModels.forEach(modelId => {
            const pill = document.createElement('div');
            pill.className = 'model-pill';
            pill.innerHTML = `
                ${modelId}
                <span class="remove-btn" onclick="app.removeModel('${modelId}')">×</span>
            `;
            container.appendChild(pill);
        });
    }
}

// Initialize the application
const app = new ModelComparisonApp();

// Utility functions for future API integration
class ApiClient {
    static async getModels() {
        // Will be implemented with actual API calls
        return [];
    }

    static async compareModels(prompt, modelIds) {
        // Will be implemented with actual API calls
        return [];
    }

    static async getModelMetadata(modelId) {
        // Will be implemented with actual API calls
        return null;
    }
}

// Export for global access (if needed)
window.ModelComparisonApp = ModelComparisonApp;
window.ApiClient = ApiClient;