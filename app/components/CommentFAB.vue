<template>
  <!-- Container fixed to bottom-right of viewport -->
  <div class="fixed bottom-6 right-6 z-[9999]">
    <!-- Floating Action Button -->
    <Button
      variant="primary"
      size="5xl"
      icon="chat"
      iconPosition="center"
      class="bg-blue-500 w-15 h-15 mr-5 flex items-center justify-center rounded-full shadow-lg ring-2 ring-blue-600 hover:bg-blue-600"
      @click="toggleFlyout"
      :aria-label="`${unresolvedCount > 0 ? 'Unresolved questions: ' + unresolvedCount : 'Open activity feed'}`"
    >
      <template v-if="unresolvedCount > 0">
        <!-- Badge -->
        <div class="absolute -top-1 -right-1 flex h-5 w-5 items-center justify-center rounded-full bg-red-500 text-xs font-medium text-white ring-2 ring-white dark:ring-gray-800">
          {{ unresolvedCount }}
        </div>
      </template>
    </Button>

    <!-- Comment Flyout (appears above the button) -->
    <CommentFlyout
      v-model:isOpen="flyoutOpen"
      :transactionId="transactionId"
      @update:isOpen="flyoutOpen = $event"
      @close="flyoutOpen = false"
      class="absolute bottom-full right-0 w-100 mr-5 transform-origin"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useComments } from '~/composables/useComments'
import CommentFlyout from '@/components/CommentFlyout.vue'
import Button from '@/components/Button.vue'

const props = defineProps({
  transactionId: {
    type: Number,
    required: true
  }
})

const emit = defineEmits([
  'update:isOpen'
])

// State
const flyoutOpen = ref(false)
const unresolvedCount = ref(0)

// Composables
const { getUnresolvedQuestionsCount } = useComments()

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