// stores/usePendingTransactionStore.ts
import { defineStore } from "pinia";
import { encryptString, decryptString } from "../utils/storageCrypto";

const PREFIX = "epe2_pending_transaction";
const TTL_MS = 24 * 60 * 60 * 1000;

interface Snapshot {
  route: string;
  action: string;
  data: Record<string, any>;
  attachmentMeta: { name: string; size: number; type: string }[];
  timestamp: string;
}

const encryptedSerializer = {
  serialize: (data: unknown): string => {
    const json = JSON.stringify(data);
    return encryptString(json);
  },
  deserialize: (data: string) => {
    const decrypted = decryptString(data);
    if (!decrypted) return { snapshots: {} };
    try {
      return JSON.parse(decrypted);
    } catch {
      return { snapshots: {} };
    }
  },
};

export const usePendingTransactionStore = defineStore(
  "epe2_pending_transaction",
  {
    state: (): { snapshots: Record<string, Snapshot> } => ({
      snapshots: {},
    }),

    actions: {
      getKey(transactionId: string, route: string): string {
        return `${PREFIX}_${transactionId}_${route.replace(/\//g, "_")}`;
      },

      saveSnapshot(
        transactionId: string,
        route: string,
        action: string,
        data: Record<string, any>,
        attachmentMeta: { name: string; size: number; type: string }[] = []
      ) {
        const key = this.getKey(transactionId, route);
        this.snapshots[key] = {
          route,
          action,
          data,
          attachmentMeta,
          timestamp: new Date().toISOString(),
        };
      },

      checkPending(transactionId: string, route: string): Snapshot | null {
        const key = this.getKey(transactionId, route);
        const snapshot = this.snapshots[key];
        if (!snapshot) return null;

        const age = Date.now() - new Date(snapshot.timestamp).getTime();
        if (age > TTL_MS) {
          delete this.snapshots[key];
          return null;
        }

        return snapshot;
      },

      discardPending(transactionId: string, route: string) {
        const key = this.getKey(transactionId, route);
        delete this.snapshots[key];
        try {
          const storeKey = "epe2_pending_transaction";
          const stored = localStorage.getItem(storeKey);
          if (stored) {
            const decrypted = decryptString(stored);
            if (decrypted) {
              const parsed = JSON.parse(decrypted);
              if (parsed.snapshots && parsed.snapshots[key]) {
                delete parsed.snapshots[key];
                localStorage.setItem(storeKey, encryptString(JSON.stringify(parsed)));
                console.log(`Discarded pending transaction for transaction ${transactionId} and route ${route}`);
              }
            }
          }
        } catch {
          // ignore
        }
      },
    },

    persist: {
      serializer: encryptedSerializer,
      pick: ["snapshots"],
    },
  }
);
