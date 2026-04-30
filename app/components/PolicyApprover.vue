<script setup lang="ts">
/**
 * PolicyApproverManager.vue
 *
 * Drag-and-drop approver management using vue-draggable-plus.
 *
 * APPROVER STRUCTURE RULES:
 * ─────────────────────────────────────────────────────────────────
 * • linesequence = the approval STEP (1, 2, 3…). Steps run in order.
 *   All approvers at step N must respond before step N+1 is notified.
 *
 * • sort = groups a MAIN approver with its PROXY approvers.
 *   Main: linesequence === 1, has a unique sort value.
 *   Proxy: linesequence > 1, shares the same sort as its main.
 *   If ANY member of a sort-group approves → the whole group is satisfied.
 *
 * HOW MANY CAN YOU ADD?
 * ─────────────────────────────────────────────────────────────────
 * • Steps (linesequences): UNLIMITED — add as many sequential steps as needed.
 * • Proxies per step: UNLIMITED — add as many fallback approvers per step as needed.
 * • There is no hard DB limit. Only practical business logic limits apply.
 *
 * DRAG BEHAVIOR:
 * ─────────────────────────────────────────────────────────────────
 * • You can drag STEPS to reorder the sequence (step 1 → 3, etc.).
 * • You can drag PROXIES within a step to reorder sort priority.
 * • After any drag, sort and linesequence values are recalculated and saved.
 */

import { ref, computed, onMounted } from "vue";
import { VueDraggable } from "vue-draggable-plus";

// ── Props ────────────────────────────────────────────────────────
const props = defineProps<{
  policyId: number;
  userId: number;
}>();

// ── Types ────────────────────────────────────────────────────────
interface Approver {
  id: number;
  policyid: number;
  approverid: string | null;
  approvername: string | null;
  approveremail: string | null;
  approvertype: string | null;
  sort: number;
  linesequence: number;
  status: string;
  isdelete: number;
  isMainApprover: boolean;
  approverRole: string;
}

/**
 * A UI "Step" groups all approvers that share the SAME linesequence.
 * Within a step, the first approver is "Main" and all others are "Proxies".
 */
interface ApproverStep {
  /** Unique UI key for vue-draggable-plus */
  key: string;
  /** The current linesequence value (recalculated on save) */
  linesequence: number;
  /** Main approver (linesequence === 1 or whichever is first) */
  main: Approver | null;
  /** Proxy approvers (same sort group, linesequence > 1) */
  proxies: Approver[];
}

// ── State ────────────────────────────────────────────────────────
const steps = ref<ApproverStep[]>([]);
const loading = ref(false);
const saving = ref(false);
const error = ref("");
const success = ref("");

// Employee search
const searchQuery = ref("");
const searchResults = ref<{ id: string; name: string; email: string }[]>([]);
const isSearching = ref(false);
const activeSearchTarget = ref<{
  stepIdx: number;
  proxyIdx?: number;
} | null>(null);

// New step form
const showAddStep = ref(false);
const newStepApprover = ref<{ id: string; name: string; email: string } | null>(
  null,
);
const newStepType = ref<"M" | "IM">("M"); // M = Main, IM = Immediate Head (no employee selection)

// New proxy form
const activeProxyStep = ref<number | null>(null);
const newProxyApprover = ref<{
  id: string;
  name: string;
  email: string;
} | null>(null);

// ── Computed ─────────────────────────────────────────────────────
const totalApprovers = computed(() =>
  steps.value.reduce((sum, s) => sum + 1 + s.proxies.length, 0),
);

// ── API Helpers ──────────────────────────────────────────────────
const apiFetch = async (url: string, opts?: RequestInit) => {
  const res = await fetch(url, opts);
  if (!res.ok) throw new Error(`API error ${res.status}`);
  return res.json();
};

// ── Load approvers from API ──────────────────────────────────────
const loadApprovers = async () => {
  loading.value = true;
  error.value = "";
  try {
    const data = await apiFetch(
      `/api/maintenance/policy/${props.policyId}/approvers`,
      { method: "POST" },
    );
    const flat: Approver[] = data.items ?? [];
    steps.value = buildSteps(flat);
  } catch (e: any) {
    error.value = e.message ?? "Failed to load approvers";
  } finally {
    loading.value = false;
  }
};

