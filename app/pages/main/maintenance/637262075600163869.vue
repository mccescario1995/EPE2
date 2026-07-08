<script setup lang="ts">
definePageMeta({
  middleware: "auth",
  // alias: "/637262075600163869",
  // name: "Policies",
  // path: "/maintenance/policies/index"
});

const router = useRouter();
import { IconView, IconEdit, IconDelete, IconUserApprover } from "#components";
import ManageApprovers from "~/components/ManageApprovers.vue";
const { breadcrumbs, currentMenuLabel } = useBreadcrumbs();

// Get the current page's menu name (should be "Policy")
const currentPageName = breadcrumbs.value[breadcrumbs.value.length - 1]?.label;
console.log("Current page Name: ", currentPageName);

type PolicyApiItem = {
  policyid: string;
  description: string;
  status: string;
  shortname: boolean;
  createddate: string;
  managerApprover?: string;
};

const userStore = useUserStore();

const userId = userStore.profile;

const loading = ref(false);
const policyData: any = ref(null);

const policyDataLength = computed(() => policyData.value?.length ?? 0);
const fetchPolicyData = async (params: {
  page: number;
  limit: number;
  search: string;
  filters: Record<string, string>;
  sortField?: string;
  sortDirection?: "asc" | "desc";
}) => {
  loading.value = true;
  const response = await apiFetch("/api/Maintenance/policy", {
    method: "POST",
    body: JSON.stringify({
      UserId: parseInt(userId.emplId),
      page: params.page,
      pageSize: params.limit,
      search: params.search,
      sortField: params.sortField,
      sortDirection: params.sortDirection,
    }),
    headers: { "Content-Type": "application/json" },
    parseJson: true,
  });
  console.log("Policies:", response.items);

  loading.value = false;

  return {
    data: response.items ?? [],
    total: response.totalCount ?? 0,
  };
};
const showDeleteConfirm = ref(false);
const deletingItem = ref<any>(null);
const deleteLoading = ref(false);

// Modal states
const createModalOpen = ref(false);
const editModalOpen = ref(false);
const viewModalOpen = ref(false);

// Selected policy for modals
const selectedPolicy = ref<any>(null);

// Create form state
const createForm = reactive({
  userId: userId.userId,
  description: "",
  shortname: "",
  status: "A",
  managerApprover: "",
});
const createLoading = ref(false);
const createError = ref("");

// Edit form state
const editForm = reactive({
  userid: userId.userId,
  policyid: "",
  description: "",
  shortname: "",
  status: "A",
  managerApprover: "",
});
const editLoading = ref(false);
const editSubmitting = ref(false);
const editError = ref("");

// Manager Approver state
const managerApproverEmployeeOptions = ref<
  Array<{ id: string; label: string; email: string }>
>([]);
const managerApproverSelectedEmployee = ref<{
  id: string;
  label: string;
  email: string;
} | null>(null);
const managerApproverSearchDebounce = ref<ReturnType<typeof setTimeout> | null>(
  null,
);

// Edit form manager approver state
const editManagerApproverEmployeeOptions = ref<
  Array<{ id: string; label: string; email: string }>
>([]);
const editManagerApproverSelectedEmployee = ref<{
  id: string;
  label: string;
  email: string;
} | null>(null);
const editManagerApproverSearchDebounce = ref<ReturnType<
  typeof setTimeout
> | null>(null);

// Approvers state
// Approvers managed by ManageApprovers component
const approversLoading = ref(false);
const approversError = ref("");
const newApprover = reactive({
  approverid: null,
  name: "",
  email: "",
  type: "",
  sequence: 1,
});
const addingApprover = ref(false);

const createApproverCount = ref(0);

const isCreateFormValid = computed(() => {
  return (
    createForm.shortname.trim().length > 0 &&
    createForm.description.trim().length > 0 &&
    createApproverCount.value > 0
  );
});

// ── Computed Validation ────────────────────────────────────────────
// const isCreateFormValid = computed(() => {
//   const hasShortname = createForm.shortname.trim().length > 0;
//   const hasDescription = createForm.description.trim().length > 0;
//   // Check approvers from the ManageApprovers inline component
//   const hasApprovers =
//     createManageApproversRef.value?.getPendingApprovers?.()?.length > 0;
//   return hasShortname && hasDescription && hasApprovers;
// });

