// app/stores/useThemeStore.ts
import { defineStore } from "pinia";

export const useThemeStore = defineStore("theme", {
  state: () => ({
    isDark: false,
    initialized: false,
  }),

  persist: true, // Enable persistence for this store

  actions: {
    /**
     * Reads saved preference (or OS preference) and applies it.
     * Must run client-side only — called from a client plugin on app boot.
     */
    initTheme() {
      if (this.initialized || !import.meta.client) return;

      console.log("Initializing theme store...");

      const saved = localStorage.getItem("theme");
      const prefersDark = window.matchMedia(
        "(prefers-color-scheme: dark)"
      ).matches;

      console.log(`Saved theme: ${saved}, prefers dark: ${prefersDark}`);

      this.isDark = saved ? saved === "dark" : prefersDark;
      this.applyTheme();
      this.initialized = true;

      // Keep in sync if the OS theme changes and the user hasn't set an explicit preference
      window
        .matchMedia("(prefers-color-scheme: dark)")
        .addEventListener("change", (e) => {
          if (!localStorage.getItem("theme")) {
            this.isDark = e.matches;
            this.applyTheme();
          }
        });
    },

    toggleTheme() {
      this.isDark = !this.isDark;
      console.log(`Toggling theme. New isDark: ${this.isDark}`);
      this.applyTheme();
    },

    setTheme(dark: boolean) {
      this.isDark = dark;
      console.log(`Setting theme. New isDark: ${this.isDark}`);
      this.applyTheme();
    },

    applyTheme() {
      if (!import.meta.client) return;
      const root = document.documentElement;
      if (this.isDark) {
        root.classList.add("dark");
        console.log("Applied dark theme");
        localStorage.setItem("theme", "dark");
      } else {
        root.classList.remove("dark");
        console.log("Applied light theme");
        localStorage.setItem("theme", "light");
      }
    },
  },
});