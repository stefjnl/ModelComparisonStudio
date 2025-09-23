// Model Comparison Studio - UI Module

export function displayErrorMessage(message, type = 'error') {
    const container = document.getElementById('selectedModels');
    const errorDiv = document.createElement('div');
    errorDiv.className = `error-message ${type}`;
    errorDiv.textContent = message;
    errorDiv.style.cssText = `
        background-color: ${type === 'validation-error' ? '#dc2626' : '#ef4444'};
        color: white;
        padding: 12px 16px;
        border-radius: 8px;
        margin-bottom: 16px;
        font-size: 14px;
        font-weight: 500;
        border-left: 4px solid ${type === 'validation-error' ? '#fca5a5' : '#f87171'};
        animation: slideIn 0.3s ease-out;
    `;
    container.appendChild(errorDiv);

    setTimeout(() => {
        errorDiv.style.animation = 'slideOut 0.3s ease-in';
        setTimeout(() => errorDiv.remove(), 300);
    }, 5000);
}

export function displaySuccessMessage(message) {
    const container = document.getElementById('selectedModels');
    const successDiv = document.createElement('div');
    successDiv.className = 'success-message';
    successDiv.textContent = message;
    successDiv.style.cssText = `
        background-color: #16a34a;
        color: white;
        padding: 12px 16px;
        border-radius: 8px;
        margin-bottom: 16px;
        font-size: 14px;
        font-weight: 500;
        border-left: 4px solid #4ade80;
        animation: slideIn 0.3s ease-out;
    `;
    container.appendChild(successDiv);

    setTimeout(() => {
        successDiv.style.animation = 'slideOut 0.3s ease-in';
        setTimeout(() => successDiv.remove(), 300);
    }, 3000);
}