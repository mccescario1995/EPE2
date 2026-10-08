<template>
  <Transition name="slide-up" appear>
     <div v-if="isOpen" :style="{ bottom: '80px', right: '35px' }" :class="{ 'resize-active': isResizingVertically }" class="fixed z-[9999]" @keydown.esc="handleClose">
       <div :style="{ height: height }"
         class="bg-white dark:bg-gray-800 rounded-2xl shadow-2xl border border-gray-200 dark:border-gray-700 overflow-hidden flex flex-col max-w-md"
         ref="flyoutPanel">
        
         <!-- Top Resize Handle -->
         <div class="absolute top-0 left-0 right-0 h-2 bg-gray-200 dark:bg-gray-700/50 cursor-ns-resize w-10 item-center mx-auto rounded-b-lg z-10"
              @mousedown="startVerticalResize"
         ></div>

        <!-- Header -->
        <div
          class="flex items-center justify-between px-4 py-3 border-b border-gray-100 dark:border-gray-700 bg-gray-50 dark:bg-gray-900/50">
          <div class="flex items-center gap-2">
            <div class="w-8 h-8 rounded-full bg-blue-100 dark:bg-blue-900/30 flex items-center justify-center">
              <svg class="w-5 h-5 text-blue-600 dark:text-blue-400" fill="none" stroke="currentColor"
                viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z">
                </path>
              </svg>
            </div>
            <div>
              <h2 class="text-lg font-semibold text-gray-900 dark:text-gray-100">Activity</h2>
              <p class="text-xs text-gray-500 dark:text-gray-400">{{ unresolvedCount }} unresolved</p>
            </div>
          </div>
          <Button variant="ghost" size="sm" icon="close" iconPosition="center" @click="handleClose"
            class="text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200" />
        </div>

        <!-- Messages Area -->
        <div class="flex-1 overflow-y-auto p-4 space-y-4" ref="commentsContainer">

          <!-- Closed Transaction Banner -->
          <div v-if="transactionStatus === 'C'"
            class="mb-4 p-3 bg-red-50 dark:bg-red-900/50 border sticky top-0 border-red-200 dark:border-red-800 rounded-lg">
            <div class="flex items-center gap-2 text-sm">
              <svg class="w-5 h-5 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
              </svg>
              <span>This request is closed. No further comments/questions allowed.</span>
            </div>
          </div>

          <!-- Loading -->
          <div v-if="loading" class="flex items-center justify-center h-full">
            <div class="flex items-center gap-2 text-gray-500 dark:text-gray-400">
              <svg class="animate-spin h-5 w-5 text-blue-500" xmlns="http://www.w3.org/2000/svg" fill="none"
                viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor"
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z">
                </path>
              </svg>
              <span>Loading...</span>
            </div>
          </div>

          <!-- Empty State -->
          <div v-else-if="!loading && threadedComments.length === 0"
            class="flex flex-col items-center justify-center h-full text-center px-6">
            <div class="w-16 h-16 rounded-full bg-gray-100 dark:bg-gray-700 flex items-center justify-center mb-4">
              <svg class="w-8 h-8 text-gray-400 dark:text-gray-500" fill="none" stroke="currentColor"
                viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
                  d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z">
                </path>
              </svg>
            </div>
            <p class="text-gray-500 dark:text-gray-400 mb-1">No activity yet</p>
            <p class="text-xs text-gray-400 dark:text-gray-500">Start the conversation by adding a comment or question
            </p>
          </div>

          <!-- Comments List -->
          <div v-else-if="!loading" class="space-y-4">
            <div v-for="(comment, index) in threadedComments" :key="comment.id" class="space-y-2">

              <!-- Main Comment/Question -->
              <CommentBubble :comment="comment" :is-current-user="comment.isCurrentUser"
                :show-reply-btn="comment.type === 'Question'" :show-resolve-btn="comment.type === 'Question'"
                :is-resolved="comment.isResolved" :replying-to-id="replyingToId"
                :inline-reply-message="inlineReplyMessage" :is-requester="isRequester(comment)"
                :is-approver="isApprover(comment)" @update:inline-reply-message="inlineReplyMessage = $event"
                @reply="startReply" @resolve="resolveQuestion" @edit="startEdit" @send-inline-reply="sendInlineReply"
                @cancel-inline-reply="cancelInlineReply" />

              <!-- Replies to this comment -->
              <div v-if="comment.replies.length > 0" class=" space-y-2  border-gray-100 dark:border-gray-700 pl-2"
                :class="isCurrentUser ? 'ml-10 border-l-3' : 'mr-14 pr-2 border-r-3'">
                <div v-for="(reply, replyIndex) in comment.replies" :key="reply.id">
                  <CommentBubble :comment="reply" :is-current-user="reply.isCurrentUser" :is-reply="true"
                    :show-reply-btn="false" :is-requester="isRequester(reply)" :is-approver="isApprover(reply)"
                    @edit="startEdit" />
                </div>
              </div>

              <!-- Resolved banner for questions -->
              <div v-if="comment.type === 'Question' && comment.isResolved"
                class="mt-1 px-2 py-0.5 text-xs text-center py-3 border-b-2 font-bold bg-green-50 text-green-600 dark:text-green-50 dark:bg-green-900/20 ">
                This question has been resolved.
              </div>
            </div>
          </div>
        </div>

        <!-- Composer -->
        <div class="border-t border-gray-100 dark:border-gray-700 bg-gray-50 dark:bg-gray-900/50 p-3">
          <!-- Type Selector -->
          <div class="flex items-center gap-2 mb-2">
            <span class="text-xs font-medium text-gray-500 dark:text-gray-400">Post as</span>
            <div class="flex gap-1 bg-gray-100 dark:bg-gray-700 rounded-lg p-1">
              <label v-for="type in ['Comment', 'Question']" :key="type"
                class="flex cursor-pointer items-center gap-1.5 rounded px-2.5 py-1 text-sm transition-colors"
                :class="commentType === type ? 'bg-white text-blue-600 shadow-sm dark:bg-gray-600 dark:text-blue-400' : 'text-gray-600 hover:text-gray-900 dark:text-gray-300 dark:hover:text-white'">
                <input type="radio" :value="type" v-model="commentType" class="sr-only" :disabled="commentsDisabled">
                <span>{{ type }}</span>
              </label>
            </div>
          </div>

          <!-- Textarea -->
          <div class="relative">
            <textarea v-model="commentMessage"
              class="w-full resize-none rounded-xl border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800 px-4 py-3 text-sm text-gray-900 dark:text-gray-100 placeholder-gray-400 outline-none transition focus:border-blue-500 focus:ring-2 focus:ring-blue-500/20"
              :rows="commentMessage.length > 0 ? Math.min(Math.ceil(commentMessage.length / 50), 4) : 2"
              @input="autoResize" @keydown.enter.exact.prevent="!commentsDisabled && sendComment"
              @keydown.enter.shift="autoResize" :disabled="commentsDisabled"
              :placeholder="commentsDisabled ? (unresolvedCount.value > 0 ? 'Cannot comment while there are unresolved questions' : 'This request is closed') : (commentType === 'Question' ? 'Ask a question...' : 'Write a comment...')"></textarea>
          </div>

          <!-- Send Button -->
          <div class="flex justify-end mt-2">
            <Button variant="primary" size="sm" :loading="sending" icon="right_arrow" iconPosition="right"
              @click="!commentsDisabled && sendComment"
              :disabled="commentsDisabled || !commentMessage.trim() || sending">
              Send
            </Button>
          </div>
        </div>

      </div>
    </div>
  </Transition>
