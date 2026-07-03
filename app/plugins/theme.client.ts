// app/plugins/theme.client.ts
import { useThemeStore } from "~/stores/useThemeStore";

export default defineNuxtPlugin(() => {
  const themeStore = useThemeStore();
  themeStore.initTheme();
});