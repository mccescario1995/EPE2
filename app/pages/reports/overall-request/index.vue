<script setup lang="ts">
import { useMenuService } from "~/services/menuService";
const {
  fetchMenuList,
  canAdd: checkCanAdd,
  canEdit: checkCanEdit,
  canDelete: checkCanDelete,
} = useMenuService();

const canAdd = ref(false);
const canEdit = ref(false);
const canDelete = ref(false);

const router = useRouter();

import { IconView, IconEdit, IconDelete, IconApproval } from "#components";
const { breadcrumbs, currentMenuLabel } = useBreadcrumbs();

// Get the current page's menu name (should be "Policy")
const currentPageName = breadcrumbs.value[breadcrumbs.value.length - 1]?.label;

const userStore = useUserStore();

const userId = userStore.profile;

const loading = ref(false);
const exemptionData: any = ref(null);
const showDeleteConfirm = ref(false);
const deletingItem = ref<any>(null);
const deleteLoading = ref(false);

// reactive length used to force BaseTable to remount when data arrives
const exemptionDataLength = computed(() => exemptionData.value?.length ?? 0);

const viewProduct = (row: any) => {
  router.push(`/main/exemptions/overall-request/view?id=${row.transactionId}`);
};
const editProduct = (row: any) => {
  router.push(`/main/exemptions/overall-request/edit?id=${row.transactionId}`);
};
const approveRequest = (row: any) => {
  router.push(`/main/exemptions/approve?id=${row.transactionId}`);
};

const confirmDelete = (row: any) => {
  deletingItem.value = row;
  showDeleteConfirm.value = true;
};

const cancelDelete = () => {
  showDeleteConfirm.value = false;
  deletingItem.value = null;
};

const tableRef = ref();

const reloadTable = () => {
  tableRef.value?.refresh();
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
      reloadTable();
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
    const responseExemptions = await apiFetch("/api/exemption/overall", {
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
  policyNames?: string;
}) => {
  loading.value = true
  const response = await apiFetch("/api/exemption/overall", {
    method: "POST",
    body: JSON.stringify({
      UserId: parseInt(userId.emplId),
      page: params.page,
      pageSize: params.limit,
      search: params.search,
      sortField: params.sortField,
      sortDirection: params.sortDirection,
      PolicyNames: params.policyNames,
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

// --- Policy filter source data ---
const allExemptionsForFilter = ref<any[]>([]);

const  policyDropdownItems = computed(() => {
  const items = allExemptionsForFilter.value ?? [];
  return items
    .filter((item) => item?.label && item.label !== '-')
    .sort((a, b) => a.label.localeCompare(b.label));
});

const fetchAllForPolicyFilter = async () => {
  try {
    const res = await apiFetch("/api/exemption/policies", {
      method: "POST",
      body: JSON.stringify({ UserId: parseInt(userId.emplId) }),
      headers: { "Content-Type": "application/json" },
      parseJson: true,
    });
    
    allExemptionsForFilter.value = res.items ?? [];
  } catch (e) {
    console.error("Error fetching policies for filter:", e);
  }
};

// Handle create navigation
const handleCreate = () => {
  router.push(`/main/exemptions/create`);
};

onMounted(async () => {
  await fetchMenuList();
  canAdd.value = checkCanAdd("/main/exemptions/overall-request");
  canEdit.value = checkCanEdit("/main/exemptions/overall-request");
  canDelete.value = checkCanDelete("/main/exemptions/overall-request");

  // Load policy options for the filter dropdown
  await fetchAllForPolicyFilter();
});

</script>

<template>
  <div class="bg-gray-50 overflow-auto h-100vh dark:bg-gray-900 dark:text-gray-200 dark:border-gray-3000">
    <header class="bg-white dark:bg-gray-800 shadow">
      <div
        class="me-auto py-4 px-4 sm:px-6 lg:px-8 flex justify-between items-center"
      >
        <div class="flex items-center">
          <h1 class="text-2xl ps-5 font-bold text-gray-900 dark:text-gray-200">
          {{ currentMenuLabel || currentPageName ||  "Exemption Requests"}}
          </h1>
        </div>

        <Button
          v-if="canAdd"
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
    <main class="max-w-full mx-auto py-6 sm:px-6 ">
      <!-- Exemption Requests List -->
      <div class="mb-6 px-2 rounded-lg">
        <base-table
          ref="tableRef"
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
              D: 'bg-yellow-100 text-yellow-800 border border-yellow-900',
            },
            finalDecision: {
              Pending: 'bg-yellow-100 text-yellow-800',
              Approved: 'bg-green-100 text-green-800',
              Disapproved: 'bg-red-100 text-red-800',
              Draft: 'bg-gray-100 text-gray-800 border border-gray-900',
            },
          }"
          :date-time-fields="['createdDate']"
          date-time-format="'MMM d, yyyy h:mm a'"
          :actions="[
            {
              label: IconView,
              tooltip: 'View Details',
              handler: viewProduct,
              iconClass: 'text-indigo-500 dark:text-indigo-400',
              buttonClass: 'hover:bg-indigo-100 dark:hover:bg-indigo-100 hover:text-indigo-600 !dark:hover:text-indigo-900',
            },
            {
              label: IconApproval,
              tooltip: 'Approve Request',
              handler: approveRequest,
              iconClass: '!text-teal-500 dark:!text-teal-400',
              buttonClass: 'hover:bg-teal-100 dark:hover:bg-teal-100 hover:text-teal-600 dark:hover:text-teal-900',
              visible: (row) => row.canApprove && row.status !== 'C', // This will only be visible to the approver not he requester and for non-completed requests
            },
            {
              label: IconEdit,
              tooltip: 'Edit Request',
              handler: editProduct,
              iconClass: '!text-blue-500 dark:!text-blue-400',
              buttonClass: 'hover:bg-blue-100 dark:hover:bg-blue-100 hover:text-blue-600 dark:hover:text-blue-900',
              visible: (row) =>
                row.requesterId === userId.emplId && row.status !== 'C', // Show if user is requester or request is not Completed
              disabled: (row) => row.status === 'P' || row.status === 'C', // Disable for Pending or Completed
            },
            {
              label: IconDelete,
              tooltip: 'Delete Request',
              handler: confirmDelete,
              iconClass: 'text-red-500 dark:text-red-400',
              buttonClass: 'hover:bg-red-50 dark:hover:bg-red-100 hover:text-red-100 dark:hover:text-white-900',
              visible: (row) =>
                row.requesterId === userId.emplId && row.status !== 'C', // Show if user is requester or request is not Completed
              disabled: (row) => row.status === 'P' || row.status === 'C', // Disable for Pending or Completed
            },
          ]"
          :policy-filter-items="policyDropdownItems"
          policy-filter-label="Policy Filter"
          policy-filter-placeholder="Select policies..."
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
