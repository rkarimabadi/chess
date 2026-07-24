export function observeSentinel(dotNetRef) {
    const sentinel = document.getElementById('rl-sentinel');
    if (!sentinel) return;

    const observer = new IntersectionObserver((entries) => {
        if (entries[0].isIntersecting) {
            dotNetRef.invokeMethodAsync('LoadMore');
        }
    }, { rootMargin: '200px' });

    observer.observe(sentinel);
}
