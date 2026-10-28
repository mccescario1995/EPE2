<template>
  <!-- Position Containers -->
  <div v-for="pos in positions" :key="pos" :class="positionClasses(pos)">
    <TransitionGroup 
      name="toast" 
      tag="div"
      class="flex flex-col gap-3"
      :class="positionGroupClasses(pos)"
    >
      <div
        v-for="toast in filteredToasts(pos)"
        :key="toast.id"
        class="rounded-xl shadow-lg p-4 flex items-start gap-3 w-80 relative overflow-hidden transition-all duration-300"
        :class="[getToastClasses(toast.type), getAnimationClass(toast.animation)]"
      >
        <!-- Progress bar -->
        <div 
          v-if="toast.duration" 
          class="absolute bottom-0 left-0 h-1 bg-current opacity-20"
          :style="{ width: `${toast.progress}%` }"
        ></div>
        
        <!-- Icon -->
        <div :class="iconContainerClasses(toast.type)">
          <svg :viewBox="iconViewBox" :class="iconClasses">
            <path :fill="iconColor" :d="iconPath(toast.type)"></path>
          </svg>
        </div>
        
        <!-- Content -->
        <div class="flex-1 min-w-0">
          <p class="font-semibold truncate">{{ toast.title }}</p>
          <p class="text-sm mt-1 line-clamp-3">{{ toast.message }}</p>
        </div>
        
        <!-- Close button -->
        <button
          @click="removeToast(toast.id)"
          class="text-current opacity-50 hover:opacity-100 transition-opacity text-sm hover:cursor-pointer shrink-0"
          aria-label="Close toast"
        >
          ✖
        </button>
      </div>
    </TransitionGroup>
  </div>
</template>

<script setup lang="ts">
import { useToastStore } from "~/composables/useToast";
import { computed, onMounted, onUnmounted } from 'vue';

const { toasts, removeToast } = useToastStore();

// Animation types
type AnimationType = 
  | 'shake' 
  | 'bounce' 
  | 'flip' 
  | 'zoom' 
  | 'swing' 
  | 'tada' 
  | 'wobble' 
  | 'pulse'
  | 'none';

// All supported positions
const positions = [
  "top-right",
  "top-left",
  "bottom-right",
  "bottom-left",
  "top-center",
  "bottom-center",
];

// Add progress tracking to toasts
const enhancedToasts = computed(() => {
  return toasts.map(toast => {
    return {
      ...toast,
      progress: toast.duration ? 100 : 0
    }
  });
});

// Handle auto-close progress
let interval: number;
onMounted(() => {
  interval = window.setInterval(() => {
    enhancedToasts.value.forEach(toast => {
      if (toast.duration && toast.progress > 0) {
        const decrement = 100 / ((toast.duration * 1000) / 50);
        toast.progress = Math.max(0, toast.progress - decrement);
        
        if (toast.progress <= 0) {
          removeToast(toast.id);
        }
      }
    });
  }, 50);
});

onUnmounted(() => {
  clearInterval(interval);
});

const filteredToasts = (position: string) =>
  enhancedToasts.value.filter((t) => t.position === position);

// Animation classes
const getAnimationClass = (animation: AnimationType = 'none') => {
  switch (animation) {
    case 'shake': return 'animate-shake';
    case 'bounce': return 'animate-bounce';
    case 'flip': return 'animate-flip';
    case 'zoom': return 'animate-zoom';
    case 'swing': return 'animate-swing';
    case 'tada': return 'animate-tada';
    case 'wobble': return 'animate-wobble';
    case 'pulse': return 'animate-pulse';
    default: return '';
  }
};

/**
 * Returns Tailwind classes based on toast position
 */
const positionClasses = (pos: string) => {
  const base = "fixed z-[9999] pointer-events-none";
  switch (pos) {
    case "top-right": return `${base} top-4 right-4`;
    case "top-left": return `${base} top-4 left-4`;
    case "bottom-right": return `${base} bottom-4 right-4`;
    case "bottom-left": return `${base} bottom-4 left-4`;
    case "top-center": return `${base} top-4 left-1/2 -translate-x-1/2`;
    case "bottom-center": return `${base} bottom-4 left-1/2 -translate-x-1/2`;
    default: return `${base} top-4 right-4`;
  }
};

// Additional classes for the toast group container
const positionGroupClasses = (pos: string) => {
  switch (pos) {
    case "top-right":
    case "top-center":
    case "top-left":
      return "items-end";
    case "bottom-right":
    case "bottom-center":
    case "bottom-left":
      return "items-start";
    default:
      return "";
  }
};

const getToastClasses = (type: string) => {
  const base = "pointer-events-auto border";
  switch (type) {
    case "success": return `${base} bg-green-50 text-green-800 border-green-100`;
    case "error": return `${base} bg-red-50 text-red-800 border-red-100`;
    case "warning": return `${base} bg-amber-50 text-amber-800 border-amber-100`;
    case "info": return `${base} bg-blue-50 text-blue-800 border-blue-100`;
    default: return `${base} bg-gray-50 text-gray-800 border-gray-100`;
  }
};

const iconContainerClasses = (type: string) => {
  const base = "flex-shrink-0 p-2 rounded-lg";
  switch (type) {
    case "success": return `${base} bg-green-100 text-green-600`;
    case "error": return `${base} bg-red-100 text-red-600`;
    case "warning": return `${base} bg-amber-100 text-amber-600`;
    case "info": return `${base} bg-blue-100 text-blue-600`;
    default: return `${base} bg-gray-100 text-gray-600`;
  }
};

const iconViewBox = "0 0 24 24";
const iconClasses = "w-5 h-5";
const iconColor = "currentColor";