const isEditFormValid = computed(() => {
  const hasShortname = editForm.shortname.trim().length > 0;
  const hasDescription = editForm.description.trim().length > 0;
  const hasApprovers = approvers.value.length > 0;
  return hasShortname && hasDescription && hasApprovers;
});

const handleCreatePolicy = () => {
  createModalOpen.value = true;
  managerApproverSelectedEmployee.value = null;
  managerApproverEmployeeOptions.value = [];
  createdPolicyId.value = null;
};

const searchManagerApproverEmployee = (query: string) => {
  if (managerApproverSearchDebounce.value)
    clearTimeout(managerApproverSearchDebounce.value);
  if (!query || query.length < 2) {
    managerApproverEmployeeOptions.value = [];
    return;
  }
  managerApproverSearchDebounce.value = setTimeout(async () => {
    try {
      const response: any = await apiFetch(
        `/api/Maintenance/employees/search?q=${encodeURIComponent(query)}`,
        { method: "GET", parseJson: true },
      );
      managerApproverEmployeeOptions.value = Array.isArray(response)
        ? response.map((e: any) => ({
            id: e.id,
            label: e.label,
            email: e.email,
          }))
        : [];
    } catch (err) {
      managerApproverEmployeeOptions.value = [];
    }
  }, 300);
};

watch(
  () => managerApproverSelectedEmployee.value,
  (newVal) => {
    if (newVal) {
      createForm.managerApprover = newVal.label;
    }
  },
);

const searchEditManagerApproverEmployee = (query: string) => {
  if (editManagerApproverSearchDebounce.value)
    clearTimeout(editManagerApproverSearchDebounce.value);
  if (!query || query.length < 2) {
    editManagerApproverEmployeeOptions.value = [];
    return;
  }
  editManagerApproverSearchDebounce.value = setTimeout(async () => {
    try {
      const response: any = await apiFetch(
        `/api/Maintenance/employees/search?q=${encodeURIComponent(query)}`,
        { method: "GET", parseJson: true },
      );
      editManagerApproverEmployeeOptions.value = Array.isArray(response)
        ? response.map((e: any) => ({
            id: e.id,
            label: e.label,
            email: e.email,
          }))
        : [];
    } catch (err) {
      editManagerApproverEmployeeOptions.value = [];
    }
  }, 300);
};

watch(
  () => editManagerApproverSelectedEmployee.value,
  (newVal) => {
    if (newVal) {
      editForm.managerApprover = newVal.label;
    }
  },
);

const viewPolicy = async (row: any) => {
  selectedPolicy.value = row;
  await fetchViewApprovers(row.policyid);
  viewModalOpen.value = true;
};

const editPolicy = async (row: any) => {
  selectedPolicy.value = row;
  await fetchEditPolicy(row.policyid);
  if (editForm.managerApprover && editManagerApproverSelectedEmployee.value) {
    editManagerApproverEmployeeOptions.value = [
      editManagerApproverSelectedEmployee.value,
    ];
  }
  await fetchApproversLocal(row.policyid);
  editModalOpen.value = true;
};

const approvers = ref<any[]>([]);

const fetchApproversLocal = async (policyid: string) => {
  approversLoading.value = true;
  try {
    const response = await apiFetch(
      `/api/Maintenance/policy/${policyid}/approvers`,
      {
        method: "POST",
        body: JSON.stringify({ UserId: userId.userId }),
        headers: { "Content-Type": "application/json" },
        parseJson: true,
      },
    );
    approvers.value = (response.items || []).map((item: any) => ({
      id: item.id || item.approverid,
      policyid: policyid,
      approverid: item.approverid || "",
      approvername: item.approvername || "",
      approveremail: item.approveremail || "",
      approvertype: item.approvertype || "",
      sort: item.sort || 0,
      linesequence: item.linesequence || 0,
      status: item.status || "",
      isdelete: item.isdelete || 0,
    }));
  } finally {
    approversLoading.value = false;
  }
};

// const manageApprovers = async (row: any) => {
//   selectedPolicy.value = row;
//   await fetchApprovers(row.policyid);
//   approversModalOpen.value = true;
// };

const confirmDeletePolicy = (row: any) => {
  deletingItem.value = row;
  showDeleteConfirm.value = true;
};

const cancelDeletePolicy = () => {
  showDeleteConfirm.value = false;
  deletingItem.value = null;
};

