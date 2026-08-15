import { defineConfig } from 'vite';

export default defineConfig({
  server: {
    proxy: {
      '/api': {
        target: 'https://localhost:5100',
        changeOrigin: true,
        secure: false,
      }
    }
  }
});