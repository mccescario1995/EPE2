<script setup lang="ts">
import { VueDraggable } from "vue-draggable-plus";

// ─── Props & Emits ────────────────────────────────────────────────────────────
const props = defineProps<{
  show: boolean;
  policy: {
    policyid: number;
    policyId: number;
    description: string;
    shortname?: string;
  } | null;
}>();

const emit = defineEmits<{
  (e: "close"): void;
  (e: "saved"): void;
}>();

// ─── Types ────────────────────────────────────────────────────────────────────
interface Approver {
  id: number;
  policyid: number;
  approverid: string | null;
  approvername: string | null;
  approveremail: string | null;
  approvertype: string | null;
  sort: number | null;
  linesequence: number | null;
  status: string;
  isdelete: number;
}

interface TreeNode {
  id: number; // keep numeric (no string parsing anymore 🔥)
  name: string;
  approver: Approver; // keep original data
  children: TreeNode[];
}

interface ApproverLists {
  main: Approver[];
  proxy: Approver[];
}

interface EmployeeOption {
  emplId: string;
  employeename2: string;
  emailadd: string;
}

// ─── State ───────────────────────────────────────────────────────────────────
const { showToast } = useToast();
const userStore = useUserStore();
const userId = userStore.profile;

const loading = ref(false);
const saving = ref(false);
const rawApprovers = ref<Approver[]>([]);
const groups = ref<TreeNode[]>([]);

const showAddForm = ref(false);
const addingAsProxy = ref<number | null>(null);
const addFormLoading = ref(false);

const newApprover = ref({
  approvertype: "M" as "M" | "IM",
  approverid: "",
  approvername: "",
  approveremail: "",
  searchQuery: "",
  selectedEmployee: null as EmployeeOption | null,
});

const employeeOptions = ref<EmployeeOption[]>([]);
const employeeSearchLoading = ref(false);
const showEmployeeDropdown = ref(false);

const isDeleteModalOpen = ref(false);
const deletingApprover = ref<Approver | null>(null);
const deleteLoading = ref(false);

// ─── Computed ─────────────────────────────────────────────────────────────────
const hasIM = computed(() =>
  groups.value.some((g) => g.approver.approvertype === "IM"),
);

// ─── Helpers ──────────────────────────────────────────────────────────────────
// function buildGroups(approvers: Approver[]): ApproverGroup[] {
//   const sorted = [...approvers].sort((a, b) => {
//     const sortA = a.sort ?? 0;
//     const sortB = b.sort ?? 0;
//     if (sortA !== sortB) return sortA - sortB;
//     return (a.linesequence ?? 0) - (b.linesequence ?? 0);
//   });

//   const groupMap = new Map<number, ApproverGroup>();

//   for (const approver of sorted) {
//     const sortKey = approver.sort ?? 0;
//     const isMain = approver.linesequence === 1;

//     if (isMain) {
//       groupMap.set(sortKey, {
//         _groupKey: `group-${approver.id}`,
//         sort: sortKey,
//         main: approver,
//         proxies: [],
//       });
//     } else {
//       const group = groupMap.get(sortKey);
//       if (group) {
//         group.proxies.push(approver);
//       }
//     }
//   }

//   return Array.from(groupMap.values());
// }
function buildGroups(approvers: Approver[]): TreeNode[] {
  const sorted = [...approvers].sort((a, b) => {
    const sortA = a.sort ?? 0;
    const sortB = b.sort ?? 0;
    if (sortA !== sortB) return sortA - sortB;
    return (a.linesequence ?? 0) - (b.linesequence ?? 0);
  });

  const map = new Map<number, TreeNode>();

  for (const a of sorted) {
    const sortKey = a.sort ?? 0;
    const isMain = a.linesequence === 1;

    if (isMain) {
      map.set(sortKey, {
        id: a.id,
        name: a.approvername ?? "",
        approver: a,
        children: [],
      });
    } else {
      const parent = map.get(sortKey);
      if (parent) {
        parent.children.push({
          id: a.id,
          name: a.approvername ?? "",
          approver: a,
          children: [],
        });
      }
    }
  }

  return Array.from(map.values());
}

