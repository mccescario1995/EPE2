<script setup lang="ts">
import { useMenuService } from "~/services/menuService";
const { fetchMenuList, canEdit: checkCanEdit, canDelete: checkCanDelete } = useMenuService();
const router = useRouter();
const route = useRoute();
const { showToast } = useToast();
import BaseTable from '@/components/BaseTable.vue';
import { IconView } from '#components';

const loading = ref(false);
const exemptionDetails = ref<any>(null);
const approversData = ref<any[]>([]);
const policyValue = ref<{ id: number; label: string } | null>(null);
const attachments = ref<any[]>([]);
const transactionLogs = ref<any[]>([]);
const transactionHeaderDetails = ref<any>(null);

const userStore = useUserStore();
const userId = userStore.profile;

const canEdit = ref(false);
const canDelete = ref(false);

const logId = computed(() => {
  return Number(route.query.logId) || null;
});

const fetchExemptionDetails = async () => {
  if (!logId.value) return;

  loading.value = true;
  try {
    const response = await apiFetch(
      `/api/exemption/transaction-log/${logId.value}`,
      {
        method: "GET",
        parseJson: true,
      }
    );

    console.log("Exemption Details Response:", response);
    if (response) {
      exemptionDetails.value = response.item;
      if (exemptionDetails.value?.transactionId) {
        await fetchTransactionHeaderDetails(exemptionDetails.value.transactionId);
        await fetchAttachments();
        await fetchTransactionLogs();
        console.log("fetchTransactionLogs", transactionLogs.value);
      }
    } else {
      showToast({
        type: "error",
        title: "Error",
        message: response?.message || "Failed to load transaction log",
        duration: 5000,
      });
      router.push("/reports/policy-transaction");
    }
  } catch (error) {
    console.error("Error fetching transaction log details:", error);
    showToast({
      type: "error",
      title: "Error",
      message: "Failed to load transaction log",
      duration: 5000,
    });
    router.push("/reports/policy-transaction");
  } finally {
    loading.value = false;
  }
};

const fetchTransactionHeaderDetails = async (transactionId: number) => {
  try {
    const response = await apiFetch(
      `/api/exemption/view/${transactionId}`,
      {
        method: "POST",
        parseJson: true,
      }
    );

    if (response && response.item) {
      transactionHeaderDetails.value = response.item;

      if (
        transactionHeaderDetails.value?.status === "D" &&
        transactionHeaderDetails.value?.policyId
      ) {
        await fetchApprovers(transactionHeaderDetails.value.policyId);
      } else if (response.approvers) {
        approversData.value = response.approvers;
      }

      if (transactionHeaderDetails.value?.policyId) {
        const policyName = await fetchPolicyName(
          transactionHeaderDetails.value.policyId,
        );
        policyValue.value = {
          id: transactionHeaderDetails.value.policyId,
          label: policyName || `Policy ${transactionHeaderDetails.value.policyId}`,
        };
      }
    }
  } catch (error) {
    console.error("Error fetching transaction header details:", error);
  }
};

const isViewableInBrowser = (fileType: string): boolean => {
  const extension = "." + fileType.toLowerCase();
  return [".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".pdf"].includes(
    extension,
  );
};

const getAttachmentButtonLabel = (filetype: string) => {
  return isViewableInBrowser(filetype) ? "View" : "Download";
};

const fetchPolicyName = async (policyId: number): Promise<string> => {
  try {
    const response = await apiFetch("/api/policy", {
      method: "POST",
      parseJson: true,
    });
    const items = response.success ? response.items : response;
    if (items && Array.isArray(items)) {
      const policy = items.find((p: any) => p.policyid === policyId);
      return policy?.description || `Policy ${policyId}`;
    }
  } catch (error) {
    console.error("Error fetching policy name:", error);
  }
  return "";
};

const fetchAttachments = async () => {
  if (!exemptionDetails.value?.transactionId) return;

  try {
    const response = await apiFetch(
      `/api/exemption/attachments/${exemptionDetails.value.transactionId}`,
      {
        parseJson: true,
      }
    );

    if (response) {
      attachments.value = response.data || [];
    }
  } catch (error) {
    console.error("Error fetching attachments:", error);
  }
};

