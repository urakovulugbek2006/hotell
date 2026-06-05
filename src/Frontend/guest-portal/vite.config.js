import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Guest portal dev server. The API base URL is read from VITE_API_URL at build/run
// time and defaults to the Frontend (Guest Portal) Service on port 5006.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    host: true
  }
});