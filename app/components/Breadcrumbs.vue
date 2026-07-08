<template>
  <nav class="flex" aria-label="Breadcrumb">
    <ol
      class="inline-flex items-center space-x-1 md:space-x-3 bg-white/50 backdrop-blur-sm rounded-xl px-4 py-3 shadow-sm border border-gray-100 dark:border-0 dark:*:bg-gray-800 dark:text-gray-200 dark:bg-gray-800"
    >
      <!-- Home icon with enhanced hover state -->
      <li class="inline-flex items-center group">
        <NuxtLink
          to="/main/dashboard/"
          class="inline-flex items-center text-sm font-medium text-gray-600 hover:text-blue-600 transition-all duration-300 hover:scale-105 dark:text-gray-400 dark:hover:text-blue-500"
        >
          <div class="relative">
            <svg
              class="w-5 h-5 text-gray-400 group-hover:text-blue-500 transition-colors duration-300 dark:text-gray-500 dark:hover:text-blue-400"
              fill="currentColor"
              viewBox="0 0 20 20"
              aria-hidden="true"
            >
              <path
                d="M10.707 2.293a1 1 0 00-1.414 0l-7 7a1 1 0 001.414 1.414L4 10.414V17a1 1 0 001 1h2a1 1 0 001-1v-2a1 1 0 011-1h2a1 1 0 011 1v2a1 1 0 001 1h2a1 1 0 001-1v-6.586l.293.293a1 1 0 001.414-1.414l-7-7z"
              />
            </svg>
            <!-- Hover effect circle -->
            <div
              class="absolute inset-0 bg-blue-100/50 rounded-full scale-0 group-hover:scale-150 transition-transform duration-300 -z-10 opacity-0 group-hover:opacity-100 dark:bg-blue-600/20"
            ></div>
          </div>
          <span class="sr-only">Home</span>
        </NuxtLink>
      </li>

      <!-- Dynamic breadcrumbs with enhanced design -->
      <template v-for="(crumb, index) in breadcrumbs" :key="index">
        <li class="inline-flex items-center">
          <div class="flex items-center group">
            <!-- Separator with animation -->
            <svg
              class="w-5 h-5 text-gray-300 mx-1 md:mx-2 transition-transform duration-300 group-hover:translate-x-1 dark:text-gray-400"
              fill="currentColor"
              viewBox="0 0 20 20"
              aria-hidden="true"
            >
              <path
                fill-rule="evenodd"
                d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z"
                clip-rule="evenodd"
              />
            </svg>

            <!-- Clickable breadcrumb (only if has index.vue) -->
            <NuxtLink
              v-if="crumb.clickable && crumb.to"
              :to="crumb.to"
              class="relative text-sm font-medium text-gray-600 hover:text-blue-600 transition-all duration-300 px-2 py-1 rounded-lg hover:bg-blue-50 dark:text-gray-400 dark:hover:text-blue-500 dark:hover:bg-blue-600/20"
            >
              <span class="relative z-10">{{ crumb.label }}</span>
              <!-- Underline effect -->
              <span
                class="absolute bottom-0 left-2 right-2 h-0.5 bg-blue-600 scale-x-0 transition-transform duration-300 origin-left group-hover:scale-x-100 dark:bg-blue-500"
              ></span>
            </NuxtLink>

            <!-- Non-clickable breadcrumb (last item OR folder without index.vue) -->
            <span
              v-else
              :class="[
                'text-sm font-semibold text-gray-800 px-3 py-1.5 rounded-lg border shadow-sm',
                crumb.clickable && index === breadcrumbs.length - 1
                  ? 'bg-linear-to-r from-blue-50 to-indigo-50 border-blue-100'
                  : 'bg-gray-100 border-gray-200 dark:bg-gray-700 dark:border-gray-600 dark:text-gray-200',
              ]"
              :aria-current="
                index === breadcrumbs.length - 1 ? 'page' : undefined
              "
            >
              {{ crumb.label }}
            </span>
          </div>
        </li>
      </template>
    </ol>
  </nav>
</template>

<script setup lang="ts">
import { useBreadcrumbs } from "~/composables/useBreadcrumbs";

const { breadcrumbs } = useBreadcrumbs();
</script>

<style scoped>
/* Enhanced micro-interactions */
a {
  transition-property: all;
  transition-duration: 300ms;
  transition-timing-function: cubic-bezier(0.4, 0, 0.2, 1);
}

/* Smooth opacity fade for separator on hover */
li:hover svg {
  opacity: 0.8;
}

/* Add subtle shadow on hover for links */
a:hover {
  text-shadow: 0 0 8px rgba(59, 130, 246, 0.1);
}
</style>
