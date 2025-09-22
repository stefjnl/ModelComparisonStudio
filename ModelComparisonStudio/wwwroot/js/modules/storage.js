// Model Comparison Studio - Storage Module

export function saveModelsToStorage(models) {
    localStorage.setItem('modelComparisonStudio_models', JSON.stringify(models));
}

export function loadModelsFromStorage() {
    const stored = localStorage.getItem('modelComparisonStudio_models');
    return stored ? JSON.parse(stored) : [];
}