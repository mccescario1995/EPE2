<script setup lang="ts">
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

interface ApproverGroup {
  _groupKey: string; // unique key for drag
  sort: number;
  main: Approver;
  proxies: Approver[];
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
const groups = ref<ApproverGroup[]>([]);

// Add approver form state
const showAddForm = ref(false);
const addingAsProxy = ref<number | null>(null); // sort value of the group we're adding proxy to
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

// Delete confirmation
const isDeleteModalOpen = ref(false);
const deletingApprover = ref<Approver | null>(null);
const deleteLoading = ref(false);

// ─── Computed ─────────────────────────────────────────────────────────────────
const hasIM = computed(() =>
  groups.value.some((g) => g.main.approvertype === "IM"),
);

// ─── Helpers ──────────────────────────────────────────────────────────────────

/** Build groups from flat approver list */
function buildGroups(approvers: Approver[]): ApproverGroup[] {
  const sorted = [...approvers].sort((a, b) => {
    const sortA = a.sort ?? 0;
    const sortB = b.sort ?? 0;
    if (sortA !== sortB) return sortA - sortB;
    return (a.linesequence ?? 0) - (b.linesequence ?? 0);
  });

  const groupMap = new Map<number, ApproverGroup>();

  for (const approver of sorted) {
    const sortKey = approver.sort ?? 0;
    const isMain = approver.linesequence === 1;

    if (isMain) {
      groupMap.set(sortKey, {
        _groupKey: `group-${approver.id}`, // Updated: Use stable approver.id instead of sortKey
        sort: sortKey,
        main: approver,
        proxies: [],
      });
    } else {
      const group = groupMap.get(sortKey);
      if (group) {
        group.proxies.push(approver);
      }
    }
  }

  return Array.from(groupMap.values());
}

/** Flatten groups back to approver list with recalculated sort/linesequence */
function flattenGroups(grps: ApproverGroup[]): Approver[] {
  const result: Approver[] = [];
  grps.forEach((group, gIndex) => {
    const newSort = gIndex + 1;
    result.push({ ...group.main, sort: newSort, linesequence: 1 });
    group.proxies.forEach((proxy, pIndex) => {
      result.push({ ...proxy, sort: newSort, linesequence: pIndex + 2 });
    });
  });
  return result;
}

// ─── Data Fetching ────────────────────────────────────────────────────────────
async function fetchApprovers() {
  if (!props.policy) return;
  loading.value = true;

  const payload = {
    userId: userId.userId,
  };
  try {
    console.log(
      "Fetching approvers(fetchApprovers) for policy:",
      props.policy.policyId,
    );
    const response = await apiFetch(
      `/api/Maintenance/policy/${props.policy.policyId}/approvers`,
      {
        method: "Post",
        parseJson: true,
      },
    );
    rawApprovers.value = response.items ?? [];
    groups.value = buildGroups(rawApprovers.value);
  } catch (err) {
    console.error("Failed to fetch approvers:", err);
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

  // ONLY clear selected if user edits after selection
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

  employeeOptions.value = []; // clear list
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

  // Validation
  if (approvertype === "IM") {
    // IM type — no manual fields needed
  } else {
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
      // Adding as proxy to an existing group
      sort = addingAsProxy.value;
      const group = groups.value.find((g) => g.sort === sort);
      linesequence = (group?.proxies.length ?? 0) + 2;
    } else {
      // Adding as new main approver (new group)
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

    console.log("Adding approver for policy:", props.policy.policyId);

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
      message: `Approver added successfully`,
      duration: 3000,
    });

    cancelAdd();
    await fetchApprovers();
  } catch (err) {
    console.error("Failed to add approver:", err);
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
    console.log("Policy to delete from:", props.policy.policyId);
    console.log("Deleting approver with ID:", deletingApprover.value.id);
    const response = await apiFetch(
      `/api/Maintenance/policy/${props.policy.policyId}/approvers/delete/${deletingApprover.value.id}`,
      {
        method: "POST",
        parseJson: true,
      },
    );

    console.log("Delete response:", response);

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

/** Called after top-level group drag ends → recalculate all sort values */
function onGroupDragEnd() {
  groups.value = groups.value.map((g, index) => ({
    ...g,
    sort: index + 1,
    main: { ...g.main, sort: index + 1 },
    proxies: g.proxies.map((p) => ({ ...p, sort: index + 1 })),
  }));
}

/** Called after proxy drag ends within a group */
function onProxyDragEnd(groupIndex: number) {
  const group = groups.value[groupIndex];
  group.proxies = group.proxies.map((p, index) => ({
    ...p,
    linesequence: index + 2, // main is 1, proxies start at 2
  }));
}

/** Move group up in the order */
function moveGroupUp(index: number) {
  if (index > 0) {
    const temp = groups.value[index];
    groups.value[index] = groups.value[index - 1];
    groups.value[index - 1] = temp;
    onGroupDragEnd();
  }
}

/** Move group down in the order */
function moveGroupDown(index: number) {
  if (index < groups.value.length - 1) {
    const temp = groups.value[index];
    groups.value[index] = groups.value[index + 1];
    groups.value[index + 1] = temp;
    onGroupDragEnd();
  }
}

// ─── Save ─────────────────────────────────────────────────────────────────────
async function saveOrder() {
  if (!props.policy) return;
  saving.value = true;
  try {
    const flatApprovers = flattenGroups(groups.value);

    await apiFetch(
      `/api/Maintenance/policy/${props.policy.policyid}/approvers/reorder`,
      {
        method: "PUT",
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
    if (newVal && props.policy) {
      fetchApprovers();
    }
  },
);

watch(
  () => props.policy,
  (val) => {
    if (val && props.show) {
      console.log("Policy changed, refetching approvers...", props.show);
      fetchApprovers();
    }
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
        <!-- <button
          class="p-1.5 rounded-lg text-gray-400 hover:text-gray-600 hover:bg-gray-200 transition-colors"
          @click="handleClose"
        >
          <svg
            class="w-5 h-5"
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
        </button> -->
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

      <!-- Empty state -->
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
          class="w-full flex items-center justify-center gap-2 py-2.5 px-4 border-2 border-dashed border-gray-200 rounded-xl text-sm text-gray-400 hover:border-blue-300 hover:text-blue-500 hover:bg-blue-50 transition-all"
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
        <!-- Info hint -->
        <div class="flex items-center gap-2 text-xs text-gray-400 mb-3">
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
              d="M7 16V4m0 0L3 8m4-4l4 4M17 8v12m0 0l4-4m-4 4l-4-4"
            />
          </svg>
          Click arrow buttons to reorder steps
        </div>

        <!-- Groups container -->
        <div class="space-y-3">
          <div
            v-for="(group, gIndex) in groups"
            :key="group._groupKey"
            class="border border-gray-200 rounded-xl overflow-hidden bg-white shadow-sm"
          >
            <!-- Main Approver Row -->
            <div class="flex items-center gap-3 px-4 py-3 bg-gray-50">
              <div class="flex items-center gap-1 flex-shrink-0">
                <!-- Move up -->
                <button
                  class="p-1 rounded-full text-gray-400 hover:text-gray-600 hover:bg-gray-200 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                  title="Move up"
                  :disabled="gIndex === 0"
                  @click="moveGroupUp(gIndex)"
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
                      d="M5 15l7-7 7 7"
                    />
                  </svg>
                </button>

                <!-- Move down -->
                <button
                  class="p-1 rounded-full text-gray-400 hover:text-gray-600 hover:bg-gray-200 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                  title="Move down"
                  :disabled="gIndex === groups.length - 1"
                  @click="moveGroupDown(gIndex)"
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
                      d="M19 9l-7 7-7-7"
                    />
                  </svg>
                </button>
              </div>

              <!-- Step number badge -->
              <span
                class="flex-shrink-0 w-6 h-6 rounded-full bg-blue-100 text-blue-700 text-xs font-semibold flex items-center justify-center"
              >
                {{ gIndex + 1 }}
              </span>

              <!-- Approver info -->
              <div class="flex-1 min-w-0">
                <div class="flex items-center gap-2 flex-wrap">
                  <span class="text-sm font-medium text-gray-900 truncate">
                    {{
                      group.main.approvertype === "IM"
                        ? "Immediate Head (Dynamic)"
                        : (group.main.approvername ?? "—")
                    }}
                  </span>
                  <span
                    class="text-xs px-2 py-0.5 rounded-full font-medium"
                    :class="
                      group.main.approvertype === 'IM'
                        ? 'bg-purple-100 text-purple-700'
                        : 'bg-blue-100 text-blue-700'
                    "
                  >
                    {{
                      group.main.approvertype === "IM"
                        ? "Immediate Manager"
                        : "Main"
                    }}
                  </span>
                </div>
                <p
                  v-if="group.main.approvertype !== 'IM'"
                  class="text-xs text-gray-400 truncate mt-0.5"
                >
                  {{ group.main.approveremail ?? "No email" }}
                </p>
                <p v-else class="text-xs text-gray-400 mt-0.5">
                  Resolved at submission time
                </p>
              </div>

              <!-- Actions -->
              <div class="flex items-center gap-1 flex-shrink-0">
                <!-- Move up -->
                <!-- <button
                  class="p-1.5 rounded-full text-gray-400 hover:text-gray-600 hover:bg-gray-200 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                  title="Move up"
                  :disabled="gIndex === 0"
                  @click="moveGroupUp(gIndex)"
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
                      d="M5 15l7-7 7 7"
                    />
                  </svg>
                </button> -->

                <!-- Move down -->
                <!-- <button
                  class="p-1.5 rounded-full text-gray-400 hover:text-gray-600 hover:bg-gray-200 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                  title="Move down"
                  :disabled="gIndex === groups.length - 1"
                  @click="moveGroupDown(gIndex)"
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
                      d="M19 9l-7 7-7-7"
                    />
                  </svg>
                </button> -->

                <!-- Delete -->
                <button
                  class="p-1.5 rounded-full text-red-400 hover:text-white hover:bg-red-500 transition-colors"
                  title="Remove approver"
                  @click="confirmDelete(group.main)"
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
            </div>

            <!-- Proxy rows (inner draggable) -->
            <div>
              <div
                v-for="proxy in group.proxies"
                :key="proxy.id"
                class="flex items-center gap-3 px-4 py-2.5 pl-30 bg-white hover:bg-gray-50 transition-colors"
              >
                <!-- Proxy drag handle -->
                <!-- <button
                  class="proxy-drag-handle cursor-grab active:cursor-grabbing text-gray-200 hover:text-gray-400 transition-colors flex-shrink-0"
                  title="Drag to reorder proxy"
                >
                  <svg
                    class="w-3.5 h-3.5"
                    fill="currentColor"
                    viewBox="0 0 20 20"
                  >
                    <path
                      d="M7 2a2 2 0 110 4 2 2 0 010-4zm6 0a2 2 0 110 4 2 2 0 010-4zM7 8a2 2 0 110 4 2 2 0 010-4zm6 0a2 2 0 110 4 2 2 0 010-4zm-6 6a2 2 0 110 4 2 2 0 010-4zm6 0a2 2 0 110 4 2 2 0 010-4z"
                    />
                  </svg>
                </button> -->

                <!-- Proxy bullet -->
                <!-- <span
                  class="w-1.5 h-1.5 rounded-full bg-orange-400 flex-shrink-0"
                /> -->

                <div class="flex-1 min-w-0">
                  <div class="flex items-center gap-2">
                    <span class="text-sm text-gray-700 truncate">
                      {{ proxy.approvername ?? "—" }}
                    </span>
                    <span
                      class="text-xs px-1.5 py-0.5 rounded-full bg-orange-100 text-orange-700 font-medium"
                    >
                      Proxy
                    </span>
                  </div>
                  <p class="text-xs text-gray-400 truncate">
                    {{ proxy.approveremail ?? "" }}
                  </p>

                  <button
                    v-if="group.main.approvertype !== 'IM'"
                    class="mt-4 p-1.5 border border-orange-500 rounded-lg text-orange-500 hover:text-white hover:bg-orange-400 transition-colors text-xs flex items-center gap-1"
                    title="Add proxy approver"
                    @click="openAddProxy(group.sort)"
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
                        d="M12 4v16m8-8H4"
                      />
                    </svg>
                    <span class="text-xs hidden sm:inline">Add Proxy</span>
                  </button>
                </div>

                <button
                  class="p-1.5 rounded-full text-red-400 hover:text-white hover:bg-red-500 transition-colors flex-shrink-0"
                  @click="confirmDelete(proxy)"
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
            </div>

            <!-- Inline proxy add form -->
            <div
              v-if="showAddForm && addingAsProxy === group.sort"
              class="px-4 py-3 pl-12 bg-blue-50 border-t border-blue-100"
            >
              <p class="text-xs font-medium text-orange-500 mb-2">
                Add Proxy Approver
              </p>
              <!-- Employee search -->
              <div class="relative mb-2">
                <input
                  v-model="newApprover.searchQuery"
                  type="text"
                  class="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-300 focus:border-blue-400 bg-white"
                  placeholder="Search employee name..."
                  @input="onEmployeeSearch(newApprover.searchQuery)"
                />
                <div
                  v-if="showEmployeeDropdown"
                  class="absolute z-200 top-full left-0 right-0 mt-1 bg-white border border-gray-200 rounded-lg shadow-lg max-h-48 overflow-y-auto"
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
                    @click="selectEmployee(emp)"
                  >
                    <div class="font-medium text-gray-900">
                      {{ emp.employeename2 }}
                    </div>
                    <div class="text-xs text-gray-400">
                      {{ emp.emailadd }}
                    </div>
                  </button>
                </div>
              </div>

              <!-- Email display -->
              <div
                v-if="newApprover.selectedEmployee"
                class="flex items-center gap-2 px-3 py-2 bg-green-50 border border-green-200 rounded-lg text-sm mb-2"
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

              <!-- Form actions -->
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
                  <span>{{ addFormLoading ? "Adding..." : "Add" }}</span>
                </button>
                <button
                  class="px-4 py-2 text-sm text-gray-600 bg-white border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
                  @click="cancelAdd"
                >
                  Cancel
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- ─── Inline Add Form (for new main approver) ──────────────── -->
      <div
        v-if="showAddForm && addingAsProxy === null"
        class="border-2 border-blue-200 border-dashed rounded-xl p-4 bg-blue-50"
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
            :title="hasIM ? 'Only one Immediate Manager approver allowed' : ''"
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
          <!-- Employee search -->
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
            <!-- <template>
              <Dropdown
                v-if="showEmployeeDropdown"
                :options="employeeOptions"
                :loading="employeeSearchLoading"
                no-results-text="No results found"
                @select="selectEmployee"
              />
            </template> -->
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
                <div class="text-xs text-gray-400">
                  {{ emp.emailadd }}
                </div>
              </button>
            </div>
          </div>

          <!-- Main approver email  -->
          <!-- <Dropdown
            v-if="!newApprover.selectedEmployee"
            v-model="newApprover.approveremail"
            type="email"
            class="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-300 focus:border-blue-400 bg-white"
            :placeholder="
              newApprover.selectedEmployee
                ? newApprover.approveremail
                : 'Enter approver email (optional)'
            "
            @input="onEmployeeSearch($event.target.value)"
          />
          <div
            v-else
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
        </div> -->

          <!-- Form actions -->
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
          class="w-full flex items-center justify-center gap-2 py-2.5 px-4 border-2 border-dashed border-gray-200 rounded-xl text-sm text-gray-400 hover:border-blue-300 hover:text-blue-500 hover:bg-blue-50 transition-all"
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
        <p class="text-xs text-gray-400">
          {{ groups.length }} Main{{ groups.length !== 1 ? "s" : "" }} ·
          {{ groups.reduce((sum, g) => sum + g.proxies.length, 0) }}
          {{
            groups.reduce((sum, g) => sum + g.proxies.length, 0) === 1
              ? "Proxy"
              : "Proxies"
          }}
        </p>
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
  <!-- Nested Modal 1 End -->

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

<style scoped>
.modal-fade-enter-active,
.modal-fade-leave-active {
  transition: opacity 0.2s ease;
}
.modal-fade-enter-from,
.modal-fade-leave-to {
  opacity: 0;
}
.modal-fade-enter-active .relative,
.modal-fade-leave-active .relative {
  transition:
    transform 0.2s ease,
    opacity 0.2s ease;
}
.modal-fade-enter-from .relative,
.modal-fade-leave-to .relative {
  transform: scale(0.96) translateY(8px);
  opacity: 0;
}
</style>
