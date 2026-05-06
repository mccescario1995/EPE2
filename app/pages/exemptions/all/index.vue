<script setup lang="ts">
const router = useRouter();

import { IconView, IconEdit, IconDelete, IconApproval } from "#components";

const userStore = useUserStore();

const userId = userStore.profile;

const loading = ref(false);
const exemptionData = ref(null);
const showDeleteConfirm = ref(false);
const deletingItem = ref<any>(null);
const deleteLoading = ref(false);

const viewProduct = (row: any) => {
  router.push(`/exemptions/view?id=${row.transactionId}`);
};
const editProduct = (row: any) => {
    router.push(`/exemptions/edit?id=${row.transactionId}`);
};
const approveRequest = (row: any) => {
  router.push(`/exemptions/approve?id=${row.transactionId}`);
};

const confirmDelete = (row: any) => {
  deletingItem.value = row;
  showDeleteConfirm.value = true;
};

const cancelDelete = () => {
  showDeleteConfirm.value = false;
  deletingItem.value = null;
};

const deleteRequest = async () => {
  if (!deletingItem.value) return;

  deleteLoading.value = true;
  try {
    const payload = {
      userId: userId.userId,
    };

    const response = await apiFetch(
      `/api/exemption/delete/${deletingItem.value.transactionId}`,
      {
        method: "POST",
        body: JSON.stringify(payload),
        headers: {
          "Content-Type": "application/json",
        },
        // parseJson: true,
      },
    );

    if (response) {
      showDeleteConfirm.value = false;
      deletingItem.value = null;
      // Refresh the list after successful delete

      showToast({
        title: "Request Deleted",
        message: "The exemption request has been successfully deleted.",
        type: "error",
        duration: 5000,
      });

      await fetchExemptionRequests();
    } else {
      console.error("Delete failed:", response.message);
    }
  } catch (error) {
    console.error("Error deleting exemption request:", error);
  } finally {
    deleteLoading.value = false;
  }
};

const fetchExemptionRequests = async () => {
  loading.value = true;
  try {
    const responseExemptions = await apiFetch("/api/exemption", {
      method: "POST",
      body: JSON.stringify({ UserId: parseInt(userId.emplId) }),
      headers: {
        "Content-Type": "application/json",
      },
      parseJson: true,
    });
    exemptionData.value = responseExemptions.items;
  } catch (error) {
    console.error("Error fetching exemption requests:", error);
  } finally {
    loading.value = false;
  }
};

// Handle create navigation
const handleCreate = () => {
  router.push(`/exemptions/create`);
};

watchEffect(() => {
  fetchExemptionRequests();
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
          <h1 class="text-2xl ps-5 font-bold text-gray-900">All Requests</h1>
        </div>

        <Button
          v-if="exemptionData"
          variant="primary"
          size="md"
          icon="add"
          iconPosition="left"
          @onclick="handleCreate"
        >
          Create Request
        </Button>
      </div>
    </header>
    <main class="max-w-7xl mx-auto py-6 sm:px-6">
      <!-- Exemption Requests List -->
      <div class="mb-6 px-2 rounded-lg">
        <base-table
          :table-data="exemptionData"
          :header="[
            'transactionNumber',
            'policyName',
            'status',
            'requesterName',
            'createdDate',
          ]"
          :badge-fields="['status', 'finalDecision']"
          :field-mappings="{
            status: {
              O: 'Open',
              P: 'Open',
              C: 'Closed',
              D: 'Draft',
            },
            finalDecision: {
              Pending: 'Pending',
              Approved: 'Approved',
              Disapproved: 'Disapproved',
              Draft: 'Draft',
            },
          }"
          :field-colors="{
            status: {
              O: 'bg-blue-100 text-blue-800 border border-blue-900',
              P: 'bg-blue-100 text-blue-800 border border-blue-900',
              C: 'bg-gray-200 text-gray-800 border border-gray-900',
              D: 'bg-gray-100 text-gray-800 border border-gray-900',
            },
            finalDecision: {
              Pending: 'bg-yellow-100 text-yellow-800',
              Approved: 'bg-green-100 text-green-800',
              Disapproved: 'bg-red-100 text-red-800',
              Draft: 'bg-gray-100 text-gray-800 border border-gray-900',
            },
          }"
          :date-fields="['createdDate']"
          :pagination="true"
          :per-page="10"
          :show-search="true"
          :actions="[
            {
              label: IconView,
              tooltip: 'View Details',
              handler: viewProduct,
              iconClass: 'text-indigo-500',
            },
            {
              label: IconApproval,
              tooltip: 'Approve Request',
              handler: approveRequest,
              iconClass: 'text-teal-500',
              visible: (row) => row.canApprove && row.status !== 'C', // This will only be visible to the approver not he requester and for non-completed requests
            },
            {
              label: IconEdit,
              tooltip: (row) => row.status == 'D' ? 'Edit Draft' : 'Edit Request',
              handler: editProduct,
              iconClass: 'text-blue-500',
              visible: (row) =>
                row.requesterId === userId.emplId && row.status !== 'C', // Show if user is requester or request is not Completed
              disabled: (row) => row.status === 'P' || row.status === 'C', // Disable for Pending or Completed
            },
            {
              label: IconDelete,
              tooltip: 'Delete Request',
              handler: confirmDelete,
              iconClass: 'text-red-500',
              visible: (row) =>
                row.requesterId === userId.emplId && row.status !== 'C', // Show if user is requester or request is not Completed
              disabled: (row) => row.status === 'P' || row.status === 'C', // Disable for Pending or Completed
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
            @click="cancelDelete"
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
              Are you sure you want to delete this exemption request? This
              action cannot be undone.
            </p>
            <div class="flex gap-3 justify-center">
              <button
                @click="cancelDelete"
                class="px-4 py-2 bg-gray-200 text-gray-700 rounded-lg hover:bg-gray-300 transition-colors"
                :disabled="deleteLoading"
              >
                Cancel
              </button>
              <button
                @click="deleteRequest"
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
  </div>
</template>
