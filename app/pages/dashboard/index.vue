<script setup lang="ts">
import { BarChart, LineChart } from "vue-chrts";

const router = useRouter();
const userStore = useUserStore();
const userId = userStore.profile;

const loading = ref(false);
const error = ref<string | null>(null);

// Period filter state
type PeriodType = "daily" | "weekly" | "monthly";
const periodType = ref<PeriodType>("daily");
const periodCount = ref(7);

// Period configurations
const periodOptions = [
  { label: "Daily", value: "daily", counts: [7, 14, 30] },
  { label: "Weekly", value: "weekly", counts: [4, 8, 12, 26] },
  { label: "Monthly", value: "monthly", counts: [3, 6, 12, 24] },
];

// Default count for each period type
const defaultCounts: Record<PeriodType, number> = {
  daily: 7,
  weekly: 4,
  monthly: 3,
};

onMounted(async () => {
  fetchAnalytics();
});

// Reset count when period type changes
watch(periodType, (newType) => {
  periodCount.value = defaultCounts[newType] || 12;
});

const summary = ref<any>(null);
const approvalRates = ref<any[]>([]);
const policyOverrides = ref<any[]>([]);
const processingByPolicy = ref<any[]>([]);
const approverWorkload = ref<any[]>([]);

type DataProps = {
  period?: string;
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

const ApprovalDecision = {
  approved: { name: "Approved", color: "#22c55e" },
  disapproved: { name: "Disapproved", color: "#ef4444" },
  pending: { name: "Pending", color: "#f59e0b" },
};

const approvalChartData = computed<DataProps[]>(() =>
  approvalRates.value.map((r) => ({
    period: r.period, // formatPeriodLabel(r.period),
    approved: r.approved,
    disapproved: r.disapproved,
    pending: r.pending,
  })),
);

// function formatPeriodLabel(period: string) {
//   // if already "Mar 2026", keep it
//   if (/^[A-Za-z]{3}\s\d{4}$/.test(period)) return period;

//   console.log("Formatting period label:", period);
//   // fallback if backend sends ISO date
//   // const d = new Date(period);
//   // if (!isNaN(d.getTime())) {
//   //   return d.toLocaleString("en-US", { month: "short", year: "numeric" });
//   // }

//   return period;
// }

const approvalChartOptions = computed(() => ({
  data: approvalChartData.value,
  categories: ApprovalDecision,
  xAxis: "period",
  xNumTicks: approvalChartData.value.length,
  xFormatter: (tick: number) => {
    const period = approvalChartData.value[tick]?.period ?? "";
    return period;
  },
}));

// const approvalChartOptions = computed(() => ({
//   data: approvalChartData.value,
//   categories: ApprovalDecision,
//   xAxis: xAxisLabel.value,
//   xNumTicks: approvalChartData.value.length,
//   tickTextHideOverlapping: false,
//   tickTextAngle: -45,
//   tickTextWidth: 80,
//   xFormatter: (tick: number, i?: number) => {
//     return approvalChartData.value[i ?? tick]?.period ?? "";
//   },
// }));
//approvalChartData.map((d) => d.period) / xAxisLabel.value

const fetchAnalytics = async () => {
  loading.value = true;
  error.value = null;
  try {
    const response = await apiFetch(
      `/api/dashboard/analytics?period=${periodType.value}&count=${periodCount.value}`,
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

      console.log(
        "Raw Approval Rates:",
        approvalRates.value.map((r) => r.period),
      );
      console.log("Approval Chart Data:", approvalChartData.value);
      console.log(
        "Approval Chart Data Length:",
        approvalChartData.value.length,
      );
    }
  } catch (e: any) {
    error.value = e.message || "Failed to load analytics";
  } finally {
    loading.value = false;
  }
};

watch([periodType, periodCount], () => fetchAnalytics());
</script>

<template>
  <div class="bg-gray-50">
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

          <!-- Period Type Buttons -->
          <div class="flex items-center gap-1">
            <button
              v-for="option in periodOptions"
              :key="option.value"
              @click="
                periodType = option.value;
                periodCount = option.counts[1] || option.counts[0];
              "
              :class="[
                'px-3 py-1.5 text-sm rounded-lg border transition-colors',
                periodType === option.value
                  ? 'bg-blue-600 text-white border-blue-600'
                  : 'bg-white text-gray-700 border-gray-300 hover:bg-gray-50',
              ]"
            >
              {{ option.label }}
            </button>
          </div>
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
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mb-3">
          <!-- Approval Rates Over Time -->
          <div class="bg-white rounded-lg border border-gray-200 p-5">
            <div class="flex items-center justify-between mb-3">
              <div>
                <h2 class="text-sm font-medium text-gray-700 mb-1">
                  Approval Rates Over Time
                </h2>
                <p class="text-xs text-gray-400 mb-3">
                  {{
                    periodType === "daily"
                      ? "Daily breakdown"
                      : periodType === "weekly"
                        ? "Weekly breakdown"
                        : "Monthly breakdown"
                  }}
                </p>
              </div>
              <!-- Count Dropdown -->
              <select
                v-model="periodCount"
                class="text-sm border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500"
              >
                <option
                  v-for="count in periodOptions.find(
                    (p) => p.value === periodType,
                  )?.counts || []"
                  :key="count"
                  :value="count"
                >
                  {{
                    periodType === "daily"
                      ? `${count} day${count > 1 ? "s" : ""}`
                      : periodType === "weekly"
                        ? `${count} week${count > 1 ? "s" : ""}`
                        : `${count} month${count > 1 ? "s" : ""}`
                  }}
                </option>
              </select>
            </div>
            <div style="position: relative">
              <BarChart
                :height="270"
                :stacked="false"
                :y-axis="['approved', 'disapproved', 'pending']"
                v-bind="approvalChartOptions"
              />
              <!-- 
                :x-axis="approvalChartData.map((d) => d.period)"
              :tickTextHideOverlapping="false"
                :tickTextAngle="-45"
              
               -->
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
            <div style="position: relative">
              <div class="mt-6">
                <table class="w-full table-auto">
                  <thead>
                    <tr class="border-b text-left">
                      <th>#</th>
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
                      <td class="text-gray-700 pe-2">
                        {{ policyOverrides.indexOf(p) + 1 }}
                      </td>
                      <td class="font-medium text-gray-700">
                        {{ p.policyName }}
                      </td>
                      <td>{{ p.totalRequests }}</td>
                      <td class="text-green-600">{{ p.approved }}</td>
                      <td class="text-red-600">{{ p.disapproved }}</td>
                      <td class="text-yellow-600">{{ p.pending }}</td>
                    </tr>
                    <tr>
                      <td colspan="2" class="text-right text-gray-400 px-4">
                        Total:
                      </td>
                      <td>
                        {{
                          policyOverrides.reduce(
                            (sum, p) => sum + p.totalRequests,
                            0,
                          )
                        }}
                      </td>
                      <td class="text-green-600">
                        {{
                          policyOverrides.reduce(
                            (sum, p) => sum + p.approved,
                            0,
                          )
                        }}
                      </td>
                      <td class="text-red-600">
                        {{
                          policyOverrides.reduce(
                            (sum, p) => sum + p.disapproved,
                            0,
                          )
                        }}
                      </td>
                      <td class="text-yellow-600">
                        {{
                          policyOverrides.reduce((sum, p) => sum + p.pending, 0)
                        }}
                      </td>
                    </tr>
                    <tr v-if="policyOverrides.length === 0">
                      <td colspan="6" class="text-center text-gray-400 py-4">
                        No policy overrides found.
                      </td>
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

<style scoped>
/* rotate x-axis tick labels */
:deep(.vis-axis.x-axis text) {
  transform: rotate(-45deg) translate(-8px, 4px);
  transform-origin: right center;
  text-anchor: end;
  font-size: 11px;
}
</style>