function flattenGroups(tree: TreeNode[]): Approver[] {
  const result: Approver[] = [];

  tree.forEach((node, index) => {
    const sort = index + 1;

    // MAIN
    result.push({
      ...node.approver,
      sort,
      linesequence: 1,
    });

    // CHILDREN
    node.children.forEach((child, childIndex) => {
      result.push({
        ...child.approver,
        sort,
        linesequence: childIndex + 2,
      });
    });
  });

  return result;
}

// ─── Data Fetching ────────────────────────────────────────────────────────────
async function fetchApprovers() {
  if (!props.policy) return;
  loading.value = true;
  try {
    const response = await apiFetch(
      `/api/Maintenance/policy/${props.policy.policyId}/approvers`,
      { method: "Post", parseJson: true },
    );
    rawApprovers.value = response.items ?? [];
    console.log("Fetched approvers ManageApprovers:", rawApprovers.value);
    groups.value = buildGroups(rawApprovers.value);
    console.log("RAW:", rawApprovers.value);
    console.log("GROUPS:", groups.value);
  } catch (err) {
    console.error("Failed to fetch approvers ManageApprovers:", err);
    showToast({
      type: "error",
      title: "Error",
      message: "Failed to load approvers",
      duration: 4000,
    });
  } finally {
    loading.value = false;
  }
}

// ─── Employee Search ──────────────────────────────────────────────────────────
const searchDebounce = ref<ReturnType<typeof setTimeout> | null>(null);

function onEmployeeSearch(query: string) {
  newApprover.value.searchQuery = query;
  if (
    newApprover.value.selectedEmployee &&
    query !== newApprover.value.selectedEmployee.employeename2
  ) {
    newApprover.value.selectedEmployee = null;
    newApprover.value.approverid = "";
    newApprover.value.approvername = "";
    newApprover.value.approveremail = "";
  }
  if (searchDebounce.value) clearTimeout(searchDebounce.value);
  if (!query || query.length < 2) {
    employeeOptions.value = [];
    showEmployeeDropdown.value = false;
    return;
  }
  searchDebounce.value = setTimeout(() => searchEmployees(query), 300);
}

async function searchEmployees(query: string) {
  employeeSearchLoading.value = true;
  try {
    const response: any = await apiFetch(
      `/api/Maintenance/employees/search?q=${encodeURIComponent(query)}`,
      { method: "GET", parseJson: true },
    );
    const list = Array.isArray(response) ? response : response?.items || [];
    employeeOptions.value = list.map((e: any) => ({
      emplId: e.id,
      employeename2: e.name,
      emailadd: e.email,
    }));
    showEmployeeDropdown.value = employeeOptions.value.length > 0;
  } catch (err) {
    console.error(err);
    employeeOptions.value = [];
    showEmployeeDropdown.value = false;
  } finally {
    employeeSearchLoading.value = false;
  }
}

function selectEmployee(emp: EmployeeOption) {
  newApprover.value.selectedEmployee = emp;
  newApprover.value.approverid = emp.emplId;
  newApprover.value.approvername = emp.employeename2;
  newApprover.value.approveremail = emp.emailadd;
  newApprover.value.searchQuery = emp.employeename2;
  employeeOptions.value = [];
  showEmployeeDropdown.value = false;
}

// ─── Add Approver ─────────────────────────────────────────────────────────────
function openAddMain() {
  addingAsProxy.value = null;
  showAddForm.value = true;
  resetAddForm();
}

function openAddProxy(groupSort: number) {
  addingAsProxy.value = groupSort;
  showAddForm.value = true;
  resetAddForm();
}

function cancelAdd() {
  showAddForm.value = false;
  addingAsProxy.value = null;
  resetAddForm();
}

function resetAddForm() {
  newApprover.value = {
    approvertype: "M" as "M" | "IM",
    approverid: "",
    approvername: "",
    approveremail: "",
    searchQuery: "",
    selectedEmployee: null,
  };
  employeeOptions.value = [];
  showEmployeeDropdown.value = false;
}

