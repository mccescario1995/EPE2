<script setup lang="ts">
const props = defineProps<{
  currentPage: number;
  perPage?: number;
  totalItems: number;
}>();

const emit = defineEmits<{
  (e: "page-change", page: number): void;
}>();

const totalPages = computed(() => {
  console.log("this is items", props.totalItems);
  console.log("this is page", props.perPage);

  return Math.ceil(props.totalItems / (props.perPage || 10));
});

const goToPage = (page: number) => {
  const pageNum = Math.max(1, Math.min(totalPages.value, page));
  emit("page-change", pageNum);
};

// Input for jump
const jumpPage = ref("");
const handleJump = () => {
  const page = parseInt(jumpPage.value);
  if (!isNaN(page)) {
    goToPage(page);
    jumpPage.value = "";
  }
};

// Displayed page buttons logic
const visiblePages = computed(() => {
  const total = totalPages.value;
  const current = props.currentPage;
  const pages: (number | string)[] = [];

  if (total <= 7) {
    for (let i = 1; i <= total; i++) pages.push(i);
  } else {
    pages.push(1);

    if (current <= 2) {
      pages.push(2, 3);
      pages.push("...");
    } else if (current == 3) {
      pages.push(2, 3, 4);
      pages.push("...");
    } else if (current >= total - 1) {
      pages.push("...");
      for (let i = total - 2; i < total; i++) {
        pages.push(i);
      }
    } else {
      pages.push("...", current - 1, current, current + 1, "...");
    }

    pages.push(total);
  }

  return pages;
});
</script>

<template>
  <div
    v-if="totalPages > 1"
    class="flex flex-col sm:flex-row justify-between items-center gap-6 mt-8 p-4 bg-gradient-to-r from-gray-50 to-white rounded-xl border border-gray-200 dark:from-gray-800 dark:to-gray-900 dark:border-gray-700"
  >
    <!-- Pagination Controls -->
    <div class="flex items-center gap-2 flex-wrap">
      <!-- First Page -->
      <button
        @click="goToPage(1)"
        :disabled="currentPage === 1"
        class="inline-flex items-center justify-center w-10 h-10 text-sm font-medium text-gray-600 bg-white border border-gray-200 rounded-xl hover:bg-gray-300 hover:text-gray-600 hover:border-gray-200 disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:bg-white disabled:hover:text-gray-600 disabled:hover:border-gray-200 transition-all duration-200 group dark:bg-gray-700 dark:text-gray-400 dark:border-gray-600 dark:hover:bg-gray-600 dark:hover:text-gray-300 dark:hover:border-gray-500 dark:disabled:hover:bg-gray-700 dark:disabled:hover:text-gray-500 dark:disabled:hover:border-gray-600"
        :class="{ 'shadow-md': currentPage !== 1 }"
      >
        <svg
          class="w-4 h-4 group-hover:scale-110 transition-transform duration-200"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M11 19l-7-7 7-7m8 14l-7-7 7-7"
          />
        </svg>
      </button>

      <!-- Previous Page -->
      <button
        @click="goToPage(currentPage - 1)"
        :disabled="currentPage === 1"
        class="inline-flex items-center justify-center w-10 h-10 text-sm font-medium text-gray-600 bg-white border border-gray-200 rounded-xl hover:bg-gray-300 hover:text-gray-600 hover:border-gray-200 disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:bg-white disabled:hover:text-gray-600 disabled:hover:border-gray-200 transition-all duration-200 group dark:bg-gray-700 dark:text-gray-400 dark:border-gray-600 dark:hover:bg-gray-600 dark:hover:text-gray-300 dark:hover:border-gray-500 dark:disabled:hover:bg-gray-700 dark:disabled:hover:text-gray-500 dark:disabled:hover:border-gray-600"
        :class="{ 'shadow-md': currentPage !== 1 }"
      >
        <svg
          class="w-4 h-4 group-hover:scale-110 transition-transform duration-200"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M15 19l-7-7 7-7"
          />
        </svg>
      </button>

      <!-- Page Numbers -->
      <div class="flex items-center gap-1">
        <template v-for="(page, i) in visiblePages" :key="i">
          <!-- Ellipsis -->
          <span
            v-if="typeof page === 'string'"
            class="inline-flex items-center justify-center w-10 h-10 text-gray-400 select-none dark:text-gray-500"
          >
            <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
              <path
                d="M10 6a2 2 0 110-4 2 2 0 010 4zM10 12a2 2 0 110-4 2 2 0 010 4zM10 18a2 2 0 110-4 2 2 0 010 4z"
              />
            </svg>
          </span>

          <!-- Clickable Page Numbers -->
          <button
            v-else
            @click="goToPage(page)"
            :class="[
              'inline-flex items-center justify-center w-10 h-10 text-sm font-medium rounded-xl transition-all duration-200 group',
              currentPage === page
                ? 'bg-gradient-to-r from-indigo-600 to-indigo-600 text-white shadow-lg shadow-indigo-500/25 hover:shadow-xl hover:scale-105'
                : 'text-gray-600 bg-white border border-gray-200 hover:bg-indigo-50 hover:text-indigo-600 hover:border-indigo-200 hover:shadow-md hover:scale-105 dark:bg-gray-700 dark:text-gray-400 dark:border-gray-600 dark:hover:bg-gray-600 dark:hover:text-indigo-400 dark:hover:border-indigo-400',
            ]"
          >
            <span
              class="group-hover:scale-110 transition-transform duration-200"
              >{{ page }}</span
            >
          </button>
        </template>
      </div>

      <!-- Next Page -->
      <button
        @click="goToPage(currentPage + 1)"
        :disabled="currentPage === totalPages"
        class="inline-flex items-center justify-center w-10 h-10 text-sm font-medium text-gray-600 bg-white border border-gray-200 rounded-xl hover:bg-indigo-50 hover:text-indigo-600 hover:border-indigo-200 disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:bg-white disabled:hover:text-gray-600 disabled:hover:border-gray-200 transition-all duration-200 group dark:bg-gray-700 dark:text-gray-400 dark:border-gray-600 dark:hover:bg-gray-600 dark:hover:text-indigo-400 dark:hover:border-indigo-400 dark:disabled:hover:bg-gray-700 dark:disabled:hover:text-gray-500 dark:disabled:hover:border-gray-600"
        :class="{ 'shadow-md': currentPage !== totalPages }"
      >
        <svg
          class="w-4 h-4 group-hover:scale-110 transition-transform duration-200"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M9 5l7 7-7 7"
          />
        </svg>
      </button>

