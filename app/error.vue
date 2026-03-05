<template>
  <div class="relative flex h-screen flex-col items-center justify-center overflow-hidden bg-linear-to-b from-[#f8fbfd] to-[#e6f3f8] dark:from-gray-900 dark:to-gray-800 text-gray-700 dark:text-gray-200">

    <!-- Floating gradient blobs -->
    <div class="absolute inset-0 overflow-hidden">
      <div
        v-for="(blob, i) in blobs"
        :key="i"
        class="absolute rounded-full blur-3xl opacity-30 animate-float"
        :class="blob.color"
        :style="blob.style"
      />
    </div>

    <!-- Main content -->
    <div class="relative z-10 text-center animate-fade-in">
      <h1
        class="text-[7rem] font-extrabold text-transparent bg-clip-text bg-linear-to-r from-sky-400 via-blue-400 to-teal-400 select-none animate-bounce-slow"
      >
        404
      </h1>
      <p class="mt-4 text-xl font-light text-gray-500">
        Oops! The page you’re looking for doesn’t exist.
      </p>
      <p class="text-gray-400 mt-1">
        It might have been moved or deleted.
      </p>

      <div class="mt-8">
        <NuxtLink
          to="/"
          class="inline-flex items-center gap-2 rounded-full bg-linear-to-r from-sky-400 to-blue-500 px-6 py-3 text-white font-medium shadow-lg shadow-blue-200 hover:scale-105 transition-all duration-300"
        >
          <Icon name="heroicons:arrow-left" class="w-5 h-5" />
          Go back home
        </NuxtLink>
      </div>
    </div>
  </div>
</template>

<script setup>
defineProps({
  error: Object,
})

// Animated background blobs
const blobs = [
  {
    color: 'bg-gradient-to-r from-sky-300 to-blue-200',
    style: 'width: 300px; height: 300px; top: 10%; left: 15%; animation-delay: 0s;',
  },
  {
    color: 'bg-gradient-to-r from-teal-200 to-sky-300',
    style: 'width: 250px; height: 250px; bottom: 15%; right: 10%; animation-delay: 1s;',
  },
  {
    color: 'bg-gradient-to-r from-blue-100 to-teal-100',
    style: 'width: 200px; height: 200px; top: 50%; right: 30%; animation-delay: 2s;',
  },
]
</script>

<style scoped>
@keyframes float {
  0%, 100% {
    transform: translateY(0px) scale(1);
  }
  50% {
    transform: translateY(-20px) scale(1.05);
  }
}

@keyframes fade-in {
  from {
    opacity: 0;
    transform: translateY(15px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes bounce-slow {
  0%, 100% {
    transform: translateY(0);
  }
  50% {
    transform: translateY(-10px);
  }
}

.animate-float {
  animation: float 8s ease-in-out infinite;
}

.animate-fade-in {
  animation: fade-in 1.5s ease-out forwards;
}

.animate-bounce-slow {
  animation: bounce-slow 3s ease-in-out infinite;
}
</style>
