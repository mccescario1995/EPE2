<template>
  <nav class="flex" aria-label="Breadcrumb" :aria-label="ariaLabel">
    <ol
      class="inline-flex items-center space-x-1 md:space-x-3 bg-white/50 backdrop-blur-sm rounded-xl px-4 py-3 shadow-sm border border-gray-100 dark:border-0 dark:*:bg-gray-800 dark:text-gray-200 dark:bg-gray-800"
    >
      <!-- Home icon with enhanced hover state -->
      <li class="inline-flex items-center group">
        <NuxtLink
          to="/main/dashboard"
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

      <!-- Dynamic breadcrumbs with responsive collapse -->
      <template v-for="(crumb, index) in breadcrumbs" :key="index">
        <!-- Show all on desktop; on mobile show first + last (with ellipsis) -->
        <li
          v-if="breadcrumbs.length <= 2 || index === 0 || index === breadcrumbs.length - 1"
          class="inline-flex items-center group"
        >
          <div class="flex items-center">
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

            <!-- Mobile ellipsis indicator -->
            <span
              v-if="index === 0 && breadcrumbs.length > 2"
              class="md:hidden inline-flex items-center text-sm font-medium text-gray-500 px-2"
              aria-hidden="true"
            >
              ...
            </span>

            <!-- Clickable breadcrumb -->
            <NuxtLink
              v-if="crumb.clickable && crumb.to"
              :to="crumb.to"
              :title="crumb.label"
              class="relative text-sm font-medium text-gray-600 hover:text-blue-600 transition-all duration-300 px-2 py-1 rounded-lg hover:bg-blue-50 dark:text-gray-400 dark:hover:text-blue-500 dark:hover:bg-blue-600/20 max-w-[120px] truncate md:max-w-none"
            >
              <span class="relative z-10">{{ crumb.label }}</span>
              <!-- Underline effect -->
              <span
                class="absolute bottom-0 left-2 right-2 h-0.5 bg-blue-600 scale-x-0 transition-transform duration-300 origin-left group-hover:scale-x-100 dark:bg-blue-500"
              ></span>
            </NuxtLink>

            <!-- Loading: unresolved menucode -->
            <span
              v-else-if="crumb.label.startsWith('#') && resolvingCodes.has(crumb.label.slice(1))"
              class="flex items-center gap-2 text-sm font-medium text-gray-600 px-3 py-1.5"
            >
              <svg class="animate-spin h-4 w-4 text-blue-600 dark:text-blue-400" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              <span class="truncate">{{ crumb.label }}</span>
            </span>

            <!-- Non-clickable breadcrumb (last item OR folder without index.vue) -->
            <span
              v-else
              :class="[
                'text-sm font-semibold text-gray-800 px-3 py-1.5 rounded-lg max-w-[120px] truncate md:max-w-none',
                crumb.clickable && index === breadcrumbs.length - 1
                  ? 'bg-linear-to-r from-blue-50 to-indigo-50 border-blue-100'
                  : ' border-gray-200  dark:text-gray-200',
              ]"
              :aria-current="index === breadcrumbs.length - 1 ? 'page' : undefined"
              :title="crumb.label"
            >
              {{ crumb.label }}
            </span>
          </div>
        </li>

        <!-- Intermediate crumbs hidden on mobile -->
        <li
          v-else
          class="hidden md:inline-flex items-center group"
        >
          <div class="flex items-center">
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

            <NuxtLink
              v-if="crumb.clickable && crumb.to"
              :to="crumb.to"
              :title="crumb.label"
              class="relative text-sm font-medium text-gray-600 hover:text-blue-600 transition-all duration-300 px-2 py-1 rounded-lg hover:bg-blue-50 dark:text-gray-400 dark:hover:text-blue-500 dark:hover:bg-blue-600/20 max-w-[120px] truncate md:max-w-none"
            >
              <span class="relative z-10">{{ crumb.label }}</span>
              <span
                class="absolute bottom-0 left-2 right-2 h-0.5 bg-blue-600 scale-x-0 transition-transform duration-300 origin-left group-hover:scale-x-100 dark:bg-blue-500"
              ></span>
            </NuxtLink>

            <span
              v-else-if="crumb.label.startsWith('#') && resolvingCodes.has(crumb.label.slice(1))"
              class="flex items-center gap-2 text-sm font-medium text-gray-600 px-3 py-1.5"
            >
              <svg class="animate-spin h-4 w-4 text-blue-600 dark:text-blue-400" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              <span class="truncate">{{ crumb.label }}</span>
            </span>

            <span
              v-else
              :class="[
                'text-sm font-semibold text-gray-800 px-3 py-1.5 rounded-lg shadow-sm max-w-[120px] truncate md:max-w-none',
                crumb.clickable && index === breadcrumbs.length - 1
                  ? 'bg-linear-to-r from-blue-50 to-indigo-50 border-blue-100'
                  : 'bg-gray-100 border-gray-200 dark:bg-gray-700 dark:border-gray-600 dark:text-gray-200',
              ]"
              :aria-current="index === breadcrumbs.length - 1 ? 'page' : undefined"
              :title="crumb.label"
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
import { computed } from "vue";
import { useHead } from "#imports";

const { breadcrumbs, resolvingCodes } = useBreadcrumbs();

const ariaLabel = computed(() => {
  const last = breadcrumbs.value[breadcrumbs.value.length - 1];
  return last ? `Breadcrumb: ${last.label}` : "Breadcrumb";
});

const jsonLd = computed(() => ({
  "@context": "https://schema.org",
  "@type": "BreadcrumbList",
  "itemListElement": breadcrumbs.value.map((crumb, index) => ({
    "@type": "ListItem",
    "position": index + 1,
    "name": crumb.label,
    "item": crumb.to ? `${window.location.origin}${crumb.to}` : undefined,
  })),
}));

useHead({
  script: [
    {
      type: "application/ld+json",
      children: JSON.stringify(jsonLd.value),
    },
  ],
});
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

/* Truncation fix for flex children */
li > div > span,
li > div > a {
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
</style>
