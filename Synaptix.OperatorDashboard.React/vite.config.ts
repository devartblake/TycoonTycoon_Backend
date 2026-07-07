import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 3000,
    proxy: {
      // Dev BFF: the backend requires X-Admin-Ops-Key on every /admin route
      // (including login). Injecting it here keeps the key out of browser JS —
      // the app must call same-origin paths (leave VITE_API_BASE_URL empty).
      '/admin': {
        target: process.env.API_PROXY_TARGET || 'http://localhost:5000',
        changeOrigin: true,
        headers: {
          'X-Admin-Ops-Key': process.env.ADMIN_OPS_KEY || 'dev-admin-ops-key',
        },
      },
      '/api/operator': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api\/operator/, '/admin'),
      },
    },
  },
  build: {
    outDir: 'dist',
    sourcemap: false,
    minify: 'terser',
  },
  define: {
    // Empty = same-origin: dev goes through the Vite proxy above, Docker goes
    // through the nginx /admin proxy — both inject the ops key server-side.
    'import.meta.env.VITE_API_BASE_URL': JSON.stringify(
      process.env.VITE_API_BASE_URL || ''
    ),
  },
})
