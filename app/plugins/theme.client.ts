// app/plugins/theme.client.ts
import { useThemeStore } from '~/stores/useThemeStore';

export default defineNuxtPlugin(() => {
  // Initialize theme store on client side
  const themeStore = useThemeStore();
  
  // Force initialization if not already done
  if (!themeStore.isDark && process.client) {
    const savedTheme = localStorage.getItem('theme');
    const prefersDark = window.matchMedia('(prefers-dark-scheme: dark)').matches;
    
    if (savedTheme === 'dark' || (!savedTheme && prefersDark)) {
      themeStore.setTheme(true);
    }
  }
});