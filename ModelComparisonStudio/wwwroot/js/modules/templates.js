// Template Management System for ModelComparisonStudio
// Provides library, editor, and integration with existing comparison workflow

export class TemplateManager {
    constructor() {
        this.templates = [];
        this.categories = [];
        this.currentTemplate = null;
        this.selectedTemplateId = null;
        this.variables = new Map();
        this.favorites = new Set();
        this.apiBaseUrl = window.location.origin;

        this.initializeTemplateSystem();
    }

    // Initialize the template system
    async initializeTemplateSystem() {
        try {
            console.log('Initializing template system...');
            await this.loadCategories();
            console.log('Categories loaded:', this.categories);
            await this.loadTemplates();
            console.log('Templates loaded:', this.templates);
            this.loadFavorites();
            this.initializeTemplateUI();
            console.log('Template system initialized successfully');
        } catch (error) {
            console.error('Failed to initialize template system:', error);
        }
    }

    // API Methods
    async loadTemplates(categoryId = null, search = '', includeFavorites = false) {
        try {
            let url = `${this.apiBaseUrl}/api/prompt-templates`;
            const params = new URLSearchParams();
            
            if (categoryId) params.append('categoryId', categoryId);
            if (search) params.append('search', search);
            if (includeFavorites) params.append('includeFavorites', 'true');
            
            if (params.toString()) url += `?${params.toString()}`;
            
            console.log('loadTemplates: Fetching from URL:', url);
            
            const response = await fetch(url);
            if (!response.ok) {
                const errorText = await response.text();
                console.error('Server response:', errorText);
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            
            const contentType = response.headers.get('content-type');
            if (!contentType || !contentType.includes('application/json')) {
                const errorText = await response.text();
                console.error('Non-JSON response:', errorText);
                throw new Error('Server did not return JSON data');
            }
            
            const data = await response.json();
            console.log('loadTemplates: Raw API response data:', data);
            console.log('loadTemplates: Number of templates received:', data.length);
            
            // Validate template data structure and normalize property names
            if (Array.isArray(data)) {
                data.forEach((template, index) => {
                    // Handle both PascalCase and camelCase property names
                    const templateId = template.Id || template.id;
                    const templateTitle = template.Title || template.title;
                    const templateContent = template.Content || template.content;
                    
                    console.log(`Template ${index}:`, {
                        id: templateId,
                        title: templateTitle,
                        hasContent: !!templateContent,
                        contentType: typeof templateContent,
                        hasCategory: !!(template.Category || template.category),
                        hasVariables: !!(template.Variables || template.variables)
                    });
                    
                    // Check for missing Content property
                    if (!templateContent) {
                        console.warn(`Template ${index} (${templateId || 'unknown'}) is missing Content property`);
                    }
                });
            }
            
            this.templates = data;
            return this.templates;
        } catch (error) {
            console.error('Error loading templates:', error);
            throw error;
        }
    }

    async loadCategories() {
        try {
            const response = await fetch(`${this.apiBaseUrl}/api/prompt-templates/categories`);
            if (!response.ok) {
                const errorText = await response.text();
                console.error('Server response:', errorText);
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            
            const contentType = response.headers.get('content-type');
            if (!contentType || !contentType.includes('application/json')) {
                const errorText = await response.text();
                console.error('Non-JSON response:', errorText);
                throw new Error('Server did not return JSON data');
            }
            
            this.categories = await response.json();
            return this.categories;
        } catch (error) {
            console.error('Error loading categories:', error);
            throw error;
        }
    }

    async createTemplate(templateData) {
        console.log('Creating template with data:', templateData);
        try {
            const createData = {
                title: templateData.title,
                description: templateData.description,
                content: templateData.content,
                category: templateData.category,
                isSystemTemplate: templateData.isSystemTemplate || false
            };

            console.log('Sending POST request to /api/prompt-templates with data:', createData);
            console.log('API Base URL:', this.apiBaseUrl);
            console.log('Full URL:', `${this.apiBaseUrl}/api/prompt-templates`);

            const response = await fetch(`${this.apiBaseUrl}/api/prompt-templates`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(createData)
            });

            console.log('Response status:', response.status);
            console.log('Response headers:', Object.fromEntries(response.headers.entries()));

            if (!response.ok) {
                console.error('Create template failed with status:', response.status);
                const responseText = await response.text();
                console.error('Response text:', responseText);
                throw new Error(`HTTP ${response.status}: ${responseText}`);
            }

            const newTemplate = await response.json();
            console.log('Template created successfully:', newTemplate);
            this.templates.push(newTemplate);
            return newTemplate;
        } catch (error) {
            console.error('Error creating template:', error);
            throw error;
        }
    }

    async updateTemplate(templateId, templateData) {
        try {
            const updateData = {
                title: templateData.title,
                description: templateData.description,
                content: templateData.content,
                category: templateData.category
            };

            const response = await fetch(`${this.apiBaseUrl}/api/prompt-templates/templates/${templateId}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(updateData)
            });

            if (!response.ok) throw new Error(`HTTP ${response.status}`);

            const updatedTemplate = await response.json();
            const index = this.templates.findIndex(t => (t.Id || t.id) === templateId);
            if (index !== -1) this.templates[index] = updatedTemplate;

            return updatedTemplate;
        } catch (error) {
            console.error('Error updating template:', error);
            throw error;
        }
    }

    async deleteTemplate(templateId) {
        try {
            const response = await fetch(`${this.apiBaseUrl}/api/prompt-templates/templates/${templateId}`, {
                method: 'DELETE',
                headers: { 'Content-Type': 'application/json' },
            });

            if (!response.ok) throw new Error(`HTTP ${response.status}`);

            this.templates = this.templates.filter(t => t.Id !== templateId);
            return true;
        } catch (error) {
            console.error('Error deleting template:', error);
            throw error;
        }
    }

    async toggleFavorite(templateId) {
        try {
            const response = await fetch(`${this.apiBaseUrl}/api/prompt-templates/templates/${templateId}/favorite`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' }
            });

            if (!response.ok) throw new Error(`HTTP ${response.status}`);

            const result = await response.json();
            // Handle both PascalCase and camelCase property names
            const isFavorite = result.isFavorite || result.isFavorite;
            if (isFavorite) {
                this.favorites.add(templateId);
            } else {
                this.favorites.delete(templateId);
            }

            this.saveFavorites();
            return result;
        } catch (error) {
            console.error('Error toggling favorite:', error);
            throw error;
        }
    }

    async expandTemplate(templateId, variables) {
        try {
            const response = await fetch(`${this.apiBaseUrl}/api/prompt-templates/templates/expand`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ templateId, variables })
            });

            if (!response.ok) throw new Error(`HTTP ${response.status}`);

            const result = await response.json();
            return result.content;
        } catch (error) {
            console.error('Error expanding template:', error);
            throw error;
        }
    }

    // Template variable management
    parseVariables(templateContent) {
        // Add defensive programming to handle undefined/null content
        if (!templateContent || typeof templateContent !== 'string') {
            console.warn('parseVariables: templateContent is undefined, null, or not a string', templateContent);
            return [];
        }
        
        try {
            const variablePattern = /\{\{([^}]+)\}\}/g;
            const matches = [...templateContent.matchAll(variablePattern)];
            const variables = new Set();
            
            matches.forEach(match => {
                variables.add(match[1].trim());
            });
            
            return Array.from(variables);
        } catch (error) {
            console.error('parseVariables: Error parsing template content:', error);
            return [];
        }
    }

    validateVariables(template, variables) {
        const templateVars = this.parseVariables(template.Content);
        const missingRequired = templateVars.filter(v => !variables[v]);
        
        return {
            isValid: missingRequired.length === 0,
            missingRequired: missingRequired
        };
    }

    // Local Storage
    loadFavorites() {
        try {
            const saved = localStorage.getItem('templateFavorites');
            if (saved) {
                this.favorites = new Set(JSON.parse(saved));
            }
        } catch (error) {
            console.error('Error loading favorites:', error);
            this.favorites = new Set();
        }
    }

    saveFavorites() {
        try {
            localStorage.setItem('templateFavorites', JSON.stringify([...this.favorites]));
        } catch (error) {
            console.error('Error saving favorites:', error);
        }
    }

    // UI Methods
    initializeTemplateUI() {
        this.renderTemplateLibrary();
        this.renderCategoryFilters();
        this.setupEventListeners();
    }

    // Template Selection Methods
    selectTemplate(templateId) {
        this.selectedTemplateId = templateId;
        this.updateTemplateSelectionUI();
    }

    clearSelection() {
        this.selectedTemplateId = null;
        this.updateTemplateSelectionUI();
    }

    updateTemplateSelectionUI() {
        // Remove selected class from all cards
        document.querySelectorAll('.template-card').forEach(card => {
            card.classList.remove('selected');
        });

        // Add selected class to the selected card
        if (this.selectedTemplateId) {
            const selectedCard = document.querySelector(`[data-template-id="${this.selectedTemplateId}"]`);
            if (selectedCard) {
                selectedCard.classList.add('selected');
            }
        }
    }

    renderTemplateLibrary() {
        const contentContainer = document.getElementById('templateLibraryContent');
        if (!contentContainer) return;
        
        // Group templates by category
        const templatesByCategory = this.groupTemplatesByCategory();
        
        let html = '';
        
        // Render categories
        if (this.categories.length === 0) {
            html = '<div class="text-slate-400 text-center py-8">No categories available</div>';
        } else {
            this.categories.forEach(category => {
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
        this.attachTemplateEvents();
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
                    ${templates.map(template => this.renderTemplateCard(template)).join('')}
                </div>
            </div>
        `;
    }

    renderCategorySection(category, templates) {
        return `
            <div class="category-section" data-category-id="${category.id || category.Id}">
                <div class="category-header">
                    <span class="category-color" style="background-color: ${category.color || '#6b7280'}"></span>
                    <h4 class="category-name">${category.name || category.Name}</h4>
                    <span class="category-count">${templates.length} templates</span>
                </div>
                <div class="template-grid">
                    ${templates.map(template => this.renderTemplateCard(template)).join('')}
                </div>
            </div>
        `;
    }

    renderTemplateCard(template) {
        // Add defensive programming and logging
        if (!template) {
            console.error('renderTemplateCard: template object is null or undefined');
            return '<div class="template-card error">Invalid template data</div>';
        }

        // Handle both PascalCase (C# style) and camelCase (JavaScript style) property names
        const templateId = template.Id || template.id;
        const templateTitle = template.Title || template.title;
        const templateDescription = template.Description || template.description;
        const templateContent = template.Content || template.content;
        const templateUsageCount = template.UsageCount || template.usageCount;

        if (!templateId) {
            console.error('renderTemplateCard: template ID is missing', template);
            return '<div class="template-card error">Template missing ID</div>';
        }

        // Log template data for debugging
        console.log('renderTemplateCard: Processing template:', {
            id: templateId,
            title: templateTitle,
            hasContent: !!templateContent,
            contentType: typeof templateContent,
            contentPreview: templateContent ? templateContent.substring(0, 50) + '...' : 'N/A'
        });

        const isFavorite = this.favorites.has(templateId);
        const isSelected = this.selectedTemplateId === templateId;
        const variableCount = this.parseVariables(templateContent).length;

        // Determine if this card should be expanded (default to collapsed for space saving)
        const isExpanded = false; // Default to collapsed

        return `
            <div class="template-card compact-template-card ${isSelected ? 'selected' : ''} ${isExpanded ? 'expanded' : 'collapsed'}" data-template-id="${templateId}">
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
                            <svg class="chevron w-4 h-4 text-slate-400" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <path d="M6 9l6 6 6-6"></path>
                            </svg>
                        </button>
                    </div>
                </div>
                <div class="template-description">${templateDescription ? this.truncateText(templateDescription, 80) : 'No description available'}</div>
                <div class="detailed-info ${isExpanded ? 'expanded' : ''}">
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

    // Helper method to truncate text
    truncateText(text, maxLength) {
        if (!text) return '';
        return text.length > maxLength ? text.substring(0, maxLength) + '...' : text;
    }

    renderCategoryFilters() {
        const filterContainer = document.getElementById('templateCategoryFilters');
        if (!filterContainer || this.categories.length === 0) return;
        
        const html = `
            <div class="category-filters">
                <h4>Filter by Category:</h4>
                <div class="filter-buttons">
                    <button class="filter-btn active" data-category-id="all">All</button>
                    ${this.categories.map(cat => `
                        <button class="filter-btn" data-category-id="${cat.id || cat.Id}">
                            <span class="color-dot" style="background-color: ${cat.color || '#6b7280'}"></span>
                            ${cat.name || cat.Name}
                        </button>
                    `).join('')}
                </div>
            </div>
        `;
        
        filterContainer.innerHTML = html;
        this.attachCategoryFilterEvents();
    }

    // Event Handlers
    setupEventListeners() {
        // Search functionality
        document.addEventListener('input', (e) => {
            if (e.target.id === 'templateSearch') {
                this.handleSearch(e.target.value);
            }
        });

        // Create template button with comprehensive logging and alert
        document.addEventListener('click', (e) => {
            const btn = e.target.closest('#createTemplateBtn');
            if (btn) {
                console.groupCollapsed('Create Template button click event');
                console.log('Button element:', btn);
                console.log('Button position:', btn.getBoundingClientRect());
                console.log('Button visibility:', window.getComputedStyle(btn).visibility);
                console.log('Button display:', window.getComputedStyle(btn).display);
                console.log('Button pointer-events:', window.getComputedStyle(btn).pointerEvents);
                
                console.log('Calling showTemplateEditor...');
                this.showTemplateEditor();
                console.groupEnd();
            }
        });
    }

    attachTemplateEvents() {
        // Template card selection
        document.querySelectorAll('.template-card').forEach(card => {
            card.addEventListener('click', (e) => {
                // Don't select if clicking on action buttons
                if (!e.target.closest('.template-card-actions') && !e.target.closest('.template-actions')) {
                    const templateId = card.getAttribute('data-template-id');
                    this.selectTemplate(templateId);
                }
            });
        });

        // Favorite buttons
        document.querySelectorAll('.favorite-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.stopPropagation();
                const templateId = btn.getAttribute('data-template-id');
                this.handleToggleFavorite(templateId);
            });
        });

        // Use template buttons
        document.querySelectorAll('.use-template-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.stopPropagation();
                const templateId = btn.getAttribute('data-template-id');
                this.handleUseTemplate(templateId);
            });
        });

        // Edit template buttons
        document.querySelectorAll('.edit-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.stopPropagation();
                const templateId = btn.getAttribute('data-template-id');
                this.showTemplateEditor(templateId);
            });
        });

        // Delete template buttons
        document.querySelectorAll('.delete-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.stopPropagation();
                const templateId = btn.getAttribute('data-template-id');
                this.handleDeleteTemplate(templateId);
            });
        });
    }

    attachCategoryFilterEvents() {
        document.querySelectorAll('.filter-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const categoryId = btn.getAttribute('data-category-id');
                this.handleCategoryFilter(categoryId);
            });
        });
    }

    // Event Handlers
    async handleSearch(searchTerm) {
        try {
            this.clearSelection(); // Clear selection when searching
            await this.loadTemplates(null, searchTerm);
            this.renderTemplateLibrary();
        } catch (error) {
            console.error('Error searching templates:', error);
        }
    }

    async handleDeleteTemplate(templateId) {
        if (!confirm('Are you sure you want to delete this template? This action cannot be undone.')) {
            return;
        }

        try {
            await this.deleteTemplate(templateId);
            this.clearSelection(); // Clear selection after deletion
            this.showNotification('Template deleted successfully', 'success');
            await this.loadTemplates(); // Reload templates from server
            this.renderTemplateLibrary();
        } catch (error) {
            console.error('Error deleting template:', error);
            this.showNotification('Error deleting template: ' + error.message, 'error');
        }
    }

    async handleToggleFavorite(templateId) {
        try {
            await this.toggleFavorite(templateId);
            this.renderTemplateLibrary();
        } catch (error) {
            console.error('Error toggling favorite:', error);
        }
    }

    async handleUseTemplate(templateId) {
        try {
            // Handle both PascalCase and camelCase property names
            const template = this.templates.find(t => (t.Id || t.id) === templateId);
            if (!template) {
                console.error('Template not found:', templateId);
                this.showNotification('Template not found. Please refresh and try again.', 'error');
                return;
            }

            // Handle both PascalCase and camelCase property names
            const templateContent = template.Content || template.content;
            if (!templateContent) {
                console.error('Template content is missing:', template);
                this.showNotification('Template content is missing.', 'error');
                return;
            }

            const variables = this.parseVariables(templateContent);

            if (variables.length === 0) {
                // No variables, just use the template
                this.insertTemplateIntoPromptInput(templateContent);
                this.recordTemplateUsage(templateId);
                return;
            }

            // Show variable input modal
            this.showVariableInputModal(template, variables);

        } catch (error) {
            console.error('Error using template:', error);
            this.showNotification('Error using template: ' + error.message, 'error');
        }
    }

    handleCategoryFilter(categoryId) {
        // Clear selection when filtering
        this.clearSelection();

        // Update active state
        document.querySelectorAll('.filter-btn').forEach(btn => {
            btn.classList.toggle('active', btn.getAttribute('data-category-id') === categoryId);
        });

        // Filter templates
        if (categoryId === 'all') {
            this.renderTemplateLibrary();
        } else {
            // Handle both PascalCase and camelCase property names
            const filteredTemplates = this.templates.filter(t => (t.Category || t.category) === categoryId);
            this.renderFilteredTemplates(filteredTemplates);
        }
    }

    // UI Helper Methods
    getTemplateLibraryContainer() {
        return document.getElementById('templateLibraryContent');
    }

    groupTemplatesByCategory() {
        const grouped = {};
        this.templates.forEach(template => {
            // Handle both PascalCase and camelCase property names
            const categoryId = template.Category || template.category;
            if (!grouped[categoryId]) {
                grouped[categoryId] = [];
            }
            grouped[categoryId].push(template);
        });
        return grouped;
    }

    renderFilteredTemplates(templates) {
        const container = this.getTemplateLibraryContainer();
        if (!container) return;

        let html = '';
        
        // Render a single "Filtered Results" section
        if (templates.length > 0) {
            html += this.renderCategorySectionWithProgressiveDisclosure({
                id: 'filtered',
                name: 'Filtered Results',
                color: '#6b7280'
            }, templates);
        } else {
            html = '<div class="text-slate-400 text-center py-8">No templates found matching the filter criteria.</div>';
        }
        
        container.innerHTML = html;
        this.attachTemplateEvents();
    }

    insertTemplateIntoPromptInput(content) {
        const promptInput = document.getElementById('promptInput');
        if (promptInput) {
            promptInput.value = content;
            promptInput.focus();
            
            // Trigger input event for button state update
            promptInput.dispatchEvent(new Event('input', { bubbles: true }));
            
            // Scroll to the prompt input field to ensure it's visible
            promptInput.scrollIntoView({ behavior: 'smooth', block: 'center' });
            
            // Show success message
            this.showNotification('Template loaded into prompt input', 'success');
        }
    }

    showNotification(message, type = 'info') {
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.textContent = message;
        
        document.body.appendChild(notification);
        
        setTimeout(() => {
            notification.remove();
        }, 3000);
    }

    // Modal Methods
    showVariableInputModal(template, variables) {
        // Create modal overlay
        const modal = document.createElement('div');
        modal.className = 'fixed inset-0 bg-black/50 backdrop-blur-sm z-50 flex items-center justify-center p-4';
        modal.id = 'variableInputModal';
        
        // Create modal content
        const modalContent = document.createElement('div');
        modalContent.className = 'bg-slate-800/95 backdrop-blur-xl rounded-2xl border border-slate-700/30 p-6 max-w-md w-full shadow-modern-2xl';
        
        // Create variable input fields
        let variableFields = '';
        variables.forEach(variable => {
            variableFields += `
                <div class="mb-4">
                    <label class="block text-sm font-medium text-slate-300 mb-2">
                        {{${variable}}}
                    </label>
                    <input type="text"
                           id="var-${variable}"
                           placeholder="Enter value for ${variable}..."
                           class="w-full bg-slate-700/50 border border-slate-600/50 rounded-xl px-4 py-2 text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent transition-all duration-200">
                </div>
            `;
        });
        
        modalContent.innerHTML = `
            <div class="flex justify-between items-center mb-6">
                <h3 class="text-xl font-semibold text-white">Template Variables</h3>
                <button onclick="this.closest('#variableInputModal').remove()"
                        class="text-slate-400 hover:text-white transition-colors duration-200">
                    <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
                    </svg>
                </button>
            </div>
            <div class="mb-6">
                <p class="text-slate-300 mb-4">Template: <strong>${template.Title || template.title}</strong></p>
                <p class="text-slate-400 text-sm mb-4">${template.Description || template.description || 'No description available'}</p>
            </div>
            <div class="variable-fields mb-6">
                ${variableFields}
            </div>
            <div class="flex gap-3">
                <button onclick="templateManager.processVariableTemplate('${template.Id || template.id}')"
                        class="flex-1 bg-gradient-to-r from-purple-600 to-pink-600 hover:from-purple-700 hover:to-pink-700 text-white font-medium py-2 px-4 rounded-xl transition-all duration-300 transform hover:-translate-y-0.5 shadow-modern-md">
                    Use Template
                </button>
                <button onclick="this.closest('#variableInputModal').remove()"
                        class="flex-1 bg-slate-600 hover:bg-slate-700 text-white font-medium py-2 px-4 rounded-xl transition-all duration-300 transform hover:-translate-y-0.5 shadow-modern-md">
                    Cancel
                </button>
            </div>
        `;
        
        modal.appendChild(modalContent);
        document.body.appendChild(modal);
        
        // Focus on first input
        const firstInput = modal.querySelector('input');
        if (firstInput) {
            firstInput.focus();
        }
        
        // Close on escape key
        modal.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                modal.remove();
            }
        });
        
        // Close on background click
        modal.addEventListener('click', (e) => {
            if (e.target === modal) {
                modal.remove();
            }
        });
    }

    showTemplateEditor(templateId = null) {
        const existingModal = document.getElementById('templateEditorModal');
        if (existingModal) {
            existingModal.remove();
        }

        console.log('showTemplateEditor called with templateId:', templateId);
        const isEditing = templateId !== null;
        // Handle both PascalCase and camelCase property names
        const template = isEditing ? this.templates.find(t => (t.Id || t.id) === templateId) : null;
        console.log('Template found for editing:', template);
        
        // Create modal overlay
        const modal = document.createElement('div');
        modal.className = 'fixed inset-0 bg-black/50 backdrop-blur-sm z-50 flex items-center justify-center p-4';
        modal.id = 'templateEditorModal';
        
        // Create modal content
        const modalContent = document.createElement('div');
        modalContent.className = 'bg-slate-800/95 backdrop-blur-xl rounded-2xl border border-slate-700/30 p-6 max-w-2xl w-full max-h-[90vh] overflow-y-auto shadow-modern-2xl';
        console.log('Modal content element created:', modalContent);
        
        // Category options
        const categoryOptions = this.categories.map(cat =>
            `<option value="${cat.id || cat.Id}" ${template && (template.Category || template.category) === (cat.id || cat.Id) ? 'selected' : ''}>${cat.name || cat.Name}</option>`
        ).join('');
        
        modalContent.innerHTML = `
            <div class="flex justify-between items-center mb-6">
                <h3 class="text-xl font-semibold text-white">${isEditing ? 'Edit Template' : 'Create New Template'}</h3>
                <button onclick="this.closest('#templateEditorModal').remove()"
                        class="text-slate-400 hover:text-white transition-colors duration-200">
                    <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
                    </svg>
                </button>
            </div>
            <form id="templateEditorForm" class="space-y-4" novalidate>
                <div>
                    <label class="block text-sm font-medium text-slate-300 mb-2">Template Name *</label>
                    <input type="text"
                           id="templateName"
                           value="${template ? template.Title : ''}"
                           placeholder="Enter template name..."
                           class="w-full bg-slate-700/50 border border-slate-600/50 rounded-xl px-4 py-2 text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent transition-all duration-200"
                           required>
                </div>
                <div>
                    <label class="block text-sm font-medium text-slate-300 mb-2">Description *</label>
                    <textarea id="templateDescription"
                              placeholder="Describe what this template is for..."
                              class="w-full bg-slate-700/50 border border-slate-600/50 rounded-xl px-4 py-2 text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent transition-all duration-200 resize-none"
                              rows="3"
                              required>${template ? template.Description : ''}</textarea>
                </div>
                <div>
                    <label class="block text-sm font-medium text-slate-300 mb-2">Category *</label>
                    <select id="templateCategory"
                            class="w-full bg-slate-700/50 border border-slate-600/50 rounded-xl px-4 py-2 text-white focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent transition-all duration-200"
                            required>
                        <option value="">Select a category...</option>
                        ${categoryOptions}
                    </select>
                </div>
                <div>
                    <label class="block text-sm font-medium text-slate-300 mb-2">Template Content *</label>
                    <textarea id="templateContent"
                              placeholder="Enter template content... Use {{variable}} for variables..."
                              class="w-full bg-slate-700/50 border border-slate-600/50 rounded-xl px-4 py-2 text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent transition-all duration-200 resize-none font-mono text-sm"
                              rows="8"
                              required>${template ? template.Content : ''}</textarea>
                    <p class="text-slate-400 text-sm mt-2">Use {{variable_name}} for variables that will be replaced when using the template.</p>
                </div>
                <div class="flex items-center">
                    <input type="checkbox"
                           id="templateIsVariableRequired"
                           class="w-4 h-4 text-purple-600 bg-slate-700/50 border-slate-600/50 rounded focus:ring-purple-500 focus:ring-2">
                    <label for="templateIsVariableRequired" class="ml-2 text-sm text-slate-300">
                        Variables are required
                    </label>
                </div>
                <div class="flex gap-3 pt-4">
                    <button type="submit" id="templateSubmitBtn"
                            class="flex-1 bg-gradient-to-r from-purple-600 to-pink-600 hover:from-purple-700 hover:to-pink-700 text-white font-medium py-2 px-4 rounded-xl transition-all duration-300 transform hover:-translate-y-0.5 shadow-modern-md">
                        ${isEditing ? 'Update Template' : 'Create Template'}
                    </button>
                    <button type="button"
                            onclick="this.closest('#templateEditorModal').remove()"
                            class="flex-1 bg-slate-600 hover:bg-slate-700 text-white font-medium py-2 px-4 rounded-xl transition-all duration-300 transform hover:-translate-y-0.5 shadow-modern-md">
                        Cancel
                    </button>
                </div>
            </form>
        `;
        
        modal.appendChild(modalContent);
        document.body.appendChild(modal);
        console.log('Modal appended to DOM:', modal);
        console.log('Modal is now in document.body:', document.body.contains(modal));

        // Focus on first input
        const firstInput = modal.querySelector('#templateName');
        console.log('First input element found:', firstInput);
        if (firstInput) {
            firstInput.focus();
            console.log('Focused on first input');
        } else {
            console.error('First input element not found!');
        }
        
        // Handle form submission
        const form = modal.querySelector('#templateEditorForm');
        const submitBtn = modal.querySelector('#templateSubmitBtn');

        console.log('Setting up form event listeners:');
        console.log('- Form element:', form);
        console.log('- Submit button element:', submitBtn);

        form.addEventListener('submit', (e) => {
            console.log('Form submit event triggered');
            e.preventDefault();
            console.log('Calling saveTemplateFromForm with templateId:', templateId);
            this.saveTemplateFromForm(templateId);
        });
        
        // Close on escape key
        modal.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                modal.remove();
            }
        });
        
        // Close on background click
        modal.addEventListener('click', (e) => {
            if (e.target === modal) {
                modal.remove();
            }
        });

        console.log('Template editor modal created and appended to DOM');
    }

    // Process template with variables
    async processVariableTemplate(templateId) {
        try {
            // Handle both PascalCase and camelCase property names
            const template = this.templates.find(t => (t.Id || t.id) === templateId);
            if (!template) {
                console.error('Template not found:', templateId);
                this.showNotification('Template not found. Please refresh and try again.', 'error');
                return;
            }

            // Handle both PascalCase and camelCase property names
            const templateContent = template.Content || template.content;
            if (!templateContent) {
                console.error('Template content is missing:', template);
                this.showNotification('Template content is missing.', 'error');
                return;
            }

            const variables = this.parseVariables(templateContent);
            const variableValues = {};

            // Collect variable values from modal inputs
            let allVariablesFilled = true;
            variables.forEach(variable => {
                const input = document.getElementById(`var-${variable}`);
                if (input) {
                    const value = input.value.trim();
                    if (value) {
                        variableValues[variable] = value;
                    } else {
                        allVariablesFilled = false;
                    }
                } else {
                    allVariablesFilled = false;
                }
            });

            if (!allVariablesFilled) {
                this.showNotification('Please fill in all variables', 'error');
                return;
            }

            // Substitute variables in template content
            let processedContent = templateContent;
            variables.forEach(variable => {
                const value = variableValues[variable] || '';
                const regex = new RegExp(`\\{\\{${variable}\\}\\}`, 'g');
                processedContent = processedContent.replace(regex, value);
            });

            // Insert processed content into prompt input
            this.insertTemplateIntoPromptInput(processedContent);

            // Record template usage
            this.recordTemplateUsage(templateId);

            // Close modal
            const modal = document.getElementById('variableInputModal');
            if (modal) {
                modal.remove();
            }

            this.showNotification('Template loaded with variables substituted', 'success');

        } catch (error) {
            console.error('Error processing template with variables:', error);
            this.showNotification('Error processing template: ' + error.message, 'error');
        }
    }

    // Save template from form data
    async saveTemplateFromForm(templateId) {
        console.log('saveTemplateFromForm called with templateId:', templateId);
        console.log('Form elements check:');
        console.log('- templateName:', document.getElementById('templateName'));
        console.log('- templateDescription:', document.getElementById('templateDescription'));
        console.log('- templateCategory:', document.getElementById('templateCategory'));
        console.log('- templateContent:', document.getElementById('templateContent'));

        try {
            const name = document.getElementById('templateName').value.trim();
            const description = document.getElementById('templateDescription').value.trim();
            const categoryId = document.getElementById('templateCategory').value;
            const content = document.getElementById('templateContent').value.trim();
            const isVariableRequired = document.getElementById('templateIsVariableRequired').checked;

            console.log('Form values:', { name, description, categoryId, content, isVariableRequired });

            if (!name || !description || !categoryId || !content) {
                console.error('Validation failed - missing required fields:', {
                    name: !name,
                    description: !description,
                    categoryId: !categoryId,
                    content: !content
                });
                this.showNotification('Please fill in all required fields', 'error');
                return;
            }

            const templateData = {
                title: name,
                description: description,
                category: categoryId,
                content: content,
                isSystemTemplate: false
            };

            let savedTemplate;
            if (templateId) {
                // Update existing template
                console.log('Updating template with data:', templateData);
                savedTemplate = await this.updateTemplate(templateId, templateData);
                this.showNotification('Template updated successfully', 'success');
            } else {
                // Create new template
                console.log('Creating new template with data:', templateData);
                savedTemplate = await this.createTemplate(templateData);
                console.log('Template creation completed, savedTemplate:', savedTemplate);
                this.showNotification('Template created successfully', 'success');
            }

            // Close modal
            const modal = document.getElementById('templateEditorModal');
            console.log('Attempting to close modal:', modal);
            if (modal) {
                modal.remove();
                console.log('Modal closed successfully');
            } else {
                console.error('Modal not found for closing');
            }

            // Clear selection and refresh template library
            console.log('Refreshing template library...');
            this.clearSelection();
            await this.loadTemplates();
            this.renderTemplateLibrary();
            console.log('Template library refreshed');

        } catch (error) {
            console.error('Error saving template:', error);
            this.showNotification('Error saving template: ' + error.message, 'error');
        }
    }
    
    // Delete template by ID (legacy method - now handled by handleDeleteTemplate)
    async deleteTemplateById(templateId) {
        await this.handleDeleteTemplate(templateId);
    }

    // Template statistics
    async recordTemplateUsage(templateId) {
        try {
            await fetch(`${this.apiBaseUrl}/api/prompt-templates/templates/${templateId}/usage`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' }
            });
        } catch (error) {
            console.error('Error recording template usage:', error);
        }
    }
}

// Export singleton instance
export const templateManager = new TemplateManager();