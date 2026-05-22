<script setup lang="ts">
const router = useRouter();
import { IconView, IconEdit, IconDelete, IconUserApprover } from "#components";
import type { I } from "vue-router/dist/router-CWoNjPRp.mjs";
// import { ManageApprover } from '../../../../.nuxt/components';
// import ManageApproversModal from './ManageApproversModal.vue';

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
  await fetchEditPolicy(row.policyId);
  editModalOpen.value = true;
};

const manageApprovers = async (row: any) => {
  // router.push(`/maintenance/policies/policy-approver-manager?policyId=${row.policyId}`); 
  selectedPolicy.value = row;
  await fetchApprovers(row.policyId);
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
        policyId: p.policyid,
        description: p.description,
        status: p.status,
        shortName: p.shortname,
        createdDate: p.createddate,
        // add others if needed
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
      `/api/Maintenance/policy/delete/${deletingItem.value.policyId}`,
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

      await fetchPolicies();
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
      // Refresh the list
      await fetchPolicies();
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

const fetchEditPolicy = async (policyId: string) => {
  editLoading.value = true;
  editError.value = "";
  try {
    const response: any = await apiFetch(
      `/api/Maintenance/policy/${policyId}`,
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
      `/api/Maintenance/policy/update/${selectedPolicy.value.policyId}`,
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
      await fetchPolicies();
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

const fetchApprovers = async (policyId:any) => {
  approversLoading.value = true;
  approversError.value = "";
  try {
    console.log("Fetching approvers for policy:", policyId);
    const response = await apiFetch(
      `/api/Maintenance/policy/${policyId}/approvers`,
      {
        method: "POST",
        body: JSON.stringify({ UserId: userId.userId }),
        headers: {
          "Content-Type": "application/json",
        },
        parseJson: true,
      },
    );
    console.log("Fetched approvers index.vue:", response);
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

const addApprover = async () => {
  if (!selectedPolicy.value) return;

  addingApprover.value = true;
  approversError.value = "";
  try {
    const response: any = await apiFetch(
      `/api/Policy/${selectedPolicy.value.policyId}/approvers`,
      {
        method: "POST",
        body: JSON.stringify({
          Policyid: selectedPolicy.value.policyId,
          Approverid: newApprover.approverid,
          Approvername: newApprover.name,
          Approveremail: newApprover.email,
          Approvertype: newApprover.type,
          Linesequence: newApprover.sequence,
        }),
        headers: {
          "Content-Type": "application/json",
        },
        parseJson: true,
      },
    );
    if (response) {
      newApprover.approverid = null;
      newApprover.name = "";
      newApprover.email = "";
      newApprover.type = "";
      newApprover.sequence = 1;
      await fetchApprovers(selectedPolicy.value.policyId);
    } else {
      approversError.value = response.message || "Failed to add approver";
    }
  } catch (err) {
    approversError.value = "An error occurred";
    console.error(err);
  } finally {
    addingApprover.value = false;
  }
};

const updateSequence = async (approver: any) => {
  // This would need an API endpoint to update sequence
  // For now, we'll just log it
  console.log("Update sequence for approver:", approver);
};

const deleteApprover = async (approverId: string) => {
  if (!selectedPolicy.value) return;

  try {
    const response: any = await apiFetch(
      `/api/Policy/${selectedPolicy.value.policyId}/approvers/${approverId}`,
      {
        method: "DELETE",
        parseJson: true,
      },
    );
    if (response) {
      await fetchApprovers(selectedPolicy.value.policyId);
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
          v-if="policyData"
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
          :table-data="policyData"
          :header="[
            'policyId',
            'shortName',
            'status',
            'description',
            'createdDate',
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
          :pagination="true"
          :per-page="10"
          :totalItems="policyDataLength"
          :key="policyDataLength"
          :show-search="true"
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
            {{ selectedPolicy.policyId }}
          </p>
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700">
            Shortname
          </label>
          <p
            class="mt-1 px-3 py-2 bg-gray-50 border border-gray-300 rounded-md"
          >
            {{ selectedPolicy.shortName ?? "-" }}
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
            {{ selectedPolicy.createdDate }}
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

    <!-- Approvers Modal -->
    <!-- <Modal
      v-model:isOpen="approversModalOpen"
      size="xl"
      position="center"
      :closeOnOverlayClick="true"
      :closeOnEsc="true"
    >
      <template #header>
        <h3 class="text-lg font-medium leading-6 text-gray-900">
          Manage Approvers for Policy {{ selectedPolicy?.policyId }}
        </h3>
      </template>

      <div v-if="approversLoading" class="text-center py-4">Loading...</div>

       <div v-else class="space-y-6"> -->
    <!-- Approvers Table -->
    <!-- <div>
      <h4 class="text-md font-medium text-gray-900 mb-2">Current Approvers</h4>
      <div class="overflow-x-auto">
        <table class="min-w-full bg-white border border-gray-300">
          <thead>
            <tr class="bg-gray-100">
              <th class="px-4 py-2 border text-left">Name</th>
              <th class="px-4 py-2 border text-left">Email</th>
              <th class="px-4 py-2 border text-left">Type</th>
              <th class="px-4 py-2 border text-left">Sequence</th>
              <th class="px-4 py-2 border text-left">Role</th>
              <th class="px-4 py-2 border text-left">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="approver in approvers"
              :key="approver.Id"
              class="hover:bg-gray-50"
            >
              <td class="px-4 py-2 border">{{ approver.Approvername }}</td>
              <td class="px-4 py-2 border">{{ approver.Approveremail }}</td>
              <td class="px-4 py-2 border">{{ approver.Approvertype }}</td>
              <td class="px-4 py-2 border">
                <input
                  v-model.number="approver.Linesequence"
                  @blur="updateSequence(approver)"
                  type="number"
                  min="1"
                  class="w-16 px-2 py-1 border border-gray-300 rounded text-center"
                />
              </td>
              <td class="px-4 py-2 border">{{ approver.ApproverRole }}</td>
              <td class="px-4 py-2 border">
                <button
                  @click="deleteApprover(approver.Id)"
                  class="text-red-500 hover:underline text-sm"
                >
                  Delete
                </button>
              </td>
            </tr>
            <tr v-if="approvers.length === 0">
              <td
                colspan="6"
                class="px-4 py-2 border text-center text-gray-500"
              >
                No approvers found
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div> -->

    <!-- Add New Approver Form -->
    <!-- <div>
          <h4 class="text-md font-medium text-gray-900 mb-2">Add New Approver</h4>
          <form
            @submit.prevent="addApprover"
            class="grid grid-cols-1 md:grid-cols-2 gap-4"
          >
            <div>
              <label for="approver-id" class="block text-sm font-medium text-gray-700">
                Approver ID
              </label>
              <input
                id="approver-id"
                v-model.number="newApprover.approverid"
                type="number"
                class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
              />
            </div>
            <div>
              <label for="approver-name" class="block text-sm font-medium text-gray-700">
                Name
              </label>
              <input
                id="approver-name"
                v-model="newApprover.name"
                type="text"
                required
                class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
              />
            </div>
            <div>
              <label for="approver-email" class="block text-sm font-medium text-gray-700">
                Email
              </label>
              <input
                id="approver-email"
                v-model="newApprover.email"
                type="email"
                required
                class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
              />
            </div>
            <div>
              <label for="approver-type" class="block text-sm font-medium text-gray-700">
                Type
              </label>
              <input
                id="approver-type"
                v-model="newApprover.type"
                type="text"
                class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
              />
            </div>
            <div>
              <label
                for="approver-sequence"
                class="block text-sm font-medium text-gray-700"
              >
                Sequence
              </label>
              <input
                id="approver-sequence"
                v-model.number="newApprover.sequence"
                type="number"
                min="1"
                required
                class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
              />
            </div>
            <div class="md:col-span-2 flex justify-end">
              <Button
                type="submit"
                variant="primary"
                :disabled="addingApprover"
                class="px-4 py-2"
              >
                {{ addingApprover ? "Adding..." : "Add Approver" }}
              </Button>
            </div>
          </form>
        </div>

        <div v-if="approversError" class="text-red-500 text-sm">
          {{ approversError }}
        </div>
      </div>

      <template #footer>
        <div class="flex justify-end">
          <Button variant="ghost" @onclick="approversModalOpen = false"> Close </Button>
        </div>
      </template>
    </Modal> -->
    <!-- In maintenance/policies/index.vue -->
    <ManageApprovers
      :show="approversModalOpen"
      :policy="selectedPolicy"
      @close="approversModalOpen = false"
      @saved="fetchPolicies"
    />
  </div>
</template>