<!-- Last Page -->
      <button
        @click="goToPage(totalPages)"
        :disabled="currentPage === totalPages"
        class="inline-flex items-center justify-center w-10 h-10 text-sm font-medium text-gray-600 bg-white border border-gray-200 rounded-xl hover:bg-gray-300 hover:text-gray-600 hover:border-gray-200 disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:bg-white disabled:hover:text-gray-600 disabled:hover:border-gray-200 transition-all duration-200 group dark:bg-gray-700 dark:text-gray-400 dark:border-gray-600 dark:hover:bg-gray-600 dark:hover:text-gray-300 dark:hover:border-gray-500 dark:disabled:hover:bg-gray-700 dark:disabled:hover:text-gray-500 dark:disabled:hover:border-gray-600"
        :class="{ 'shadow-md': currentPage !== totalPages }"
      >
        <svg
          class="w-4 h-4 group-hover:scale-110 transition-transform duration-200"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M13 5l7 7-7 7M5 5l7 7-7 7"
          />
        </svg>
      </button>
    </div>

    <!-- Go to Page -->
    <div
      class="flex items-center gap-3 bg-white rounded-xl border border-gray-200 px-4 py-2 shadow-sm hover:shadow-md transition-shadow duration-200 dark:bg-gray-800 dark:border-gray-700"
    >
      <label
        for="goTo"
        class="text-sm font-medium text-gray-700 dark:text-gray-300 whitespace-nowrap"
      >
        Go to page:
      </label>
      <div class="relative">
        <input
          id="goTo"
          v-model="jumpPage"
          @keydown.enter="handleJump"
          type="number"
          min="1"
          :max="totalPages"
          class="w-16 px-3 py-1.5 text-sm font-medium text-center bg-gray-50 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent focus:bg-white transition-all duration-200 dark:bg-gray-700 dark:border-gray-600 dark:text-gray-200 dark:focus:ring-indigo-500 dark:focus:border-indigo-500 dark:placeholder-gray-500"
          :placeholder="String(currentPage)"
        />
      </div>
      <span class="text-sm text-gray-500 dark:text-gray-400">of {{ totalPages }}</span>
    </div>
  </div>
</template>
