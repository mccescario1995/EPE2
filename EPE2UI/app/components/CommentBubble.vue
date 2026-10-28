<template>
  <div class="flex items-start gap-2" :class="isCurrentUser ? 'flex-row-reverse' : ''">
    <!-- Avatar -->
    <div class="flex-shrink-0 mt-0.5 mx-1">
      <div :class="[
        'rounded-full flex items-center justify-center font-medium ',
        isCurrentUser ? 'w-8 h-8 bg-blue-100 text-blue-600 text-sm' : 'w-8 h-8 bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-300 text-sm',
        isReply && !isCurrentUser ? 'w-7 h-7 text-xs' : ''
      ]">
        {{ comment.employeeName.charAt(0) }}
      </div>
    </div>

    <!-- Message Content -->
    <div class="flex-1 min-w-0 max-w-[75%]">

      <!-- Header with name, badge, time -->
      <div class="flex items-center gap-1.5 mb-1" :class="isCurrentUser ? 'flex-row-reverse' : ''">
        <div class="flex items-center gap-1.5" :class="isCurrentUser ? 'flex-row-reverse' : ''">
          <span class="font-medium text-sm whitespace-nowrap" :class="comment.type === 'Question' || comment.type === 'Reply' ? 'dark:text-gray-950' : 'dark:text-light-100' ">
            {{ comment.employeeName }}
            <span v-if="isCurrentUser && !isReply"
              class="ml-1.5 text-[10px] font-medium text-green-600 bg-green-50 px-1.5 py-0.5 rounded-full" :class="comment.type === 'Question' ? 'border dark:border-green-600 dark:text-green-600' : 'dark:bg-green-900/30 dark:text-green-300' ">You</span>
          </span>

          <!-- Type Badge -->
          <span v-if="comment.type === 'Question'"
            class="inline-flex items-center px-1.5 py-0.5 rounded-full text-[10px] font-medium bg-amber-50 text-amber-700 border border-amber-600  dark:text-amber-800">
            Question
          </span>
          <span v-else-if="comment.type === 'Comment'"
            class="inline-flex items-center px-1.5 py-0.5 rounded-full text-[10px] font-medium bg-blue-50 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300">
            Comment
          </span>
          <span v-else-if="comment.type === 'Reply'"
            class="inline-flex items-center px-1.5 py-0.5 rounded-full text-[10px] font-medium bg-purple-50 text-purple-700 dark:bg-purple-200 dark:text-purple-600">
            Reply
          </span>
        </div>


      </div>

      <!-- Message Bubble -->
      <div :class="[
        'relative px-4 py-2 rounded-2xl max-w-full break-words whitespace-pre-line text-sm leading-relaxed',
        isCurrentUser 
          ? 'bg-blue-600 text-white rounded-tr-sm' 
          : 'bg-gray-100 dark:bg-gray-700 text-gray-900 dark:text-gray-50 rounded-tl-sm',
        isReply && !isCurrentUser ? 'bg-gray-100/80 dark:bg-gray-700/80' : ''
      ]">
        {{ comment.message }}
        
        
        
        <!-- Edit/Delete actions on hover for own messages -->
        <div v-if="isCurrentUser && comment.canEdit && !editing"
          class="absolute top-1 right-1 opacity-0 group-hover:opacity-100 transition-opacity flex items-center gap-1">
          <Button variant="ghost" size="xs" icon="edit" iconPosition="center" @click="$emit('edit', comment.id)"
            class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300" title="Edit" />
        </div>
      </div>


      <!-- Timestamp -->
      <time class="text-[9px] text-gray-400 dark:text-gray-500 whitespace-nowrap flex items-center"
        :class="isCurrentUser ? 'flex-row-reverse mr-2' : 'ml-2'" :title="formatFullDate(comment.createdAt)">
        {{ formatTime(comment.createdAt) }}
      </time>

      <!-- Inline Reply Input -->
      <div v-if="!isReply && replyingToId === comment.id" class="mt-2 flex items-start gap-2"
        :class="isCurrentUser ? 'flex-row-reverse' : ''">
        <div class="flex-shrink-0">
          <div
            class="w-7 h-7 rounded-full bg-blue-100 dark:bg-blue-900/30 flex items-center justify-center text-xs font-medium text-blue-600 dark:text-blue-400">
            {{ userStore.profile?.employeename?.charAt(0) || 'U' }}
          </div>
        </div>
        <div class="flex-1 min-w-0">
          <textarea :value="inlineReplyMessage"
            @input="$emit('update:inlineReplyMessage', ($event.target as HTMLTextAreaElement).value)"
            placeholder="Write a reply..."
            class="w-full resize-none rounded-xl border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-2 text-sm text-gray-900 dark:text-gray-100 placeholder-gray-400 outline-none transition focus:border-blue-500 focus:ring-2 focus:ring-blue-500/20"
            rows="2" @keydown.enter.exact.prevent="$emit('send-inline-reply', comment.id)"
            @keydown.enter.shift="autoResizeInline"></textarea>
          <div class="flex justify-end gap-1.5 mt-1.5">
            <Button variant="ghost" size="xs" @click="$emit('cancel-inline-reply')"
              class="text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200">
              Cancel
            </Button>
            <Button variant="primary" size="xs" :loading="sending" @click="$emit('send-inline-reply', comment.id)"
              :disabled="!inlineReplyMessage.trim() || sending">
              Reply
            </Button>
          </div>
        </div>
        
      </div>
      
      <!-- Actions row (Reply, Resolve) -->
      <div v-if="(showReplyBtn || showResolveBtn) && !isResolved" class="flex items-center gap-1 mt-1.5"
        :class="isCurrentUser ? 'flex-row-reverse' : ''">
        <Button v-if="showReplyBtn" variant="ghost" size="xs" icon="reply" iconPosition="center"
          @click="$emit('reply', comment.id)" :class="[
            replyingToId === comment.id ? 'text-blue-600 bg-blue-50 dark:bg-blue-900/20' : 'text-gray-400 hover:text-blue-600'
          ]" :title="replyingToId === comment.id ? 'Cancel reply' : 'Reply'" />

        <Button v-if="showResolveBtn && !isResolved && (isApprover || isRequester)" variant="ghost" size="xs"
          icon="check" iconPosition="center" @click="$emit('resolve', comment.id)" :disabled="resolving === comment.id  || isResolved"
          class="text-green-600 hover:bg-green-50 dark:hover:bg-green-900/20" title="Mark resolved" />
        <Button v-if="showResolveBtn && isResolved" variant="ghost" size="xs" icon="check" iconPosition="center"
          class="text-gray-400 cursor-not-allowed" disabled title="Resolved" />
      </div>
