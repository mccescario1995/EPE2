<template>
  <div v-if="isLoading" class="absolute inset-0 z-50">
    <!-- Backdrop with blur effect - now relative to parent -->
    <div class="absolute inset-0 bg-black/30 backdrop-blur-sm"></div>

    <!-- Loading card - centered within the slot area -->
    <div class="absolute inset-0 flex items-center justify-center">
      <div
        class="relative bg-white/90 backdrop-blur-md rounded-2xl shadow-2xl border border-white/20 p-8 mx-4 max-w-sm w-full"
      >
        <!-- Subtle gradient background -->
        <div
          class="absolute inset-0 bg-linear-to-br from-blue-50/50 to-purple-50/50 rounded-2xl"
        ></div>

        <!-- Content -->
        <div class="relative flex flex-col items-center space-y-6">
          <!-- <client-only>
            <AnimationLoading
              src="bluetruck.json"
              :loop="true"
              :autoplay="true"
            />
          </client-only> -->
          
          <!-- Modern spinner -->
          <div class="relative">
            <!-- Outer ring -->
            <div class="w-16 h-16 rounded-full border-4 border-gray-200"></div>

            <!-- Animated ring with dynamic color -->
            <div
              class="absolute inset-0 w-16 h-16 rounded-full border-4 border-transparent animate-spin"
              :class="spinnerClasses"
            ></div>

            <!-- Inner pulsing dot -->
            <div class="absolute inset-0 flex items-center justify-center">
              <div
                class="w-2 h-2 rounded-full animate-pulse"
                :class="dotClasses"
              ></div>
            </div>
          </div>

          <!-- Text content -->
          <div class="text-center space-y-2">
            <h3 class="text-lg font-semibold text-gray-800 tracking-wide">
              {{ loadingConfig.loadingText }}
            </h3>
            <p
              v-if="loadingConfig.subText"
              class="text-sm text-gray-600 font-medium"
            >
              {{ loadingConfig.subText }}
            </p>
          </div>

          <!-- Animated progress dots (ellipsis) -->
          <div v-if="loadingConfig.showEllipsis" class="flex space-x-1">
            <div
              v-for="i in 3"
              :key="i"
              class="w-2 h-2 rounded-full animate-pulse"
              :class="ellipsisClasses"
              :style="{
                animationDelay: `${(i - 1) * 0.15}s`,
                animationDuration: '1s',
              }"
            ></div>
          </div>
        </div>

        <!-- Subtle shimmer effect -->
        <div
          class="absolute inset-0 -top-px rounded-2xl bg-linear-to-r from-transparent via-white/10 to-transparent opacity-20 animate-pulse"
        ></div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useLoading } from "~/composables/useLoading";
import { computed, ref, onMounted } from "vue";
import lottie from "lottie-web";

const container = ref<HTMLDivElement | null>(null);
// Use the composable
const { isLoading, loadingConfig } = useLoading();

// Computed classes for dynamic spinner colors
const spinnerClasses = computed(() => {
  const color = loadingConfig.value.spinnerColor;
  switch (color) {
    case "blue":
      return "border-t-blue-500 border-r-blue-600";
    case "purple":
      return "border-t-purple-500 border-r-purple-600";
    case "green":
      return "border-t-green-500 border-r-green-600";
    case "red":
      return "border-t-red-500 border-r-red-600";
    case "orange":
      return "border-t-orange-500 border-r-orange-600";
    case "pink":
      return "border-t-pink-500 border-r-pink-600";
    case "indigo":
      return "border-t-indigo-500 border-r-indigo-600";
    case "teal":
      return "border-t-teal-500 border-r-teal-600";
    case "cyan":
      return "border-t-cyan-500 border-r-cyan-600";
    case "gray":
      return "border-t-gray-500 border-r-gray-600";
    case "black":
      return "border-t-black border-r-black";
    default:
      return "border-t-blue-500 border-r-purple-500";
  }
});

// Computed classes for pulsing dot
const dotClasses = computed(() => {
  const color = loadingConfig.value.spinnerColor;
  switch (color) {
    case "blue":
      return "bg-gradient-to-r from-blue-500 to-blue-600";
    case "purple":
      return "bg-gradient-to-r from-purple-500 to-purple-600";
    case "green":
      return "bg-gradient-to-r from-green-500 to-green-600";
    case "red":
      return "bg-gradient-to-r from-red-500 to-red-600";
    case "orange":
      return "bg-gradient-to-r from-orange-500 to-orange-600";
    case "pink":
      return "bg-gradient-to-r from-pink-500 to-pink-600";
    case "indigo":
      return "bg-gradient-to-r from-indigo-500 to-indigo-600";
    case "teal":
      return "bg-gradient-to-r from-teal-500 to-teal-600";
    case "cyan":
      return "bg-gradient-to-r from-cyan-500 to-cyan-600";
    case "gray":
      return "bg-gradient-to-r from-gray-500 to-gray-600";
    case "black":
      return "bg-gradient-to-r from-gray-500 to-gray-600";
    default:
      return "bg-gradient-to-r from-blue-500 to-purple-500";
  }
});

// Computed classes for ellipsis dots
const ellipsisClasses = computed(() => {
  const color = loadingConfig.value.spinnerColor;
  switch (color) {
    case "blue":
      return "bg-gradient-to-r from-blue-400 to-blue-500";
    case "purple":
      return "bg-gradient-to-r from-purple-400 to-purple-500";
    case "green":
      return "bg-gradient-to-r from-green-400 to-green-500";
    case "red":
      return "bg-gradient-to-r from-red-400 to-red-500";
    case "orange":
      return "bg-gradient-to-r from-orange-400 to-orange-500";
    case "pink":
      return "bg-gradient-to-r from-pink-400 to-pink-500";
    case "indigo":
      return "bg-gradient-to-r from-indigo-400 to-indigo-500";
    case "teal":
      return "bg-gradient-to-r from-teal-400 to-teal-500";
    case "cyan":
      return "bg-gradient-to-r from-cyan-400 to-cyan-500";
    case "gray":
      return "bg-gradient-to-r from-gray-400 to-gray-500";
    default:
      return "bg-gradient-to-r from-blue-400 to-purple-400";
  }
});

onMounted(async () => {
  const animationData = (await import("@/assets/animations/bluetruck.json"))
    .default;
  lottie.loadAnimation({
    container: container.value!,
    renderer: "svg",
    loop: true,
    autoplay: true,
    animationData,
  });
});
</script>

<style scoped>
/* Additional custom animations if needed */
@keyframes shimmer {
  0% {
    transform: translateX(-100%);
  }
  100% {
    transform: translateX(100%);
  }
}

.shimmer {
  animation: shimmer 2s infinite;
}
</style>
