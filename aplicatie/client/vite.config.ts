/// <reference types="vitest/config" />
import path from 'path'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'
import { VitePWA } from 'vite-plugin-pwa'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    // PWA (NUT-33, ADR-013): installable app, precached app shell, and the food catalog readable offline.
    VitePWA({
      registerType: 'autoUpdate',
      includeAssets: ['favicon.svg', 'apple-touch-icon.png'],
      manifest: {
        name: 'NutriScan',
        short_name: 'NutriScan',
        description: 'Monitorizare nutrițională cu scanarea etichetelor',
        lang: 'ro',
        start_url: '/diary',
        scope: '/',
        display: 'standalone',
        orientation: 'portrait',
        theme_color: '#059669',
        background_color: '#f1f5f9',
        icons: [
          { src: 'pwa-192x192.png', sizes: '192x192', type: 'image/png' },
          { src: 'pwa-512x512.png', sizes: '512x512', type: 'image/png' },
          { src: 'maskable-512x512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
        ],
      },
      workbox: {
        // Any app route opens the cached shell offline; API and Swagger requests never get index.html instead.
        navigateFallback: '/index.html',
        navigateFallbackDenylist: [/^\/api\//, /^\/swagger/],
        runtimeCaching: [
          {
            // Only the food catalog: personal data (diary, profile) is never written to the service-worker cache.
            urlPattern: ({ url, request }) => url.pathname.startsWith('/api/foods') && request.method === 'GET',
            handler: 'NetworkFirst',
            options: {
              cacheName: 'api-catalog',
              networkTimeoutSeconds: 4,
              expiration: { maxEntries: 200, maxAgeSeconds: 7 * 24 * 60 * 60 },
              cacheableResponse: { statuses: [200] },
            },
          },
        ],
      },
    }),
  ],
  resolve: {
    alias: {
      '@': path.resolve(import.meta.dirname, './src'),
    },
  },
  server: {
    host: '0.0.0.0',
    port: 3000,
    // Same-origin API in development: no CORS, and the refresh cookie (Path=/api/auth) is sent as-is (ADR-011).
    proxy: {
      '/api': { target: process.env.VITE_API_PROXY_TARGET ?? 'http://localhost:5000' },
    },
    watch: {
      usePolling: true,
      interval: 300,
      ignored: ['**/node_modules/**', '**/.git/**', '**/dist/**'],
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
  },
})