/**
 * Converts a flat array of approvers (from API) into UI steps.
 *
 * Grouping rules:
 *  - Each unique `sort` value = one step group.
 *  - Within a group, linesequence===1 entries are Mains, others are Proxies.
 *  - Sort groups are ordered by their minimum linesequence.
 */
const buildSteps = (flat: Approver[]): ApproverStep[] => {
  // Group by sort value
  const bySort = new Map<number, Approver[]>();
  for (const a of flat) {
    if (!bySort.has(a.sort)) bySort.set(a.sort, []);
    bySort.get(a.sort)!.push(a);
  }

  // Sort groups by their minimum linesequence so step order is preserved
  const sortedGroups = [...bySort.entries()].sort(
    ([, aArr], [, bArr]) =>
      Math.min(...aArr.map((x) => x.linesequence)) -
      Math.min(...bArr.map((x) => x.linesequence)),
  );

  return sortedGroups.map(([sortVal, members], idx) => {
    // Sort members: mains first, then proxies
    const sorted = [...members].sort((a, b) => a.linesequence - b.linesequence);
    const main = sorted[0] ?? null;
    const proxies = sorted.slice(1);
    return {
      key: `step-${sortVal}-${idx}`,
      linesequence: idx + 1,
      main,
      proxies,
    };
  });
};

/**
 * Flattens UI steps back into a flat approver list with corrected
 * linesequence and sort values, then persists each change via the API.
 *
 * Numbering after drag:
 *  - Step position in the array → linesequence for its Main.
 *  - Proxies get linesequence = (step position + 1), same sort as Main.
 */
const saveOrder = async () => {
  saving.value = true;
  error.value = "";
  success.value = "";
  try {
    for (let stepIdx = 0; stepIdx < steps.value.length; stepIdx++) {
      const step: any = steps.value[stepIdx];
      const newSort = stepIdx + 1;
      const mainSeq = stepIdx + 1;
      const proxySeq = stepIdx + 2; // proxies are in next sequence slot

      const updates: Array<{ id: number; sort: number; linesequence: number }> =
        [];

      if (step.main) {
        updates.push({
          id: step.main.id,
          sort: newSort,
          linesequence: mainSeq,
        });
      }
      for (const proxy of step.proxies) {
        updates.push({ id: proxy.id, sort: newSort, linesequence: proxySeq });
      }

      // POST reorder to backend
      await apiFetch(
        `/api/maintenance/policy/${props.policyId}/approvers/reorder`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ items: updates }),
        },
      );
    }
    success.value = "Order saved successfully!";
    setTimeout(() => (success.value = ""), 3000);
  } catch (e: any) {
    error.value = e.message ?? "Failed to save order";
  } finally {
    saving.value = false;
  }
};

// ── Employee Search ──────────────────────────────────────────────
let searchTimer: ReturnType<typeof setTimeout> | null = null;
const onSearchInput = () => {
  if (searchTimer) clearTimeout(searchTimer);
  if (!searchQuery.value.trim()) {
    searchResults.value = [];
    return;
  }
  searchTimer = setTimeout(async () => {
    isSearching.value = true;
    try {
      const results = await apiFetch(
        `/api/maintenance/employees/search?q=${encodeURIComponent(searchQuery.value)}`,
      );
      searchResults.value = results;
    } catch {
      searchResults.value = [];
    } finally {
      isSearching.value = false;
    }
  }, 350);
};

const selectEmployee = (emp: { id: string; name: string; email: string }) => {
  if (activeSearchTarget.value !== null) {
    if (activeSearchTarget.value.proxyIdx === undefined) {
      // Selecting for a new step
      newStepApprover.value = emp;
    } else {
      newProxyApprover.value = emp;
    }
  }
  searchQuery.value = emp.name;
  searchResults.value = [];
  activeSearchTarget.value = null;
};

