// Template UI Module for ModelComparisonStudio
// Handles UI-specific functionality for template cards and layout improvements

export class TemplateUI {
    constructor(templateManager) {
        this.templateManager = templateManager;
        this.expandedCards = new Set();
        this.initializeUI();
    }

    initializeUI() {
        this.setupTemplateCardInteractions();
        this.setupStickyPromptSection();
        this.setupCompactViewToggle();
    }

    setupTemplateCardInteractions() {
        document.addEventListener('click', (e) => {
            const card = e.target.closest('.template-card');
            if (card) {
                const templateId = card.getAttribute('data-template-id');
                this.toggleTemplateCard(card, templateId);
            }
        });
    }

    toggleTemplateCard(card, templateId) {
        const isExpanded = card.classList.contains('expanded');
        
        if (isExpanded) {
            card.classList.remove('expanded');
            card.classList.add('collapsed');
            this.expandedCards.delete(templateId);
        } else {
            card.classList.remove('collapsed');
            card.classList.add('expanded');
            this.expandedCards.add(templateId);
        }
        
        // Update the chevron icon
        const chevron = card.querySelector('.chevron');
        if (chevron) {
            chevron.classList.toggle('rotate-180');
        }
    }

    setupStickyPromptSection() {
        // For smaller screens, make the prompt section sticky at the bottom
        if (window.innerWidth < 1024) {
            const promptSection = document.querySelector('#promptSection');
            if (promptSection) {
                promptSection.classList.add('mobile-template-controls');
            }
        }

        // Listen for window resize events
        window.addEventListener('resize', () => {
            const promptSection = document.querySelector('#promptSection');
            if (!promptSection) return;

            if (window.innerWidth < 1024) {
                promptSection.classList.add('mobile-template-controls');
            } else {
                promptSection.classList.remove('mobile-template-controls');
            }
        });
    }

    setupCompactViewToggle() {
        // Add compact view functionality for template cards
        document.addEventListener('click', (e) => {
            if (e.target.closest('.toggle-compact-view')) {
                this.toggleCompactView();
            }
        });
    }

    toggleCompactView() {
        const cards = document.querySelectorAll('.template-card');
        cards.forEach(card => {
            card.classList.toggle('compact-view');
        });
    }

    // Method to render compact template cards
    renderCompactTemplateCard(template) {
        // Handle both PascalCase (C# style) and camelCase (JavaScript style) property names
        const templateId = template.Id || template.id;
        const templateTitle = template.Title || template.title;
        const templateDescription = template.Description || template.description;
        const templateContent = template.Content || template.content;
        const templateUsageCount = template.UsageCount || template.usageCount;

        if (!templateId) {
            console.error('renderCompactTemplateCard: template ID is missing', template);
            return '<div class="template-card error">Template missing ID</div>';
        }

        const isFavorite = this.templateManager.favorites.has(templateId);
        const isSelected = this.templateManager.selectedTemplateId === templateId;
        const variableCount = this.templateManager.parseVariables(templateContent).length;

        const chevronClass = this.expandedCards.has(templateId) ? 'rotate-180' : '';
        
        return `
            <div class="template-card compact-template-card ${isSelected ? 'selected' : ''} ${this.expandedCards.has(templateId) ? 'expanded' : 'collapsed'}" data-template-id="${templateId}">
                <div class="template-card-header">
                    <h5 class="template-name">${templateTitle || 'Untitled Template'}</h5>
                    <div class="template-card-actions">
                        <button class="template-action-btn edit-btn" data-template-id="${templateId}" title="Edit Template">
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path>
                                <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path>
                            </svg>
                        </button>
                        <button class="template-action-btn delete-btn" data-template-id="${templateId}" title="Delete Template">
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <polyline points="3,6 5,6 21,6"></polyline>
                                <path d="M19,6v14a2,2 0 0,1-2,2H7a2,2 0 0,1-2-2V6m3,0V4a2,2 0 0,1,2-2h4a2,2 0 0,1,2,2v2"></path>
                                <line x1="10" y1="11" x2="10" y2="17"></line>
                                <line x1="14" y1="11" x2="14" y2="17"></line>
                            </svg>
                        </button>
                        <button class="favorite-btn ${isFavorite ? 'favorited' : ''}" data-template-id="${templateId}" title="Toggle Favorite">
                            ${isFavorite ? '★' : '☆'}
                        </button>
                        <button class="chevron-btn" title="Expand/Collapse">
                            <svg class="chevron w-4 h-4 text-slate-400 ${chevronClass}" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <path d="M6 9l6 6 6-6"></path>
                            </svg>
                        </button>
                    </div>
                </div>
                <div class="template-description">${templateDescription || 'No description available'}</div>
                <div class="detailed-info">
                    <div class="template-meta">
                        <span class="template-variables">${variableCount} variables</span>
                        <span class="template-usage">${templateUsageCount || 0} uses</span>
                    </div>
                    <div class="template-actions">
                        <button class="use-template-btn" data-template-id="${templateId}">Use Template</button>
                    </div>
                </div>
            </div>
        `;
    }

    // Method to render template library with progressive disclosure
    renderTemplateLibraryWithProgressiveDisclosure() {
        const contentContainer = document.getElementById('templateLibraryContent');
        if (!contentContainer) return;

        // Group templates by category
        const templatesByCategory = this.templateManager.groupTemplatesByCategory();

        let html = '';

        // Render categories
        if (this.templateManager.categories.length === 0) {
            html = '<div class="text-slate-400 text-center py-8">No categories available</div>';
        } else {
            this.templateManager.categories.forEach(category => {
                const categoryId = category.Id || category.id;
                const templates = templatesByCategory[categoryId] || [];
                if (templates.length > 0) {
                    html += this.renderCategorySectionWithProgressiveDisclosure(category, templates);
                }
            });

            if (html === '') {
                html = '<div class="text-slate-400 text-center py-8">No templates available</div>';
            }
        }

        contentContainer.innerHTML = html;
        this.templateManager.attachTemplateEvents();
    }

    renderCategorySectionWithProgressiveDisclosure(category, templates) {
        return `
            <div class="category-section" data-category-id="${category.id || category.Id}">
                <div class="category-header">
                    <span class="category-color" style="background-color: ${category.color || '#6b7280'}"></span>
                    <h4 class="category-name">${category.name || category.Name}</h4>
                    <span class="category-count">${templates.length} templates</span>
                </div>
                <div class="template-grid">
                    ${templates.map(template => this.renderCompactTemplateCard(template)).join('')}
                </div>
            </div>
        `;
    }

    // Update the template library rendering to use compact cards
    updateTemplateLibrary() {
        this.renderTemplateLibraryWithProgressiveDisclosure();
    }
}

// Export a function to initialize the template UI
export const initializeTemplateUI = (templateManager) => {
    return new TemplateUI(templateManager);
};