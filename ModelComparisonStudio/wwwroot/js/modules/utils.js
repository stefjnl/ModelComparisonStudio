// Model Comparison Studio - Utilities Module

export function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

export function formatResponseContent(response) {
    if (!response) return '';

    // Replace line breaks with <br> tags for proper display
    let formatted = response
        .replace(/\n\n/g, '</p><p>')  // Double line breaks become paragraphs
        .replace(/\n/g, '<br>');      // Single line breaks become <br>

    // Handle markdown-style bold text (**text**)
    formatted = formatted.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');

    // Handle markdown-style italic text (*text*)
    formatted = formatted.replace(/\*(.*?)\*/g, '<em>$1</em>');

    // Wrap in paragraph tags if it contains multiple lines
    if (formatted.includes('<br>') || formatted.includes('</p>')) {
        formatted = '<p>' + formatted + '</p>';
    }

    return formatted;
}

export function generatePromptId(prompt) {
    // Simple hash function for prompt ID generation
    let hash = 0;
    for (let i = 0; i < prompt.length; i++) {
        const char = prompt.charCodeAt(i);
        hash = ((hash << 5) - hash) + char;
        hash = hash & hash; // Convert to 32-bit integer
    }
    return `prompt_${Math.abs(hash)}`;
}

export function isValidModelFormat(modelId) {
    return /^[a-zA-Z0-9_-]+\/[a-zA-Z0-9_-]+$/.test(modelId);
}