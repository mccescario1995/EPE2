<script setup lang="ts">
import lottie from 'lottie-web'

const props = defineProps<{
  src: string
  loop?: boolean
  autoplay?: boolean
  size?: Number
}>()

const container = ref<HTMLDivElement | null>(null)

// Tell Vite all possible animation files ahead of time
const animations = import.meta.glob(
  '../../assets/animations/*.json'
)

onMounted(async () => {
  await nextTick()

  const path = `../../assets/animations/${props.src}`
  const loader = animations[path]

  if (!loader) {
    console.error(`Lottie animation not found: ${props.src}`)
    return
  }

  const animationData = (await loader() as { default: unknown }).default

  lottie.loadAnimation({
    container: container.value!,
    renderer: 'svg',
    loop: props.loop ?? true,
    autoplay: props.autoplay ?? true,
    animationData,
  })
})
</script>


<template>
  <div ref="container" class="w-64 h-64 mx-auto"></div>
</template>
