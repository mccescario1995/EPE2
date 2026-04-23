<script setup lang="ts">
const route = useRoute();
const router = useRouter();

const status = computed(() => route.query.status as string);
const action = computed(() => route.query.action as string);
const transactionId = computed(() => route.query.transactionId as string);

const config = {
  invalid: {
    icon: "⚠️",
    color: "text-red-600",
    bg: "bg-red-50",
    title: "Invalid Link",
    message: "This approval link is invalid or does not exist.",
  },
  expired: {
    icon: "⏰",
    color: "text-orange-600",
    bg: "bg-orange-50",
    title: "Link Expired",
    message:
      "This approval link has expired (72-hour limit). Please visit the app to respond.",
  },
  already_used: {
    icon: "✅",
    color: "text-blue-600",
    bg: "bg-blue-50",
    title: "Already Responded",
    message: "This link has already been used. Your response was recorded.",
  },
  already_closed: {
    icon: "🔒",
    color: "text-gray-600",
    bg: "bg-gray-50",
    title: "Request Closed",
    message: "This request has already been closed.",
  },
  already_responded: {
    icon: "✅",
    color: "text-blue-600",
    bg: "bg-blue-50",
    title: "Already Responded",
    message: "You have already responded to this request.",
  },
  success: {
    icon: action.value === "approved" ? "✅" : "❌",
    color: action.value === "approved" ? "text-green-600" : "text-red-600",
    bg: action.value === "approved" ? "bg-green-50" : "bg-red-50",
    title:
      action.value === "approved" ? "Request Approved!" : "Request Disapproved",
    message: `You have successfully ${action.value} this exemption request.`,
  },
  error: {
    icon: "❌",
    color: "text-red-600",
    bg: "bg-red-50",
    title: "Something Went Wrong",
    message:
      "An error occurred while processing your response. Please try again via the app.",
  },
};

const current = computed(
  () => config[status.value as keyof typeof config] ?? config.error,
);
</script>

<template>
  <div class="min-h-3/4 bg-gray-50 flex items-center justify-center">
    <div class="bg-white rounded-2xl shadow-lg max-w-md w-full p-8 text-center">
      <div class="text-6xl mb-4">{{ current.icon }}</div>
      <h1 class="text-2xl font-bold mb-2" :class="current.color">
        {{ current.title }}
      </h1>
      <p class="text-gray-600 mb-6">{{ current.message }}</p>

      <div class="flex flex-col gap-3">
        <button
          v-if="transactionId"
          @click="router.push(`/exemptions/view?id=${transactionId}`)"
          class="w-full py-3 px-4 bg-blue-600 text-white rounded-lg font-medium hover:bg-blue-700"
        >
          View Request
        </button>
        <button
          @click="router.push('/exemptions')"
          class="w-full py-3 px-4 bg-gray-100 text-gray-700 rounded-lg font-medium hover:bg-gray-200"
        >
          Back to Dashboard
        </button>
      </div>
    </div>
  </div>
</template>
