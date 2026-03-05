import piniaPluginPersistedstate from "pinia-plugin-persistedstate";
import type { Pinia } from "pinia";

export default defineNuxtPlugin((nuxtApp) => {
  const pinia = nuxtApp.$pinia as unknown as Pinia;
  pinia.use(piniaPluginPersistedstate);
});
