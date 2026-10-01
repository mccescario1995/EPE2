<script setup lang="ts">
import { BarChart, LineChart } from "vue-chrts";
import Dropdown from "~/components/Dropdown.vue";

const router = useRouter();
const userStore = useUserStore();
const userId = userStore.profile;

const loading = ref(false);
const error = ref<string | null>(null);

// Responsive chart height
const chartHeight = ref(280);
const updateChartHeight = () => {
  chartHeight.value = window.innerWidth < 640 ? 220 : 280;
};
onMounted(() => {
  updateChartHeight();
  window.addEventListener("resize", updateChartHeight);
});
onUnmounted(() => {
  window.removeEventListener("resize", updateChartHeight);
});

// Period filter state
type PeriodType = "daily" | "weekly" | "monthly" | "all";
const periodType = ref<PeriodType>("all");
const periodCount = ref(0);

// Period configurations
const periodOptions = [
  { label: "Daily", value: "daily", counts: [7, 14, 30] },
  { label: "Weekly", value: "weekly", counts: [4, 8, 12, 26] },
  { label: "Monthly", value: "monthly", counts: [3, 6, 12, 24] },
  { label: "All", value: "all", counts: [] },
];

// Dropdown items for mobile period selector
const periodDropdownItems = periodOptions.map((o) => ({
  id: o.value,
  label: o.label,
}));

const selectedPeriodItem = computed(() => {
  return periodDropdownItems.find((item) => item.id === periodType.value) || null;
});

const handlePeriodDropdownSelect = (item: { id: string; label: string } | null) => {
  if (item) {
    const option = periodOptions.find((o) => o.value === item.id);
    if (option) changePeriod(option);
  }
};

// Count dropdown items for Approval Rates Over Time
const countDropdownItems = computed(() => {
  const currentOption = periodOptions.find((p) => p.value === periodType.value);
  if (!currentOption || currentOption.counts.length === 0) return [];
  return currentOption.counts.map((count) => ({
    id: String(count),
    label:
      periodType.value === "daily"
        ? `${count} day${count > 1 ? "s" : ""}`
        : periodType.value === "weekly"
          ? `${count} week${count > 1 ? "s" : ""}`
          : `${count} month${count > 1 ? "s" : ""}`,
  }));
});

const selectedCountItem = computed(() => {
  return countDropdownItems.value.find((item) => item.id === String(periodCount.value)) || null;
});

const handleCountDropdownSelect = (item: { id: string; label: string } | null) => {
  if (item) {
    periodCount.value = Number(item.id);
  }
};

// Default count for each period type
const defaultCounts: Record<PeriodType, number> = {
  daily: 7,
  weekly: 4,
  monthly: 3,
  all: 0,
};

onMounted(async () => {
  fetchAnalytics();
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
    }
  } catch (e: any) {
    error.value = e.message || "Failed to load analytics";
  } finally {
    loading.value = false;
  }
};

const changePeriod = (option: (typeof periodOptions)[number]) => {
  periodType.value = option.value;

  periodCount.value =
    option.value === "all" ? 0 : (option.counts[1] ?? option.counts[0]);
};

watch([periodType, periodCount], () => fetchAnalytics());
</script>