</template>

<script setup lang="ts">
import { ref, computed, watch, onBeforeUnmount, nextTick } from 'vue'
import { useComments } from '~/composables/useComments'
import { useUserStore } from '~/stores/useUserStore'
import { showToast } from '~/stores/globalvar'
import Button from '@/components/Button.vue'
import CommentBubble from '@/components/CommentBubble.vue'

const props = defineProps({
  isOpen: { type: Boolean, default: false },
  transactionId: { type: Number, required: true },
  requesterId: { type: [String, Number], default: null },
  approvers: { type: Array, default: () => [] }
})

const emit = defineEmits(['update:isOpen', 'close'])

// State
const commentMessage = ref('')
const commentType = ref<'Comment' | 'Question'>('Comment')
const loading = ref(false)
const sending = ref(false)
const editingId = ref<number | null>(null)
const resolvingId = ref<number | null>(null)
const replyingToId = ref<number | null>(null)
const inlineReplyMessage = ref<string>('')
const commentsContainer = ref<HTMLElement | null>(null)

// Resize functionality
const height = ref<string>('50vh')
const isResizingVertically = ref(false)
let resizeStartY = 0
let resizeStartHeight = 0

// Composables
const { fetchComments, addComment, updateComment, deleteComment, resolveQuestion, threadedComments, getUnresolvedQuestionsCount, transactionStatus } = useComments()
const userStore = useUserStore()

