<template>
  <!-- Flyout panel that grows upward from the button -->
  <Transition name="scale-fade" appear>
    <div
      v-if="isOpen"
      class="absolute bottom-full bg-white dark:bg-gray-800 rounded-t-lg shadow-lg transform-origin"
      @keydown.esc="handleClose"
      @click.self="handleClose"
    >
      <!-- Header -->
      <div class="flex items-center justify-between p-4 border-b dark:border-gray-700">
        <h2 class="text-xl font-semibold text-gray-900 dark:text-gray-100">
          Activity & Questions
        </h2>
        <Button
          variant="ghost"
          size="sm"
          icon="close"
          iconPosition="center"
          @click="handleClose"
          title="Close"
        />
      </div>

      <!-- Content -->
      <div class="p-4 space-y-4">
        <!-- Comments Loading State -->
        <div v-if="loading" class="text-center py-8">
          <svg class="animate-spin h-6 w-6 text-blue-500 dark:text-blue-400" xmlns="http://www.w3.org/2000/svg"
            fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor"
              d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z">
            </path>
          </svg>
          <span class="ml-2 text-gray-500 dark:text-gray-400">Loading comments...</span>
        </div>

        <!-- Comments List -->
        <div v-else-if="!loading && threadedComments.length === 0" class="text-center py-8">
          <p class="text-gray-500 dark:text-gray-400">
            No activity yet. Be the first to add a comment or ask a question!
          </p>
        </div>

        <div v-else-if="!loading">
          <!-- Comment Items -->
          <div v-for="(comment, index) in threadedComments" :key="comment.id" class="space-y-2">
            <!-- Comment Header -->
            <div class="flex items-start space-x-3">
              <!-- Avatar -->
              <div class="flex-shrink-0">
                <div class="h-8 w-8 rounded-full bg-blue-100 text-blue-600 flex items-center justify-center text-sm font-medium">
                  {{ comment.employeeName.charAt(0) }}
                </div>
              </div>

              <!-- Comment Content -->
              <div class="flex-1 min-w-0">
                <div class="flex items-center justify-between mb-1">
                  <div>
                    <p class="font-medium text-gray-900 dark:text-gray-100">
                      {{ comment.employeeName }}
                      <span v-if="comment.isCurrentUser" class="ml-1 text-xs text-green-500 bg-green-100 px-1.5 py-0.5 rounded">You</span>
                    </p>
                    <p class="text-xs text-gray-500 dark:text-gray-400">
                      {{ new Date(comment.createdAt).toLocaleString() }}
                    </p>
                  </div>

                  <!-- Comment Actions -->
                  <div class="flex items-center space-x-2">
                    <template v-if="comment.canEdit">
                      <Button
                        variant="ghost"
                        size="xs"
                        icon="edit"
                        iconPosition="center"
                        @click="startEdit(comment.id)"
                        title="Edit"
                        :disabled="editingId !== null && editingId !== comment.id"
                      />
                    </template>

                    <template v-if="comment.canDelete">
                      <Button
                        variant="ghost"
                        size="xs"
                        icon="trash"
                        iconPosition="center"
                        @click="confirmDelete(comment.id)"
                        title="Delete"
                        :disabled="deletingId === comment.id"
                      />
                    </template>

                    <template v-if="comment.type === 'Question' && !comment.isResolved && (comment.isCurrentUserAnApprover || comment.isRequester)">
                      <Button
                        variant="ghost"
                        size="xs"
                        icon="check"
                        iconPosition="center"
                        :class="[{ 'text-green-600': !comment.isResolved, 'text-gray-400': comment.isResolved }]"
                        @click="resolveQuestion(comment.id)"
                        title="Mark as resolved"
                        :disabled="resolvingId === comment.id"
                      />
                    </template>
                  </div>
                </div>

                <p class="text-gray-800 dark:text-gray-200 whitespace-pre-line break-words">
                  {{ comment.message }}
                </p>

                <!-- Replies -->
                <div v-if="comment.replies.length > 0" class="mt-3 pl-4 border-l-2 border-gray-200 dark:border-gray-600">
                  <div v-for="(reply, replyIndex) in comment.replies" :key="reply.id" class="mt-2">
                    <div class="flex items-start space-x-2">
                      <div class="flex-shrink-0">
                        <div class="h-6 w-6 rounded-full bg-green-100 text-green-600 flex items-center justify-center text-xs font-medium">
                          {{ reply.employeeName.charAt(0) }}
                        </div>
                      </div>
                      <div class="flex-1 min-w-0">
                        <div class="flex items-center justify-between mb-1">
                          <div>
                            <p class="font-medium text-gray-900 dark:text-gray-100">
                              {{ reply.employeeName }}
                              <span v-if="reply.isCurrentUser" class="ml-1 text-xs text-green-500 bg-green-100 px-1.5 py-0.5 rounded">You</span>
                            </p>
                            <p class="text-xs text-gray-500 dark:text-gray-400">
                              {{ new Date(reply.createdAt).toLocaleString() }}
                            </p>
                          </div>

                          <div class="flex items-center space-x-1">
                            <template v-if="reply.canEdit">
                              <Button
                                variant="ghost"
                                size="xs"
                                icon="edit"
                                iconPosition="center"
                                @clock="startEdit(reply.id)"
                                title="Edit"
                                :disabled="editingId !== null && editingId !== reply.id"
                              />
                            </template>

                            <template v-if="reply.canDelete">
                              <Button
                                variant="ghost"
                                size="xs"
                                icon="trash"
                                iconPosition="center"
                                @click="confirmDelete(reply.id)"
                                title="Delete"
                                :disabled="deletingId === reply.id"
                              />
                            </template>
                          </div>
                        </div>

                        <p class="text-gray-800 dark:text-gray-200 text-sm whitespace-pre-line break-words">
                          {{ reply.message }}
                        </p>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <!-- Divider (except for last item) -->
            <div v-if="index < threadedComments.length - 1" class="border-t border-gray-200 dark:border-gray-600 my-4"></div>
          </div>
        </div>
      </div>

      <!-- Comment Input -->
      <div class="border-t border-gray-200 dark:border-gray-600 pt-4">
        <div class="flex items-center space-x-3 p-4">
          <!-- Input Type Selector -->
          <div class="flex items-center space-x-2 text-sm">
            <label class="cursor-select text-gray-600 dark:text-gray-300">
              <input
                type="radio"
                value="Comment"
                v-model="commentType"
                class="form-radio h-4 w-4 text-indigo-600"
              >
              Comment
            </label>
            <label class="cursor-select text-gray-600 dark:text-gray-300">
              <input
                type="radio"
                value="Question"
                v-model="commentType"
                class="form-radio h-4 w-4 text-indigo-600"
              >
              Question
            </label>
          </div>

          <!-- Text Input -->
          <div class="flex-1">
            <textarea
              v-model="commentMessage"
              placeholder="Write a comment or ask a question..."
              class="w-full p-2 border border-gray-300 rounded dark:bg-gray-700 dark:border-gray-600 focus:outline-none focus:ring-2 focus:ring-indigo-500 resize-none"
              :rows="commentMessage.length > 0 ? Math.min(Math.ceil(commentMessage.length / 50), 4) : 3"
              @input="autoResize"
            ></textarea>
          </div>

          <!-- Send Button -->
          <div class="flex-shrink-0">
            <Button
              variant="primary"
              size="sm"
              :loading="sending"
              icon="paper_plane"
              iconPosition="left"
              @click="sendComment"
              :disabled="!commentMessage.trim() || sending"
            >
              Send
            </Button>
          </div>
        </div>
      </div>
    </div>
  </Transition>