// ── Add Step ─────────────────────────────────────────────────────
const addStep = async () => {
  if (!newStepApprover.value && newStepType.value !== "IM") {
    error.value = "Please select an employee first.";
    return;
  }

  const newSort = steps.value.length + 1;
  const newSeq = steps.value.length + 1;

  try {
    const body = {
      policyid: props.policyId,
      approverid: newStepApprover.value?.id ?? null,
      approvername:
        newStepType.value === "IM"
          ? "Immediate Head"
          : newStepApprover.value?.name,
      approveremail:
        newStepType.value === "IM" ? null : newStepApprover.value?.email,
      approvertype: newStepType.value,
      sort: newSort,
      linesequence: newSeq,
      createdBy: props.userId,
    };

    const res = await apiFetch(
      `/api/maintenance/policy/${props.policyId}/approvers/create`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
      },
    );

    // Add to local state immediately for snappy UX
    const added: Approver = {
      ...res.item,
      isMainApprover: true,
      approverRole: "Main",
    };
    steps.value.push({
      key: `step-new-${Date.now()}`,
      linesequence: newSeq,
      main: added,
      proxies: [],
    });

    // Reset form
    showAddStep.value = false;
    newStepApprover.value = null;
    newStepType.value = "Main";
    searchQuery.value = "";
    success.value = "Step added!";
    setTimeout(() => (success.value = ""), 3000);
  } catch (e: any) {
    error.value = e.message ?? "Failed to add step";
  }
};

// ── Add Proxy ────────────────────────────────────────────────────
const addProxy = async (stepIdx: number) => {
  if (!newProxyApprover.value) {
    error.value = "Please select an employee for the proxy.";
    return;
  }
  const step = steps.value[stepIdx];
  if (!step) return;

  const mainSort = step.main?.sort ?? stepIdx + 1;
  const proxySeq = (step.main?.linesequence ?? stepIdx + 1) + 1;

  try {
    const body = {
      policyid: props.policyId,
      approverid: newProxyApprover.value.id,
      approvername: newProxyApprover.value.name,
      approveremail: newProxyApprover.value.email,
      approvertype: "M",
      sort: mainSort,
      linesequence: proxySeq,
      createdBy: props.userId,
    };

    const res = await apiFetch(
      `/api/maintenance/policy/${props.policyId}/approvers/create`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
      },
    );

    const added: Approver = {
      ...res.item,
      isMainApprover: false,
      approverRole: "Proxy",
    };
    steps.value[stepIdx].proxies.push(added);

    // Reset proxy form
    activeProxyStep.value = null;
    newProxyApprover.value = null;
    searchQuery.value = "";
    success.value = "Proxy added!";
    setTimeout(() => (success.value = ""), 3000);
  } catch (e: any) {
    error.value = e.message ?? "Failed to add proxy";
  }
};

// ── Delete ───────────────────────────────────────────────────────
const deleteApprover = async (
  approverId: number,
  stepIdx: number,
  isProxy: boolean,
  proxyIdx?: number,
) => {
  if (!confirm("Delete this approver?")) return;
  try {
    await apiFetch(
      `/api/maintenance/policy/${props.policyId}/approvers/delete/${approverId}`,
      { method: "POST" },
    );

    if (!isProxy) {
      // Remove entire step
      steps.value.splice(stepIdx, 1);
    } else if (proxyIdx !== undefined) {
      steps.value[stepIdx].proxies.splice(proxyIdx, 1);
    }
    success.value = "Approver deleted.";
    setTimeout(() => (success.value = ""), 3000);
  } catch (e: any) {
    error.value = e.message ?? "Failed to delete";
  }
};

// ── Drag event handlers ──────────────────────────────────────────

/**
 * Called after a step drag ends.
 * vue-draggable-plus mutates `steps.value` in place.
 * We just recalculate the display linesequence numbers for labels.
 */
const onStepDragEnd = () => {
  steps.value = steps.value.map((s, idx) => ({
    ...s,
    linesequence: idx + 1,
    key: `step-reordered-${idx}`,
  }));
};

onMounted(loadApprovers);
</script>

