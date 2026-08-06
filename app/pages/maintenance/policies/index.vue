<script setup lang="ts">
definePageMeta({
  middleware: "auth",
  name: "Policies",
});

import { IconView, IconEdit, IconDelete } from "#components";
const { breadcrumbs, currentMenuLabel } = useBreadcrumbs();

// Get the current page's menu name (should be "Policy")
const currentPageName = breadcrumbs.value[breadcrumbs.value.length - 1]?.label;
const router = useRouter();

const userStore = useUserStore();
const userId = userStore.profile;

const loading = ref(false);

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

  loading.value = false;

  return {
    data: response.items ?? [],
    total: response.totalCount ?? 0,
  };
};

// ── Delete state ───────────────────────────────────────────────────
const showDeleteConfirm = ref(false);
const deletingItem = ref<any>(null);
const deleteLoading = ref(false);

const tableRef = ref();

// ── Navigation to separate pages ───────────────────────────────────
const handleCreatePolicy = () => {
  navigateTo("/maintenance/policies/create");
};

const viewPolicy = (row: any) => {
  router.push(`/maintenance/policies/${row.policyid}`);
};

const editPolicy = (row: any) => {
  router.push(`/maintenance/policies/${row.policyid}/edit`);
};

// ── Delete ─────────────────────────────────────────────────────────
const confirmDeletePolicy = (row: any) => {
  deletingItem.value = row;
  showDeleteConfirm.value = true;
};

const cancelDeletePolicy = () => {
  showDeleteConfirm.value = false;
  deletingItem.value = null;
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
      },
    );

    if (response) {
      showDeleteConfirm.value = false;
      deletingItem.value = null;

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
            @click="router.back()"
          />
          <h1 class="text-2xl ps-5 font-bold text-gray-900 dark:text-gray-200">
            {{ currentMenuLabel || currentPageName || "Policies" }}
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
              A: 'bg-green-100 text-green-800 border border-green-900',
              I: 'bg-red-50 text-red-800 border-red-900',
            },
          }"
          :date-time-fields="['createddate']"
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
  </div>
</template>
