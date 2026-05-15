// Service worker de producció — precaching de tots els assets de Blazor WASM
// El build genera service-worker-assets.js amb la llista completa d'arxius

self.importScripts('./service-worker-assets.js');

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;

self.addEventListener('install', event => {
    event.waitUntil(onInstall(event));
});

self.addEventListener('activate', event => {
    event.waitUntil(onActivate(event));
});

self.addEventListener('fetch', event => {
    event.respondWith(onFetch(event));
});

async function onInstall(event) {
    self.skipWaiting();
    const cache = await caches.open(cacheName);
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => asset.url !== 'service-worker.js')
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
    await cache.addAll(assetsRequests);
}

async function onActivate(event) {
    const cacheKeys = await caches.keys();
    await Promise.all(
        cacheKeys
            .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
            .map(key => caches.delete(key))
    );
}

async function onFetch(event) {
    let cachedResponse = null;
    if (event.request.method === 'GET') {
        const shouldServeIndexHtml =
            event.request.mode === 'navigate' &&
            !event.request.url.includes('/api/');

        const request = shouldServeIndexHtml
            ? 'index.html'
            : event.request;

        const cache = await caches.open(cacheName);
        cachedResponse = await cache.match(request);
    }
    return cachedResponse || fetch(event.request);
}
