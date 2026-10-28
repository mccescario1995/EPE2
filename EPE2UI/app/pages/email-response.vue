<script setup lang="ts">
import { ref, computed, onMounted } from "vue";

// ─── Layout ───────────────────────────────────────────────────────────────────
definePageMeta({ layout: "empty" });

const router = useRouter();

// ─── Hash-mode safe token extraction ─────────────────────────────────────────
// nuxt.config uses hashMode: true → route is after "#", so route.query is
// populated from the hash segment. However, on first paint the hash may not
// be parsed yet — so we also fall back to manual window.location.hash parsing.
function getHashParam(key: string): string {
  if (typeof window === "undefined") return "";

  const hash = window.location.hash;
  const queryStart = hash.indexOf("?");
  if (queryStart === -1) return "";

  const params = new URLSearchParams(hash.slice(queryStart + 1));
  const val = params.get(key) ?? "";
  console.debug("[email-response] getHashParam", { key, hash, value: val });
  return val;
}

// ─── State ────────────────────────────────────────────────────────────────────
const token = ref("");
const previewData = ref<any>(null);
const resultStatus = ref("");
const resultAction = ref("");
const resultTxId = ref("");

const stage = ref<"loading" | "confirm" | "result" | "error">("loading");
const errorKey = ref("");
const processing = ref(false);

// ─── Status / result display config ──────────────────────────────────────────
const statusConfig: Record<
  string,
  { icon: string; color: string; bg: string; title: string; message: string }
> = {
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
  error: {
    icon: "❌",
    color: "text-red-600",
    bg: "bg-red-50",
    title: "Something Went Wrong",
    message:
      "An error occurred while processing your response. Please try again via the app.",
  },
  network_error: {
    icon: "📡",
    color: "text-red-600",
    bg: "bg-red-50",
    title: "Network Error",
    message: "Could not reach the server. Please check your connection and try again.",
  },
};

// ─── Derived display ──────────────────────────────────────────────────────────
const displayConfig = computed(() => {
  if (stage.value === "result") {
    const approved = resultAction.value === "approved";
    return {
      icon: approved ? "✅" : "❌",
      color: approved ? "text-green-600" : "text-red-600",
      bg: approved ? "bg-green-50" : "bg-red-50",
      title: approved ? "Request Approved!" : "Request Disapproved",
      message: `You have successfully ${resultAction.value} this exemption request.`,
    };
  }
  return statusConfig[errorKey.value] ?? statusConfig.error;
});

const tokenAction = computed<"approve" | "disapprove" | null>(() => {
  return previewData.value?.action ?? null;
});

const actionLabel = computed(() => {
  if (tokenAction.value === "disapprove")
    return { verb: "Disapprove", color: "bg-red-600 hover:bg-red-700" };
  return { verb: "Approve", color: "bg-green-600 hover:bg-green-700" };
});

// ─── Load preview ─────────────────────────────────────────────────────────────
async function loadPreview() {
  stage.value = "loading";
  console.debug("[email-response] loadPreview start", { token: token.value });

  try {
    const data: any = await apiFetch(
      `/api/emailapproval/preview?token=${encodeURIComponent(token.value)}`,
      {
        method: "GET",
        parseJson: true,
      }
    );

    console.debug("[email-response] preview response", data);

    if (!data?.ok) {
      console.warn("[email-response] preview not ok", data);
      errorKey.value = data?.error ?? "error";
      resultTxId.value = String(data?.transactionId ?? "");
      stage.value = "error";
      return;
    }

    previewData.value = data;
    resultTxId.value = String(data.transaction?.id ?? "");
    stage.value = "confirm";
  } catch (err: any) {
    console.error("[email-response] Preview fetch failed:", err);
    errorKey.value = "network_error";
    stage.value = "error";
  }
}

// ─── Execute action ───────────────────────────────────────────────────────────
async function executeAction() {
  if (processing.value || !token.value) return;

  processing.value = true;
  console.debug("[email-response] executeAction start", {
    token: token.value,
    action: tokenAction.value,
  });
  try {
    const data: any = await apiFetch(
      `/api/emailapproval/act?token=${encodeURIComponent(token.value)}`,
      {
        headers: {
          Accept: "application/json",
          "X-Requested-With": "XMLHttpRequest",
        },
      }
    );

    console.debug("[email-response] act response", data);

    if (data?.ok) {
      resultAction.value = data.result ?? "approved";
      resultTxId.value = String(data.transactionId ?? resultTxId.value);
      stage.value = "result";
    } else {
      console.warn("[email-response] act not ok", data);
      errorKey.value = data?.error ?? "error";
      resultTxId.value = String(data?.transactionId ?? resultTxId.value);
      stage.value = "error";
    }
  } catch (err: any) {
    console.error("[email-response] Act fetch failed:", err);
    errorKey.value = "network_error";
    stage.value = "error";
  } finally {
    processing.value = false;
  }
}

// ─── Navigation helpers ───────────────────────────────────────────────────────
function viewRequest() {
  if (!resultTxId.value) return;
  router.push({ path: "/main/pending-for-approval/view", query: { id: resultTxId.value } });
}

function goDashboard() {
  router.push("/main/dashboard");
}

// ─── Init ─────────────────────────────────────────────────────────────────────
onMounted(() => {
  const route = useRoute();
  let t = route.query.token;
  console.debug("[email-response] route.query.token raw", t);
  token.value = Array.isArray(t) ? t[0] ?? "" : t ?? "";
  console.debug("[email-response] token after route parse", token.value);

  if (!token.value) {
    token.value = getHashParam("token");
    console.debug("[email-response] token after hash parse", token.value);
  }

  if (!token.value) {
    console.warn("[email-response] No token found - marking invalid");
    errorKey.value = "invalid";
    stage.value = "error";
    return;
  }

  loadPreview();
});
</script>

