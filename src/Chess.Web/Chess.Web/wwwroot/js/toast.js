export function showSwiftToast(toastId) {
    const toastEl = document.getElementById(toastId);
    if (!toastEl) return;
    setTimeout(() => {
        toastEl.classList.add('show');
    }, 10);
}

export function hideSwiftToast(toastId) {
    const toastEl = document.getElementById(toastId);
    if (!toastEl) return;
    toastEl.classList.remove('show');
}