</template>

<script setup lang="ts">
import { ref, computed, watch, onBeforeUnmount } from 'vue'
import { useComments } from '~/composables/useComments'
import { useUserStore } from '~/stores/useUserStore'
import { showToast } from '~/stores/globalvar'
import Button from '@/components/Button.vue'

const props = defineProps({
  isOpen: {
    type: Boolean,
    default: false
  },
  transactionId: {
    type: Number,
    required: true
  }
})

const emit = defineEmits([
  'update:isOpen',
  'close'
])

// State
const commentMessage = ref('')
const commentType = ref<'Comment' | 'Question'>('Comment')
const loading = ref(false)
const sending = ref(false)
const editingId = ref<number | null>(null)
const deletingId = ref<number | null>(null)
const resolvingId = ref<number | null>(null)

// Composables
const { fetchComments, addComment, updateComment, deleteComment, resolveQuestion, threadedComments, getUnresolvedQuestionsCount } = useComments()
const userStore = useUserStore()

// Initialize
watch(
  () => props.transactionId,
  (newVal) => {
    if (newVal) {
      fetchComments(newVal)
    }
  },
  { immediate: true }
)

// Watch for open/close state
watch(
  () => props.isOpen,
  (newVal) => {
    if (!newVal) {
      // Reset form when closing
      commentMessage.value = ''
      commentType.value = 'Comment'
      editingId.value = null
      deletingId.value = null
      resolvingId.value = null
    }
  }
)