<template>
  <div class="min-h-screen bg-gray-50 flex items-center justify-center p-4">
    <div class="bg-white rounded-2xl shadow-lg max-w-md w-full p-8">
      <!-- ── Loading ── -->
      <div v-if="stage === 'loading'" class="flex flex-col items-center gap-4 py-8">
        <svg class="animate-spin h-10 w-10 text-blue-500" fill="none" viewBox="0 0 24 24">
          <circle
            class="opacity-25"
            cx="12"
            cy="12"
            r="10"
            stroke="currentColor"
            stroke-width="4"
          />
          <path
            class="opacity-75"
            fill="currentColor"
            d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
          />
        </svg>
        <p class="text-gray-500 text-sm">Verifying your link…</p>
      </div>

      <!-- ── Confirm ── -->
      <div v-else-if="stage === 'confirm'" class="space-y-6">
        <div class="text-center">
          <!-- Action badge -->
          <span
            class="inline-block mb-3 text-xs font-semibold uppercase tracking-wider px-3 py-1 rounded-full"
            :class="
              tokenAction === 'disapprove'
                ? 'bg-red-100 text-red-700'
                : 'bg-green-100 text-green-700'
            "
          >
            {{ tokenAction === "disapprove" ? "Disapproval" : "Approval" }} Request
          </span>
          <h2 class="text-2xl font-bold text-gray-800">Confirm Your Response</h2>
          <p class="text-gray-500 text-sm mt-1">
            You are about to
            <strong>{{ tokenAction === "disapprove" ? "disapprove" : "approve" }}</strong>
            this exemption request. This action cannot be undone.
          </p>
        </div>

        <!-- Transaction details -->
        <div
          v-if="previewData?.transaction"
          class="text-left p-4 bg-gray-50 rounded-lg border border-gray-200 space-y-2"
        >
          <div>
            <span class="text-xs font-medium text-gray-500 uppercase tracking-wide"
              >Transaction #</span
            >
            <p class="text-sm font-semibold text-blue-700 mt-0.5">
              {{ previewData.transaction.number }}
            </p>
          </div>
          <div v-if="previewData.transaction.summary">
            <span class="text-xs font-medium text-gray-500 uppercase tracking-wide"
              >Details</span
            >
            <p class="text-sm text-gray-700 mt-0.5 line-clamp-4">
              {{ previewData.transaction.summary }}
            </p>
          </div>
          <div v-if="previewData.expiresAt">
            <span class="text-xs font-medium text-gray-500 uppercase tracking-wide"
              >Link expires</span
            >
            <p class="text-sm text-gray-600 mt-0.5">
              {{ new Date(previewData.expiresAt).toLocaleString() }}
            </p>
          </div>
        </div>

        <!-- Confirm button (single, matches token action) -->
        <div class="flex flex-col gap-3">
          <button
            @click="executeAction"
            :disabled="processing"
            class="w-full py-3 px-4 rounded-lg font-semibold text-white text-sm transition-colors disabled:opacity-50 flex items-center justify-center gap-2"
            :class="actionLabel.color"
          >
            <svg
              v-if="processing"
              class="animate-spin h-4 w-4"
              fill="none"
              viewBox="0 0 24 24"
            >
              <circle
                class="opacity-25"
                cx="12"
                cy="12"
                r="10"
                stroke="currentColor"
                stroke-width="4"
              />
              <path
                class="opacity-75"
                fill="currentColor"
                d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
              />
            </svg>
            {{ processing ? "Processing…" : `Confirm ${actionLabel.verb}` }}
          </button>

          <button
            @click="goDashboard"
            :disabled="processing"
            class="w-full py-3 px-4 bg-gray-100 text-gray-700 rounded-lg font-medium text-sm hover:bg-gray-200 transition-colors disabled:opacity-50"
          >
            Cancel — Go to Dashboard
          </button>
        </div>
      </div>

      <!-- ── Result ── -->
      <div v-else-if="stage === 'result'" class="text-center space-y-5">
        <div class="text-5xl">{{ displayConfig?.icon }}</div>
        <div :class="['text-2xl font-bold', displayConfig?.color]">
          {{ displayConfig?.title }}
        </div>
        <p class="text-gray-500 text-sm">{{ displayConfig?.message }}</p>

        <div class="flex flex-col gap-3 pt-2">
          <button
            v-if="resultTxId"
            @click="viewRequest"
            class="w-full py-3 px-4 bg-blue-600 text-white rounded-lg font-medium text-sm hover:bg-blue-700 transition-colors"
          >
            View Request
          </button>
          <button
            @click="goDashboard"
            class="w-full py-3 px-4 bg-gray-100 text-gray-700 rounded-lg font-medium text-sm hover:bg-gray-200 transition-colors"
          >
            Back to Dashboard
          </button>
        </div>
      </div>

      <!-- ── Error ── -->
      <div v-else class="text-center space-y-5">
        <div class="text-5xl">{{ displayConfig?.icon }}</div>
        <div :class="['text-2xl font-bold', displayConfig?.color]">
          {{ displayConfig?.title }}
        </div>
        <p class="text-gray-500 text-sm">{{ displayConfig?.message }}</p>

        <div class="flex flex-col gap-3 pt-2">
          <button
            v-if="resultTxId"
            @click="viewRequest"
            class="w-full py-3 px-4 bg-blue-600 text-white rounded-lg font-medium text-sm hover:bg-blue-700 transition-colors"
          >
            View Request
          </button>
          <button
            @click="goDashboard"
            class="w-full py-3 px-4 bg-gray-100 text-gray-700 rounded-lg font-medium text-sm hover:bg-gray-200 transition-colors"
          >
            Back to Dashboard
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
