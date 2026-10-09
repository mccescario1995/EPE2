import { ref, computed } from 'vue'
import { apiFetch } from './useApiFetch'
import { showToast } from '~/stores/globalvar'
import { useUserStore } from '~/stores/useUserStore'

interface Comment {
  id: number
  transactionId: number
  userId: number
  employeeId: string
  employeeName: string
  employeeEmail: string
  message: string
  type: string
  parentCommentId: number | null
  isResolved: boolean
  createdAt: string
  updatedAt: string | null
  metadata: string | null
  isCurrentUser: boolean
  canEdit: boolean
  canDelete: boolean
  replies: Comment[]
}

interface CommentListResponse {
  success: boolean
  items: Comment[]
  totalCount: number
  page: number
  pageSize: number
  unresolvedCount: number
  transactionStatus: string
}

export const useComments = () => {
  const comments = ref<Comment[]>([])
  const loading = ref<boolean>(false)
  const unresolvedCount = ref<number>(0)
  const transactionStatus = ref<string>('O')
  const transactionId = ref<number | null>(null)
  const userStore = useUserStore()
  const userId = userStore.profile

  const fetchComments = async (txId: number, params?: {
    type?: string
    unresolvedOnly?: boolean
    page?: number
    pageSize?: number
  }) => {
    if (!txId) return

    transactionId.value = txId
    loading.value = true

    try {
      const queryParams = new URLSearchParams()
      if (params?.type) queryParams.append('type', params.type)
      if (params?.unresolvedOnly !== undefined) queryParams.append('unresolvedOnly', String(params.unresolvedOnly))
      if (params?.page) queryParams.append('page', String(params.page))
      if (params?.pageSize) queryParams.append('pageSize', String(params.pageSize))
      // Pass current user ID so backend can compute IsCurrentUser
      if (userId?.userId) queryParams.append('userId', String(userId.userId))

      const response = await apiFetch<CommentListResponse>(
        `/api/transactioncomment/transaction/${txId}?${queryParams.toString()}`,
        {
          method: "GET",
          parseJson: true
        }
      )

       if (response) {
         comments.value = response.items
         unresolvedCount.value = response.unresolvedCount
         transactionStatus.value = response.transactionStatus
       } else {
        showToast({
          title: 'Error',
          message: 'Failed to load comments',
          type: 'error'
        })
      }
    } catch (error: any) {
      showToast({
        title: 'Error',
        message: error?.message || 'Failed to load comments',
        type: 'error'
      })
    } finally {
      loading.value = false
    }
  }

  const addComment = async (
    txId: number,
    message: string,
    type: string = 'Comment',
    parentCommentId: number | null = null
  ): Promise<Comment | null> => {
    if (!txId || !message.trim()) return null

    try {
      const response = await apiFetch<any>(
        `/api/transactioncomment/transaction/${txId}`,
        {
          method: 'POST',
          body: JSON.stringify({ message, type, parentCommentId, UserId: userId?.userId }),
          headers: { 'Content-Type': 'application/json' },
          credentials: 'include',
          parseJson: true
        }
      )

       if (response.success) {
         // Refresh comments from server to ensure we have the latest state
         await fetchComments(txId)
         showToast({
           title: 'Success',
           message: 'Comment added successfully',
           type: 'success'
         })
         return response.comment
       } else {
         showToast({
           title: 'Error',
           message: 'Failed to add comment',
           type: 'error'
         })
         return null
       }
    } catch (error: any) {
      showToast({
        title: 'Error',
        message: error?.message || 'Failed to add comment',
        type: 'error'
      })
      return null
    }
  }

  const updateComment = async (
    commentId: number,
    message: string
  ): Promise<boolean> => {
    if (!commentId || !message.trim()) return false

    try {
      const response = await apiFetch<any>(
        `/api/transactioncomment/${commentId}/update`,
        {
          method: 'POST',
          body: JSON.stringify({ message, UserId: userId?.userId }),
          headers: { 'Content-Type': 'application/json' },
          credentials: 'include',
          parseJson: true
        }
      )

      if (response.success) {
        // Find and update the comment in our list
        const index = comments.value.findIndex(c => c.id === commentId)
        if (index !== -1) {
          comments.value[index] = {
            ...comments.value[index],
            message: response.comment.message,
            updatedAt: response.comment.updatedAt
          }
        }

        showToast({
          title: 'Success',
          message: 'Comment updated successfully',
          type: 'success'
        })

        return true
      } else {
        showToast({
          title: 'Error',
          message: 'Failed to update comment',
          type: 'error'
        })
        return false
      }
    } catch (error: any) {
      showToast({
        title: 'Error',
        message: error?.message || 'Failed to update comment',
        type: 'error'
      })
      return false
    }
  }

  const deleteComment = async (commentId: number): Promise<boolean> => {
    if (!commentId) return false

    try {
      const response = await apiFetch<any>(
        `/api/transactioncomment/${commentId}/delete`,
        { method: 'POST', credentials: 'include', parseJson: true }
      )

      if (response.success) {
        // Remove the comment from our list
        comments.value = comments.value.filter(c => c.id !== commentId)

        showToast({
          title: 'Success',
          message: 'Comment deleted successfully',
          type: 'success'
        })

        return true
      } else {
        showToast({
          title: 'Error',
          message: 'Failed to delete comment',
          type: 'error'
        })
        return false
      }
    } catch (error: any) {
      showToast({
        title: 'Error',
        message: error?.message || 'Failed to delete comment',
        type: 'error'
      })
      return false
    }
  }

  const resolveQuestion = async (commentId: number): Promise<boolean> => {
    if (!commentId) return false

    try {
      const response = await apiFetch<any>(
        `/api/transactioncomment/${commentId}/resolve`,
        {
          method: 'POST',
          body: JSON.stringify({ UserId: userId?.userId }),
          headers: { 'Content-Type': 'application/json' },
          credentials: 'include',
          parseJson: true
        }
      )

      if (response.success) {
        // Find and update the comment in our list
        const index = comments.value.findIndex(c => c.id === commentId)
        if (index !== -1 && comments.value[index].type === 'Question') {
          comments.value[index] = {
            ...comments.value[index],
            isResolved: true,
            updatedAt: response.comment.updatedAt
          }

          // Decrease unresolved count
          unresolvedCount.value = Math.max(0, unresolvedCount.value - 1)
        }

        showToast({
          title: 'Success',
          message: 'Question marked as resolved',
          type: 'success'
        })

        return true
      } else {
        showToast({
          title: 'Error',
          message: 'Failed to resolve question',
          type: 'error'
        })
        return false
      }
    } catch (error: any) {
      showToast({
        title: 'Error',
        message: error?.message || 'Failed to resolve question',
        type: 'error'
      })
      return false
    }
  }

  // Get unresolved questions count for the current transaction
  const getUnresolvedQuestionsCount = computed(() => unresolvedCount.value)

  // Get comments grouped by top-level comments (with replies nested)
  // API already returns nested replies, so just filter top-level and sort
  const threadedComments = computed(() => {
    return comments.value
      .filter(comment => comment.parentCommentId === null)
      .sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime())
  })

   return {
     comments,
     loading,
     unresolvedCount,
     transactionStatus,
     transactionId,
     fetchComments,
     addComment,
     updateComment,
     deleteComment,
     resolveQuestion,
     getUnresolvedQuestionsCount,
     threadedComments
   }
}