<template>
  <div>
    <button
      @click="firstModal = true"
      class="px-4 py-2 bg-blue-500 text-white rounded"
    >
      Open Modal
    </button>

    <Modal
      v-model:isOpen="firstModal"
      size="lg"
      position="center"
      :closeOnOverlayClick="true"
      :closeOnEsc="true"
      :zIndex="50"
    >
      <template #header>
        <h3 class="text-lg font-medium leading-6 text-gray-500">
          Top Centered Modal
        </h3>
      </template>

      <div class="mt-2">
        <p>This is a simple nested modal example.</p>
        <div>
          <button
            @click="secondModal = true"
            class="px-4 py-2 bg-blue-500 text-white rounded"
          >
            Open Modal
          </button>
        </div>
        <Modal
          v-model:isOpen="secondModal"
          size="lg"
          position="center"
          :closeOnOverlayClick="true"
          :closeOnEsc="true"
          :zIndex="60"
        >
          <template #header>
            <h3 class="text-lg font-medium leading-6 text-gray-500">
              Nested Modal
            </h3>
          </template>

          <div class="mt-2">
            <p>This is a simple nested modal example.</p>
          </div>

          <template #footer>
            <button
              @click="secondModal = false"
              class="px-4 py-2 bg-blue-500 text-white rounded"
            >
              Close
            </button>
          </template>
        </Modal>
      </div>

      <template #footer>
        <button
          @click="firstModal = false"
          class="px-4 py-2 bg-blue-500 text-white rounded"
        >
          Close
        </button>
      </template>
    </Modal>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';

const firstModal = ref(false);
const secondModal = ref(false);
</script>

<style scoped>
@import "tailwindcss";
input,
textarea {
  @apply block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm py-3 px-4 border;
}
</style>
