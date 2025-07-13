// Toast Notifications System
class ToastManager {
    constructor() {
        this.container = null;
        this.toastCounter = 0;
        this.init();
    }

    init() {
        // Create toast container if it doesn't exist
        if (!document.querySelector('.toast-container')) {
            this.container = document.createElement('div');
            this.container.className = 'toast-container';
            document.body.appendChild(this.container);
        } else {
            this.container = document.querySelector('.toast-container');
        }
    }

    show(message, type = 'info', duration = 5000) {
        const toastId = 'toast-' + (++this.toastCounter);
        const icons = {
            success: '✓',
            error: '✕',
            warning: '⚠',
            info: 'ℹ'
        };

        const titles = {
            success: 'Success',
            error: 'Error',
            warning: 'Warning',
            info: 'Information'
        };

        const toast = document.createElement('div');
        toast.id = toastId;
        toast.className = `toast toast-${type}`;
        toast.innerHTML = `
            <div class="toast-header">
                <div class="toast-icon">${icons[type]}</div>
                <div class="toast-title">${titles[type]}</div>
                <button class="toast-close" onclick="toastManager.hide('${toastId}')">&times;</button>
            </div>
            <div class="toast-body">${message}</div>
            <div class="toast-progress">
                <div class="toast-progress-bar" style="animation: progress ${duration}ms linear"></div>
            </div>
        `;

        // Add progress bar animation
        const style = document.createElement('style');
        style.textContent = `
            @keyframes progress {
                from { width: 100%; }
                to { width: 0%; }
            }
        `;
        if (!document.querySelector('#progress-animation')) {
            style.id = 'progress-animation';
            document.head.appendChild(style);
        }

        this.container.appendChild(toast);

        // Trigger show animation
        setTimeout(() => {
            toast.classList.add('show');
        }, 100);

        // Auto hide
        if (duration > 0) {
            setTimeout(() => {
                this.hide(toastId);
            }, duration);
        }

        return toastId;
    }

    hide(toastId) {
        const toast = document.getElementById(toastId);
        if (toast) {
            toast.classList.add('hide');
            setTimeout(() => {
                if (toast.parentNode) {
                    toast.parentNode.removeChild(toast);
                }
            }, 400);
        }
    }

    success(message, duration = 4000) {
        return this.show(message, 'success', duration);
    }

    error(message, duration = 6000) {
        return this.show(message, 'error', duration);
    }

    warning(message, duration = 5000) {
        return this.show(message, 'warning', duration);
    }

    info(message, duration = 4000) {
        return this.show(message, 'info', duration);
    }

    // Clear all toasts
    clearAll() {
        const toasts = this.container.querySelectorAll('.toast');
        toasts.forEach(toast => {
            this.hide(toast.id);
        });
    }
}

// Initialize global toast manager
const toastManager = new ToastManager();

// Global helper functions
function showToast(message, type = 'info', duration = 5000) {
    return toastManager.show(message, type, duration);
}

function showSuccess(message, duration = 4000) {
    return toastManager.success(message, duration);
}

function showError(message, duration = 6000) {
    return toastManager.error(message, duration);
}

function showWarning(message, duration = 5000) {
    return toastManager.warning(message, duration);
}

function showInfo(message, duration = 4000) {
    return toastManager.info(message, duration);
}