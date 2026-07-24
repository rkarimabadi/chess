const CACHE_NAME = 'chess-pwa-v2';

const PRECACHE_URLS = [
    '/',
    '/app.css',
    '/manifest.json',
    '/icons/icon.svg',
    '/icons/icon-192.png',
    '/icons/icon-512.png',
    '/offline.html'
];

self.addEventListener('install', (event) => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => cache.addAll(PRECACHE_URLS))
            .then(() => self.skipWaiting())
    );
});

self.addEventListener('activate', (event) => {
    event.waitUntil(
        caches.keys().then(keys =>
            Promise.all(keys.filter(k => k !== CACHE_NAME).map(k => caches.delete(k)))
        ).then(() => self.clients.claim())
    );
});

self.addEventListener('fetch', (event) => {
    const { request } = event;

    // Network-first for API and SignalR
    if (request.url.includes('/api/') || request.url.includes('/hubs/')) {
        event.respondWith(
            fetch(request).catch(() =>
                new Response(JSON.stringify({ error: 'offline' }), {
                    status: 503,
                    headers: { 'Content-Type': 'application/json' }
                })
            )
        );
        return;
    }

    // Cache-first for WASM/dll files
    if (request.url.endsWith('.wasm') || request.url.endsWith('.dll')) {
        event.respondWith(
            caches.match(request).then(cached =>
                cached || fetch(request).then(response => {
                    const clone = response.clone();
                    caches.open(CACHE_NAME).then(cache => cache.put(request, clone));
                    return response;
                })
            )
        );
        return;
    }

    // Stale-while-revalidate for static assets
    if (request.url.endsWith('.js') || request.url.endsWith('.css') || request.url.endsWith('.png')) {
        event.respondWith(
            caches.match(request).then(cached => {
                const fetchPromise = fetch(request).then(response => {
                    const clone = response.clone();
                    caches.open(CACHE_NAME).then(cache => cache.put(request, clone));
                    return response;
                }).catch(() => cached);
                return cached || fetchPromise;
            })
        );
        return;
    }

    // Default: network-first with offline fallback
    event.respondWith(
        fetch(request).then(response => {
            const clone = response.clone();
            caches.open(CACHE_NAME).then(cache => cache.put(request, clone));
            return response;
        }).catch(() =>
            caches.match(request).then(cached =>
                cached || caches.match('/offline.html')
            )
        )
    );
});
