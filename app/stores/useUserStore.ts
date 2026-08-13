// stores/useUserStore.ts
import { defineStore } from "pinia";
import { encryptString, decryptString } from "../utils/storageCrypto";

const encryptedSerializer = {
  serialize: (data: unknown): string => {
    const json = JSON.stringify(data);
    return encryptString(json);
  },
  deserialize: (data: string) => {
    const decrypted = decryptString(data);
    if (!decrypted) return {};
    try {
      return JSON.parse(decrypted);
    } catch {
      return {};
    }
  }
};

export const useUserStore = defineStore("epe2_user", {
  state: () => ({
    token: null as string | null,
    profile: null as any,
    menu: null as any,
    menuAccess: null as any,
    menuVersion: null as string | null,
    role: null as string | null,
  }),

  getters: {
    isAuthenticated: (state) => !!state.token,
    isAdmin: (state) => {
      const roles = ['admin', 'administrator', 'superadmin'];
      return state.role && roles.includes(state.role.toLowerCase());
    },
    userRole: (state) => state.role || state.profile?.role || state.profile?.userRole || null,
  },

  actions: {
    setUser(token: string, profile: any) {
      this.token = token;
      this.profile = profile;
      this.role = profile?.role || profile?.userRole || null;
    },

    setToken(token: string) {
      this.token = token;
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
      // Handled automatically by encrypted persistence
    },

    logout() {
      this.token = null;
      this.profile = null;
      this.role = null;
      this.menu = null;
      nextTick(() => {
        localStorage.removeItem("user");
        localStorage.removeItem("epe2_user");
        localStorage.removeItem("auth_token");
        localStorage.removeItem("user_profile");
      });
      navigateTo("/");
    },

    async refreshMenuIfNeeded() {
      try {
        const backend: any = await apiFetch("/menu/version");
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
  persist: {
    serializer: encryptedSerializer,
    paths: ['token', 'profile', 'menu', 'menuAccess', 'menuVersion', 'role'],
  },
});
