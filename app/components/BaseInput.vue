<!-- components/BaseInput.vue -->
<template>
  <div class="w-full relative">
    <!-- Label (optional) -->
    <label
      v-if="label"
      :for="id"
      class="block mb-1 text-sm font-medium text-gray-700"
    >
      {{ label }}
    </label>

    <!-- Wrapper for input + password toggle -->
    <div class="relative">
      <component
        :is="isTextarea ? 'textarea' : 'input'"
        v-model="innerValue"
        :value="innerValue"
        :id="id"
        :type="!isTextarea ? inputType : undefined"
        :rows="isTextarea ? rowCount : undefined"
        :maxlength="maxCharCount || undefined"
        :class="[
          customClass ||
            'block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm py-3 px-4 border',
        ]"
        @input="handleInput"
        @blur="validateInput"
      />

      <!-- Password show/hide toggle -->
      <button
        v-if="type === 'password'"
        type="button"
        @click="togglePassword"
        class="absolute inset-y-0 right-3 flex items-center text-gray-500 hover:text-gray-700 text-sm"
      >
        <!-- If custom icons passed in props, use them -->
        <component
          v-if="showPassword ? hideIcon : showIcon"
          :is="showPassword ? hideIcon : showIcon"
          class="w-5 h-5"
        />
        <!-- Fallback default icons -->
        <svg
          v-else-if="showPassword"
          xmlns="http://www.w3.org/2000/svg"
          class="h-5 w-5"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M13.875 18.825A10.05 10.05 0 0112 19c-5.523 0-10-4.477-10-10a9.959 9.959 0 011.701-5.558m3.26 3.26A3.001 3.001 0 1115 12a2.996 2.996 0 01-4.734 2.546M9.88 9.88l4.24 4.24"
          />
        </svg>
        <svg
          v-else
          xmlns="http://www.w3.org/2000/svg"
          class="h-5 w-5"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
          />
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M2.458 12C3.732 7.943 7.523 5 12 5c4.477 0 8.268 2.943 9.542 7-1.274 4.057-5.065 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"
          />
        </svg>
      </button>
    </div>

    <div class="flex justify-between">
      <!-- Error message -->
      <p v-if="errorMessage" class="mt-1 text-xs text-red-500">
        {{ errorMessage }}
      </p>
      <!-- Character counter -->
      <div
        v-if="maxCharCount"
        class="mt-1 text-xs text-gray-500 flex justify-end"
      >
        {{ charCount }} / {{ maxCharCount }}
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";

interface Props {
  modelValue?: string | number | null;
  label?: string;
  type?: string;
  maxCharCount?: number;
  rowCount?: number;
  isTextarea?: boolean;
  id?: string;
  errorMessage?: string;
  customClass?: string;
  rules?: ((value: string | number | null) => true | string)[];
  // 👇 Optional custom icons for password toggle
  showIcon?: any; // Vue component for "show password"
  hideIcon?: any; // Vue component for "hide password"
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: "",
  type: "text",
  rowCount: 3,
  isTextarea: false,
  id: `input-${Math.random().toString(36).substr(2, 9)}`,
  customClass: "",
  rules: () => [],
});

const emit = defineEmits(["update:modelValue", "error"]);

const innerValue = ref(props.modelValue);
const errorMessage = ref<string>("");

// ✅ Password toggle state
const showPassword = ref(false);

// Switch input type (normal vs password)
const inputType = computed(() => {
  if (props.type === "password") {
    return showPassword.value ? "text" : "password";
  }
  return props.type;
});

// Toggle show/hide password
const togglePassword = () => {
  showPassword.value = !showPassword.value;
};

// Sync external changes
watch(
  () => props.modelValue,
  (newVal) => {
    innerValue.value = newVal;
  },
  { immediate: true }
);

const handleInput = (event: Event) => {
  const target = event.target as HTMLInputElement | HTMLTextAreaElement;
  let value: string | number = target.value;

  if (props.type === "number" && value !== "") {
    const parsed = Number(value);
    value = isNaN(parsed) ? "" : parsed;
  }

  innerValue.value = value;
  emit("update:modelValue", value);
  validateInput();
};

const validateInput = () => {
  if (props.rules && props.rules.length > 0) {
    for (const rule of props.rules) {
      const result = rule(innerValue.value);
      if (result !== true) {
        errorMessage.value = result as string;
        emit("error", errorMessage.value);
        return false;
      }
    }
  }
  errorMessage.value = "";
  emit("error", "");
  return true;
};

const charCount = computed(() => {
  return innerValue.value ? innerValue.value.toString().length : 0;
});
</script>
