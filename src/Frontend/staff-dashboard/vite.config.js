import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Staff dashboard dev server. VITE_DASHBOARD_URL points at the Dashboard Service
// (REST + SignalR hub), defaulting to port 5005.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 3001,
    host: true
  }
});