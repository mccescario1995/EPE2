<script setup lang="ts">
import { BarChart, LineChart } from "vue-chrts";

const router = useRouter();
const userStore = useUserStore();
const userId = userStore.profile;

const loading = ref(false);
const error = ref<string | null>(null);
const months = ref(6);

const summary = ref<any>(null);
const approvalRates = ref<any[]>([]);
const policyOverrides = ref<any[]>([]);
const processingByPolicy = ref<any[]>([]);
const approverWorkload = ref<any[]>([]);

type DataProps = {
  month?: string;
  pending?: number;
  approved?: number;
  disapproved?: number;
};

type PolicyDataProps = {
  policyName: string;
  approved: number;
  disapproved: number;
  pending: number;
};

const policyChartData = computed<PolicyDataProps[]>(() =>
  policyOverrides.value.map((p) => ({
    policyName: p.policyName,
    approved: p.approved,
    disapproved: p.disapproved,
    pending: p.pending,
  })),
);

const policyChartOptions = computed(() => ({
  data: policyChartData.value,
  categories: {
    approved: { name: "Approved", color: "#22c55e" },
    disapproved: { name: "Disapproved", color: "#ef4444" },
    pending: { name: "Pending", color: "#f59e0b" },
  },
  xAxis: "policyName",
  xNumTicks: policyChartData.value.length,

  // REQUIRED (same fix as your month issue)
  xFormatter: (tick: number, i?: number) =>
    policyChartData.value[i ?? tick]?.policyName,
}));

const ApprovalDecision = {
  approved: { name: "Approved", color: "#22c55e" },
  disapproved: { name: "Disapproved", color: "#ef4444" },
  pending: { name: "Pending", color: "#f59e0b" },
};

const approvalChartData = computed<DataProps[]>(() =>
  approvalRates.value.map((r) => ({
    month: r.month,
    approved: r.approved,
    disapproved: r.disapproved,
    pending: r.pending,
  })),
);

const approvalChartOptions = computed(() => ({
  data: approvalChartData.value,
  categories: ApprovalDecision,
  xAxis: "month" as const,
  xFormatter: (tick: number, i?: number) => {
    return approvalChartData.value[i ?? tick]?.month ?? "";
  },
}));

const fetchAnalytics = async () => {
  loading.value = true;
  error.value = null;
  try {
    const response = await apiFetch(
      `/api/dashboard/analytics?months=${months.value}`,
      {
        method: "GET",
        parseJson: true,
      },
    );

    if (response) {
      summary.value = response.summary;
      approvalRates.value = response.approvalRates;
      policyOverrides.value = response.policyOverrides;
      processingByPolicy.value = response.processingByPolicy;
      approverWorkload.value = response.approverWorkload;
    }

    console.log("Analytics response:", response);
  } catch (e: any) {
    error.value = e.message || "Failed to load analytics";
  } finally {
    loading.value = false;
  }
};

const getResponseBadge = (rate: number) => {
  if (rate >= 85) return { text: "Active", cls: "bg-green-100 text-green-800" };
  if (rate >= 70)
    return { text: "Moderate", cls: "bg-yellow-100 text-yellow-800" };
  return { text: "Low", cls: "bg-red-100 text-red-800" };
};

const formatHours = (h: number) => {
  if (h < 24) return `${h}h`;
  return `${Math.round((h / 24) * 10) / 10}d`;
};

watch(months, () => fetchAnalytics());

onMounted(async () => {
  fetchAnalytics();
});
</script>

