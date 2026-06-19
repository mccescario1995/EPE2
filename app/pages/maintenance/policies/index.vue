<script setup lang="ts">
definePageMeta({
  middleware: "auth",
  // alias: "/637262075600163869",                                                                                                                                                                                                                                                                                                                                                                                                                
  name: "Policies",
  // path: "/maintenance/policies/index"
})

const router = useRouter();
import { IconView, IconEdit, IconDelete, IconUserApprover } from "#components";

type PolicyApiItem = {
  policyid: string;
  description: string;
  status: string;
  shortname: boolean;
  createddate: string;
};

const userStore = useUserStore();

const userId = userStore.profile;

const loading = ref(false);
const policyData :any = ref(null);

const policyDataLength = computed(() => policyData.value?.length ?? 0);
const fetchPolicyData =  async (params: {
  page: number;
  limit: number;
  search: string;
  filters: Record<string, string>;
  sortField?: string;
  sortDirection?: 'asc' | 'desc';
}) => {
  loading.value = true;
  const response = await apiFetch('/api/Maintenance/policy', {
    method: 'POST',
    body: JSON.stringify({
      UserId: parseInt(userId.emplId),
      page: params.page,
      pageSize: params.limit,
      search: params.search,
      sortField: params.sortField,
      sortDirection: params.sortDirection,
    }),
    headers: { 'Content-Type': 'application/json' },
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
const approversModalOpen = ref(false);

// Selected policy for modals
const selectedPolicy = ref<any>(null);

// Create form state
const createForm = reactive({
  userId: userId.userId,
  description: "",
  shortname: "",
  status: "A",
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
});
const editLoading = ref(false);
const editSubmitting = ref(false);
const editError = ref("");

// Approvers state
// Approvers managed by ManageApprovers component
const approvers: any = ref([]);
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

const handleCreatePolicy = () => {
  createModalOpen.value = true;
};

const viewPolicy = (row: any) => {
  selectedPolicy.value = row;
  viewModalOpen.value = true;
};

const editPolicy = async (row: any) => {
  selectedPolicy.value = row;
  await fetchEditPolicy(row.policyid);
  editModalOpen.value = true;
};

const manageApprovers = async (row: any) => {
  selectedPolicy.value = row;
  await fetchApprovers(row.policyid);
  approversModalOpen.value = true;
};

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
      createModalOpen.value = false;
      // Reset form
      createForm.description = "";
      createForm.shortname = "";
      createForm.status = "A";

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
      editModalOpen.value = false;
      selectedPolicy.value = null;
      // Refresh the list
      // await fetchPolicies();
      tableRef.value?.refresh();
      showToast({
        title: "Policy Updated",
        message: "The policy has been successfully updated.",
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

const fetchApprovers = async (policyid:any) => {
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
      approvers.value = response.items || [];
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
</script>

<template>
  <div class="min-h-[50vh] bg-gray-50 overflow-auto h-100vh">
    <header class="bg-white shadow">
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
            @onclick="$router.back()"
          />
          <h1 class="text-2xl ps-5 font-bold text-gray-900">Policies</h1>
        </div>

        <Button
          variant="primary"
          size="md"
          icon="add"
          iconPosition="left"
          @onclick="handleCreatePolicy"
        >
          Create Request
        </Button>
      </div>
    </header>
    <main class="max-w-7xl mx-auto py-6 sm:px-6">
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
              iconClass: 'text-indigo-500',
            },
            {
              label: IconEdit,
              tooltip: 'Edit Request',
              handler: editPolicy,
              iconClass: 'text-blue-500',
            },
            {
              label: IconUserApprover,
              tooltip: 'Manage Approvers',
              handler: manageApprovers,
              iconClass: 'text-purple-500',
            },
            {
              label: IconDelete,
              tooltip: 'Delete Request',
              handler: confirmDeletePolicy,
              iconClass: 'text-red-500',
            },
          ]"
        />
      </div>
    </main>

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
            class="relative bg-white rounded-lg shadow-xl max-w-md w-full p-6"
          >
            <div
              class="flex items-center justify-center w-12 h-12 mx-auto bg-red-100 rounded-full mb-4"
            >
              <svg
                class="w-6 h-6 text-red-600"
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
            <h3 class="text-lg font-medium text-gray-900 text-center mb-2">
              Delete Request
            </h3>
            <p class="text-gray-500 text-center mb-6">
              Are you sure you want to delete this policy? This action cannot be
              undone.
            </p>
            <div class="flex gap-3 justify-center">
              <button
                @click="cancelDeletePolicy"
                class="px-4 py-2 bg-gray-200 text-gray-700 rounded-lg hover:bg-gray-300 transition-colors"
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

    <!-- Create Policy Modal -->
    <Modal
      v-model:isOpen="createModalOpen"
      size="lg"
      position="center"
      :closeOnOverlayClick="true"
      :closeOnEsc="true"
    >
      <template #header>
        <h3 class="text-lg font-medium leading-6 text-gray-900">
          Create Policy
        </h3>
      </template>

      <form @submit.prevent="submitCreateForm" class="space-y-4">
        <div>
          <label
            for="create-shortname"
            class="block text-sm font-medium text-gray-700"
          >
            Shortname
          </label>
          <input
            id="create-shortname"
            v-model="createForm.shortname"
            type="text"
            required
            class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
          />
        </div>

        <div>
          <label
            for="create-description"
            class="block text-sm font-medium text-gray-700"
          >
            Description <span class="text-red-500 text-md">*</span>
          </label>
          <textarea
            id="create-description"
            v-model="createForm.description"
            required
            class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            rows="3"
          ></textarea>
        </div>

        <div>
          <label
            for="create-status"
            class="block text-sm font-medium text-gray-700"
          >
            Status <span class="text-red-500 text-md">*</span>
          </label>
          <select
            id="create-status"
            v-model="createForm.status"
            class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
          >
            <option value="A">Active</option>
            <option value="I">Inactive</option>
          </select>
        </div>

        <div v-if="createError" class="text-red-500 text-sm">
          {{ createError }}
        </div>
      </form>

      <template #footer>
        <div class="flex justify-end space-x-2">
          <Button
            variant="ghost"
            @onclick="createModalOpen = false"
            :disabled="createLoading"
          >
            Cancel
          </Button>
          <Button
            variant="primary"
            @onclick="submitCreateForm"
            :disabled="createLoading"
          >
            {{ createLoading ? "Creating..." : "Create Policy" }}
          </Button>
        </div>
      </template>
    </Modal>

    <!-- Edit Policy Modal -->
    <Modal
      v-model:isOpen="editModalOpen"
      size="lg"
      position="center"
      :closeOnOverlayClick="true"
      :closeOnEsc="true"
    >
      <template #header>
        <h3 class="text-lg font-medium leading-6 text-gray-900">
          Edit Policy {{ editForm.policyid }}
        </h3>
      </template>

      <div v-if="editLoading" class="text-center py-4">Loading...</div>

      <form v-else @submit.prevent="submitEditForm" class="space-y-4">
        <div>
          <label
            for="edit-shortname"
            class="block text-sm font-medium text-gray-700"
          >
            Shortname
          </label>
          <input
            id="edit-shortname"
            v-model="editForm.shortname"
            type="text"
            required
            class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
          />
        </div>

        <div>
          <label
            for="edit-description"
            class="block text-sm font-medium text-gray-700"
          >
            Description
          </label>
          <textarea
            id="edit-description"
            v-model="editForm.description"
            required
            class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            rows="3"
          ></textarea>
        </div>

        <div>
          <label
            for="edit-status"
            class="block text-sm font-medium text-gray-700"
          >
            Status
          </label>
          <select
            id="edit-status"
            v-model="editForm.status"
            class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
          >
            <option value="A">Active</option>
            <option value="I">Inactive</option>
          </select>
        </div>

        <div v-if="editError" class="text-red-500 text-sm">{{ editError }}</div>
      </form>

      <template #footer>
        <div class="flex justify-end space-x-2">
          <Button
            variant="ghost"
            @onclick="editModalOpen = false"
            :disabled="editSubmitting"
          >
            Cancel
          </Button>
          <Button
            variant="primary"
            @onclick="submitEditForm"
            :disabled="editSubmitting || editLoading"
          >
            {{ editSubmitting ? "Updating..." : "Update Policy" }}
          </Button>
        </div>
      </template>
    </Modal>

    <!-- View Policy Modal -->
    <Modal
      v-model:isOpen="viewModalOpen"
      size="lg"
      position="center"
      :closeOnOverlayClick="true"
      :closeOnEsc="true"
    >
      <template #header>
        <h3 class="text-lg font-medium leading-6 text-gray-900">View Policy</h3>
      </template>

      <div v-if="selectedPolicy" class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-gray-700">
            Policy ID
          </label>
          <p
            class="mt-1 px-3 py-2 bg-gray-50 border border-gray-300 rounded-md"
          >
            {{ selectedPolicy.policyid }}
          </p>
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700">
            Shortname
          </label>
          <p
            class="mt-1 px-3 py-2 bg-gray-50 border border-gray-300 rounded-md"
          >
            {{ selectedPolicy.shortname ?? "-" }}
          </p>
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700">
            Description
          </label>
          <p
            class="mt-1 px-3 py-2 bg-gray-50 border border-gray-300 rounded-md"
          >
            {{ selectedPolicy.description }}
          </p>
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700">
            Status
          </label>
          <p
            class="mt-1 px-3 py-2 bg-gray-50 border border-gray-300 rounded-md"
          >
            <span
              v-if="selectedPolicy.status === 'A'"
              class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800"
            >
              Active
            </span>
            <span
              v-else
              class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-800"
            >
              Inactive
            </span>
          </p>
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700">
            Created Date
          </label>
          <p
            class="mt-1 px-3 py-2 bg-gray-50 border border-gray-300 rounded-md"
          >
            {{ selectedPolicy.createddate }}
          </p>
        </div>
      </div>

      <template #footer>
        <div class="flex justify-end">
          <Button variant="ghost" @onclick="viewModalOpen = false">
            Close
          </Button>
        </div>
      </template>
    </Modal>

    <ManageApprovers
      :show="approversModalOpen"
      :policy="selectedPolicy"
      @close="approversModalOpen = false"
      @saved="fetchPolicies"
    />
  </div>
</template>








