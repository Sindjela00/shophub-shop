import path from 'node:path'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  // Relative, not absolute-from-root: this app has no fixed origin of its own once bundled (see
  // backend/Dockerfile) — it can be reached directly at a shop pod's root, or through
  // shophub-app's "/shop-proxy/{id}" reverse proxy under a different prefix per shop. Relative
  // references resolve correctly in both cases once combined with the <base href> that proxy
  // injects (see ShopProxyTransformer in shophub-app's Program.cs) — absolute-from-root ones
  // would always resolve against the wrong origin when proxied.
  base: './',
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
})
