// app/composables/useDarkMode.ts
import { ref, watch } from 'vue';

export const useDarkMode = () => {
  const isDark = ref(false);

  // Check localStorage on client-side
  const initializeTheme = () => {
    if (process.client) {
      const savedTheme = localStorage.getItem('theme');
      const prefersDark = window.matchMedia('(prefers-dark-scheme: dark)').matches;
      
      if (savedTheme === 'dark' || (!savedTheme && prefersDark)) {
        isDark.value = true;
        document.documentElement.classList.add('dark');
      }
    }
  };

  const toggleTheme = () => {
    isDark.value = !isDark.value;
  };

  const setTheme = (dark: boolean) => {
    isDark.value = dark;
  };

  // Watch for changes and update DOM
  watch(isDark, (newDark) => {
    if (process.client) {
      if (newDark) {
        document.documentElement.classList.add('dark');
        localStorage.setItem('theme', 'dark');
      } else {
        document.documentElement.classList.remove('dark');
        localStorage.setItem('theme', 'light');
      }
    }
  });

  // Initialize on mount
  if (process.client) {
    initializeTheme();
  }

  return { isDark, toggleTheme, setTheme };
};