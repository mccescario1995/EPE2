<script setup lang="ts">
const router = useRouter();

import { IconView, IconEdit, IconDelete, IconApproval } from "#components";

const userStore = useUserStore();

const userId = userStore.profile;

const loading = ref(false);
const exemptionData: any = ref(null);
const showDeleteConfirm = ref(false);
const deletingItem = ref<any>(null);
const deleteLoading = ref(false);

// reactive length used to force BaseTable to remount when data arrives
const exemptionDataLength = computed(() => exemptionData.value?.length ?? 0);

// Store last-known filter state for export
const lastSearch = ref<string>('')
const lastFilters = ref<Record<string, string>>({})
const lastSortField = ref<string>('')
const lastSortDirection = ref<'asc' | 'desc'>('asc')

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
    const responseExemptions = await apiFetch("/api/exemption/closed", {
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

const fetchExemptionData = async (params: {
  page: number;
  limit: number;
  search: string;
  filters: Record<string, string>;
  sortField?: string;
  sortDirection?: "asc" | "desc";
}) => {
  // Store the current filter state for export
  lastSearch.value = params.search
  lastFilters.value = { ...params.filters }
  lastSortField.value = params.sortField ?? ''
  lastSortDirection.value = params.sortDirection ?? 'asc'

  loading.value = true
  const response = await apiFetch("/api/exemption/closed", {
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
  })

  loading.value = false

  return {
    data: response.items ?? [],
    total: response.totalCount ?? 0,
  }
}

const fetchAllForExport = async () => {
  // Build active filters same as in fetchTable (using stored state)
  const activeFilters: Record<string, string> = {}
  Object.entries(lastFilters.value).forEach(([key, val]) => {
    if (val) activeFilters[key] = val
  })

  let allData = []
  let page = 1
  const limit = 100 // reasonable page size for fetching
  let total = 0

  do {
    const res = await fetchExemptionData({
      page,
      limit,
      search: lastSearch.value,
      filters: activeFilters,
      sortField: lastSortField.value || undefined,
      sortDirection: lastSortField.value ? lastSortDirection.value : undefined,
    })

    allData = [...allData, ...res.data]
    if (total === 0) {
      total = res.total
    }
    page++
  } while (allData.length < total)

  return {
    data: allData,
    total,
  }
}

// Handle create navigation
const handleCreate = () => {
  router.push(`/exemptions/create`);
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
          <h1 class="text-2xl ps-5 font-bold text-gray-900">Closed Requests</h1>
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
          :server-pagination="true"
          :fetch-data="fetchExemptionData"
          :pagination="true"
          :per-page="5"
          :show-search="true"
          :showlimitfilter="true"
          :limitfilter="[5, 10, 20, 30, 40, 50]"
          :sortable-columns="[
            'transactionNumber',
            'policyName',
            'requesterName',
            'createdDate',
          ]"
          :filterable-columns="['status']"
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
            },
            finalDecision: {
              Pending: 'Pending',
              Approved: 'Approved',
              Disapproved: 'Disapproved',
            },
          }"
          :field-colors="{
            status: {
              O: 'bg-blue-100 text-blue-800 border border-blue-900',
              P: 'bg-blue-100 text-blue-800 border border-blue-900',
              C: 'bg-gray-200 text-gray-800 border border-gray-900',
            },
            finalDecision: {
              Pending: 'bg-yellow-100 text-yellow-800',
              Approved: 'bg-green-100 text-green-800',
              Disapproved: 'bg-red-100 text-red-800',
            },
          }"
          :date-fields="['createdDate']"
          :show-export="true"
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
              tooltip: 'Edit Request',
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
          :export-fetch-data="fetchAllForExport"
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