const fetchTransactionLogs = async () => {
  if (!exemptionDetails.value?.transactionId) return;
  try {
    const response = await apiFetch(
      `/api/exemption/transaction-logs/${exemptionDetails.value.transactionId}`,
      {
        method: "GET",
        parseJson: true,
      }
    );
    console.log("Transaction Logs Response:", response);
    if (response) {
      transactionLogs.value = response.items ?? [];
    }
  } catch (error) {
    console.error("Error fetching transaction logs:", error);
  }
};

const fetchApprovers = async (policyId: number) => {
  if (!policyId) {
    approversData.value = [];
    return;
  }

  loading.value = true;
  try {
    const response = await apiFetch(
      `/api/Maintenance/policy/${policyId}/approvers`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ UserId: userId.userId }),
        credentials: "include",
        parseJson: true,
      }
    );

    const items = response.success ? response.items : response;

    if (!items || !Array.isArray(items)) {
      console.error("Invalid approvers response:", items);
      approversData.value = [];
      return;
    }

    approversData.value = items
      .filter((a: any) => a.approvername && a.approvertype)
      .map((a: any) => ({
        transApproverId: a.id,
        approverName: a.approvername,
        approverEmail: a.approveremail,
        approverRole:
          a.approvertype === "IM" ? "Immediate Head" : a.approverRole,
        respond: a.respond ?? 3,
        sort: a.sort,
        lineSequence: a.linesequence,
      }));
  } catch (error) {
    console.error("Error fetching approvers:", error);
    approversData.value = [];
  } finally {
    loading.value = false;
  }
};