async function confirmAddApprover() {
  if (!props.policy) return;
  const { approvertype, approverid, approvername, approveremail } =
    newApprover.value;

  if (approvertype !== "IM") {
    if (!approvername.trim() || !approveremail.trim()) {
      showToast({
        type: "warning",
        title: "Validation",
        message: "Please fill in approver name and email",
        duration: 3000,
      });
      return;
    }
  }

  addFormLoading.value = true;
  try {
    let sort: number;
    let linesequence: number;

    if (addingAsProxy.value !== null) {
      sort = addingAsProxy.value;
      const group = groups.value[addingAsProxy.value! - 1];
      linesequence = group.children.length + 2;
    } else {
      sort = groups.value.length + 1;
      linesequence = 1;
    }

    const payload = {
      policyid: props.policy.policyId,
      approverid: approvertype === "IM" ? null : approverid || null,
      approvername: approvertype === "IM" ? "Immediate Head" : approvername,
      approveremail: approvertype === "IM" ? null : approveremail,
      approvertype,
      sort,
      linesequence,
    };

    await apiFetch(
      `/api/Maintenance/policy/${props.policy.policyId}/approvers/create`,
      {
        method: "POST",
        body: JSON.stringify(payload),
        headers: { "Content-Type": "application/json" },
      },
    );

    showToast({
      type: "success",
      title: "Added",
      message: "Approver added successfully",
      duration: 3000,
    });
    cancelAdd();
    await fetchApprovers();
  } catch (err) {
    console.error(
      "Failed to add approver ManageApprovers                                                                                                                                                                                                                                                                                                 :",
      err,
    );
    showToast({
      type: "error",
      title: "Error",
      message: "Failed to add approver",
      duration: 4000,
    });
  } finally {
    addFormLoading.value = false;
  }
}

// ─── Delete Approver ──────────────────────────────────────────────────────────
function confirmDelete(approver: Approver) {
  deletingApprover.value = approver;
}

function cancelDelete() {
  deletingApprover.value = null;
}

async function executeDelete() {
  if (!deletingApprover.value || !props.policy) return;
  deleteLoading.value = true;
  try {
    await apiFetch(
      `/api/Maintenance/policy/${props.policy.policyId}/approvers/delete/${deletingApprover.value.id}`,
      { method: "POST", parseJson: true },
    );
    showToast({
      type: "success",
      title: "Deleted",
      message: "Approver removed",
      duration: 3000,
    });
    deletingApprover.value = null;
    await fetchApprovers();
  } catch {
    showToast({
      type: "error",
      title: "Error",
      message: "Failed to delete approver",
      duration: 4000,
    });
  } finally {
    deleteLoading.value = false;
  }
}

// ─── Drag & Drop Handlers ─────────────────────────────────────────────────────

// ─── Save ─────────────────────────────────────────────────────────────────────
async function saveOrder() {
  if (!props.policy) return;
  saving.value = true;
  try {
    const flatApprovers = flattenGroups(groups.value);
    await apiFetch(
      `/api/Maintenance/policy/${props.policy.policyId}/approvers/reorder`,

      {
        method: "POST",
        body: JSON.stringify(
          flatApprovers.map((a) => ({
            id: a.id,
            sort: a.sort,
            linesequence: a.linesequence,
          })),
        ),
        headers: { "Content-Type": "application/json" },
      },
    );

    showToast({
      type: "success",
      title: "Saved",
      message: "Approver order saved successfully",
      duration: 3000,
    });
    emit("saved");
    handleClose();
  } catch (err) {
    console.error("Failed to save order:", err);
    showToast({
      type: "error",
      title: "Error",
      message: "Failed to save approver order",
      duration: 4000,
    });
  } finally {
    saving.value = false;
  }
}

// ─── Close ────────────────────────────────────────────────────────────────────
function handleClose() {
  cancelAdd();
  deletingApprover.value = null;
  groups.value = [];
  rawApprovers.value = [];
  emit("close");
}

// ─── Watchers ─────────────────────────────────────────────────────────────────
watch(
  () => props.show,
  (newVal) => {
    if (newVal && props.policy) fetchApprovers();
  },
);

watch(
  () => props.policy,
  (val) => {
    if (val && props.show) fetchApprovers();
  },
);

watch(
  () => deletingApprover.value,
  (newVal) => {
    isDeleteModalOpen.value = newVal !== null;
  },
  { immediate: true },
);
</script>

