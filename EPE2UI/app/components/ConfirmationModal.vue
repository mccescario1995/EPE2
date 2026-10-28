<template>
  <Teleport to="body">
    <transition name="fade">
      <div
        v-if="isOpen"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50"
        @keydown.esc="handleClose"
      >
        <div
          class="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md mx-4 relative animate-fade-in"
          role="dialog"
          aria-modal="true"
        >
          <!-- Icon and Title -->
          <div class="flex items-center gap-3 mb-4">
            <component :is="iconComponent" class="w-6 h-6" :class="iconColor" />
            <h2 class="text-xl font-semibold text-gray-800">
              <slot name="title">{{ title }}</slot>
            </h2>
          </div>

          <!-- Message -->
          <p class="text-gray-600 mb-6">{{ message }}</p>

          <!-- Buttons -->
          <div class="flex justify-end gap-2">
            <button
              @click="handleClose"
              class="px-4 py-2 text-sm font-medium text-gray-600 hover:text-gray-800 transition"
            >
              Cancel
            </button>
            <button
              @click="handleConfirm"
              class="px-4 py-2 transition"
              :class="buttonClasses"
            >
              Confirm
            </button>
          </div>

          <!-- Close (X) -->
          <button
            @click="handleClose"
            class="absolute top-3 right-3 text-gray-400 hover:text-gray-600 transition"
            aria-label="Close"
          >
            &times;
          </button>
        </div>
      </div>
    </transition>
  </Teleport>
</template>

<script setup lang="ts">
import {
  ExclamationTriangleIcon,
  CheckCircleIcon,
  TrashIcon,
  InformationCircleIcon,
  QuestionMarkCircleIcon,
} from "@heroicons/vue/24/outline";

const props = defineProps<{
  isOpen: boolean;
  message: string;
  type: "save" | "delete" | "info" | "question" | "warning";
  title: string;
}>();

const emit = defineEmits<{
  (e: "confirm"): void;
  (e: "close"): void;
}>();

const handleClose = () => emit("close");
const handleConfirm = () => {
  emit("confirm");
  emit("close");
};

// Dynamic icon and styling based on type
const iconComponent = computed(() => {
  switch (props.type) {
    case "save":
      return CheckCircleIcon;
    case "delete":
      return TrashIcon;
    case "info":
      return InformationCircleIcon;
    case "question":
      return QuestionMarkCircleIcon;
    case "warning":
      return ExclamationTriangleIcon;
    default:
      return InformationCircleIcon;
  }
});

const iconColor = computed(() => {
  switch (props.type) {
    case "save":
      return "text-blue-500";
    case "delete":
      return "text-red-500";
    case "info":
      return "text-cyan-500";
    case "question":
      return "text-yellow-500";
    case "warning":
      return "text-orange-500";
    default:
      return "text-gray-500";
  }
});

const buttonClasses = computed(() => {
  switch (props.type) {
    case "save":
      return "bg-blue-600 text-white text-sm font-medium rounded hover:bg-blue-700";
    case "delete":
      return "bg-red-600 text-white text-sm font-medium rounded hover:bg-red-700";
    case "info":
      return "bg-cyan-600 text-white text-sm font-medium rounded hover:bg-cyan-700";
    case "question":
      return "bg-yellow-500 text-white text-sm font-medium rounded hover:bg-yellow-600";
    case "warning":
      return "bg-orange-500 text-white text-sm font-medium rounded hover:bg-orange-600";
    default:
      return "bg-gray-500 text-white text-sm font-medium rounded hover:bg-gray-600";
  }
});
</script>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
@keyframes fade-in {
  from {
    transform: scale(0.95);
    opacity: 0;
  }
  to {
    transform: scale(1);
    opacity: 1;
  }
}
.animate-fade-in {
  animation: fade-in 0.2s ease-out;
}
</style>
