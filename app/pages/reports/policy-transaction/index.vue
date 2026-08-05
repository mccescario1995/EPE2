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

import { IconView } from "#components";
const { breadcrumbs, currentMenuLabel } = useBreadcrumbs();

const currentPageName = breadcrumbs.value[breadcrumbs.value.length - 1]?.label;

const userStore = useUserStore();

const userId = userStore.profile;

const loading = ref(false);
const exemptionData: any = ref(null);

const exemptionDataLength = computed(() => exemptionData.value?.length ?? 0);

const viewProduct = (row: any) => {
  router.push(`/reports/policy-transaction/view?logId=${row.id}`);
};

const tableRef = ref();

const fetchExemptionData = async (params: {
  page: number;
  limit: number;
  search: string;
  filters: Record<string, string>;
  sortField?: string;
  sortDirection?: "asc" | "desc";
  policyNames?: string;
  startDate?: string;
  endDate?: string;
}) => {
  loading.value = true
  const response = await apiFetch("/api/exemption/transaction-logs", {
    method: "POST",
    body: JSON.stringify({
      UserId: parseInt(userId.emplId),
      page: params.page,
      pageSize: params.limit,
      search: params.search,
      sortField: params.sortField,
      sortDirection: params.sortDirection,
      PolicyNames: params.policyNames,
      StartDate: params.startDate,
      EndDate: params.endDate,
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

const allExemptionsForFilter = ref<any[]>([]);

const policyDropdownItems = computed(() => {
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

const handleCreate = () => {
  router.push(`/main/exemptions/create`);
};

onMounted(async () => {
  await fetchMenuList();
  canAdd.value = checkCanAdd("/main/exemptions/overall-request");
  canEdit.value = checkCanEdit("/main/exemptions/overall-request");
  canDelete.value = checkCanDelete("/main/exemptions/overall-request");

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
            {{ currentMenuLabel || currentPageName || "Policy Transaction Reports" }}
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
      <div class="mb-6 px-2 rounded-lg">
        <base-table
          ref="tableRef"
          :server-pagination="true"
          :fetch-data="(params) => fetchExemptionData(params)"
          :pagination="true"
          :per-page="5"
          :show-search="true"
          :showlimitfilter="true"
          :limitfilter="[5, 10, 20, 30, 40, 50]"
          :sortable-columns="[
            'transactionNumber',
            'transactionType',
            'performedAt',
            'performedBy',
          ]"
          :filterable-columns="['transactionType']"
          :header="[
            'transactionNumber',
            'transactionType',
            'performedBy',
            'performedAt',
          ]"
          :badge-fields="['transactionType']"
          :field-mappings="{
            transactionType: {
              CREATE: 'Create',
              DRAFT: 'Draft',
              SUBMIT: 'Submit',
              RESPOND: 'Respond',
              DELETE: 'Delete',
            },
          }"
          :field-colors="{
            transactionType: {
              CREATE: 'bg-green-100 text-green-800 border border-green-900',
              DRAFT: 'bg-gray-100 text-gray-800 border border-gray-900',
              SUBMIT: 'bg-blue-100 text-blue-800 border border-blue-900',
              RESPOND: 'bg-yellow-100 text-yellow-800 border border-yellow-900',
              DELETE: 'bg-red-100 text-red-800 border border-red-900',
            },
          }"
          :date-time-fields="['performedAt']"
          date-time-format="'MMM d, yyyy h:mm a'"
          :actions="[
            {
              label: IconView,
              tooltip: 'View Log Details',
              handler: viewProduct,
              iconClass: 'text-indigo-500 dark:text-indigo-400',
              buttonClass: 'hover:bg-indigo-100 dark:hover:bg-indigo-100 hover:text-indigo-600 !dark:hover:text-indigo-900',
            },
          ]"
          :policy-filter-items="policyDropdownItems"
          policy-filter-label="Policy Filter"
          policy-filter-placeholder="Select policies..."
        />
      </div>
    </main>
  </div>
</template>
