<template>
  <!-- Comment Flyout (renders at fixed position via v-model) -->
  <CommentFlyout   v-model:isOpen="flyoutOpen" :transactionId="transactionId" :requesterId="requesterId" :approvers="approvers" :transactionStatus="transactionStatus" @close="flyoutOpen = false" />

  <!-- Floating Action Button -->
  <div v-if="commentsEnabled" class="fixed bottom-3 right-10 z-[9999] hidden sm:block">
    <Button variant="primary" size="2xl" icon="chat" iconPosition="center"
      class="bg-blue-500 w-14 h-14 flex items-center justify-center rounded-full shadow-lg ring-2 ring-blue-600 hover:bg-blue-600"
      @click="toggleFlyout"
      :aria-label="`${unresolvedCount > 0 ? 'Unresolved questions: ' + unresolvedCount : 'Open activity feed'}`">
      <!-- Badge -->
      <div v-if="unresolvedCount > 0"
        class="absolute -top-2 -right-2 flex h-5 w-5 items-center justify-center rounded-full bg-red-500 text-xs font-medium text-white ring-2 ring-white dark:ring-gray-800">
        {{ unresolvedCount }}
      </div>
    </Button>
  </div>

</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useComments } from '~/composables/useComments'
import CommentFlyout from '@/components/CommentFlyout.vue'
import Button from '@/components/Button.vue'

const props = defineProps({
  transactionId: { type: Number, required: true },
  requesterId: { type: [String, Number], default: null },
  approvers: { type: Array, default: () => [] }
})

const emit = defineEmits([
  'update:isOpen'
])

// State
const flyoutOpen = ref(false)
const unresolvedCount = ref(0)

// Feature flag check
const config = useRuntimeConfig()
const commentsEnabled = computed(() => config.public.commentsEnabled)

// Composables
const { getUnresolvedQuestionsCount, transactionStatus } = useComments()

// Watch for unresolved count changes
watch(
  () => getUnresolvedQuestionsCount.value,
  (newVal) => {
    unresolvedCount.value = newVal
  }
)

// Toggle flyout
const toggleFlyout = () => {
  flyoutOpen.value = !flyoutOpen.value
}

// Emit update event for v-model
watch(
  () => flyoutOpen.value,
  (newVal) => {
    emit('update:isOpen', newVal)
  }
)
</script>