<template>
  <div class="bg-gray-50 dark:bg-gray-900 ">
    <!-- Header -->
    <header class="bg-white dark:bg-gray-800 shadow">
      <div
        class="max-w-full mx-auto py-4 px-4 sm:px-6 lg:px-8 flex flex-col sm:flex-row sm:justify-between sm:items-center gap-3"
      >
        <div class="flex items-center">
          <h1 class="text-xl sm:text-2xl ps-0 sm:ps-5 font-bold text-gray-900 dark:text-gray-100">Dashboard</h1>
        </div>

        <!-- Period filter -->
        <div class="flex flex-wrap items-center gap-2 sm:gap-3">
          <label class="text-sm text-gray-600 dark:text-gray-300">Period:</label>

          <!-- Mobile: Custom Dropdown -->
          <div class="sm:hidden w-[80%] absolute right-2">
            <Dropdown
              :model-value="selectedPeriodItem"
              :initial-items="periodDropdownItems"
              placeholder="Select period"
              :close-on-select="true"
              :searchable="false"
              class="w-full"
              @update:model-value="handlePeriodDropdownSelect"
            />
          </div>

          <!-- Desktop: Button group -->
          <div class="hidden sm:flex items-center gap-1">
            <button
              v-for="option in periodOptions"
              :key="option.value"
              @click="changePeriod(option)"
              :class="[
                'px-3 py-1.5 text-sm rounded-lg border transition-colors',
                periodType === option.value
                  ? 'bg-blue-600 text-white border-blue-600'
                  : 'bg-white text-gray-700 border-gray-300 hover:bg-gray-50 dark:bg-gray-700 dark:text-gray-300 dark:border-gray-600 dark:hover:bg-gray-600',
              ]"
            >
              {{ option.label }}
            </button>
          </div>
        </div>
      </div>
    </header>

    <main class="max-w-full mx-auto py-6 sm:px-6 lg:px-8">
      <!-- Loading -->
      <div v-if="loading" class="flex items-center justify-center py-24">
        <svg
          class="animate-spin h-8 w-8 text-blue-600 dark:text-blue-400"
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
        <span class="ml-3 text-gray-600 dark:text-gray-300">Loading analytics...</span>
      </div>

      <!-- Error -->
      <div
        v-else-if="error"
        class="bg-red-50 border border-red-200 rounded-lg p-6 text-center dark:bg-red-900 dark:border-red-800"
      >
        <p class="text-red-700 dark:text-red-400">{{ error }}</p>
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
        <div class="grid grid-cols-1 sm:grid-cols-3 gap-3 sm:gap-4 mb-4">
          <div class="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-600 p-3 sm:p-4">
            <p class="text-xs text-gray-500 dark:text-gray-400 mb-1">Total requests</p>
            <p class="text-xl sm:text-2xl font-semibold text-gray-900 dark:text-gray-100">
              {{ summary.totalRequests }}
            </p>
            <p class="text-xs text-gray-400 dark:text-gray-300 mt-1">All time</p>
          </div>
          <div class="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-600 p-3 sm:p-4">
            <p class="text-xs text-gray-500 dark:text-gray-400 mb-1">Approval rate</p>
            <p class="text-xl sm:text-2xl font-semibold text-green-700 dark:text-green-400">
              {{ summary.overallApprovalRate }}%
            </p>
            <p class="text-xs text-gray-400 dark:text-gray-300 mt-1">
              {{ summary.totalApproved }} of
              {{ summary.totalApproved + summary.totalDisapproved }} closed
            </p>
          </div>
          <div class="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-600 p-3 sm:p-4">
            <p class="text-xs text-gray-500 dark:text-gray-400 mb-1">Pending decisions</p>
            <p class="text-xl sm:text-2xl font-semibold text-amber-600 dark:text-amber-400">
              {{ summary.totalPending }}
            </p>
            <p class="text-xs text-gray-400 dark:text-gray-300 mt-1">Awaiting response</p>
          </div>
        </div>

        <!-- Charts row 1 -->
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-4 sm:gap-6 mb-3 ">
          <!-- Approval Rates Over Time -->
          <div class="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-600 p-3 sm:p-5">
            <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2 mb-3">
              <div>
                <h2 class="text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">
                  Approval Rates Over Time
                </h2>
                <p class="text-xs text-gray-400 dark:text-gray-300">
                  {{
                    periodType === "daily"
                      ? "Daily breakdown"
                      : periodType === "weekly"
                        ? "Weekly breakdown"
                        : periodType === "monthly"
                          ? "Monthly breakdown"
                          : "All-time data"
                  }}
                </p>
              </div>
              <!-- Count Dropdown - hidden for "All" period -->
              <select
                v-model="periodCount"
                v-if="periodType !== 'all'"
                class="hidden sm:block text-sm border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 dark:bg-gray-700 dark:border-gray-600 dark:text-gray-200"
              >
                <option
                  v-for="count in periodOptions.find(
                    (p) => p.value === periodType,
                  )?.counts || []"
                  class="w-full text-sm text-gray-700 dark:text-gray-200"
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
              <div v-if="periodType !== 'all'" class="sm:hidden w-28">
                <Dropdown
                  :model-value="selectedCountItem"
                  :initial-items="countDropdownItems"
                  placeholder="Count"
                  :close-on-select="true"
                  :searchable="false"
                  class="w-full"
                  @update:model-value="handleCountDropdownSelect"
                />
              </div>
              <div v-else class="text-sm text-gray-500 dark:text-gray-400">All time</div>
            </div>
            <div style="position: relative" class="w-full overflow-hidden">
              <BarChart
                :height="chartHeight"
                :stacked="false"
                :y-axis="['approved', 'disapproved', 'pending']"
                v-bind="approvalChartOptions"
              />
            </div>
          </div>

          <!-- Common Policy Overrides -->
          <div class="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-600 p-3 sm:p-5">
            <h2 class="text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">
              Common Policy Overrides
            </h2>
            <p class="text-xs text-gray-400 dark:text-gray-300 mb-3">
              By number of exemption requests
            </p>

            <!-- Mobile: Compact table -->
            <div v-if="policyOverrides.length > 0" class="sm:hidden overflow-hidden">
              <table class="w-full table-fixed text-xs">
                <colgroup>
                  <col class="w-5" />
                  <col class="w-20" />
                  <col class="w-10" />
                  <col class="w-10" />
                  <col class="w-10" />
                  <col class="w-10" />
                </colgroup>
                <thead>
                  <tr class="border-b border-gray-200 dark:border-gray-600">
                    <th class="py-1.5 text-left font-medium text-gray-500 dark:text-gray-400 pl-0.5">#</th>
                    <th class="py-1.5 text-left font-medium text-gray-500 dark:text-gray-400">Policy</th>
                    <th class="py-1.5 text-right font-medium text-gray-500 dark:text-gray-400 pr-0.5" title="Total">Total</th>
                    <th class="py-1.5 text-right font-medium text-gray-500 dark:text-gray-400 pr-0.5" title="Approved">Appr...</th>
                    <th class="py-1.5 text-right font-medium text-gray-500 dark:text-gray-400 pr-0.5" title="Disapproved">Disappr</th>
                    <th class="py-1.5 text-right font-medium text-gray-500 dark:text-gray-400 pr-1" title="Pending">Pending</th>
                  </tr>
                </thead>
                <tbody>
                  <tr
                    v-for="(p, index) in policyOverrides.slice(0, 5)"
                    :key="p.policyId"
                    class="border-b border-gray-100 dark:border-gray-700"
                  >
                    <td class="py-1 pl-0.5 text-gray-500 dark:text-gray-400">{{ index + 1 }}</td>
                    <td class="py-1 font-medium text-gray-700 dark:text-gray-200 truncate">{{ p.policyName }}</td>
                    <td class="py-1 text-right text-gray-700 dark:text-gray-200 pr-0.5">{{ p.totalRequests }}</td>
                    <td class="py-1 text-right text-green-600 dark:text-green-400 pr-0.5">{{ p.approved }}</td>
                    <td class="py-1 text-right text-red-600 dark:text-red-400 pr-0.5">{{ p.disapproved }}</td>
                    <td class="py-1 text-right text-yellow-600 dark:text-yellow-400 pr-1">{{ p.pending }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
            <div v-else class="sm:hidden p-4 text-center text-sm text-gray-400 dark:text-gray-300">
              No policy overrides found.
            </div>

            <!-- Desktop: Table layout -->
            <div style="position: relative" class="hidden sm:block">
              <table class="w-full table-auto">
                <thead>
                  <tr class="border-b border-gray-200 dark:border-gray-600">
                    <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400">
                      #
                    </th>
                    <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400">
                      Policy
                    </th>
                    <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400">
                      Total
                    </th>
                    <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400">
                      Approved
                    </th>
                    <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400">
                      Disapproved
                    </th>
                    <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400">
                      Pending
                    </th>
                  </tr>
                </thead>
                <tbody>
                  <tr
                    v-for="(p, index) in policyOverrides.slice(0, 5)"
                    :key="p.policyId"
                    class="border-b border-gray-100 dark:border-gray-700"
                  >
                    <td class="px-4 py-2 text-sm text-gray-700 dark:text-gray-200">
                      {{ index + 1 }}
                    </td>
                    <td class="px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-200">
                      {{ p.policyName }}
                    </td>
                    <td class="px-4 py-2 text-sm text-gray-700 dark:text-gray-200">{{ p.totalRequests }}</td>
                    <td class="px-4 py-2 text-sm text-green-600 dark:text-green-400">{{ p.approved }}</td>
                    <td class="px-4 py-2 text-sm text-red-600 dark:text-red-400">{{ p.disapproved }}</td>
                    <td class="px-4 py-2 text-sm text-yellow-600 dark:text-yellow-400">{{ p.pending }}</td>
                  </tr>
                  <tr v-if="policyOverrides.length === 0">
                    <td colspan="6" class="px-4 py-4 text-center text-sm text-gray-400 dark:text-gray-300">
                      No policy overrides found.
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>

        <!-- Decision breakdown pills -->
        <div class="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-600 p-3 sm:p-5">
          <h2 class="text-sm font-medium text-gray-700 dark:text-gray-200 mb-4">
            Overall Decision Breakdown
          </h2>
          <div class="flex items-center gap-2 mb-2">
            <div
              class="h-3 sm:h-4 rounded-l-full bg-green-500 dark:bg-green-400 transition-all"
              :style="`width:${summary.overallApprovalRate}%`"
            ></div>
            <div
              class="h-3 sm:h-4 bg-red-500 transition-all"
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
              class="h-3 sm:h-4 rounded-r-full bg-amber-500 flex-1 transition-all"
            ></div>
          </div>
          <div class="flex flex-wrap gap-3 sm:gap-6 text-xs text-gray-600 dark:text-gray-300 mt-2">
            <span class="flex items-center gap-1.5">
              <span
                class="w-2.5 h-2.5 rounded-sm inline-block bg-green-500 dark:bg-green-400"
              ></span>
              Approved {{ summary.totalApproved }} ({{
                summary.overallApprovalRate
              }}%)
            </span>
            <span class="flex items-center gap-1.5">
              <span
                class="w-2.5 h-2.5 rounded-sm inline-block bg-red-400 dark:bg-red-300"
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
                class="w-2.5 h-2.5 rounded-sm inline-block bg-amber-300 dark:bg-amber-200"
              ></span>
              Pending {{ summary.totalPending }}
            </span>
          </div>
        </div>
      </div>

      <!-- Empty state -->
      <div v-else class="text-center py-24 text-gray-400 dark:text-gray-500">
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