const fetchPolicies = async () => {
  loading.value = true;
  try {
    const response = await apiFetch("/api/Maintenance/policy", {
      method: "GET",
      parseJson: true,
    });

    if (response) {
      console.log("Fetched policies:", response);
      // policyData.value = response.items || [];
      policyData.value = (response.items || []).map((p: PolicyApiItem) => ({
        policyid: p.policyid,
        description: p.description,
        status: p.status,
        shortname: p.shortname,
        createddate: p.createddate,
        managerApprover: p.managerApprover,
      }));
    } else {
      console.error("Failed to fetch policies:", response.message);
    }
  } catch (error) {
    console.error("Error fetching policies:", error);
  } finally {
    loading.value = false;
  }
};

const deleteRequestPolicy = async () => {
  if (!deletingItem.value) return;

  deleteLoading.value = true;

  try {
    const payload = {
      userId: userId.userId,
    };

    const response = await apiFetch(
      `/api/Maintenance/policy/delete/${deletingItem.value.policyid}`,
      {
        method: "POST",
        body: JSON.stringify(payload),
        headers: {
          "Content-Type": "application/json",
        },
        // parseJson: true,
      },
    );

    console.log("Delete response:", response);

    if (response) {
      showDeleteConfirm.value = false;
      deletingItem.value = null;
      // Refresh the list after successful delete

      showToast({
        title: "Request Deleted",
        message: "The policy has been successfully deleted.",
        type: "error",
        duration: 5000,
      });

      tableRef.value?.refresh();
    } else {
      console.error("Delete failed:", response.message);
    }
  } catch (error) {
    console.error("Error deleting policy:", error);
  } finally {
    deleteLoading.value = false;
  }
};

const submitCreateForm = async () => {
  createLoading.value = true;
  createError.value = "";
  try {
    const response: any = await apiFetch("/api/Maintenance/policy/create", {
      method: "POST",
      body: JSON.stringify(createForm),
      headers: {
        "Content-Type": "application/json",
      },
      parseJson: true,
    });
    if (response) {
      // Backend returns { success, message, data: policy }
      const policyid =
        response.data?.policyid ??
        response.data?.Policyid ??
        response.items?.policyid ??
        response.policyid;

      console.log("response policyid", policyid);

      createdPolicyId.value = policyid;

      if (!policyid) {
        console.error("Could not extract policyid from response:", response);
      }

      if (createManageApproversRef.value && policyid) {
        await createManageApproversRef.value.createApprovers(
          policyid,
          userId.userId,
        );
      }

      createModalOpen.value = false;
      // Reset form
      createForm.description = "";
      createForm.shortname = "";
      createForm.status = "A";
      createForm.managerApprover = "";
      createdPolicyId.value = null;

      tableRef.value?.refresh();

      showToast({
        title: "Policy Created",
        message: "The policy has been successfully created.",
        type: "success",
        duration: 5000,
      });
    } else {
      createError.value = response.message || "Failed to create policy";
    }
  } catch (err) {
    createError.value = "An error occurred";
    console.error(err);
  } finally {
    createLoading.value = false;
  }
};

const fetchEditPolicy = async (policyid: string) => {
  editLoading.value = true;
  editError.value = "";
  try {
    const response: any = await apiFetch(
      `/api/Maintenance/policy/${policyid}`,
      {
        method: "GET",
        parseJson: true,
      },
    );
    console.log("Fetched policy for edit:", response);
    if (response) {
      editForm.policyid = response.items.policyid || "";
      editForm.description = response.items.description || "";
      editForm.shortname = response.items.shortname || "-";
      editForm.status = response.items.status || "A";
      editForm.managerApprover = response.items.managerApprover || "";
      if (response.items.managerApprover) {
        editManagerApproverSelectedEmployee.value = {
          id: response.items.managerApproverId || "",
          label: response.items.managerApprover,
          email: response.items.managerApproverEmail || "",
        };
      }
    } else {
      editError.value = "Policy not found";
    }
  } catch (err) {
    editError.value = "Failed to load policy";
    console.error(err);
  } finally {
    editLoading.value = false;
  }
};

const submitEditForm = async () => {
  if (!selectedPolicy.value) return;

  editSubmitting.value = true;
  editError.value = "";
  try {
    const response: any = await apiFetch(
      `/api/Maintenance/policy/update/${selectedPolicy.value.policyid}`,
      {
        method: "POST",
        body: JSON.stringify(editForm),
        headers: {
          "Content-Type": "application/json",
        },
        // parseJson: true,
      },
    );
    console.log("Edit response:", response);
    if (response) {
      if (manageApproversRef.value) {
        await manageApproversRef.value.saveOrder();
      }
      editModalOpen.value = false;
      selectedPolicy.value = null;
      // Refresh the list
      // await fetchPolicies();
      tableRef.value?.refresh();
      showToast({
        title: "Success",
        message: "The policy successfully updated.",
        type: "success",
        duration: 5000,
      });
    } else {
      editError.value = response.message || "Failed to update policy";
    }
  } catch (err) {
    editError.value = "An error occurred";
    console.error(err);
  } finally {
    editSubmitting.value = false;
  }
};

