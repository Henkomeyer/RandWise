import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import { defineConfig } from "vitest/config";

const repositoryName = process.env.GITHUB_REPOSITORY?.split("/")[1];
const defaultBase = repositoryName?.endsWith(".github.io") ? "/" : `/${repositoryName ?? ""}/`;
const base = process.env.VITE_BASE_PATH ?? (process.env.GITHUB_ACTIONS ? defaultBase : "/");

export default defineConfig({
  base,
  plugins: [react(), tailwindcss()],
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: "./vitest.setup.ts",
    css: true
  }
});