// Handle close
const handleClose = () => {
  emit('update:isOpen', false)
  emit('close')
}

// Start editing a comment
const startEdit = (commentId: number) => {
  editingId.value = commentId
  // Find the comment and set the message
  const comment = threadedComments.value.find(c => c.id === commentId) || 
                  threadedComments.value.flatMap(c => [c, ...c.replies]).find(r => r.id === commentId)
  if (comment) {
    commentMessage.value = comment.message
    // Focus the textarea after a short delay
    setTimeout(() => {
      const textarea = document.querySelector('textarea') as HTMLTextAreaElement
      textarea?.focus()
    }, 100)
  }
}

// Confirm delete
const confirmDelete = (commentId: number) => {
  if (window.confirm('Are you sure you want to delete this comment?')) {
    deleteComment(commentId)
  }
}

// Send comment
const sendComment = async () => {
  if (!commentMessage.value.trim() || sending.value) return

  sending.value = true
  try {
    const result = await addComment(
      props.transactionId,
      commentMessage.value,
      commentType.value
    )

    if (result) {
      // Reset form
      commentMessage.value = ''
      commentType.value = 'Comment'

      // Focus the textarea for quick follow-up
      setTimeout(() => {
        const textarea = document.querySelector('textarea') as HTMLTextAreaElement
        textarea?.focus()
      }, 100)
    }
  } catch (error: any) {
    showToast({
      title: 'Error',
      message: error?.message || 'Failed to send comment',
      type: 'error'
    })
  } finally {
    sending.value = false
  }
}

// Update comment (called from composable)
const handleUpdateComment = async (commentId: number, message: string) => {
  if (editingId.value !== commentId) return

  try {
    await updateComment(commentId, message)
    editingId.value = null
    commentMessage.value = ''
  } catch (error: any) {
    showToast({
      title: 'Error',
      message: error?.message || 'Failed to update comment',
      type: 'error'
    })
  }
}

// Delete comment (called from composable)
const handleDeleteComment = (commentId: number) => {
  if (deletingId.value !== commentId) return

  deletingId.value = null
}

// Auto-resize textarea
const autoResize = () => {
  const textarea = document.querySelector('textarea') as HTMLTextAreaElement
  if (textarea) {
    textarea.style.height = 'auto'
    textarea.style.height = (textarea.scrollHeight) + 'px'
  }
}

// Clean up
onBeforeUnmount(() => {
  // Remove any event listeners if needed
})
</script>

<style scoped>
.scale-fade-enter-active,
.scale-fade-leave-active {
  transition: transform 0.3s ease, opacity 0.3s ease;
}
.scale-fade-enter-from {
  transform: scaleY(0);
  opacity: 0;
}
.scale-fade-enter-to {
  transform: scaleY(1);
  opacity: 1;
}
.scale-fade-leave-from {
  transform: scaleY(1);
  opacity: 1;
}
.scale-fade-leave-to {
  transform: scaleY(0);
  opacity: 0;
}
.transform-origin {
  transform-origin: bottom;
}
</style>