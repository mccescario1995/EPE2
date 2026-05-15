<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
const route = useRoute();
const router = useRouter();

const rawToken = computed(() => {
  const t = route.query.token;
  if (Array.isArray(t)) return t[0] ?? '';
  return (t ?? '') as string;
});

const localStatus = ref('');
const localAction = ref('');
const transactionId = ref('');
const token = ref(rawToken.value || '');
const showConfirm = ref(true);
const processing = ref(false);
const errorMessage = ref('');
const transaction = ref<any>(null);

const config: Record<string, any> = {
  invalid: {
    icon: '⚠️',
    color: 'text-red-600',
    bg: 'bg-red-50',
    title: 'Invalid Link',
    message: 'This approval link is invalid or does not exist.',
  },
  expired: {
    icon: '⏰',
    color: 'text-orange-600',
    bg: 'bg-orange-50',
    title: 'Link Expired',
    message: 'This approval link has expired (72-hour limit). Please visit the app to respond.',
  },
  already_used: {
    icon: '✅',
    color: 'text-blue-600',
    bg: 'bg-blue-50',
    title: 'Already Responded',
    message: 'This link has already been used. Your response was recorded.',
  },
  already_closed: {
    icon: '🔒',
    color: 'text-gray-600',
    bg: 'bg-gray-50',
    title: 'Request Closed',
    message: 'This request has already been closed.',
  },
  already_responded: {
    icon: '✅',
    color: 'text-blue-600',
    bg: 'bg-blue-50',
    title: 'Already Responded',
    message: 'You have already responded to this request.',
  },
  error: {
    icon: '❌',
    color: 'text-red-600',
    bg: 'bg-red-50',
    title: 'Something Went Wrong',
    message: 'An error occurred while processing your response. Please try again via the app.',
  },
};

const current = computed(() => {
  if (localStatus.value === 'success') {
    const act = localAction.value === 'approved' ? 'approved' : 'disapproved';
    return {
      icon: act === 'approved' ? '✅' : '❌',
      color: act === 'approved' ? 'text-green-600' : 'text-red-600',
      bg: act === 'approved' ? 'bg-green-50' : 'bg-red-50',
      title: act === 'approved' ? 'Request Approved!' : 'Request Disapproved',
      message: `You have successfully ${act} this exemption request.`,
    };
  }
  return config[localStatus.value] ?? config.error;
});

async function loadPreview() {
  if (!token.value) {
    localStatus.value = 'invalid';
    showConfirm.value = false;
    return;
  }

  try {
    processing.value = true;
    errorMessage.value = '';
    const data = await $fetch(`/api/emailapproval/preview?token=${encodeURIComponent(token.value)}`, {
      headers: { Accept: 'application/json' },
    });

    if (!(data as any).ok) {
      const err = (data as any).error ?? 'invalid';
      localStatus.value = err;
      showConfirm.value = false;
      transaction.value = null;
      transactionId.value = ((data as any).transactionId ?? '') + '';
      return;
    }

    transaction.value = (data as any).transaction;
    transactionId.value = transaction.value?.id ?? '';
    showConfirm.value = true;
  } catch (err: any) {
    // network or server error
    errorMessage.value = err?.data?.error ?? err?.message ?? 'Network error';
    localStatus.value = 'error';
    showConfirm.value = false;
  } finally {
    processing.value = false;
  }
}

async function actToken() {
  if (processing.value || !token.value) return;
  processing.value = true;
  errorMessage.value = '';
  try {
    const data = await $fetch(`/api/emailapproval/act?token=${encodeURIComponent(token.value)}`, {
      headers: { Accept: 'application/json', 'X-Requested-With': 'XMLHttpRequest' },
    });

    if ((data as any).ok) {
      localStatus.value = 'success';
      localAction.value = (data as any).result ?? 'approved';
      transactionId.value = (data as any).transactionId ?? transactionId.value;
      showConfirm.value = false;
    } else {
      const err = (data as any).error ?? 'error';
      localStatus.value = err;
      showConfirm.value = false;
    }
  } catch (err: any) {
    errorMessage.value = err?.data?.error ?? err?.message ?? 'Network error';
    localStatus.value = 'error';
    showConfirm.value = false;
  } finally {
    processing.value = false;
  }
}

function viewRequest() {
  if (!transactionId.value) return;
  router.push({ path: '/exemptions/view', query: { id: transactionId.value } });
}

function goDashboard() {
  router.push('/exemptions/dashboard');
}

function onConfirmApprove() {
  // act endpoint determines action from token; call act
  actToken();
}
function onConfirmDisapprove() {
  actToken();
}

onMounted(() => {
  token.value = rawToken.value || '';
  loadPreview();
});
</script>

<template>
  <div class="min-h-3/4 bg-gray-50 flex items-center justify-center">
    <div class="bg-white rounded-2xl shadow-lg max-w-md w-full p-8 text-center">

      <div v-if="showConfirm" class="space-y-6">
        <div class="text-2xl font-bold">Confirm Response</div>
        <p class="text-gray-600">
          You are about to respond to this exemption request. Please confirm your action.
        </p>

        <div v-if="transaction" class="text-left p-4 bg-gray-50 rounded">
          <div class="font-semibold">Transaction #:</div>
          <div class="text-sm text-gray-700 mb-2">{{ transaction.number }}</div>
          <div class="font-semibold">Summary:</div>
          <div class="text-sm text-gray-700">{{ transaction.summary }}</div>
        </div>

        <div class="flex gap-3">
          <button
            @click="onConfirmApprove"
            :disabled="processing"
            class="flex-1 py-3 px-4 bg-green-600 text-white rounded-lg font-medium hover:bg-green-700 disabled:opacity-50"
          >
            Confirm Approve
          </button>
          <button
            @click="onConfirmDisapprove"
            :disabled="processing"
            class="flex-1 py-3 px-4 bg-red-600 text-white rounded-lg font-medium hover:bg-red-700 disabled:opacity-50"
          >
            Confirm Disapprove
          </button>
        </div>
        <div class="flex gap-3">
          <button
            @click="goDashboard"
            :disabled="processing"
            class="w-full py-3 px-4 bg-gray-100 text-gray-700 rounded-lg font-medium hover:bg-gray-200"
          >
            Cancel
          </button>
        </div>
        <div v-if="processing" class="text-sm text-gray-600">Processing...</div>
        <div v-if="errorMessage" class="text-sm text-red-600">{{ errorMessage }}</div>
      </div>

      <div v-else>
        <div class="text-6xl mb-4">{{ current.icon }}</div>
        <h1 class="text-2xl font-bold mb-2" :class="current.color">
          {{ current.title }}
        </h1>
        <p class="text-gray-600 mb-6">{{ current.message }}</p>

        <div class="flex flex-col gap-3">
          <button
            v-if="transactionId"
            @click="viewRequest"
            class="w-full py-3 px-4 bg-blue-600 text-white rounded-lg font-medium hover:bg-blue-700"
          >
            View Request
          </button>
          <button
            @click="goDashboard"
            class="w-full py-3 px-4 bg-gray-100 text-gray-700 rounded-lg font-medium hover:bg-gray-200"
          >
            Back to Dashboard
          </button>
        </div>
      </div>

    </div>
  </div>
</template>