const formatDate = (date: string | Date | null) => {
  if (!date) return "N/A";
  return new Date(date).toLocaleDateString("en-PH", {
    year: "numeric",
    month: "long",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
};

const formatDateTime = (date: string | Date | null) => {
  if (!date) return { date: "N/A", time: "" };
  const d = new Date(date);
  return {
    date: d.toLocaleDateString("en-PH", {
      year: "numeric",
      month: "long",
      day: "numeric",
    }),
    time: d.toLocaleTimeString("en-PH", {
      hour: "2-digit",
      minute: "2-digit",
      hour12: true,
    }),
  };
};

const formatStatus = (status: string | number | null | undefined) => {
  if (status === null || status === undefined) return "N/A";
  const statusMap: Record<string, string> = {
    0: "Open",
    1: "Closed",
    2: "Pending",
    3: "Draft",
    4: "Deleted",
    N: "N/A ",
    O: "Open",
    C: "Closed",
    P: "Pending",
    D: "Draft",
    X: "Deleted",
    A: "Approved",
    Z: "Disapproved",
  };
  return statusMap[status] || String(status);
};

const getStatusDisplay = (status: string) => {
  const statusMap: Record<string, string> = {
    O: "Open",
    P: "Open",
    C: "Closed",
    D: "Draft",
    X: "Deleted",
    A: "Approved",
    Z: "Disapproved",
  };
  return statusMap[status] || status;
};

const getFinalDecisionDisplay = (decision: string) => {
  const decisionMap: Record<string, string> = {
    Pending: "Pending",
    Approved: "Approved",
    Disapproved: "Disapproved",
    Draft: "Draft",
  };
  return decisionMap[decision] || decision;
};

const getApproverStatusDisplay = (respond: number) => {
  const statusMap: Record<number, string> = {
    0: "Pending",
    1: "Approved",
    2: "Disapproved",
    3: "Open",
  };
  return statusMap[respond] ?? "Pending";
};

const getApproverStatusColor = (respond: number) => {
  const colorMap: Record<number, string> = {
    0: "bg-blue-100 text-blue-800 border border-blue-900",
    1: "bg-green-100 text-green-800 border border-green-900",
    2: "bg-red-100 text-red-800 border border-red-900",
    3: "bg-yellow-100 text-yellow-800 border border-yellow-900",
  };
  return colorMap[respond] || "bg-gray-100 text-gray-800 border border-gray-900";
};

const isCurrentUser = (requesterId: number): boolean => {
  return userId?.emplId === requesterId;
};

const approversTableData = computed(() => {
  return allApproversList.value.map(approver => ({
    approver: `${approver.approverName}${approver.approverEmail ? '\n' + approver.approverEmail : ''}`,
    approverRole: approver.approverRole,
    respondStatus: getApproverStatusDisplay(approver.respond),
    respondDate: approver.respondDate ? formatDateTime(approver.respondDate).date : 'N/A',
    remarks: approver.remarks || '-'
  }));
});

const transactionLogsTableData = computed(() => {
  return transactionLogs.value.map(log => ({
    transactionType: log.transactionType,
    previousStatus: log.previousStatus,
    newStatus: log.newStatus,
    performedBy: log.performedBy,
    performedAt: log.performedAt,
    remarks: log.remarks || '-',
    reason: log.reason || '-',
  }));
});

const allApproversList = computed(() => {
  const list: any[] = [];

  approversData.value.forEach((approver) => {
    const isMain = approver.lineSequence === 1;
    list.push({
      ...approver,
      approverRole: isMain ? "Main" : "Proxy",
    });
  });

  return list.sort((a, b) => {
    if (a.sort !== b.sort) return (a.sort || 0) - (b.sort || 0);
    return (a.lineSequence || 0) - (b.lineSequence || 0);
  });
});

const isCurrentLog = (logId: number | undefined) => {
  return exemptionDetails.value?.id === logId;
};

const handleBack = () => {
  router.push("/reports/policy-transaction");
};

const viewLog = (logId: number) => {
  router.push(`/reports/policy-transaction/view?logId=${logId}`);
};

onMounted(async () => {
  await fetchMenuList();
  canEdit.value = checkCanEdit("/reports/policy-transaction/");
  canDelete.value = checkCanDelete("/reports/policy-transaction/");

  fetchExemptionDetails();
});
</script>

<template>
  <div class="min-h-[50vh] bg-gray-50 overflow-auto h-100vh dark:bg-gray-900">
    <header class="bg-white dark:bg-gray-800 shadow">
      <div
        class="max-w-full py-4 px-4 sm:px-6 lg:px-8 flex justify-between items-center"
      >
        <div class="flex items-center">
          <Button
            variant="ghost-primary"
            size="lg"
            icon="left_arrow"
            iconPosition="center"
            customClass="rounded-full"
            @onclick="handleBack"
          />
          <h1 class="text-2xl ps-5 font-bold text-gray-900 dark:text-gray-100">
            Transaction Log Details
          </h1>
        </div>
      </div>
    </header>

    <main class="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
      <div v-if="loading" class="flex items-center justify-center py-20">
        <svg
          class="animate-spin h-8 w-8 text-blue-600 dark:text-blue-400"
          xmlns="http://www.w3.org/2000/svg"
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
          ></circle>
          <path
            class="opacity-75"
            fill="currentColor"
            d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
          />
        </svg>
        <span class="ml-3 text-gray-600 dark:text-gray-400"
          >Loading transaction log...</span
        >
      </div>
      <div v-else-if="exemptionDetails" class="space-y-6">
        <div class="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
          <div class="mb-6">
            <h2
              class="text-xl font-semibold text-gray-800 dark:text-gray-100 mb-4 pb-2 border-b"
            >
              Transaction Log Information
            </h2>
            <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <label
                  class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                >
                  Transaction KLog Number
                </label>
                <p class="text-gray-900 dark:text-gray-100">
                  {{ exemptionDetails.id || "N/A" }}
                  
                </p>
              </div>
              <div>
                <label
                  class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                >
                  Transaction Type
                </label>
                <p class="text-gray-900 dark:text-gray-100">
                  {{ exemptionDetails.transactionType || "N/A" }}
                </p>
              </div>
              <div>
                <label
                  class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                >
                  Previous Status
                </label>
                <p class="text-gray-900 dark:text-gray-100">
                  {{ formatStatus(exemptionDetails.previousStatus) }}
                </p>
              </div>
              <div>
                <label
                  class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                >
                  New Status
                </label>
                <p class="text-gray-900 dark:text-gray-100">
                  {{ formatStatus(exemptionDetails.newStatus) }}
                </p>
              </div>
              <div>
                <label
                  class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                >
                  Performed By
                </label>
                <p class="text-gray-900 dark:text-gray-100">
                  {{ exemptionDetails.performedBy || "N/A" }}
                
                </p>
              </div>
              <div>
                <label
                  class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                >
                  Performed At
                </label>
                <p class="text-gray-900 dark:text-gray-100">
                  {{ formatDate(exemptionDetails.performedAt) }}
                </p>
              </div>
              <div v-if="exemptionDetails.remarks">
                <label
                  class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                >
                  Remarks
                </label>
                <p class="text-gray-900 dark:text-gray-100 whitespace-pre-wrap">
                  {{ exemptionDetails.remarks }}
                </p>
              </div>
              <div v-if="exemptionDetails.reason">
                <label
                  class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                >
                  Reason
                </label>
                <p class="text-gray-900 dark:text-gray-100 whitespace-pre-wrap">
                  {{ exemptionDetails.reason }}
                </p>
              </div>
              <div v-if="exemptionDetails.metadata">
                <label
                  class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                >
                  Metadata
                </label>
                <p class="text-gray-900 dark:text-gray-100 whitespace-pre-wrap">
                  {{ exemptionDetails.metadata }}
                </p>
              </div>
              <!-- <div v-if="exemptionDetails.workstation">
                <label
                  class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                >
                  Workstation
                </label>
                <p class="text-gray-900 dark:text-gray-100">
                  {{ exemptionDetails.workstation }}
                </p>
              </div> -->
              <!-- <div v-if="exemptionDetails.ipAddress">
                <label
                  class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                >
                  IP Address
                </label>
                <p class="text-gray-900 dark:text-gray-100">
                  {{ exemptionDetails.ipAddress }}
                </p>
              </div> -->
              <!-- <div v-if="exemptionDetails.macAddress">
                <label
                  class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                >
                  MAC Address
                </label>
                <p class="text-gray-900 dark:text-gray-100">
                  {{ exemptionDetails.macAddress }}
                </p>
              </div> -->
            </div>
          </div>
        </div>

        <div v-if="transactionHeaderDetails" class="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div class="lg:col-span-2 bg-white dark:bg-gray-800 rounded-lg shadow p-6">
            <div class="mb-6">
              <div
                class="text-xl font-semibold text-gray-800 dark:text-gray-100 mb-4 pb-2 border-b flex justify-between items-center"
              >
                <h2>Request Information</h2>
                <span
                  class="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium"
                  :class="{
                    'bg-blue-100 text-blue-800 dark:bg-blue-800/20 dark:text-blue-300 border border-blue-900':
                      transactionHeaderDetails.status === 'O',
                    'bg-yellow-100 text-yellow-800 dark:bg-yellow-700/20 dark:text-yellow-300 border border-yellow-900':
                      transactionHeaderDetails.status === 'P' || transactionHeaderDetails.status === 'D',
                    'bg-red-200 text-red-800 dark:bg-red-700/20 dark:text-red-300 border border-red-900':
                      transactionHeaderDetails.status === 'C',
                    'bg-yellow-100 text-gray-800 dark:bg-gray-700/20 dark:text-gray-300 border border-gray-900':
                      transactionHeaderDetails.status === 'D'
                  }"
                >
                  {{ getStatusDisplay(transactionHeaderDetails.status) }}
                </span>
              </div>
              <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div>
                  <label
                    class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                  >
                    Transaction Number
                  </label>
                  <p class="text-gray-900 dark:text-gray-100">
                    {{ transactionHeaderDetails.transactionNumber || "N/A" }}
                  </p>
                </div>
                <div>
                  <label
                    class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                  >
                    Requestor Name
                  </label>
                  <p class="text-gray-900 dark:text-gray-100">
                    {{ transactionHeaderDetails.requesterName || "N/A" }}
                  </p>
                </div>
              </div>
              <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mt-4">
                <div>
                  <label
                    class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                  >
                    Created Date
                  </label>
                  <p class="text-gray-900 dark:text-gray-100">
                    {{ formatDate(transactionHeaderDetails.createdDate) }}
                  </p>
                </div>
                <div v-if="transactionHeaderDetails.modifiedDate">
                  <label
                    class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                  >
                    Last Modified
                  </label>
                  <p class="text-gray-900 dark:text-gray-100">
                    {{ formatDate(transactionHeaderDetails.modifiedDate) }}
                  </p>
                </div>
              </div>
            </div>

            <div class="mb-6">
              <div class="space-y-6">
                <div class="grid grid-cols-1 gap-6">
                  <div>
                    <label
                      class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                    >
                      Policy To Override
                    </label>
                    <p class="text-gray-900 dark:text-gray-100">
                      {{
                        transactionHeaderDetails.policyName ||
                        policyValue?.label ||                        
                        "N/A"
                      }}
                    </p>
                  </div>
                </div>

                <div class="grid grid-cols-1 gap-6">
                  <div>
                    <label
                      class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                    >
                      Policy Description
                    </label>
                    <p class="text-gray-900 dark:text-gray-100">
                      {{
                        policyValue?.label || "N/A"
                      }}
                    </p>
                  </div>
                </div>

                <div>
                  <label
                    class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                  >
                    Details
                  </label>
                  <div
                    class="bg-gray-50 rounded-lg p-4 border border-gray-200 dark:border-gray-600 dark:bg-gray-700"
                  >
                    <TipTapEditor
                      :modelValue="transactionHeaderDetails.details" readonly
                      placeholder="No details provided"
                    />
                  </div>
                </div>
              </div>
            </div>

            <div v-if="transactionHeaderDetails?.finalDecision" class="mb-6">
              <h2
                class="text-xl font-semibold text-gray-800 dark:text-gray-100 mb-4 pb-2 border-b"
              >
                Status
              </h2>
              <span
                class="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium"
                :class="{
                  'bg-yellow-100 text-yellow-800 dark:bg-yellow-800/20 dark:text-yellow-300':
                    transactionHeaderDetails.finalDecision === 'Pending',
                  'bg-green-100 text-green-800 dark:bg-green-800/20 dark:text-green-300':
                    transactionHeaderDetails.finalDecision === 'Approved',
                  'bg-red-100 text-red-800 dark:bg-red-800/20 dark:text-red-300':
                    transactionHeaderDetails.finalDecision === 'Disapproved',
                  'bg-gray-100 text-gray-800 dark:bg-gray-700/20 dark:text-gray-300':
                    transactionHeaderDetails.finalDecision === 'Draft',
                }"
              >
                {{ getFinalDecisionDisplay(transactionHeaderDetails.finalDecision) }}
              </span>
            </div>

            <div v-if="transactionHeaderDetails?.remark" class="mb-6">
              <h2
                class="text-xl font-semibold text-gray-800 dark:text-gray-100 mb-4 pb-2 border-b"
              >
                Remarks
              </h2>
              <div
                class="bg-gray-50 dark:bg-gray-700 rounded-lg p-4 border border-gray-200 dark:border-gray-600"
              >
                <p
                  class="text-gray-900 dark:text-gray-100 whitespace-pre-wrap wrap-break-words sm:break-all"
                >
                  <span
                    class="inline-block max-w-full bg-gray-100 dark:bg-gray-700 rounded-md px-2 py-1"
                  >
                    {{ transactionHeaderDetails.remark }}
                  </span>
                </p>
              </div>
            </div>

            <div class="mb-8">
              <h2
                class="text-xl font-semibold text-gray-800 dark:text-gray-100 mb-4 pb-2 border-b"
              >
                Attachments
              </h2>

              <div v-if="attachments.length > 0" class="space-y-2">
                <div
                  v-for="attachment in attachments"
                  :key="attachment.attachId"
                  class="flex items-center justify-between p-3 bg-gray-50 rounded-lg border border-gray-200 dark:bg-gray-700 dark:border-gray-600"
                >
                  <div class="flex items-center">
                    <svg
                      class="h-5 w-5 text-gray-400 dark:text-gray-500 mr-3"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                    >
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="2"
                        d="M15.172 7l-6.586 6.586a2 2 0 102.828 2.828l6.414-6.586a4 4 0 00-5.656-5.656l-6.415 6.585a6 6 0 108.486 8.486L20.5 13"
                      />
                    </svg>
                    <div>
                      <p
                        class="text-sm font-medium text-gray-900 dark:text-gray-100"
                      >
                        {{ attachment.uploadFilename }}
                      </p>
                      <p class="text-xs text-gray-500 dark:text-gray-400">
                        {{ attachment.filetype }} -
                        {{
                          new Date(attachment.createdate).toLocaleDateString()
                        }}
                      </p>
                    </div>
                  </div>
                  <a v-if="isViewableInBrowser(attachment.filetype)"
                    :href="`${$config.public.apiBase}/uploads/attachments/${attachment.newfilename}`"
                    target="_blank"
                    class="inline-flex items-center px-3 py-1 rounded-md text-sm font-medium bg-blue-100 text-blue-700 hover:bg-blue-200  dark:bg-blue-800 dark:text-blue-300 dark:border-blue-800 dark:hover:bg-blue-700 dark:hover:text-white"
                  >
                    View
                  </a>
                  <a v-else
                    :href="`${$config.public.apiBase}/uploads/attachments/${attachment.newfilename}`"
                    download
                    class="inline-flex items-center px-3 py-1 rounded-md text-sm font-medium bg-green-100 text-green-700 hover:bg-green-200 dark:bg-green-800 dark:text-green-300 dark:hover:bg-green-700 dark:hover:text-white"
                  >
                    Download
                  </a>
                </div>
              </div>
              <p v-else class="text-gray-500 dark:text-gray-400 text-sm">
                No attachments for this request.
              </p>
            </div>
          </div>

          <div class="lg:col-span-1 bg-white dark:bg-gray-800 rounded-lg shadow px-6 py-6">
            <div class="mb-6">
              <h2
                class="text-xl font-semibold text-gray-800 dark:text-gray-100 mb-4 pb-2 border-b"
              >
                Creation Log
              </h2>
              <div class="space-y-4">
                 
                <div>
                  <label
                    class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                  >
                    Created Date
                  </label>
                  <p class="text-gray-900 dark:text-gray-100">
                    {{ formatDate(transactionHeaderDetails?.createdDate) }}
                  </p>
                </div>
                <div v-if="transactionHeaderDetails?.modifiedBy">
                  <label
                    class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                  >
                    Modified By
                  </label>
                  <p class="text-gray-900 dark:text-gray-100">
                    {{ transactionHeaderDetails?.modifiedBy }}
                  </p>
                </div>
                <div v-if="transactionHeaderDetails?.modifiedDate">
                  <label
                    class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                  >
                    Modified Date
                  </label>
                  <p class="text-gray-900 dark:text-gray-100">
                    {{ formatDate(transactionHeaderDetails?.modifiedDate) }}
                  </p>
                </div>
              </div>
            </div>

            <div v-if="transactionHeaderDetails?.workstation || transactionHeaderDetails?.ipAddress">
              <h2
                class="text-xl font-semibold text-gray-800 dark:text-gray-100 mb-4 pb-2 border-b"
              >
                System Info
              </h2>
              <div class="space-y-4">
                <div v-if="transactionHeaderDetails.workstation">
                  <label
                    class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                  >
                    Workstation
                  </label>
                  <p class="text-gray-900 dark:text-gray-100">
                    {{ transactionHeaderDetails.workstation }}
                  </p>
                </div>
                <div v-if="transactionHeaderDetails.ipAddress">
                  <label
                    class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                  >
                    IP Address
                  </label>
                  <p class="text-gray-900 dark:text-gray-100">
                    {{ transactionHeaderDetails.ipAddress }}
                  </p>
                </div>
                <div v-if="transactionHeaderDetails.macAddress">
                  <label
                    class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                  >
                    MAC Address
                  </label>
                  <p class="text-gray-900 dark:text-gray-100">
                    {{ transactionHeaderDetails.macAddress }}
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div class="bg-white dark:bg-gray-800 rounded-lg shadow px-6 py-6">
          <div class="mb-6">
            <div class="border-b flex justify-between">
              <h2
                class="text-xl font-semibold text-gray-800 dark:text-gray-100 mb-2"
              >
                Approver Logs
              </h2>
            </div>
          </div>

          <div class="mt-4">
            <BaseTable
              :table-data="approversTableData"
              :header="[
                'approverRole',
                'approver',
                'respondStatus',
                'respondDate',
                'remarks',
              ]"
              :badge-fields="['respondStatus', 'approverRole']"
              :field-colors="{
                respondStatus: {
                  Open: 'bg-blue-100 text-blue-800 border border-blue-900',
                  Approved: 'bg-green-100 text-green-800 border border-green-900',
                  Disapproved: 'bg-red-100 text-red-800 border border-red-900',
                  Pending: 'bg-yellow-100 text-yellow-800 border border-yellow-900',
                },
                approverRole: {
                  Main: 'bg-blue-100 text-blue-800 border border-blue-900',
                  Proxy: 'bg-orange-100 text-orange-800 border border-orange-900',
                }
              }"
              :date-time-fields="['respondDate']" 
              date-time-format="'MMM d, yyyy h:mm a'"
            />
          </div>
        </div>

        <div class="bg-white dark:bg-gray-800 rounded-lg shadow px-6 py-6">
          <div class="mb-6">
            <h2 class="text-xl font-semibold text-gray-800 dark:text-gray-100 mb-4 pb-2 border-b">
              All Transaction Logs
            </h2>
          </div>

          <div class="mt-4">
            <BaseTable
              :table-data="transactionLogsTableData"
              :header="[
                'transactionType', 
                'previousStatus', 
                'newStatus', 
                'performedBy', 
                'performedAt', 
              ]"
              :field-mappings="{
                transactionType: {
                  CREATE: 'Create',
                  DRAFT: 'Draft',
                  SUBMIT: 'Submit',
                  RESPOND: 'Respond',
                  DELETE: 'Delete',
                  UPDATE: 'Update',
                  CLOSE: 'Close',
                },
                previousStatus: {
                  N: 'N/A',
                  O: 'Open',
                  C: 'Closed',
                  P: 'Pending',
                  D: 'Draft',
                  X: 'Deleted',
                  A: 'Approved',
                  Z: 'Disapproved',
                },
                newStatus: {
                  O: 'Open',
                  C: 'Closed',
                  P: 'Pending',
                  D: 'Draft',
                  X: 'Deleted',
                  A: 'Approved',
                  Z: 'Disapproved',
                }
              }"
              :field-colors="{
                transactionType: {
                  CREATE: 'bg-green-100 text-green-800 border border-green-900',
                  DRAFT: 'bg-gray-100 text-gray-800 border border-gray-900',
                  SUBMIT: 'bg-blue-100 text-blue-800 border border-blue-900',
                  RESPOND: 'bg-yellow-100 text-yellow-800 border border-yellow-900',
                  DELETE: 'bg-red-100 text-red-800 border border-red-900',
                  UPDATE: 'bg-purple-100 text-purple-800 border border-purple-900',
                  CLOSE: 'bg-red-100 text-red-800 border border-red-900',
                },
                previousStatus: {
                    O: 'bg-blue-100 text-blue-800 border border-blue-900',
                    C: 'bg-red-100 text-red-800 border border-red-900',
                    P: 'bg-yellow-100 text-yellow-800 border border-yellow-900',
                    D: 'bg-gray-100 text-gray-800 border border-gray-900',
                    X: 'bg-red-100 text-red-800 border border-red-900',
                    A: 'bg-green-100 text-green-800 border border-green-900',
                    Z: 'bg-red-100 text-red-800 border border-red-900',
                },
                newStatus: {
                    O: 'bg-blue-100 text-blue-800 border border-blue-900',
                    C: 'bg-red-100 text-red-800 border border-red-900',
                    P: 'bg-yellow-100 text-yellow-800 border border-yellow-900',
                    D: 'bg-gray-100 text-gray-800 border border-gray-900',
                    X: 'bg-red-100 text-red-800 border border-red-900',
                    A: 'bg-green-100 text-green-800 border border-green-900',
                    Z: 'bg-red-100 text-red-800 border border-red-900',
                }
              }"
              :badge-fields="['transactionType', 'previousStatus', 'newStatus']"
              :date-time-fields="['performedAt']" 
              date-time-format="'MMM d, yyyy h:mm a'"
            />
          </div>
        </div>
      </div>

      <div
        v-else
        class="bg-white dark:bg-gray-800 rounded-lg shadow px-6 py-12 text-center"
      >
        <p class="text-gray-500 dark:text-gray-400 text-lg mb-4">
          No transaction log found.
        </p>
        <Button variant="primary" size="lg" @onclick="handleBack">
          Back to List
        </Button>
      </div>
    </main>
  </div>
</template>