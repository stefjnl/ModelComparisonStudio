// Model Details Page JavaScript Module
class ModelDetailsPage {
    constructor() {
        this.modelId = null;
        this.modelData = null;
        this.evaluations = null;

        this.init();
    }

    init() {
        // Get model ID from URL parameters
        const urlParams = new URLSearchParams(window.location.search);
        this.modelId = urlParams.get('modelId');

        if (!this.modelId) {
            this.showError('No model ID provided in URL');
            return;
        }

        // Set up event listeners
        this.setupEventListeners();

        // Load model data
        this.loadModelData();
    }

    setupEventListeners() {
        // Back to rankings buttons
        document.getElementById('backToRankingsBtn')?.addEventListener('click', () => {
            window.location.href = 'index.html#rankingDashboard';
        });

        document.getElementById('backToRankingsBtnBottom')?.addEventListener('click', () => {
            window.location.href = 'index.html#rankingDashboard';
        });

        // Retry button
        document.getElementById('retryBtn')?.addEventListener('click', () => {
            this.loadModelData();
        });

        // Select model button
        document.getElementById('selectModelBtn')?.addEventListener('click', () => {
            this.selectModel();
        });
    }

    async loadModelData() {
        try {
            this.showLoading();

            // Load all model statistics and filter for our model
            const statsResponse = await fetch('/api/evaluations/statistics');
            if (!statsResponse.ok) {
                throw new Error(`Failed to load model statistics: ${statsResponse.status}`);
            }
            const allStats = await statsResponse.json();

            // Find our model in the statistics
            this.modelData = allStats.find(stat => stat.modelId === this.modelId);

            if (!this.modelData) {
                throw new Error(`Model "${this.modelId}" not found in statistics`);
            }

            // Load all evaluations and filter for this model
            const evaluationsResponse = await fetch('/api/evaluations');
            if (!evaluationsResponse.ok) {
                throw new Error(`Failed to load evaluations: ${evaluationsResponse.status}`);
            }
            const allEvaluations = await evaluationsResponse.json();
            // Filter evaluations for this specific model
            this.evaluations = allEvaluations.filter(e => e.modelId === this.modelId);

            this.renderModelData();
            this.hideLoading();

        } catch (error) {
            console.error('Error loading model data:', error);
            this.showError(error.message);
        }
    }

    showLoading() {
        document.getElementById('loadingState').classList.remove('hidden');
        document.getElementById('errorState').classList.add('hidden');
        document.getElementById('modelDetailsContent').classList.add('hidden');
    }

    hideLoading() {
        document.getElementById('loadingState').classList.add('hidden');
    }

    showError(message) {
        document.getElementById('loadingState').classList.add('hidden');
        document.getElementById('modelDetailsContent').classList.add('hidden');
        document.getElementById('errorState').classList.remove('hidden');
        document.getElementById('errorMessage').textContent = message;
    }

    renderModelData() {
        if (!this.modelData) return;

        // Update model header
        document.getElementById('modelName').textContent = this.formatModelName(this.modelData.modelId);
        document.getElementById('modelProvider').textContent = this.getProviderFromModelId(this.modelData.modelId);
        document.getElementById('averageRating').textContent = this.modelData.averageRating ? `${this.modelData.averageRating.toFixed(1)}★` : 'N/A';

        // Update metrics cards
        document.getElementById('totalEvaluations').textContent = this.modelData.totalEvaluations || 0;
        document.getElementById('avgResponseTime').textContent = this.modelData.averageSpeed ? `${Math.round(this.modelData.averageSpeed)} ms` : 'N/A';
        document.getElementById('avgTokens').textContent = this.modelData.averageTokens ? Math.round(this.modelData.averageTokens) : 'N/A';
        document.getElementById('commentRate').textContent = this.modelData.commentRate ? `${this.modelData.commentRate.toFixed(1)}%` : '0%';

        // Render rating distribution
        this.renderRatingDistribution();

        // Render comments
        this.renderComments();

        // Show content
        document.getElementById('modelDetailsContent').classList.remove('hidden');
    }

