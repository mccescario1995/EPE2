<!-- app/components/ThemeToggle.vue -->
<template>
  <button
    type="button"
    @click="toggleTheme"
    :aria-label="isDark ? 'Switch to light mode' : 'Switch to dark mode'"
    class="relative inline-flex h-6 w-9 items-center rounded-full transition-colors duration-300 focus:outline-none focus:ring-2 focus:ring-gray-500 ring-gray-900 border dark:border-gray-100 dark:border-2"
    :class="isDark ? 'bg-slate-700' : 'bg-yellow-100'"
  >
    <!-- Toggle Thumb -->
    <span
      class="absolute flex h-4 w-4 items-center justify-center rounded-full bg-white shadow-md transition-transform duration-200"
      :class="isDark ? 'translate-x-4' : 'translate-x-0.5'"
    >
      <!-- Sun -->
      <!-- <svg
        v-if="!isDark"
        xmlns="http://www.w3.org/2000/svg"
        class="h-4 w-4 text-yellow-500"
        fill="currentColor"
        viewBox="0 0 20 20"
      >
        <path
          d="M10 4a1 1 0 011 1v1a1 1 0 11-2 0V5a1 1 0 011-1zm0 9a2 2 0 100-4 2 2 0 000 4zm6-3a1 1 0 011 1h-1a1 1 0 110-2h1a1 1 0 01-1 1zM4 10a1 1 0 100 2H3a1 1 0 100-2h1zm10.95 4.536a1 1 0 011.414 1.414l-.707.707a1 1 0 11-1.414-1.414l.707-.707zM6.464 5.05A1 1 0 115.05 6.464l-.707-.707A1 1 0 115.757 4.343l.707.707zm8.486-1.414a1 1 0 010 1.414l-.707.707A1 1 0 1112.83 5.05l.707-.707a1 1 0 011.414 0zM6.464 13.536a1 1 0 010 1.414l-.707.707A1 1 0 114.343 14.95l.707-.707a1 1 0 011.414 0z"
        />
      </svg> -->

      <svg2
        v-if="!isDark"
        xmlns="http://www.w3.org/2000/svg"
        class="h-4 w-4 text-yellow-400 border rounded-full"
        fill="currentColor"
        viewBox="0 0 640 640"
      >
        <!--!Font Awesome Free v7.3.0 by @fontawesome - https://fontawesome.com License - https://fontawesome.com/license/free Copyright 2026 Fonticons, Inc.-->
        <path
          d="M320 32C328.4 32 336.3 36.4 340.6 43.7L396.1 136.3L500.9 110C509.1 108 517.8 110.4 523.7 116.3C529.6 122.2 532 131 530 139.1L503.7 243.8L596.4 299.3C603.6 303.6 608.1 311.5 608.1 319.9C608.1 328.3 603.7 336.2 596.4 340.5L503.7 396.1L530 500.8C532 509 529.6 517.7 523.7 523.6C517.8 529.5 509 532 500.9 530L396.2 503.7L340.7 596.4C336.4 603.6 328.5 608.1 320.1 608.1C311.7 608.1 303.8 603.7 299.5 596.4L243.9 503.7L139.2 530C131 532 122.4 529.6 116.4 523.7C110.4 517.8 108 509 110 500.8L136.2 396.1L43.6 340.6C36.4 336.2 32 328.4 32 320C32 311.6 36.4 303.7 43.7 299.4L136.3 243.9L110 139.1C108 130.9 110.3 122.3 116.3 116.3C122.3 110.3 131 108 139.2 110L243.9 136.2L299.4 43.6L301.2 41C305.7 35.3 312.6 31.9 320 31.9zM320 176C240.5 176 176 240.5 176 320C176 399.5 240.5 464 320 464C399.5 464 464 399.5 464 320C464 240.5 399.5 176 320 176zM320 416C267 416 224 373 224 320C224 267 267 224 320 224C373 224 416 267 416 320C416 373 373 416 320 416z"
        />
      </svg2>

      <!-- Moon -->
      <svg
        v-else
        xmlns="http://www.w3.org/2000/svg"
        class="h-4 w-4 text-slate-700"
        fill="currentColor"
        viewBox="0 0 20 20"
      >
        <path
          d="M17.293 13.293A8 8 0 016.707 2.707a8.001 8.001 0 1010.586 10.586z"
        />
      </svg>
    </span>
  </button>
</template>

<script setup>
import { storeToRefs } from "pinia";
import { useThemeStore } from "~/stores/useThemeStore";

const themeStore = useThemeStore();

const { isDark } = storeToRefs(themeStore);
const { toggleTheme } = themeStore;
</script>
