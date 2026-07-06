// app/stores/useThemeStore.ts
import { defineStore } from "pinia";

export const useThemeStore = defineStore("theme", {
  state: () => ({
    isDark: false,
    // Tracks whether the USER explicitly picked a theme (via toggle/setTheme),
    // vs. just following the OS preference.
    hasUserPreference: false,
  }),

  persist: true,

  actions: {
    initTheme() {
      if (!import.meta.client) return;

      if (!this.hasUserPreference) {
        this.isDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
      }

      // Always apply on boot — this used to be gated behind a persisted
      // "initialized" flag, which is what caused the light-on-reload bug:
      // that flag survived reload, so this function returned early and
      // never touched the DOM, even though isDark was correctly restored.
      this.applyTheme();

      window
        .matchMedia("(prefers-color-scheme: dark)")
        .addEventListener("change", (e) => {
          if (!this.hasUserPreference) {
            this.isDark = e.matches;
            this.applyTheme();
          }
        });
    },

    toggleTheme() {
      this.isDark = !this.isDark;
      this.hasUserPreference = true;
      this.applyTheme();
    },

    setTheme(dark: boolean) {
      this.isDark = dark;
      this.hasUserPreference = true;
      this.applyTheme();
    },

    applyTheme() {
      if (!import.meta.client) return;
      const root = document.documentElement;
      if (this.isDark) {
        root.classList.add("dark");
      } else {
        root.classList.remove("dark");
      }
      // No manual localStorage writes here anymore — persist: true
      // already handles saving isDark/hasUserPreference for us,
      // and there's nothing left to collide with.
    },
  },
});