<!-- Resolved banner for questions -->
        <!-- <div v-if="!isReply && comment.type === 'Question' && isResolved" class="mt-1 px-2 py-0.5 text-xs font-medium bg-green-50 text-green-600 dark:text-green-50 dark:bg-green-900/20 rounded-lg">
          This question has been resolved
        </div> -->
    </div>
    
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useUserStore } from '~/stores/useUserStore'
import Button from '@/components/Button.vue'

const props = defineProps({
  comment: { type: Object, required: true },
  isCurrentUser: { type: Boolean, default: false },
  isReply: { type: Boolean, default: false },
  showReplyBtn: { type: Boolean, default: false },
  showResolveBtn: { type: Boolean, default: false },
  isResolved: { type: Boolean, default: false },
  replyingToId: { type: Number, default: null },
  inlineReplyMessage: { type: String, default: '' },
  sending: { type: Boolean, default: false },
  isRequester: { type: Boolean, default: false },
  isApprover: { type: Boolean, default: false }
})

const emit = defineEmits(['reply', 'resolve', 'edit', 'send-inline-reply', 'cancel-inline-reply'])

const userStore = useUserStore()
const editing = ref(false)

const formatTime = (dateStr: string) => {
  const date = new Date(dateStr)
  return date.toLocaleTimeString('en-PH', { hour: '2-digit', minute: '2-digit', hour12: true })
}

const formatFullDate = (dateStr: string) => {
  const date = new Date(dateStr)
  return date.toLocaleDateString('en-PH', { year: 'numeric', month: 'long', day: 'numeric' })
}

const autoResizeInline = () => {
  const textarea = document.querySelector('textarea[rows="2"]') as HTMLTextAreaElement
  if (textarea) {
    textarea.style.height = 'auto'
    textarea.style.height = `${textarea.scrollHeight}px`
  }
}
</script>

<style scoped>
/* Group hover for edit button */
.group:hover .opacity-0 {
  opacity: 1;
}
</style>