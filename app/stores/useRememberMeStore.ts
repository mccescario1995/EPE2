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
  },
};

export const useRememberMeStore = defineStore("epe2_remember_me", {
  state: () => ({
    isRemembered: false as boolean,
    email: null as string | null,
  }),
  persist: {
    serializer: encryptedSerializer,
    pick: ["isRemembered", "email"],
  },
  actions: {
    setRemembered(isRemembered: boolean, credentials?: string) {
      this.isRemembered = isRemembered;
      if (credentials && isRemembered) {
        this.email = credentials;
      } else {
        this.email = null;
      }
    },
    getRememberedCredentials() {
      return this.email;
    },
  },
});
