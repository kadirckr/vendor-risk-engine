import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Dev server proxies "/api/*" to the .NET backend so the browser stays same-origin
// (no CORS config needed). Adjust the target if the API runs on a different port.
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5242',
        changeOrigin: true,
      },
    },
  },
})
