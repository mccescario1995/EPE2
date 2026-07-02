// app/plugins/theme.client.ts
export default defineNuxtPlugin(() => {
  const themeStore = useThemeStore();
  themeStore.initTheme();
});