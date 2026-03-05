// stores/useUserStore.ts
import { defineStore } from "pinia";

export const useUserStore = defineStore("user", {
  state: () => ({
    token: null as string | null,
    profile: null as any,
    menu: null as any,
    menuAccess: null as any,
    menuVersion: null as string | null,
  }),

  getters: {
    isAuthenticated: (state) => !!state.token,
  },

  actions: {
    setUser(token: string, profile: any) {
      this.token = token;
      this.profile = profile;
      localStorage.setItem("auth_token", token);
    },

    setToken(token: string) {
      this.token = token;
      localStorage.setItem("auth_token", token);
    },

    getToken() {
      return this.token;
    },

    setMenu(menu: any) {
      this.menu = menu;
    },

    setMenuAccess(menu: any) {
      this.menuAccess = menu;
    },

    loadUserFromStorage() {
      const savedToken = localStorage.getItem("auth_token");
      if (savedToken) this.token = savedToken;
    },

    logout() {
      this.token = null;
      this.profile = null;
      this.menu = null;
      localStorage.removeItem("auth_token");
      navigateTo("/");
    },
    
    // Refresh menu only if backend version changed
    async refreshMenuIfNeeded() {
      try {

        // Step 1: get current backend menu version
        const backend: any = await apiFetch("/menu/version");

        // Step 2: compare backend vs local
        if (backend.version !== this.menuVersion) {
          console.log("Menu version changed — refetching menu...");
          const newMenu = await apiFetch("/menu");
          this.menu = newMenu;
          this.menuVersion = backend.version;
        } else {
          console.log("Menu version is up-to-date.");
        }
      } catch (err) {
        console.error("Failed to refresh menu:", err);
      }
    },
  },
  persist: true,
});