const unresolvedCount = computed(() => getUnresolvedQuestionsCount.value)

// Check if comments should be disabled (unresolved questions exist or transaction is closed)
const commentsDisabled = computed(() => {
  return unresolvedCount.value > 0 || transactionStatus.value === 'C'
})

// Computed helpers for resolve button
const isRequester = (comment: any) => props.requesterId != null && String(comment.employeeId) === String(props.requesterId)
const isApprover = (comment: any) => props.approvers.length > 0 && props.approvers.some((a: any) => String(a) === String(comment.employeeId))

// Scroll to bottom utility
const scrollToBottom = async () => {
  await nextTick()
  if (commentsContainer.value) {
    commentsContainer.value.scrollTop = commentsContainer.value.scrollHeight
  }
}

// Initialize
watch(() => props.transactionId, (newVal) => {
  if (newVal) fetchComments(newVal)
}, { immediate: true })

// Auto-scroll on new comments
watch(() => threadedComments.value, () => scrollToBottom(), { deep: true })

// Scroll to bottom when opening
watch(() => props.isOpen, (newVal) => {
  if (!newVal) {
    commentMessage.value = ''
    commentType.value = 'Comment'
    editingId.value = null
    resolvingId.value = null
    replyingToId.value = null
    inlineReplyMessage.value = ''
  } else {
    scrollToBottom()
  }
})

const handleClose = () => {
  emit('update:isOpen', false)
  emit('close')
}

const startEdit = (commentId: number) => {
  editingId.value = commentId
  const comment = threadedComments.value.find(c => c.id === commentId) ||
    threadedComments.value.flatMap(c => [c, ...c.replies]).find(r => r.id === commentId)
  if (comment) {
    commentMessage.value = comment.message
    setTimeout(() => {
      const textarea = document.querySelector('textarea') as HTMLTextAreaElement
      textarea?.focus()
    }, 100)
  }
}

const startReply = (commentId: number) => {
  if (replyingToId.value === commentId) {
    cancelInlineReply()
    return
  }
  replyingToId.value = commentId
  inlineReplyMessage.value = ''
  setTimeout(() => {
    const textarea = document.querySelector('textarea[rows="2"]') as HTMLTextAreaElement
    textarea?.focus()
  }, 100)
}

const sendComment = async () => {
  if (!commentMessage.value.trim() || sending.value) return
  sending.value = true
  try {
    const result = await addComment(props.transactionId, commentMessage.value, commentType.value, null)
    if (result) {
      commentMessage.value = ''
      commentType.value = 'Comment'
      scrollToBottom()
    }
  } catch (error: any) {
    showToast({ title: 'Error', message: error?.message || 'Failed to send', type: 'error' })
  } finally { sending.value = false }
}

const sendInlineReply = async (parentCommentId: number) => {
  if (!inlineReplyMessage.value.trim() || sending.value) return
  sending.value = true
  try {
    const result = await addComment(props.transactionId, inlineReplyMessage.value, 'Reply', parentCommentId)
    if (result) {
      inlineReplyMessage.value = ''
      replyingToId.value = null
      scrollToBottom()
    }
  } catch (error: any) {
    showToast({ title: 'Error', message: error?.message || 'Failed to send reply', type: 'error' })
  } finally { sending.value = false }
}

