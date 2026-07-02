<!-- app/components/ThemeToggle.vue -->
<template>
  <button
    @click="toggleTheme"
    class="flex items-center justify-center w-10 h-10 rounded-full bg-gray-200 dark:bg-gray-700 transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-blue-500"
    :aria-label="isDark ? 'Switch to light mode' : 'Switch to dark mode'"
  >
    <!-- Sun icon (light mode) -->
    <svg
      v-if="!isDark"
      class="w-5 h-5 text-gray-700 dark:text-gray-300"
      fill="currentColor"
      viewBox="0 0 20 20"
    >
      <path d="M10 2a8 8 0 00-8 8c0 .386.02.763.054 1.125L10 12.5l7.946-2.471C17.98 11.763 18 11.386 18 11a8 8 0 00-8-8z" />
      <path fill-rule="evenodd" d="M5.172 14.172a1 1 0 011.414 0L10 14.586l3.414-3.414a1 1 0 011.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clip-rule="evenodd" />
    </svg>
    
    <!-- Moon icon (dark mode) -->
    <svg
      v-else
      class="w-5 h-5 text-gray-300"
      fill="currentColor"
      viewBox="0 0 20 20"
    >
      <path d="M17.293 13.293A8 8 0 016.707 2.707a8.001 8.001 0 1010.586 10.586z" />
    </svg>
  </button>
</template>

<script setup>
import { storeToRefs } from "pinia";
import { useThemeStore } from "~/stores/useThemeStore";

const themeStore = useThemeStore();
const { isDark, toggleTheme } = storeToRefs(themeStore);
</script>