const fetchViewApprovers = async (policyid: any) => {
  approversLoading.value = true;
  approversError.value = "";
  try {
    console.log("Fetching approvers for policy:", policyid);
    const response = await apiFetch(
      `/api/Maintenance/policy/${policyid}/approvers`,
      {
        method: "POST",
        body: JSON.stringify({ UserId: userId.userId }),
        headers: {
          "Content-Type": "application/json",
        },
        parseJson: true,
      },
    );
    console.log("Fetched approver item:", response.items);
    if (response) {
      approvers.value = (response.items || []).map((item: any) => ({
        id: item.id || item.approverid,
        approverid: item.approverid || "",
        name: item.approvername || "",
        email: item.approveremail || "",
        type: item.approvertype || "",
        sequence: item.sort || 0,
        linesequence: item.linesequence || 0,
      }));
    }
  } catch (err) {
    approversError.value = "Failed to load approvers";
    console.error(err);
  } finally {
    approversLoading.value = false;
  }
};

// Computed property to group main approvers with their proxies
const groupedApprovers = computed(() => {
  const mainApprovers: any[] = [];
  const proxiesMap: Map<number, any[]> = new Map();

  // Sort approvers by sequence and linesequence
  const sorted = [...approvers.value].sort((a, b) => {
    const sortA = a.sequence ?? 0;
    const sortB = b.sequence ?? 0;
    if (sortA !== sortB) return sortA - sortB;
    return (a.linesequence ?? 0) - (b.linesequence ?? 0);
  });

  sorted.forEach((approver) => {
    const isMain = approver.linesequence === 1;
    if (isMain) {
      mainApprovers.push(approver);
      proxiesMap.set(approver.sequence, []);
    } else {
      const mainApprover = mainApprovers.find(
        (m) => m.sequence === approver.sequence,
      );
      if (mainApprover) {
        proxiesMap.get(mainApprover.sequence)?.push(approver);
      }
    }
  });

  return { mainApprovers, proxiesMap };
});

const getProxies = (sequence: number): any[] => {
  return groupedApprovers.value.proxiesMap.get(sequence) || [];
};

const fetchApprovers = async (policyid: any) => {
  approversLoading.value = true;
  approversError.value = "";
  try {
    console.log("Fetching approvers for policy:", policyid);
    const response = await apiFetch(
      `/api/Maintenance/policy/${policyid}/approvers`,
      {
        method: "POST",
        body: JSON.stringify({ UserId: userId.userId }),
        headers: {
          "Content-Type": "application/json",
        },
        parseJson: true,
      },
    );
    console.log("Fetched approver item:", response.items);
    if (response) {
      approvers.value = (response.items || []).map((item: any) => ({
        id: item.id || item.approverid,
        approverid: item.approverid || "",
        name: item.approvername || "",
        email: item.approveremail || "",
        type: item.approvertype || "",
        sequence: item.sort || 0,
      }));
    }
  } catch (err) {
    approversError.value = "Failed to load approvers";
    console.error(err);
  } finally {
    approversLoading.value = false;
  }
};

const deleteApprover = async (approverId: string) => {
  if (!selectedPolicy.value) return;

  try {
    const response: any = await apiFetch(
      `/api/Policy/${selectedPolicy.value.policyid}/approvers/${approverId}`,
      {
        method: "DELETE",
        parseJson: true,
      },
    );
    if (response) {
      await fetchApprovers(selectedPolicy.value.policyid);
      showToast({
        title: "Approver Deleted",
        message: "The approver has been successfully removed.",
        type: "success",
        duration: 5000,
      });
    }
  } catch (err) {
    approversError.value = "Failed to delete approver";
    console.error(err);
  }
};

onMounted(() => {
  fetchPolicies();
});

const tableRef = ref();

const reloadTable = () => {
  tableRef.value?.refresh();
};

const manageApproversRef = ref();

// Created policy tracking for approver management
const createdPolicyId = ref<string | null>(null);
const createManageApproversRef = ref();
</script>