    renderRatingDistribution() {
        const container = document.getElementById('ratingDistribution');
        if (!this.modelData.ratingDistribution) {
            container.innerHTML = '<p class="text-slate-400 text-center py-4">No rating data available</p>';
            return;
        }

        const maxCount = Math.max(...this.modelData.ratingDistribution);
        const totalRatings = this.modelData.ratingDistribution.reduce((sum, count) => sum + count, 0);

        container.innerHTML = '';

        for (let i = 10; i >= 1; i--) {
            const count = this.modelData.ratingDistribution[i - 1] || 0;
            const percentage = totalRatings > 0 ? (count / totalRatings) * 100 : 0;

            const ratingBar = document.createElement('div');
            ratingBar.className = 'rating-bar';
            ratingBar.innerHTML = `
                <div class="flex items-center gap-2 min-w-0">
                    <span class="text-sm font-medium text-slate-300 min-w-0">${i}★</span>
                    <span class="text-xs text-slate-400">(${count})</span>
                </div>
                <div class="rating-bar-bg">
                    <div class="rating-bar-fill" style="width: ${percentage}%"></div>
                </div>
            `;

            container.appendChild(ratingBar);
        }
    }

    renderComments() {
        const container = document.getElementById('commentsSection');
        const noComments = document.getElementById('noComments');

        if (!this.evaluations || this.evaluations.length === 0) {
            container.innerHTML = '';
            noComments.classList.remove('hidden');
            return;
        }

        noComments.classList.add('hidden');
        container.innerHTML = '';

        // Filter and sort comments (most recent first, only with comments)
        const comments = this.evaluations
            .filter(e => e.Comment && e.Comment.trim())
            .sort((a, b) => new Date(b.UpdatedAt) - new Date(a.UpdatedAt))
            .slice(0, 5); // Show only 5 most recent

        if (comments.length === 0) {
            noComments.classList.remove('hidden');
            return;
        }

        comments.forEach(evaluation => {
            const commentCard = document.createElement('div');
            commentCard.className = 'bg-slate-700/30 rounded-xl p-4 border border-slate-600/30';

            const rating = evaluation.Rating ? `${evaluation.Rating}★` : 'No rating';
            const date = new Date(evaluation.UpdatedAt).toLocaleDateString();

            commentCard.innerHTML = `
                <div class="flex items-start justify-between mb-2">
                    <div class="flex items-center gap-2">
                        <div class="text-yellow-400 text-sm font-medium">${rating}</div>
                        <div class="text-slate-400 text-xs">${date}</div>
                    </div>
                </div>
                <p class="text-slate-200 text-sm leading-relaxed">${this.escapeHtml(evaluation.Comment)}</p>
            `;

            container.appendChild(commentCard);
        });
    }

    formatModelName(modelId) {
        if (!modelId) return 'Unknown Model';

        // Clean up model ID for display
        return modelId
            .replace(/[_-]/g, ' ')
            .replace(/\b\w/g, l => l.toUpperCase());
    }

    getProviderFromModelId(modelId) {
        if (!modelId) return 'Unknown Provider';

        // Simple heuristic to determine provider
        if (modelId.includes('openai') || modelId.includes('gpt')) {
            return 'OpenAI';
        } else if (modelId.includes('anthropic') || modelId.includes('claude')) {
            return 'Anthropic';
        } else if (modelId.includes('google') || modelId.includes('gemini') || modelId.includes('palm')) {
            return 'Google';
        } else if (modelId.includes('meta') || modelId.includes('llama')) {
            return 'Meta';
        } else if (modelId.includes('nano') || modelId.includes('deepseek')) {
            return 'NanoGPT';
        } else {
            return 'Unknown Provider';
        }
    }

    selectModel() {
        // Store selected model in localStorage for the main app
        localStorage.setItem('selectedModelId', this.modelId);

        // Navigate back to main app
        window.location.href = 'index.html';

        // Show success message (will be handled by main app)
        this.showNotification('Model selected successfully!', 'success');
    }

    showNotification(message, type = 'info') {
        // Simple notification - in a real app you might use a more sophisticated system
        const notification = document.createElement('div');
        notification.className = `fixed top-4 right-4 z-50 px-4 py-2 rounded-xl text-white font-medium shadow-modern-lg ${
            type === 'success' ? 'bg-green-600' : 'bg-blue-600'
        }`;
        notification.textContent = message;

        document.body.appendChild(notification);

        setTimeout(() => {
            notification.remove();
        }, 3000);
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
}

// Initialize the page when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new ModelDetailsPage();
});