const cancelInlineReply = () => {
  inlineReplyMessage.value = ''
  replyingToId.value = null
}

const handleUpdateComment = async (commentId: number, message: string) => {
  if (editingId.value !== commentId) return
  try {
    await updateComment(commentId, message)
    editingId.value = null
    commentMessage.value = ''
  } catch (error: any) {
    showToast({ title: 'Error', message: error?.message || 'Failed to update', type: 'error' })
  }
}

const handleDeleteComment = (commentId: number) => {
  if (deletingId.value !== commentId) return
  deletingId.value = null
}

const autoResize = () => {
  const textarea = document.querySelector('textarea') as HTMLTextAreaElement
  if (textarea) {
    textarea.style.height = 'auto'
    textarea.style.height = `${textarea.scrollHeight}px`
  }
}

const startVerticalResize = (e: MouseEvent) => {
  e.preventDefault()
  isResizingVertically.value = true
  resizeStartY = e.clientY
  resizeStartHeight = parseFloat(height.value)
  
  // Add window listeners
  window.addEventListener('mousemove', onVerticalMouseMove)
  window.addEventListener('mouseup', onVerticalMouseUp)
}

const onVerticalMouseMove = (e: MouseEvent) => {
  if (!isResizingVertically.value) return
  
  // Calculate deltaY (change in Y position)
  const dy = e.clientY - resizeStartY
  
  // Calculate new height (moving top down increases height, moving top up decreases height)
  // let newHeight = resizeStartHeight + dy
  let newHeight = resizeStartHeight - dy
  
  // Apply constraints (min/max in pixels)
  const minHeight = 400
  const maxHeight = window.innerHeight * 0.8 // 80vh
  
  newHeight = Math.max(minHeight, Math.min(newHeight, maxHeight))
  
  // Update refs (will trigger reactive update)
  height.value = `${newHeight}px`
}

const onVerticalMouseUp = () => {
  if (!isResizingVertically.value) return
  isResizingVertically.value = false
  window.removeEventListener('mousemove', onVerticalMouseMove)
  window.removeEventListener('mouseup', onVerticalMouseUp)
  
  // Optional: Save to localStorage
  try {
    localStorage.setItem(
      `commentFlyoutSize_${props.transactionId}`, 
      JSON.stringify({ height: height.value })
    )
  } catch (e) {
    console.warn('Failed to save flyout height:', e)
  }
}

onBeforeUnmount(() => { })

// Load saved size on mount
onMounted(() => {
  try {
    const saved = localStorage.getItem(`commentFlyoutSize_${props.transactionId}`)
    if (saved) {
      const { height: savedHeight } = JSON.parse(saved)
      // Basic validation - check if it's a valid CSS value
      if (savedHeight && 
          (savedHeight.endsWith('px') || savedHeight.endsWith('vh') || savedHeight.endsWith('%'))) {
        height.value = savedHeight
      }
    }
  } catch (e) {
    console.warn('Failed to load flyout height:', e)
  }
})
</script>

<style scoped>
.slide-up-enter-active,
.slide-up-leave-active {
  transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
}

.slide-up-enter-from {
  transform: translateY(20px) scale(0.95);
  opacity: 0;
}

.slide-up-enter-to {
  transform: translateY(0) scale(1);
  opacity: 1;
}

.slide-up-leave-from {
  transform: translateY(0) scale(1);
  opacity: 1;
}

.slide-up-leave-to {
  transform: translateY(20px) scale(0.95);
  opacity: 0;
}

/* Scrollbar styling */
.flex-1::-webkit-scrollbar {
  width: 6px;
}

.flex-1::-webkit-scrollbar-track {
  background: transparent;
}

.flex-1::-webkit-scrollbar-thumb {
  background: #cbd5e1;
  border-radius: 3px;
}

.flex-1::-webkit-scrollbar-thumb:hover {
  background: #94a3b8;
}

.dark .flex-1::-webkit-scrollbar-thumb {
  background: #475569;
}

.dark .flex-1::-webkit-scrollbar-thumb:hover {
  background: #64748b;
}

.resize-active {
  transition: none !important;
}

.resize-active {
  transition: none !important;
}
</style>