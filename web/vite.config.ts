import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // Lets a tunnel (localtunnel/ngrok/cloudflared) reach the dev server under its own hostname;
    // Vite otherwise rejects requests whose Host header isn't localhost. Dev-only, unused in the
    // Docker/production build.
    allowedHosts: true,
    proxy: {
      // The API has no /api prefix, so strip it. Compose points VITE_API_URL at the api service.
      "/api": {
        target: process.env.VITE_API_URL ?? "http://localhost:5122",
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api/, ""),
      },
    },
  },
  test: {
    environment: "jsdom",
    setupFiles: ["./src/test/setup.ts"],
  },
});
