function showToast(message, variant = 'danger') {
    const toast = document.getElementById('toast');
    toast.className = `toast align-items-center text-bg-${variant} border-0`;
    document.getElementById('toastBody').textContent = message;
    bootstrap.Toast.getOrCreateInstance(toast).show();
}

document.body.addEventListener('htmx:responseError', () => showToast('Something went wrong.'));