<template>
  <Modal
    v-model:isOpen="props.show"
    size="lg"
    position="top-center"
    :show-close-button="false"
  >
    <template #header>
      <div
        class="flex items-start justify-between border-b border-gray-100 bg-gray-50"
      >
        <div>
          <h2 class="text-lg font-semibold text-gray-900">Manage Approvers</h2>
          <p class="text-sm text-gray-500 mt-0.5">
            {{ policy?.description }}
            <span
              v-if="policy?.shortname"
              class="ml-1 text-xs bg-gray-200 text-gray-600 px-1.5 py-0.5 rounded"
              >{{ policy.shortname }}</span
            >
          </p>
        </div>
      </div>
    </template>

    <div class="space-y-4">
      <!-- Loading -->
      <div v-if="loading" class="flex items-center justify-center py-16">
        <svg
          class="animate-spin h-7 w-7 text-blue-500"
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
        <span class="ml-2 text-sm text-gray-500">Loading approvers...</span>
      </div>

      <!-- Empty state (no groups, no add form) -->
      <div
        v-else-if="!loading && groups.length === 0 && !showAddForm"
        class="flex flex-col items-center justify-center py-14 text-center"
      >
        <div
          class="w-14 h-14 bg-blue-50 rounded-full flex items-center justify-center mb-3"
        >
          <svg
            class="w-7 h-7 text-blue-400"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="1.5"
              d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z"
            />
          </svg>
        </div>
        <p class="text-sm font-medium text-gray-700">No approvers yet</p>
        <p class="text-xs text-gray-400 mt-1">
          Add the first approver for this policy
        </p>
        <button
          class="mt-4 w-full flex items-center justify-center gap-2 py-2.5 px-4 border-2 border-dashed border-gray-200 rounded-xl text-sm text-gray-400 hover:border-blue-300 hover:text-blue-500 hover:bg-blue-50 transition-all"
          @click="openAddMain"
        >
          <svg
            class="w-4 h-4"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M12 4v16m8-8H4"
            />
          </svg>
          Add Approver Step
        </button>
      </div>

      <!-- ─── Approver Groups (Draggable) ─────────────────────────── -->
      <div v-else-if="!loading">
        <!-- Drag hint -->
        <div class="flex items-center gap-2 text-xs text-gray-400 mb-3">
          <!-- <svg
            class="w-3.5 h-3.5"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M7 16V4m0 0L3 8m4-4l4 4M17 8v12m0 0l4-4m-4 4l-4-4"
            />
          </svg> -->
          Drag <span class="font-bold px-0.5">⠿</span> handles to reorder approvers and their proxies
        </div>

        <!-- Testing groups -->
        <!-- <div v-for="g in groups" :key="g.id">
          {{ g.name }}
        </div> -->

        <!-- Top-level draggable: reorder groups -->
        <VueDraggable
          v-model="groups"
          :animation="150"
          ghost-class="ghost"
          :fallbackOnBody="true"
          class="flex flex-col gap-2"
        >
          <div
            v-for="element in groups"
            :key="element.id"
            class="bg-white border rounded p-3"
          >
            <!-- MAIN -->
            <div class="font-medium text-gray-900 flex items-center gap-2">
              <div
                class="group-drag-handle cursor-grab active:cursor-grabbing text-gray-300 hover:text-gray-500 select-none px-0.5 text-lg leading-none flex-shrink-0"
                title="Drag to reorder step"
              >
                ⠿
              </div>
              {{ element.name }}
              <button
                class="ml-auto text-red-500 hover:text-red-700 transition-colors"
                title="Delete approver"
                @click="confirmDelete(element.approver)"
              >
                <svg
                  class="w-4 h-4"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                  />
                </svg>
              </button>
            </div>

            <!-- CHILDREN DRAG -->
            <VueDraggable
              v-model="element.children"
              :animation="150"
              group="nested"
              class="ml-4 mt-2 flex flex-col gap-2"
            >
              <div
                v-for="child in element.children"
                :key="child.id"
                class="bg-gray-100 p-2 rounded flex items-center gap-2 text-sm text-gray-700"
              >
                <div
                  class="group-drag-handle cursor-grab active:cursor-grabbing text-gray-300 hover:text-gray-500 select-none px-0.5 text-lg leading-none flex-shrink-0"
                  title="Drag to reorder step"
                >
                  ⠿
                </div>
                {{ child.name }}
                <button
                  class="ml-auto text-red-400 hover:text-red-600 transition-colors"
                  title="Delete approver"
                  @click="confirmDelete(child.approver)"
                >
                  <svg
                    class="w-3.5 h-3.5"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                    />
                  </svg>
                </button>
              </div>
            </VueDraggable>
          </div>
        </VueDraggable>

        <!-- ─── Inline Add Form (for new main approver) ──────────────── -->
        <div
          v-if="showAddForm && addingAsProxy === null"
          class="mt-3 border-2 border-blue-200 border-dashed rounded-xl p-4 bg-blue-50"
        >
          <p
            class="text-xs font-semibold text-blue-700 mb-3 uppercase tracking-wide"
          >
            New Approver — Step {{ groups.length + 1 }}
          </p>

          <!-- Type selector -->
          <div class="flex gap-2 mb-3">
            <button
              class="flex-1 py-2 px-3 rounded-lg text-sm font-medium border-2 transition-all"
              :class="
                newApprover.approvertype === 'M'
                  ? 'border-blue-500 bg-blue-500 text-white'
                  : 'border-gray-200 bg-white text-gray-600 hover:border-gray-300'
              "
              @click="newApprover.approvertype = 'M'"
            >
              Main
            </button>
            <button
              class="flex-1 py-2 px-3 rounded-lg text-sm font-medium border-2 transition-all"
              :class="[
                newApprover.approvertype === 'IM'
                  ? 'border-purple-500 bg-purple-500 text-white'
                  : 'border-gray-200 bg-white text-gray-600 hover:border-gray-300',
                hasIM ? 'opacity-50 cursor-not-allowed' : '',
              ]"
              :disabled="hasIM"
              :title="
                hasIM ? 'Only one Immediate Manager approver allowed' : ''
              "
              @click="!hasIM && (newApprover.approvertype = 'IM')"
            >
              Immediate Manager
            </button>
          </div>

          <!-- IM info -->
          <div
            v-if="newApprover.approvertype === 'IM'"
            class="flex items-start gap-2 p-3 bg-purple-50 rounded-lg mb-3 text-xs text-purple-700"
          >
            <svg
              class="w-4 h-4 mt-0.5 flex-shrink-0"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
            The immediate head of each requester will be automatically resolved
            when a request is submitted.
          </div>

          <!-- Main fields -->
          <div v-if="newApprover.approvertype === 'M'" class="space-y-2 mb-3">
            <div class="relative">
              <input
                type="text"
                :value="newApprover.searchQuery"
                class="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-300 focus:border-blue-400 bg-white"
                placeholder="Search employee name..."
                @input="
                  (e) => onEmployeeSearch((e.target as HTMLInputElement).value)
                "
                @focus="showEmployeeDropdown = employeeOptions.length > 0"
                @blur="setTimeout(() => (showEmployeeDropdown = false), 200)"
              />
              <div
                v-if="showEmployeeDropdown"
                class="absolute z-20 top-full left-0 right-0 mt-1 bg-white border border-gray-200 rounded-lg shadow-lg max-h-48 overflow-y-auto"
              >
                <div
                  v-if="employeeSearchLoading"
                  class="px-3 py-2 text-xs text-gray-400"
                >
                  Searching...
                </div>
                <div
                  v-else-if="employeeOptions.length === 0"
                  class="px-3 py-2 text-xs text-gray-400"
                >
                  No results found
                </div>
                <button
                  v-for="emp in employeeOptions"
                  :key="emp.emplId"
                  class="w-full text-left px-3 py-2 text-sm hover:bg-blue-50 transition-colors"
                  @mousedown.prevent="selectEmployee(emp)"
                >
                  <div class="font-medium text-gray-900">
                    {{ emp.employeename2 }}
                  </div>
                  <div class="text-xs text-gray-400">{{ emp.emailadd }}</div>
                </button>
              </div>
            </div>

            <!-- Selected employee confirmation -->
            <div
              v-if="newApprover.selectedEmployee"
              class="flex items-center gap-2 px-3 py-2 bg-green-50 border border-green-200 rounded-lg text-sm"
            >
              <svg
                class="w-4 h-4 text-green-500"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M5 13l4 4L19 7"
                />
              </svg>
              <span class="text-green-700 text-xs">{{
                newApprover.approveremail
              }}</span>
              <button
                class="ml-auto text-gray-400 hover:text-gray-600"
                @click="
                  newApprover.selectedEmployee = null;
                  newApprover.searchQuery = '';
                  newApprover.approverid = '';
                  newApprover.approvername = '';
                  newApprover.approveremail = '';
                "
              >
                <svg
                  class="w-3.5 h-3.5"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
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
          </div>

          <div class="flex gap-2">
            <button
              class="flex-1 py-2 px-4 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 disabled:opacity-50 transition-colors flex items-center justify-center gap-1.5"
              :disabled="addFormLoading"
              @click="confirmAddApprover"
            >
              <svg
                v-if="addFormLoading"
                class="animate-spin w-4 h-4"
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
              <span>{{ addFormLoading ? "Adding..." : "Add Approver" }}</span>
            </button>
            <button
              class="px-4 py-2 text-sm text-gray-600 bg-white border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
              @click="cancelAdd"
            >
              Cancel
            </button>
          </div>
        </div>

        <!-- Add main approver button -->
        <button
          v-if="!showAddForm"
          class="mt-3 w-full flex items-center justify-center gap-2 py-2.5 px-4 border-2 border-dashed border-blue-200 rounded-xl text-sm text-blue-400 hover:border-blue-300 hover:text-blue-600 hover:bg-blue-100 transition-all"
          @click="openAddMain"
        >
          <svg
            class="w-4 h-4"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M12 4v16m8-8H4"
            />
          </svg>
          Add Approver Step
        </button>
      </div>
    </div>

    <template #footer>
      <div class="flex items-center justify-between">
        <!-- <p class="text-xs text-gray-400">
          {{ groups.length }} Main{{ groups.length !== 1 ? "s" : "" }} ·
          {{ groups.reduce((sum, g) => sum + g.proxies.length, 0) }}
          {{ groups.reduce((sum, g) => sum + g.proxies.length, 0) === 1 ? "Proxy" : "Proxies" }}
        </p> -->
        <div class="flex gap-3 ms-2">
          <button
            class="px-4 py-2 text-sm text-gray-600 bg-white border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
            :disabled="saving"
            @click="handleClose"
          >
            Cancel
          </button>
          <button
            class="px-5 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-700 disabled:opacity-50 transition-colors flex items-center gap-2"
            :disabled="saving || loading"
            @click="saveOrder"
          >
            <svg
              v-if="saving"
              class="animate-spin w-4 h-4"
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
            {{ saving ? "Saving..." : "Save Order" }}
          </button>
        </div>
      </div>
    </template>
  </Modal>

  <!-- ─── Delete Confirmation Modal ────────────────────────────────────── -->
  <Modal
    v-model:isOpen="isDeleteModalOpen"
    size="sm"
    position="center"
    :closeOnOverlayClick="true"
    :closeOnEsc="true"
    @update:isOpen="cancelDelete"
  >
    <div
      class="flex items-center justify-center w-12 h-12 mx-auto bg-red-100 rounded-full mb-4"
    >
      <svg
        class="w-6 h-6 text-red-600"
        fill="none"
        viewBox="0 0 24 24"
        stroke="currentColor"
      >
        <path
          stroke-linecap="round"
          stroke-linejoin="round"
          stroke-width="2"
          d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
        />
      </svg>
    </div>
    <h3 class="text-base font-semibold text-gray-900 text-center mb-1">
      Remove Approver
    </h3>
    <p class="text-sm text-gray-500 text-center mb-5">
      Remove
      <strong>{{
        deletingApprover?.approvertype === "IM"
          ? "Immediate Manager"
          : deletingApprover?.approvername
      }}</strong>
      from this policy? This cannot be undone.
    </p>
    <div class="flex gap-3">
      <button
        class="flex-1 py-2 px-4 text-sm text-gray-600 bg-gray-100 rounded-lg hover:bg-gray-200 transition-colors"
        :disabled="deleteLoading"
        @click="cancelDelete"
      >
        Cancel
      </button>
      <button
        class="flex-1 py-2 px-4 text-sm font-medium text-white bg-red-600 rounded-lg hover:bg-red-700 disabled:opacity-50 transition-colors"
        :disabled="deleteLoading"
        @click="executeDelete"
      >
        {{ deleteLoading ? "Removing..." : "Remove" }}
      </button>
    </div>
  </Modal>
</template>
