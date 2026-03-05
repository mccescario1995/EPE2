<template>
  <div>
    <button
      @click="isModalOpen = true"
      class="px-4 py-2 bg-blue-500 text-white rounded"
    >
      Open Modal
    </button>

    <Modal
      v-model:isOpen="isModalOpen"
      size="lg"
      position="Centered"
      :closeOnOverlayClick="true"
      :closeOnEsc="true"
    >
      <template #header>
        <h3 class="text-lg font-medium leading-6 text-gray-900">
          Top Centered Modal
        </h3>
      </template>

      <div class="mt-2">
        <form class="mx-auto grid max-w-screen-md gap-4 sm:grid-cols-2">
          <div>
            <label
              for="first-name"
              class="mb-2 inline-block text-sm text-gray-800 sm:text-base"
              >First name</label
            >
            <input
              name="first-name"
              class="w-full rounded border bg-gray-50 px-3 py-2 text-gray-800 outline-none ring-indigo-300 transition duration-100 focus:ring"
            />
          </div>

          <div>
            <label
              for="last-name"
              class="mb-2 inline-block text-sm text-gray-800 sm:text-base"
              >Last name</label
            >
            <input
              name="last-name"
              class="w-full rounded border bg-gray-50 px-3 py-2 text-gray-800 outline-none ring-indigo-300 transition duration-100 focus:ring"
            />
          </div>

          <div class="sm:col-span-2">
            <label
              for="Contact"
              class="mb-2 inline-block text-sm text-gray-800 sm:text-base"
              >Contact
            </label>
            <input
              name="Contact"
              class="w-full rounded border bg-gray-50 px-3 py-2 text-gray-800 outline-none ring-indigo-300 transition duration-100 focus:ring"
            />
          </div>

          <div class="sm:col-span-2">
            <label
              for="email"
              class="mb-2 inline-block text-sm text-gray-800 sm:text-base"
              >Email</label
            >
            <input
              name="email"
              class="w-full rounded border bg-gray-50 px-3 py-2 text-gray-800 outline-none ring-indigo-300 transition duration-100 focus:ring"
            />
          </div>
          <div class="sm:col-span-2">
            <label
              for="message"
              class="mb-2 inline-block text-sm text-gray-800 sm:text-base"
              >Message</label
            >
            <textarea
              name="message"
              class="h-64 w-full rounded border bg-gray-50 px-3 py-2 text-gray-800 outline-none ring-indigo-300 transition duration-100 focus:ring"
            ></textarea>
          </div>

          <div class="flex items-center justify-between sm:col-span-2">
            <button
              class="mx-2 px-4 py-2 text-sm rounded text-white bg-blue-500 focus:outline-none hover:bg-blue-300"
            >
              Send
            </button>
          </div>
        </form>
      </div>

      <template #footer>
        <button
          @click="isModalOpen = false"
          class="px-4 py-2 bg-blue-500 text-white rounded"
        >
          Close
        </button>
      </template>
    </Modal>
  </div>
</template>

<script setup lang="ts">
const isModalOpen = ref(false);
</script>