<template>
  <div class="min-h-screen bg-gray-50">
    <!-- Header -->
    <header class="bg-white shadow">
      <div
        class="max-w-7xl mx-auto py-4 px-4 sm:px-6 lg:px-8 flex justify-between items-center"
      >
        <div class="flex items-center">
          <h1 class="text-2xl ps-5 font-bold text-gray-900">Dashboard</h1>
        </div>

        <!-- Period filter -->
        <div class="flex items-center gap-3">
          <label class="text-sm text-gray-600">Period:</label>
          <select
            v-model="months"
            class="text-sm border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500"
          >
            <option :value="3">Last 3 months</option>
            <option :value="6">Last 6 months</option>
            <option :value="12">Last 12 months</option>
            <option :value="24">Last 24 months</option>
          </select>
        </div>
      </div>
    </header>

    <main class="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
      <!-- Loading -->
      <div v-if="loading" class="flex items-center justify-center py-24">
        <svg
          class="animate-spin h-8 w-8 text-blue-600"
          xmlns="http://www.w3.org/2000/svg"
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
        <span class="ml-3 text-gray-600">Loading analytics...</span>
      </div>

      <!-- Error -->
      <div
        v-else-if="error"
        class="bg-red-50 border border-red-200 rounded-lg p-6 text-center"
      >
        <p class="text-red-700">{{ error }}</p>
        <button
          @click="fetchAnalytics"
          class="mt-4 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700"
        >
          Retry
        </button>
      </div>

      <!-- Content -->
      <div v-else-if="summary">
        <!-- KPI Cards -->
        <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
          <div class="bg-white rounded-lg border border-gray-200 p-4">
            <p class="text-xs text-gray-500 mb-1">Total requests</p>
            <p class="text-2xl font-semibold text-gray-900">
              {{ summary.totalRequests }}
            </p>
            <p class="text-xs text-gray-400 mt-1">All time</p>
          </div>
          <div class="bg-white rounded-lg border border-gray-200 p-4">
            <p class="text-xs text-gray-500 mb-1">Approval rate</p>
            <p class="text-2xl font-semibold text-green-700">
              {{ summary.overallApprovalRate }}%
            </p>
            <p class="text-xs text-gray-400 mt-1">
              {{ summary.totalApproved }} of
              {{ summary.totalApproved + summary.totalDisapproved }} closed
            </p>
          </div>
          <div class="bg-white rounded-lg border border-gray-200 p-4">
            <p class="text-xs text-gray-500 mb-1">Pending decisions</p>
            <p class="text-2xl font-semibold text-amber-600">
              {{ summary.totalPending }}
            </p>
            <p class="text-xs text-gray-400 mt-1">Awaiting response</p>
          </div>
        </div>

        <!-- Charts row 1 -->
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mb-6">
          <!-- Approval Rates Over Time -->
          <div class="bg-white rounded-lg border border-gray-200 p-5">
            <h2 class="text-sm font-medium text-gray-700 mb-1">
              Approval Rates Over Time
            </h2>
            <p class="text-xs text-gray-400 mb-3">Monthly breakdown</p>

            <div style="position: relative; height: 220px">
              <!-- <div ref="chartTrendRef" class="h-full w-full"></div> -->
              <!-- <VChart :option="chartTrend" /> -->
              <BarChart
                :height="200"
                :stacked="false"
                :y-axis="['approved', 'disapproved', 'pending']"
                v-bind="approvalChartOptions"
              />
            </div>
          </div>

          <!-- Common Policy Overrides -->
          <div class="bg-white rounded-lg border border-gray-200 p-5">
            <h2 class="text-sm font-medium text-gray-700 mb-1">
              Common Policy Overrides
            </h2>
            <p class="text-xs text-gray-400 mb-3">
              By number of exemption requests
            </p>
            <div
              style="position: relative"
              :style="`height:${Math.max(220, policyOverrides.length * 44)}px`"
            >
              <div class="mt-6">
                <table class="w-full table-auto">
                  <thead>
                    <tr class="border-b text-left">
                      <th>Rank</th>
                      <th>Policy</th>
                      <th>Total</th>
                      <th>Approved</th>
                      <th>Disapproved</th>
                      <th>Pending</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr
                      v-for="p in policyOverrides"
                      :key="p.policyId"
                      class="border-b"
                    >
                      <td>{{ policyOverrides.indexOf(p) + 1 }}</td>
                      <td>{{ p.policyName }}</td>
                      <td>{{ p.totalRequests }}</td>
                      <td class="text-green-600">{{ p.approved }}</td>
                      <td class="text-red-600">{{ p.disapproved }}</td>
                      <td class="text-yellow-600">{{ p.pending }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </div>
        <!-- Decision breakdown pills -->
        <div class="bg-white rounded-lg border border-gray-200 p-5">
          <h2 class="text-sm font-medium text-gray-700 mb-4">
            Overall Decision Breakdown
          </h2>
          <div class="flex items-center gap-2 mb-2">
            <div
              class="h-4 rounded-l-full bg-green-500 transition-all"
              :style="`width:${summary.overallApprovalRate}%`"
            ></div>
            <div
              class="h-4 bg-red-400 transition-all"
              :style="`width:${
                summary.totalRequests > 0
                  ? (
                      (summary.totalDisapproved / summary.totalRequests) *
                      100
                    ).toFixed(1)
                  : 0
              }%`"
            ></div>
            <div
              class="h-4 rounded-r-full bg-amber-300 flex-1 transition-all"
            ></div>
          </div>
          <div class="flex gap-6 text-xs text-gray-600 mt-2">
            <span class="flex items-center gap-1.5">
              <span
                class="w-2.5 h-2.5 rounded-sm inline-block bg-green-500"
              ></span>
              Approved {{ summary.totalApproved }} ({{
                summary.overallApprovalRate
              }}%)
            </span>
            <span class="flex items-center gap-1.5">
              <span
                class="w-2.5 h-2.5 rounded-sm inline-block bg-red-400"
              ></span>
              Disapproved {{ summary.totalDisapproved }} ({{
                summary.totalRequests > 0
                  ? (
                      (summary.totalDisapproved / summary.totalRequests) *
                      100
                    ).toFixed(1)
                  : 0
              }}%)
            </span>
            <span class="flex items-center gap-1.5">
              <span
                class="w-2.5 h-2.5 rounded-sm inline-block bg-amber-300"
              ></span>
              Pending {{ summary.totalPending }}
            </span>
          </div>
        </div>
      </div>

      <!-- Empty state -->
      <div v-else class="text-center py-24 text-gray-400">
        No analytics data available.
      </div>
    </main>
  </div>
</template>