<template>
  <div class="min-h-screen bg-slate-50 p-6">
    <!-- ─── Header ─────────────────────────────────────────────── -->
    <div class="mb-8 flex items-start justify-between">
      <div>
        <h1 class="text-2xl font-bold text-slate-800">Approver Sequence</h1>
        <p class="mt-1 text-sm text-slate-500">
          Policy #{{ policyId }} ·
          <span class="font-medium text-slate-700"
            >{{ steps.length }} step(s)</span
          >
          ·
          <span class="font-medium text-slate-700"
            >{{ totalApprovers }} approver(s)</span
          >
        </p>
        <p class="mt-1 text-xs text-slate-400">
          Drag steps to reorder the approval sequence. Proxies are fallbacks
          within the same step.
        </p>
      </div>
      <div class="flex gap-2">
        <button
          class="rounded-lg border border-slate-200 bg-white px-4 py-2 text-sm font-medium text-slate-600 shadow-sm hover:bg-slate-50 transition"
          @click="loadApprovers"
        >
          ↺ Refresh
        </button>
        <button
          class="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-blue-700 transition disabled:opacity-50"
          :disabled="saving"
          @click="saveOrder"
        >
          {{ saving ? "Saving…" : "💾 Save Order" }}
        </button>
      </div>
    </div>

    <!-- ─── Toast ─────────────────────────────────────────────── -->
    <div
      v-if="error"
      class="mb-4 rounded-lg bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700"
    >
      ⚠ {{ error }}
    </div>
    <div
      v-if="success"
      class="mb-4 rounded-lg bg-green-50 border border-green-200 px-4 py-3 text-sm text-green-700"
    >
      ✓ {{ success }}
    </div>

    <!-- ─── Legend ────────────────────────────────────────────── -->
    <div class="mb-6 flex gap-4 text-xs text-slate-500">
      <span class="flex items-center gap-1">
        <span class="inline-block h-3 w-3 rounded-full bg-blue-500"></span> Main
        Approver
      </span>
      <span class="flex items-center gap-1">
        <span class="inline-block h-3 w-3 rounded-full bg-amber-400"></span>
        Proxy (fallback)
      </span>
      <span class="flex items-center gap-1">
        <span class="inline-block h-3 w-3 rounded-full bg-purple-400"></span> IM
        (Immediate Head)
      </span>
    </div>

    <!-- ─── Loading ───────────────────────────────────────────── -->
    <div v-if="loading" class="flex justify-center py-20">
      <svg
        class="animate-spin h-8 w-8 text-blue-500"
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
          d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
        />
      </svg>
    </div>

    <!-- ─── Draggable Steps ───────────────────────────────────── -->
    <div v-else>
      <!--
        VueDraggable wraps the list of steps.
        - `v-model` = the reactive array to reorder
        - `item-key` = unique key for each item (required by vue-draggable-plus)
        - `handle` = CSS selector for the drag handle (prevents accidental drags)
        - `animation` = smooth transition duration in ms
        - `ghost-class` = class applied to the placeholder while dragging
        - `@end` = called after drag completes; we recalculate sequence numbers
      -->
      <VueDraggable
        v-model="steps"
        item-key="key"
        handle=".drag-handle"
        :animation="200"
        ghost-class="opacity-40"
        @end="onStepDragEnd"
        tag="div"
        class="space-y-4"
      >
        <template #item="{ element: step, index: stepIdx }">
          <div
            :key="step.key"
            class="rounded-xl border border-slate-200 bg-white shadow-sm overflow-hidden"
          >
            <!-- Step Header -->
            <div
              class="flex items-center gap-3 bg-slate-50 px-4 py-3 border-b border-slate-100"
            >
              <!-- Drag Handle -->
              <div
                class="drag-handle cursor-grab active:cursor-grabbing text-slate-400 hover:text-slate-600 select-none"
                title="Drag to reorder step"
              >
                <svg class="h-5 w-5" fill="currentColor" viewBox="0 0 20 20">
                  <path
                    d="M7 2a2 2 0 1 0 0 4 2 2 0 0 0 0-4zm6 0a2 2 0 1 0 0 4 2 2 0 0 0 0-4zM7 8a2 2 0 1 0 0 4 2 2 0 0 0 0-4zm6 0a2 2 0 1 0 0 4 2 2 0 0 0 0-4zm-6 6a2 2 0 1 0 0 4 2 2 0 0 0 0-4zm6 0a2 2 0 1 0 0 4 2 2 0 0 0 0-4z"
                  />
                </svg>
              </div>
              <!-- Step Number Badge -->
              <span
                class="flex h-7 w-7 items-center justify-center rounded-full bg-blue-600 text-xs font-bold text-white"
              >
                {{ stepIdx + 1 }}
              </span>
              <span class="text-sm font-semibold text-slate-700">
                Step {{ stepIdx + 1 }}
              </span>
              <span class="ml-auto text-xs text-slate-400">
                {{ 1 + step.proxies.length }} approver(s) in this step
              </span>
            </div>

            <div class="p-4 space-y-3">
              <!-- Main Approver Row -->
              <div
                v-if="step.main"
                class="flex items-center gap-3 rounded-lg border border-blue-100 bg-blue-50 px-3 py-2"
              >
                <span
                  class="h-2.5 w-2.5 rounded-full bg-blue-500 shrink-0"
                ></span>
                <div class="min-w-0 flex-1">
                  <p class="text-sm font-medium text-slate-800 truncate">
                    {{
                      step.main.approvertype === "IM"
                        ? "⭐ Immediate Head"
                        : step.main.approvername
                    }}
                  </p>
                  <p class="text-xs text-slate-500 truncate">
                    {{
                      step.main.approvertype === "IM"
                        ? "Dynamically resolved at request time"
                        : (step.main.approveremail ?? "—")
                    }}
                  </p>
                </div>
                <span
                  class="shrink-0 rounded-full px-2 py-0.5 text-xs font-medium"
                  :class="
                    step.main.approvertype === 'IM'
                      ? 'bg-purple-100 text-purple-700'
                      : 'bg-blue-100 text-blue-700'
                  "
                >
                  {{ step.main.approvertype === "IM" ? "IM" : "M" }}
                </span>
                <button
                  class="shrink-0 text-slate-400 hover:text-red-500 transition"
                  title="Remove main approver (removes step)"
                  @click="deleteApprover(step.main.id, stepIdx, false)"
                >
                  <svg
                    class="h-4 w-4"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M6 18L18 6M6 6l12 12"
                    />
                  </svg>
                </button>
              </div>

              <!-- ── Proxy Rows (draggable within step) ──────── -->
              <!--
                Inner VueDraggable for proxy reordering within a step.
                Proxies share the same sort value as their main approver.
                Dragging here changes priority order among proxies.
              -->
              <VueDraggable
                v-model="step.proxies"
                item-key="id"
                handle=".proxy-handle"
                :animation="150"
                ghost-class="opacity-40"
                tag="div"
                class="space-y-2"
              >
                <template #item="{ element: proxy, index: proxyIdx }">
                  <div
                    :key="proxy.id"
                    class="flex items-center gap-3 rounded-lg border border-amber-100 bg-amber-50 px-3 py-2 ml-4"
                  >
                    <!-- Proxy drag handle -->
                    <div
                      class="proxy-handle cursor-grab active:cursor-grabbing text-amber-400 hover:text-amber-600 select-none"
                      title="Drag to reorder proxies"
                    >
                      <svg
                        class="h-4 w-4"
                        fill="currentColor"
                        viewBox="0 0 20 20"
                      >
                        <path
                          d="M7 2a2 2 0 1 0 0 4 2 2 0 0 0 0-4zm6 0a2 2 0 1 0 0 4 2 2 0 0 0 0-4zM7 8a2 2 0 1 0 0 4 2 2 0 0 0 0-4zm6 0a2 2 0 1 0 0 4 2 2 0 0 0 0-4zm-6 6a2 2 0 1 0 0 4 2 2 0 0 0 0-4zm6 0a2 2 0 1 0 0 4 2 2 0 0 0 0-4z"
                        />
                      </svg>
                    </div>
                    <span
                      class="h-2 w-2 rounded-full bg-amber-400 shrink-0"
                    ></span>
                    <div class="min-w-0 flex-1">
                      <p class="text-sm font-medium text-slate-800 truncate">
                        {{ proxy.approvername }}
                      </p>
                      <p class="text-xs text-slate-500 truncate">
                        {{ proxy.approveremail ?? "—" }}
                      </p>
                    </div>
                    <span
                      class="shrink-0 rounded-full bg-amber-100 px-2 py-0.5 text-xs font-medium text-amber-700"
                    >
                      Proxy {{ proxyIdx + 1 }}
                    </span>
                    <button
                      class="shrink-0 text-slate-400 hover:text-red-500 transition"
                      @click="deleteApprover(proxy.id, stepIdx, true, proxyIdx)"
                    >
                      <svg
                        class="h-4 w-4"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path
                          stroke-linecap="round"
                          stroke-linejoin="round"
                          stroke-width="2"
                          d="M6 18L18 6M6 6l12 12"
                        />
                      </svg>
                    </button>
                  </div>
                </template>
              </VueDraggable>

              <!-- Add Proxy Form -->
              <div
                v-if="activeProxyStep === stepIdx"
                class="ml-4 rounded-lg border border-dashed border-amber-300 bg-amber-50 p-3"
              >
                <p class="text-xs font-semibold text-amber-700 mb-2">
                  Add Proxy Approver
                </p>
                <div class="relative">
                  <input
                    v-model="searchQuery"
                    class="w-full rounded-md border border-amber-200 bg-white px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-amber-400"
                    placeholder="Search employee…"
                    @input="onSearchInput"
                    @focus="activeSearchTarget = { stepIdx, proxyIdx: 0 }"
                  />
                  <div
                    v-if="searchResults.length"
                    class="absolute z-30 mt-1 w-full rounded-lg border border-slate-200 bg-white shadow-lg max-h-40 overflow-y-auto"
                  >
                    <button
                      v-for="emp in searchResults"
                      :key="emp.id"
                      class="w-full text-left px-3 py-2 text-sm hover:bg-slate-50"
                      @click="
                        selectEmployee(emp);
                        newProxyApprover = emp;
                      "
                    >
                      <span class="font-medium">{{ emp.name }}</span>
                      <span class="ml-2 text-xs text-slate-400">{{
                        emp.email
                      }}</span>
                    </button>
                  </div>
                </div>
                <div class="mt-2 flex gap-2">
                  <button
                    class="rounded-md bg-amber-500 px-3 py-1.5 text-xs font-medium text-white hover:bg-amber-600 transition"
                    @click="addProxy(stepIdx)"
                  >
                    Add Proxy
                  </button>
                  <button
                    class="rounded-md border border-slate-200 px-3 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-50 transition"
                    @click="
                      activeProxyStep = null;
                      newProxyApprover = null;
                      searchQuery = '';
                    "
                  >
                    Cancel
                  </button>
                </div>
              </div>

              <!-- Add Proxy Button -->
              <button
                v-if="activeProxyStep !== stepIdx"
                class="ml-4 flex items-center gap-1 text-xs text-amber-600 hover:text-amber-800 transition"
                @click="
                  activeProxyStep = stepIdx;
                  searchQuery = '';
                  newProxyApprover = null;
                "
              >
                <svg
                  class="h-3.5 w-3.5"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M12 4v16m8-8H4"
                  />
                </svg>
                Add proxy approver
              </button>
            </div>
          </div>
        </template>
      </VueDraggable>

      <!-- Empty state -->
      <div
        v-if="!loading && steps.length === 0"
        class="rounded-xl border-2 border-dashed border-slate-200 py-16 text-center text-slate-400"
      >
        <p class="text-lg font-medium">No approvers yet</p>
        <p class="text-sm">Add the first approval step below.</p>
      </div>

      <!-- ─── Add Step Section ─────────────────────────────────── -->
      <div class="mt-6">
        <div v-if="!showAddStep">
          <button
            class="flex w-full items-center justify-center gap-2 rounded-xl border-2 border-dashed border-slate-300 py-4 text-sm font-medium text-slate-500 hover:border-blue-400 hover:text-blue-600 transition"
            @click="showAddStep = true"
          >
            <svg
              class="h-5 w-5"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M12 4v16m8-8H4"
              />
            </svg>
            Add Approval Step
          </button>
        </div>

        <div v-else class="rounded-xl border border-blue-200 bg-blue-50 p-5">
          <p class="text-sm font-semibold text-blue-800 mb-3">
            New Step {{ steps.length + 1 }}
          </p>

          <!-- Type selector -->
          <div class="mb-3 flex gap-3">
            <label class="flex items-center gap-2 text-sm cursor-pointer">
              <input
                v-model="newStepType"
                type="radio"
                value="M"
                class="accent-blue-600"
              />
              M (select employee)
            </label>
            <label class="flex items-center gap-2 text-sm cursor-pointer">
              <input
                v-model="newStepType"
                type="radio"
                value="IM"
                class="accent-purple-600"
              />
              IM (Immediate Head)
            </label>
          </div>

          <!-- Employee search (only for Main) -->
          <div v-if="newStepType === 'M'" class="relative mb-3">
            <input
              v-model="searchQuery"
              class="w-full rounded-md border border-blue-200 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400"
              placeholder="Search employee by name, ID or email…"
              @input="onSearchInput"
              @focus="activeSearchTarget = { stepIdx: -1 }"
            />
            <div
              v-if="isSearching"
              class="absolute right-3 top-2.5 text-slate-400 text-xs"
            >
              Searching…
            </div>
            <div
              v-if="searchResults.length && !newStepApprover"
              class="absolute z-30 mt-1 w-full rounded-lg border border-slate-200 bg-white shadow-lg max-h-48 overflow-y-auto"
            >
              <button
                v-for="emp in searchResults"
                :key="emp.id"
                class="w-full text-left px-3 py-2 text-sm hover:bg-blue-50"
                @click="selectEmployee(emp)"
              >
                <span class="font-medium">{{ emp.name }}</span>
                <span class="ml-2 text-xs text-slate-400"
                  >{{ emp.id }} · {{ emp.email }}</span
                >
              </button>
            </div>
            <div v-if="newStepApprover" class="mt-1 text-xs text-blue-700">
              Selected: <strong>{{ newStepApprover.name }}</strong> ({{
                newStepApprover.email
              }})
              <button
                class="ml-2 text-red-500 hover:underline"
                @click="
                  newStepApprover = null;
                  searchQuery = '';
                "
              >
                clear
              </button>
            </div>
          </div>

          <div
            v-else-if="newStepType === 'IM'"
            class="mb-3 rounded-md bg-purple-50 border border-purple-200 px-3 py-2 text-sm text-purple-700"
          >
            The Immediate Head of the requester will be resolved automatically
            when a new request is submitted.
          </div>

          <div class="flex gap-2">
            <button
              class="rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 transition"
              @click="addStep"
            >
              Add Step
            </button>
            <button
              class="rounded-md border border-slate-200 bg-white px-4 py-2 text-sm font-medium text-slate-600 hover:bg-slate-50 transition"
              @click="
                showAddStep = false;
                newStepApprover = null;
                searchQuery = '';
              "
            >
              Cancel
            </button>
          </div>
        </div>
      </div>

      <!-- ─── How It Works box ─────────────────────────────────── -->
      <details class="mt-6 rounded-xl border border-slate-200 bg-white p-4">
        <summary class="cursor-pointer text-sm font-semibold text-slate-700">
          ℹ How approver limits work
        </summary>
        <div class="mt-3 space-y-2 text-sm text-slate-600">
          <p>
            <strong>Steps:</strong> Unlimited. Each step must be fully satisfied
            before the next is notified. A step is satisfied when at least one
            approver in it responds.
          </p>
          <p>
            <strong>Proxies per step:</strong> Unlimited. Proxies are fallbacks
            — if the main approver is unavailable, a proxy can approve instead.
            Only <em>one</em> person in the step needs to approve.
          </p>
          <p>
            <strong>IM (Immediate Head):</strong> A special type resolved
            dynamically from the requester's HR record at request submission
            time. You can only have one IM per policy.
          </p>
          <p>
            <strong>Drag to reorder:</strong> Grab the ⠿ handle on a step to
            change the sequence. Grab the smaller handle on a proxy to reorder
            proxies within a step. Click <em>Save Order</em> to persist changes
            to the database.
          </p>
        </div>
      </details>
    </div>
  </div>
</template>
