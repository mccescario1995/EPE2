<template>
  <main class="mx-auto max-w-3xl space-y-4 p-6">
    <h1 class="text-xl font-semibold">Rich Text Editor</h1>

    <div class="flex flex-wrap items-center gap-4 text-sm">
      <label class="flex items-center gap-2">
        <input v-model="locked" type="checkbox" class="rounded border-gray-300" />
        Disabled
      </label>
      <span class="text-gray-500 dark:text-gray-400">
        Output length: {{ output.length }} characters
      </span>
    </div>

    <TipTapEditor
      v-model="output"
      label="Description"
      placeholder="Start typing…"
      :maxCharCount="200"
      :minCharCount="10"
      :rowCount="10"
      :disabled="locked"
      :rules="[(value) => (value.includes('!') ? 'Output must not contain an exclamation mark.' : true)]"
      customClass="w-full"
    />

    <details class="text-sm">
      <summary class="cursor-pointer text-gray-600 dark:text-gray-300">v-model output (HTML)</summary>
      <pre class="mt-2 overflow-x-auto rounded-md bg-gray-100 p-3 text-xs dark:bg-gray-800">{{ output }}</pre>
    </details>
  </main>
</template>

<script setup lang="ts">
import { ref } from 'vue'

const output = ref('<p>Hello world</p>')
const locked = ref(false)
</script>
