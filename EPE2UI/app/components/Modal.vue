<!-- components/Modal.vue -->
<template>
  <Transition name="modal" appear>
    <!-- Updated to fixed to ensure it covers the entire viewport, especially for nested modals , removed the zindex from the container and moved it to the overlay and modal container for better stacking control -->
    <div
      v-if="isOpen"
      class="fixed inset-0 overflow-y-auto" 
      :class="`z-${props.zIndex}`"
      @keydown.esc="handleEsc"
      @click.self="handleOutsideClick"
    >
      <!-- Overlay -->
      <div
        class="fixed inset-0 bg-black dark:bg-0 opacity-50 transition-opacity"
        @click.self="handleOutsideClick"
      ></div>

      <!-- Modal container with positioning classes -->
      <div class="flex min-h-screen p-4 text-center" :class="positionClasses">
        <!-- Modal content -->
        <div
          class="relative inline-block transform overflow-hidden rounded-lg bg-white dark:bg-gray-800 text-left align-bottom shadow-xl transition-all sm:my-8 sm:w-full sm:align-middle"
          :class="sizeClasses"
          role="dialog"
          aria-modal="true"
          aria-labelledby="modal-title"
          @click.stop
        >
          <!-- Close button -->
          <button
            v-if="showCloseButton"
            @click="close()"
            class="absolute right-2 top-2 rounded-md p-1 text-gray-400 hover:text-gray-500 focus:outline-none"
            aria-label="Close"
          >
            <svg
              class="h-6 w-6"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M6 18L18 6M6 6l12 12"
              />
            </svg>
          </button>

          <!-- Header slot -->
          <div
            v-if="$slots.header"
            class="border-b border-gray-200 dark:border-gray-500 px-4 py-4 sm:px-6"
          >
            <slot name="header"></slot>
          </div>

          <!-- Default slot (main content) -->
          <div class="px-4 py-5 sm:p-6">
            <slot></slot>
          </div>

          <!-- Footer slot -->
          <div
            v-if="$slots.footer"
            class="border-t border-gray-200 dark:border-gray-500 bg-gray-50 dark:bg-gray-800 px-4 py-4 sm:px-6 sm:flex sm:flex-row-reverse"
          >
            <slot name="footer"></slot>
          </div>
        </div>
      </div>
    </div>
  </Transition>
</template>

<script setup lang="ts">
import { computed, onMounted, onBeforeUnmount } from "vue";

// added for nested modals to prevent body scroll when multiple modals are open
declare global {
  interface Window {
    modalCount: number;
  }
}

const props = defineProps({
  isOpen: {
    type: Boolean,
    required: true,
  },
  size: {
    type: String,
    default: "md",
    validator: (value: string) =>
      ["sm", "md", "lg", "xl", "2xl", "3xl", "4xl", "5xl", "6xl", "7xl", "full"].includes(value),
  },
  position: {
    type: String,
    default: "center",
    validator: (value: string) =>
      ["top", "center", "bottom", "top-center", "bottom-center"].includes(
        value
      ),
  },
  closeOnOverlayClick: {
    type: Boolean,
    default: true,
  },
  closeOnEsc: {
    type: Boolean,
    default: true,
  },
  showCloseButton: {
    type: Boolean,
    default: true,
  },

  // Added - zIndex prop to allow custom stacking of modals, especially for nested modals
  zIndex: {
    type: Number,
    default: 50,
  },
});

const emit = defineEmits(["close", "update:isOpen"]);

// Position classes
const positionClasses = computed(() => {
  const classes: Record<
    "top" | "center" | "bottom" | "top-center" | "bottom-center",
    string
  > = {
    top: "items-start pt-20",
    center: "items-center justify-center",
    bottom: "items-end pb-20",
    "top-center": "items-start justify-center pt-20",
    "bottom-center": "items-end justify-center pb-20",
  };
  return classes[props.position as keyof typeof classes] || classes.center;
});

// Size classes
const sizeClasses = computed(() => {
  return {
    sm: "sm:max-w-sm",
    md: "sm:max-w-md",
    lg: "sm:max-w-lg",
    xl: "sm:max-w-xl",
    "2xl": "sm:max-w-2xl",
    "3xl": "sm:max-w-3xl",
    "4xl": "sm:max-w-4xl",
    "5xl": "sm:max-w-5xl",
    "6xl": "sm:max-w-6xl",
    "7xl": "sm:max-w-7xl",
    full: "sm:max-w-full",
  }[props.size];
});

// Close modal
const close = () => {
  emit("update:isOpen", false);
  emit("close");
};

// Handle ESC key press
const handleEsc = (e: KeyboardEvent) => {
  if (props.closeOnEsc && e.key === "Escape") {
    close();
  }
};

// Handle outside click
const handleOutsideClick = () => {
  if (props.closeOnOverlayClick) {
    close();
  }
};

// Added - Prevent body scroll when modal is open, especially for nested modals
// Global modal counter for body overflow
if (typeof window !== 'undefined' && !window.modalCount) {
  window.modalCount = 0;
}

// Add/remove event listeners when modal opens/closes
onMounted(() => {
  if (props.isOpen) {
    document.addEventListener("keydown", handleEsc);
    // Increment modal count and disable body scroll if this is the first modal - important for nested modals
    window.modalCount++;
    if (window.modalCount === 1) {
      document.body.style.overflow = "hidden";
    }
  }
});

onBeforeUnmount(() => {
  document.removeEventListener("keydown", handleEsc);
  // Decrement modal count and re-enable body scroll if this was the last modal - important for nested modals
  window.modalCount--;
  if (window.modalCount === 0) {
    document.body.style.overflow = "";
  }
});

watch(
  () => props.isOpen,
  (newVal) => {
    if (newVal) {
      document.addEventListener("keydown", handleEsc);
      //Updated for nested modals: Increment modal count and disable body scroll if this is the first modal
      window.modalCount++;
      if (window.modalCount === 1) {
        document.body.style.overflow = "hidden";
      }
    } else {
      document.removeEventListener("keydown", handleEsc);
      //Updated for nested modals: Decrement modal count and re-enable body scroll if this was the last modal
      window.modalCount--;
      if (window.modalCount === 0) {
        document.body.style.overflow = "";
      }
    }
  }
);
</script>

<style scoped>
.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.3s ease;
}

.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}

.modal-enter-active .modal-container,
.modal-leave-active .modal-container {
  transition: all 0.3s ease;
}

.modal-enter-from .modal-container,
.modal-leave-to .modal-container {
  opacity: 0;
  transform: translateY(20px);
}

/* Prevent body scroll when modal is open */
body.modal-open {
  overflow: hidden;
}
</style>
