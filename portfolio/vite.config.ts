import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { tanstackStart } from "@tanstack/react-start/plugin/vite";
import tsConfigPaths from "vite-tsconfig-paths";
import tailwindcss from "@tailwindcss/vite";
import { nitro } from "nitro/vite";

export default defineConfig({
  plugins: [
    tanstackStart(),
    nitro({ preset: "vercel", vercel: { entryFormat: "node" } }),
    react(),
    tsConfigPaths(),
    tailwindcss(),
  ],
  resolve: {
    alias: {
      "@": "/src",
    },
  },
  optimizeDeps: {
    include: [
      "use-sync-external-store/shim/with-selector",
      "@tanstack/react-store",
      // Pre-bundle lucide-react once so the browser doesn't pay
      // toPascalCase / toCamelCase costs on every hydration.
      "lucide-react",
    ],
  },
  build: {
    rollupOptions: {
      output: {
        manualChunks(id) {
          if (id.includes("lucide-react")) return "icons";
          if (id.includes("framer-motion")) return "motion";
        },
      },
    },
  },
  server: {
    port: 5173,
    host: true,
    allowedHosts: true,
    watch: {
      ignored: ["**/.vercel/**", "**/dist/**"],
    },
    proxy: {
      "/api": {
        target: "http://localhost:5039",
        changeOrigin: true,
      },
    },
  },
});