const iconPath = (type: string) => {
  switch (type) {
    case "success": return "M12,0A12,12,0,1,0,24,12,12.014,12.014,0,0,0,12,0Zm6.927,8.2-6.845,9.289a1.011,1.011,0,0,1-1.43.188L5.764,13.769a1,1,0,1,1,1.25-1.562l4.076,3.261,6.227-8.451A1,1,0,1,1,18.927,8.2Z";
    case "error": return "M11.983,0a12.206,12.206,0,0,0-8.51,3.653A11.8,11.8,0,0,0,0,12.207,11.779,11.779,0,0,0,11.8,24h.214A12.111,12.111,0,0,0,24,11.791h0A11.766,11.766,0,0,0,11.983,0ZM10.5,16.542a1.476,1.476,0,0,1,1.449-1.53h.027a1.527,1.527,0,0,1,1.523,1.47,1.475,1.475,0,0,1-1.449,1.53h-.027A1.529,1.529,0,0,1,10.5,16.542ZM11,12.5v-6a1,1,0,0,1,2,0v6a1,1,0,1,1-2,0Z";
    case "warning": return "M23.119,20,13.772,2.15h0a2,2,0,0,0-3.543,0L.881,20a2,2,0,0,0,1.772,2.928H21.347A2,2,0,0,0,23.119,20ZM11,8.423a1,1,0,0,1,2,0v6a1,1,0,1,1-2,0Zm1.05,11.51h-.028a1.528,1.528,0,0,1-1.522-1.47,1.476,1.476,0,0,1,1.448-1.53h.028A1.527,1.527,0,0,1,13.5,18.4,1.475,1.475,0,0,1,12.05,19.933Z";
    case "info": return "M12,0A12,12,0,1,0,24,12,12.013,12.013,0,0,0,12,0Zm.25,5a1.5,1.5,0,1,1-1.5,1.5A1.5,1.5,0,0,1,12.25,5ZM14.5,18.5h-4a1,1,0,0,1,0-2h.75a.25.25,0,0,0,.25-.25v-4.5a.25.25,0,0,0-.25-.25H10.5a1,1,0,0,1,0-2h1a2,2,0,0,1,2,2v4.75a.25.25,0,0,0,.25.25h.75a1,1,0,1,1,0,2Z";
    default: return "M12,2A10,10,0,1,0,22,12,10,10,0,0,0,12,2Zm0,18a8,8,0,1,1,8-8A8,8,0,0,1,12,20Z";
  }
};
</script>

<style scoped>
/* Base transition */
.toast-move,
.toast-enter-active,
.toast-leave-active {
  transition: all 0.3s cubic-bezier(0.22, 1, 0.36, 1);
}

.toast-enter-from,
.toast-leave-to {
  opacity: 0;
  transform: translateY(-20px) scale(0.95);
}

.toast-leave-active {
  position: absolute;
}

.toast-enter-active {
  transition-delay: 0.1s;
}

/* Animation effects */
.animate-shake {
  animation: shake 0.5s cubic-bezier(0.36, 0.07, 0.19, 0.97) both;
}

.animate-bounce {
  animation: bounce 0.5s ease both;
}

.animate-flip {
  animation: flip 0.6s ease both;
  backface-visibility: visible;
}

.animate-zoom {
  animation: zoom 0.3s ease both;
}

.animate-swing {
  transform-origin: top center;
  animation: swing 0.6s ease both;
}

.animate-tada {
  animation: tada 0.6s ease both;
}

.animate-wobble {
  animation: wobble 0.6s ease both;
}

.animate-pulse {
  animation: pulse 1s ease infinite;
}

/* Keyframes */
@keyframes shake {
  0%, 100% { transform: translateX(0); }
  10%, 30%, 50%, 70%, 90% { transform: translateX(-5px); }
  20%, 40%, 60%, 80% { transform: translateX(5px); }
}

@keyframes bounce {
  0%, 20%, 50%, 80%, 100% { transform: translateY(0); }
  40% { transform: translateY(-20px); }
  60% { transform: translateY(-10px); }
}

@keyframes flip {
  0% { transform: perspective(400px) rotateY(90deg); opacity: 0; }
  40% { transform: perspective(400px) rotateY(-10deg); }
  70% { transform: perspective(400px) rotateY(10deg); }
  100% { transform: perspective(400px) rotateY(0deg); opacity: 1; }
}

@keyframes zoom {
  0% { transform: scale(0.5); opacity: 0; }
  100% { transform: scale(1); opacity: 1; }
}

@keyframes swing {
  20% { transform: rotate(15deg); }
  40% { transform: rotate(-10deg); }
  60% { transform: rotate(5deg); }
  80% { transform: rotate(-5deg); }
  100% { transform: rotate(0deg); }
}

@keyframes tada {
  0% { transform: scale(1); }
  10%, 20% { transform: scale(0.9) rotate(-3deg); }
  30%, 50%, 70%, 90% { transform: scale(1.1) rotate(3deg); }
  40%, 60%, 80% { transform: scale(1.1) rotate(-3deg); }
  100% { transform: scale(1) rotate(0); }
}

@keyframes wobble {
  0% { transform: translateX(0%); }
  15% { transform: translateX(-5%) rotate(-5deg); }
  30% { transform: translateX(4%) rotate(3deg); }
  45% { transform: translateX(-3%) rotate(-3deg); }
  60% { transform: translateX(2%) rotate(2deg); }
  75% { transform: translateX(-1%) rotate(-1deg); }
  100% { transform: translateX(0%); }
}

@keyframes pulse {
  0%, 100% { transform: scale(1); }
  50% { transform: scale(1.05); }
}
</style>