<template>
  <div
    class="w-min-full min-h-[50vh] bg-gray-50 overflow-auto h-100vh dark:bg-gray-900 dark:text-gray-200 dark:border-gray-3000"
  >
    <header class="bg-white dark:bg-gray-800 shadow">
      <div
        class="me-auto py-4 px-4 sm:px-6 lg:px-8 flex justify-between items-center"
      >
        <div class="flex items-center">
          <Button
            variant="ghost-primary"
            size="lg"
            icon="left_arrow"
            iconPosition="center"
            customClass="rounded-full"
            class="dark:bg-gray-700 dark:text-gray-200 dark:hover:bg-gray-600"
            @onclick="$router.back()"
          />
          <h1 class="text-2xl ps-5 font-bold text-gray-900 dark:text-gray-200">
            {{ currentMenuLabel || currentPageName  || "Policies"}}
          </h1>
        </div>

        <Button
          variant="primary"
          size="md"
          icon="add"
          iconPosition="left"
          @onclick="handleCreatePolicy"
        >
          Create Policy
        </Button>
      </div>
    </header>
    <main class="max-w-full mx-auto py-6 sm:px-6">
      <!-- Policy Requests List -->
      <div class="mb-6 px-2 rounded-lg">
        <base-table
          ref="tableRef"
          :server-pagination="true"
          :fetch-data="fetchPolicyData"
          :pagination="true"
          :per-page="5"
          :show-search="true"
          :showlimitfilter="true"
          :limitfilter="[5, 10, 20, 30, 40, 50]"
          :sortable-columns="[
            'policyid',
            'shortname',
            'status',
            'description',
            'createddate',
          ]"
          :filterable-columns="['status']"
          :header="[
            'policyid',
            'shortname',
            'status',
            'description',
            'createddate',
          ]"
          :badge-fields="['status']"
          :field-mappings="{
            status: {
              A: 'Active',
              I: 'Inactive',
            },
          }"
          :field-colors="{
            status: {
              A: 'bg-green-100 text-green-800 border border-green-900  w-full',
              I: 'bg-red-50 text-red-800 border border-red-900 w-full',
            },
          }"
          :date-fields="['createdDate']"
          :actions="[
            {
              label: IconView,
              tooltip: 'View Details',
              handler: viewPolicy,
              iconClass: 'text-indigo-500 dark:text-indigo-400',
              buttonClass:
                'hover:bg-indigo-100 dark:hover:bg-indigo-100 hover:text-indigo-600 !dark:hover:text-indigo-900',
            },
            {
              label: IconEdit,
              tooltip: 'Edit Request',
              handler: editPolicy,
              iconClass: '!text-blue-500 dark:text-blue-400',
              buttonClass:
                'hover:bg-blue-100 dark:hover:bg-blue-100 hover:text-blue-600 dark:hover:text-blue-900',
            },
            {
              label: IconDelete,
              tooltip: 'Delete Request',
              handler: confirmDeletePolicy,
              iconClass: 'text-red-500 dark:text-red-400',
              buttonClass:
                'hover:bg-red-50 dark:hover:bg-red-100 hover:text-red-100 dark:hover:text-white-900',
            },
          ]"
        />
      </div>
    </main>

    <!-- {
              label: IconUserApprover,
              tooltip: 'Manage Approvers',
              handler: manageApprovers,
              iconClass: 'text-purple-500',
            }, -->

    <!-- Delete Confirmation Modal -->
    <Teleport to="body">
      <div v-if="showDeleteConfirm" class="fixed inset-0 z-50 overflow-y-auto">
        <div class="flex min-h-full items-center justify-center p-4">
          <!-- Backdrop -->
          <div
            class="fixed inset-0 bg-black opacity-50 transition-opacity"
            @click="cancelDeletePolicy"
          ></div>

          <!-- Modal Content -->
          <div
            class="relative bg-white dark:bg-gray-800 rounded-lg shadow-xl max-w-md w-full p-6 border border-gray-200 dark:border-gray-700"
          >
            <div
              class="flex items-center justify-center w-12 h-12 mx-auto bg-red-100 dark:bg-red-900/30 rounded-full mb-4"
            >
              <svg
                class="w-6 h-6 text-red-600 dark:text-red-400"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
                />
              </svg>
            </div>
            <h3
              class="text-lg font-medium text-gray-900 dark:text-gray-100 text-center mb-2"
            >
              Delete Request
            </h3>
            <p class="text-gray-500 dark:text-gray-400 text-center mb-6">
              Are you sure you want to delete this policy? This action cannot be
              undone.
            </p>
            <div class="flex gap-3 justify-center">
              <button
                @click="cancelDeletePolicy"
                class="px-4 py-2 bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-200 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors border border-gray-200 dark:border-gray-600"
                :disabled="deleteLoading"
              >
                Cancel
              </button>
              <button
                @click="deleteRequestPolicy"
                class="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors disabled:opacity-50"
                :disabled="deleteLoading"
              >
                <span v-if="deleteLoading">Deleting...</span>
                <span v-else>Delete</span>
              </button>
            </div>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Create Policy Modal create modal-->
    <Modal
      v-model:isOpen="createModalOpen"
      size="4xl"
      position="center"
      :closeOnOverlayClick="false"
      :closeOnEsc="false"
      class="dark:bg-0 "
    >
      <template #header>
        <h3
          class="text-lg font-medium leading-6 text-gray-900 dark:text-gray-100"
        >
          Create Policy
        </h3>
      </template>

      <div
        class="grid md:grid-cols-2 gap-6 bg-white dark:bg-gray-800 rounded-lg p-6 border border-gray-200 dark:border-0"
      >
        <!-- Policy Form -->
        <div
          class="sm:border-b-1 sm:pb-6 md:border-b-0 md:pb-0 md:border-r dark:md:border-0 md:border-gray-200"
        >
          <form @submit.prevent="submitCreateForm" class="space-y-4">
            <div>
              <label
                for="create-shortname"
                class="block text-sm font-medium text-gray-700 dark:text-gray-300"
              >
                Short Name <span class="text-red-500 text-md">*</span>
              </label>
              <BaseInput
                id="create-shortname"
                v-model="createForm.shortname"
                type="text"
                required
                class="mt-1 block w-full focus:outline-none focus:ring-blue-500 focus:border-blue-500 bg-white dark:bg-gray-700 border-gray-300 dark:border-gray-600 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500"
                :max-char-count="50"
                :min-char-count="1"
              />
            </div>

            <div>
              <label
                for="create-description"
                class="block text-sm font-medium text-gray-700 dark:text-gray-300"
              >
                Description <span class="text-red-500 text-md">*</span>
              </label>
              <BaseInput
                id="create-description"
                v-model="createForm.description"
                required
                class="mt-1 block w-full focus:outline-none focus:ring-blue-500 focus:border-blue-500 bg-white dark:bg-gray-700 border-gray-300 dark:border-gray-600 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500"
                rows="3"
                is-textarea
                :max-char-count="1000"
                :min-char-count="10"
              ></BaseInput>
            </div>

            <div>
              <label
                for="create-status"
                class="block text-sm font-medium text-gray-700 dark:text-gray-300"
              >
                Status <span class="text-red-500 text-md">*</span>
              </label>
              <select
                id="create-status"
                v-model="createForm.status"
                class="mt-1 block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
              >
                <option value="A">Active</option>
                <option value="I">Inactive</option>
              </select>
            </div>

            <div v-if="createError" class="text-red-500 text-sm">
              {{ createError }}
            </div>
          </form>
        </div>

        <!-- Approvers Management (in create mode) -->
        <div
          class="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-0 dark:border-gray-700"
        >
          <ManageApprovers
            ref="createManageApproversRef"
            :show="createModalOpen"
            :policy="{
              policyid: 'new',
              description: createForm.description,
              shortName: createForm.shortname,
            }"
            inline
            :create-mode="true"
            @approver-count-change="(count) => (createApproverCount = count)"
          />
        </div>
      </div>

      <template #footer>
        <div
          class="flex justify-end space-x-2 bg-white dark:bg-gray-800 border-t border-gray-200 dark:border-0 p-4 rounded-b-lg"
        >
          <Button
            variant="ghost"
            @click="createModalOpen = false"
            :disabled="createLoading"
            class="dark:hover:bg-gray-700 dark:text-gray-200"
          >
            Cancel
          </Button>
          <Button
            variant="primary"
            @click="submitCreateForm"
            :disabled="createLoading || !isCreateFormValid"
          >
            {{ createLoading ? "Creating..." : "Create Policy" }}
          </Button>
        </div>
      </template>
    </Modal>

    <!-- Edit Policy Modal edit modal-->
    <Modal
      v-model:isOpen="editModalOpen"
      size="4xl"
      position="center"
      :closeOnOverlayClick="false"
      :closeOnEsc="false"
      class="dark:bg-gray-900"
    >
      <template #header>
        <h3
          class="text-lg font-medium leading-6 text-gray-900 dark:text-gray-100"
        >
          Edit Policy {{ editForm.policyid }}
        </h3>
      </template>
      <div
        class="grid grid-cols-2 gap-6 bg-white dark:bg-gray-800 rounded-lg p-6 border border-gray-200 dark:border-0"
      >
        <div
          v-if="editLoading"
          class="text-center py-4 text-gray-500 dark:text-gray-400"
        >
          Loading...
        </div>

        <form v-else @submit.prevent="submitEditForm" class="space-y-4">
          <div>
            <label
              for="edit-shortname"
              class="block text-sm font-medium text-gray-700 dark:text-gray-300"
            >
              Shortname
            </label>
            <input
              id="edit-shortname"
              v-model="editForm.shortname"
              type="text"
              required
              class="mt-1 block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
            />
          </div>

          <div>
            <label
              for="edit-description"
              class="block text-sm font-medium text-gray-700 dark:text-gray-300"
            >
              Description
            </label>
            <textarea
              id="edit-description"
              v-model="editForm.description"
              required
              class="mt-1 block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
              rows="3"
            ></textarea>
          </div>

          <div>
            <label
              for="edit-status"
              class="block text-sm font-medium text-gray-700 dark:text-gray-300"
            >
              Status
            </label>
            <select
              id="edit-status"
              v-model="editForm.status"
              class="mt-1 block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
            >
              <option value="A">Active</option>
              <option value="I">Inactive</option>
            </select>
          </div>

          <div v-if="editError" class="text-red-500 text-sm">
            {{ editError }}
          </div>
        </form>

        <div
          class="border-t border-l md:border-l-0 md:border-t-0 pt-4 md:pt-0 md:border-l-gray-200 dark:md:border-l-gray-700 md:border-t-gray-200 dark:md:border-t-gray-700"
        >
          <ManageApprovers
            ref="manageApproversRef"
            :key="selectedPolicy?.policyid"
            :show="editModalOpen"
            :policy="selectedPolicy"
            :initial-approvers="approvers"
            inline
            @saved="fetchApproversLocal(selectedPolicy?.policyid)"
          />
        </div>
      </div>

      <template #footer>
        <div
          class="flex justify-end space-x-2 bg-white dark:bg-gray-800 border-t border-gray-200 dark:border-0 dark:border-gray-700 p-4 rounded-b-lg"
        >
          <Button
            variant="ghost"
            @onclick="editModalOpen = false"
            :disabled="editSubmitting"
            class="dark:hover:bg-gray-700 dark:text-gray-200"
          >
            Cancel
          </Button>
          <Button
            variant="primary"
            @onclick="submitEditForm"
            :disabled="editSubmitting || editLoading || !isEditFormValid"
          >
            {{ editSubmitting ? "Updating..." : "Update Policy" }}
          </Button>
        </div>
      </template>
    </Modal>

    <!-- View Policy Modal -->
    <Modal
      v-model:isOpen="viewModalOpen"
      size="4xl"
      position="center"
      :closeOnOverlayClick="false"
      :closeOnEsc="false"
      class="dark:bg-gray-900"
    >
      <template #header>
        <div
          class="flex items-start justify-between border-b border-gray-100 dark:border-gray-700 bg-gray-50 dark:bg-gray-800"
        >
          <div>
            <h2 class="text-lg font-semibold text-gray-900 dark:text-gray-100">
              View Policy
            </h2>
            <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">
              {{ selectedPolicy?.shortname }}
              <span
                v-if="selectedPolicy.status === 'A'"
                class="inline-flex items-center px-2.5 py-0.5 rounded text-xs font-medium bg-green-100 text-green-800 border border-green-200 dark:bg-green-900/20 dark:text-green-300 dark:border-green-800"
              >
                Active
              </span>
              <span
                v-else
                class="inline-flex items-center px-2.5 py-0.5 rounded text-xs font-medium bg-red-100 text-red-800 border border-red-200 dark:bg-red-900/20 dark:text-red-300 dark:border-red-800"
              >
                Inactive
              </span>
            </p>
          </div>
        </div>
      </template>

      <div
        class="grid grid-cols-2 gap-6 bg-white dark:bg-gray-800 rounded-lg p-6 border border-gray-200 dark:border-gray-700"
      >
        <div v-if="selectedPolicy" class="grid grid-cols-2 gap-4">
          <div class="col-span-full">
            <div class="">
              <label
                class="text-sm font-medium text-gray-700 dark:text-gray-300"
              >
                Policy ID
              </label>
              <p
                class="mt-1 px-3 py-2 bg-gray-50 dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-md text-sm"
              >
                {{ selectedPolicy.policyid }}
              </p>
            </div>
            <div>
              <label
                class="text-sm font-medium text-gray-700 dark:text-gray-300"
              >
                Created Date
              </label>
              <p
                class="mt-1 px-3 py-2 bg-gray-50 dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-md text-sm"
              >
                {{ new Date(selectedPolicy.createddate).toLocaleString() }}
              </p>
            </div>
          </div>
          <div class="col-span-full">
            <div class="col-span-full">
              <label
                class="text-sm font-medium text-gray-700 dark:text-gray-300"
              >
                Description
              </label>
              <base-input
                isTextarea
                :class="'bg-gray-50 dark:bg-gray-700 border border-gray-300 dark:border-gray-600'"
                disabled
                v-model="selectedPolicy.description"
              >
              </base-input>
            </div>
          </div>
        </div>

        <div>
          <div
            class="flex items-start justify-between border-b border-gray-100 dark:border-gray-700 py-3"
          >
            <div>
              <h3
                class="text-md font-semibold text-gray-900 dark:text-gray-100"
              >
                Approvers
              </h3>
              <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                Approver list for this policy
              </p>
            </div>
          </div>

          <div
            v-if="approversLoading"
            class="flex items-center justify-center py-16"
          >
            <svg
              class="animate-spin h-7 w-7 text-blue-500 dark:text-blue-400"
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
            <span class="ml-2 text-sm text-gray-500 dark:text-gray-400"
              >Loading approvers...</span
            >
          </div>

          <div
            v-else-if="!approversLoading && approvers.length === 0"
            class="flex flex-col items-center justify-center py-14 text-center"
          >
            <div
              class="w-14 h-14 bg-blue-50 dark:bg-gray-800/20 rounded-full flex items-center justify-center mb-3"
            >
              <svg
                class="w-7 h-7 text-blue-400 dark:text-blue-300"
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
            <p class="text-sm font-medium text-gray-700 dark:text-gray-300">
              No approvers yet
            </p>
            <p class="text-xs text-gray-400 dark:text-gray-500 mt-1">
              No approvers configured for this policy
            </p>
          </div>
          <div v-else class="space-y-3">
            <div
              v-for="(mainApprover, index) in groupedApprovers.mainApprovers"
              :key="mainApprover.approverid"
              class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-600 rounded p-3"
            >
              <div
                class="font-medium text-gray-900 dark:text-gray-100 flex items-center gap-2"
              >
                <span
                  class="rounded-full bg-blue-200 dark:bg-blue-800 h-7 w-7 flex justify-center items-center text-xs font-semibold"
                >
                  {{ index + 1 }}
                </span>
                <div>
                  <div class="text-sm w-full">{{ mainApprover.name }}</div>

                  <div class="text-xs text-gray-500 dark:text-gray-400">
                    {{ mainApprover.email }}
                  </div>
                </div>
              </div>

              <!-- Proxy approvers -->
              <div
                v-if="getProxies(mainApprover.sequence).length > 0"
                class="ml-6 mt-2 space-y-1"
              >
                <div
                  v-for="proxy in getProxies(mainApprover.sequence)"
                  :key="proxy.approverid"
                  class="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400"
                >
                  <span
                    class="rounded-full bg-orange-100 dark:bg-orange-600 dark:text-gray-300 h-5 w-5 flex justify-center items-center text-xs font-semibold"
                  >
                    P
                  </span>
                  <div>
                    <div>
                      {{ proxy.name }}
                    </div>
                    <div class="text-xs text-gray-400 dark:text-gray-500">
                      {{ proxy.email }}
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <template #footer>
        <div
          class="flex justify-end bg-white dark:bg-gray-800 border-t border-gray-200 dark:border-gray-700 p-4 rounded-b-lg"
        >
          <Button variant="ghost" @click="viewModalOpen = false">
            Close
          </Button>
        </div>
      </template>
    </Modal>

    <!-- <ManageApprovers
      :show="approversModalOpen"
      :policy="selectedPolicy"
      @close="approversModalOpen = false"
      @saved="fetchPolicies"
    /> -->
  </div>
</template>
