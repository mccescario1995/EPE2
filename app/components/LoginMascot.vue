<template>
  <div
    class="relative bg-yellow-200 w-40 h-40 rounded-2xl flex items-center justify-center shadow-md transition-all duration-300"
    ref="mascot"
  >
    <!-- 👁️ Eyes -->
    <div
      class="absolute flex justify-between w-20"
      :class="{ 'opacity-0': isCoveringEyes }"
      style="top: 40%;"
    >
      <div class="eye w-8 h-8 bg-white rounded-full flex items-center justify-center overflow-hidden">
        <div class="pupil bg-black rounded-full w-3 h-3"></div>
      </div>
      <div class="eye w-8 h-8 bg-white rounded-full flex items-center justify-center overflow-hidden">
        <div class="pupil bg-black rounded-full w-3 h-3"></div>
      </div>
    </div>

    <!-- ✋ Hands -->
    <div
      v-if="isCoveringEyes"
      class="absolute flex justify-between w-32 top-1/3"
    >
      <div class="w-10 h-10 bg-yellow-300 rounded-full shadow-md -rotate-12"></div>
      <div class="w-10 h-10 bg-yellow-300 rounded-full shadow-md rotate-12"></div>
    </div>

    <!-- 😐 / 😍 / 😭 Mouth -->
    <div
      class="absolute w-8 h-2 bg-black rounded-full transition-all duration-300"
      :class="{
        'scale-y-150': expression === 'typing',
        'rounded-full': expression === 'neutral',
        'w-6 bg-red-500': expression === 'error',
        'w-10 h-4 bg-green-600 rounded-b-full': expression === 'happy',
      }"
      style="bottom: 25%;"
    ></div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted, watch } from 'vue'

// 🧠 Props
const props = defineProps({
  focusTarget: String,
  coverEyes: Boolean,
  expression: String,
})

// ✨ Refs and State
const mascot = ref(null)
const isCoveringEyes = ref(false)

watch(() => props.coverEyes, (val) => {
  isCoveringEyes.value = val
})

// 👀 Eye tracking logic
function handleMouseMove(e) {
  if (isCoveringEyes.value) return
  moveEyes(e.clientX, e.clientY)
}

// 🧠 Track focused input position
function moveEyes(x, y) {
  const eyes = mascot.value.querySelectorAll('.eye')
  eyes.forEach((eye) => {
    const pupil = eye.querySelector('.pupil')
    const rect = eye.getBoundingClientRect()
    const dx = x - (rect.left + rect.width / 2)
    const dy = y - (rect.top + rect.height / 2)
    const angle = Math.atan2(dy, dx)
    const moveX = Math.cos(angle) * 5
    const moveY = Math.sin(angle) * 5
    pupil.style.transform = `translate(${moveX}px, ${moveY}px)`
  })
}

// 🧭 Focus tracking (eyes look at input field instead of mouse)
let focusInterval
function trackInputFocus() {
  clearInterval(focusInterval)
  if (!props.focusTarget) return

  const input = document.querySelector(props.focusTarget)
  if (!input) return

  focusInterval = setInterval(() => {
    if (document.activeElement === input) {
      const rect = input.getBoundingClientRect()
      const caretX = rect.left + rect.width * 0.6
      const caretY = rect.top + rect.height / 2
      moveEyes(caretX, caretY)
    }
  }, 50)
}

onMounted(() => {
  window.addEventListener('mousemove', handleMouseMove)
  trackInputFocus()
})
onUnmounted(() => {
  window.removeEventListener('mousemove', handleMouseMove)
  clearInterval(focusInterval)
})
</script>

<style scoped>
.eye {
  transition: transform 0.2s ease;
}
.pupil {
  transition: transform 0.1s ease;
}
</style>
