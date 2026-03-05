<script setup>
import { ref } from "vue";

const employees = ref([]);
const progress = ref(0);
const totalCount = ref(0);
const isLoading = ref(false);
const errorMessage = ref("");
const lastLoadedCount = ref(0);
let abortController = null;

const streamUrl = "https://localhost:7164/api/employees/stream";

const fetchStream = async (isResume = false) => {
  isLoading.value = true;
  errorMessage.value = "";

  // Compute skip if resuming
  const skip = isResume ? employees.value.length : 0;

  // If not resuming, reset everything
  if (!isResume) {
    employees.value = [];
    progress.value = 0;
    totalCount.value = 0;
  }

  abortController = new AbortController();

  try {
    const response = await fetch(`${streamUrl}?skip=${skip}`, {
      signal: abortController.signal,
    });

    if (!response.ok) throw new Error("Failed to connect to API");

    const reader = response.body.getReader();
    const decoder = new TextDecoder("utf-8");

    let received = "";
    let firstLine = skip === 0; // only first time we need totalCount
    let processed = skip;

    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      received += decoder.decode(value, { stream: true });
      const lines = received.split("\n");
      received = lines.pop() || "";

      for (const line of lines) {
        if (!line.trim()) continue;

        let obj;
        try {
          obj = JSON.parse(line);
        } catch {
          continue;
        }

        // Handle total count on first stream only
        if (firstLine && obj.totalCount !== undefined) {
          totalCount.value = obj.totalCount;
          firstLine = false;
        } else {
          employees.value.push(obj);
          processed++;
          progress.value = Math.min(
            100,
            Math.round((processed / totalCount.value) * 100)
          );
        }
      }
    }
  } catch (err) {
    if (err.name !== "AbortError") {
      errorMessage.value = "Stream failed: " + err.message;
    }
  } finally {
    isLoading.value = false;
  }
};

const stopStream = () => {
  if (abortController) {
    abortController.abort();
    isLoading.value = false;
  }
};

const resumeStream = () => {
  if (!isLoading.value && employees.value.length < totalCount.value) {
    fetchStream(true);
  }
};
</script>

<template>
  <div class="p-4 space-y-4">
    <div class="flex gap-2">
      <button
        @click="fetchStream(false)"
        :disabled="isLoading"
        class="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50"
      >
        Stream Employees
      </button>

      <button
        v-if="isLoading"
        @click="stopStream"
        class="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700"
      >
        Stop
      </button>

      <button
        v-if="!isLoading && employees.length > 0 && employees.length < totalCount"
        @click="resumeStream"
        class="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700"
      >
        Resume
      </button>
    </div>

    <div v-if="isLoading" class="mt-2">
      <div class="w-full bg-gray-200 rounded-full h-2.5 overflow-hidden">
        <div
          class="bg-blue-600 h-2.5 transition-all duration-200"
          :style="{ width: progress + '%' }"
        ></div>
      </div>
      <p class="text-sm mt-1 text-gray-600">
        {{ progress.toFixed(1) }}% — {{ employees.length }} / {{ totalCount }}
        loaded
      </p>
    </div>

    <p v-if="errorMessage" class="text-red-500 text-sm mt-2">
      {{ errorMessage }}
    </p>

    <ul class="max-h-96 overflow-y-auto border rounded p-2 text-sm">
      <li v-for="e in employees" :key="e.EmplId">
        {{ e.Fullname }} — {{ e.Positionname }}
      </li>
    </ul>
  </div>
</template>
