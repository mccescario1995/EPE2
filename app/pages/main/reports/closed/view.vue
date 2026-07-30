<script setup lang="ts">
import { useMenuService } from "~/services/menuService";
const { fetchMenuList, canEdit: checkCanEdit, canDelete: checkCanDelete } = useMenuService();
const router = useRouter();
const route = useRoute();
const { showToast } = useToast();

const loading = ref(false);
const exemptionDetails = ref<any>(null);
const approversData = ref<any[]>([]);
const policyValue = ref<{ id: number; label: string } | null>(null);
const attachments = ref<any[]>([]);
const isLoadingApprovers = ref(false);

const userStore = useUserStore();
const userId = userStore.profile;

const canEdit = ref(false);
const canDelete = ref(false);

const exemptionId = computed(() => {
  return Number(route.query.id) || null;
});

const fetchExemptionDetails = async () => {
  if (!exemptionId.value) return;

  loading.value = true;
  try {
    const response = await apiFetch(
      `/api/exemption/view/${exemptionId.value}`,
      {
        method: "POST",
        parseJson: true,
      },
    );

    if (response && response.item) {
      exemptionDetails.value = response.item;

      if (
        exemptionDetails.value?.status === "D" &&
        exemptionDetails.value?.policyId
      ) {
        await fetchApprovers(exemptionDetails.value.policyId);
      } else if (response.approvers) {
        approversData.value = response.approvers;
      }

      await fetchAttachments();

      if (exemptionDetails.value?.policyId) {
        const policyName = await fetchPolicyName(
          exemptionDetails.value.policyId,
        );
        policyValue.value = {
          id: exemptionDetails.value.policyId,
          label: policyName || `Policy ${exemptionDetails.value.policyId}`,
        };
      }
    } else {
      showToast({
        type: "error",
        title: "Error",
        message: response?.message || "Failed to load exemption details",
        duration: 5000,
      });
      router.push("/main/reports/closed");
    }
  } catch (error) {
    console.error("Error fetching exemption details:", error);
    showToast({
      type: "error",
      title: "Error",
      message: "Failed to load exemption details",
      duration: 5000,
    });
    router.push("/main/reports/closed");
  } finally {
    loading.value = false;
  }
};

const isViewableInBrowser = (fileType: string): boolean => {
  const extension = "." + fileType.toLowerCase();
  return [".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".pdf"].includes(
    extension,
  );
};

const getAttachmentButtonLabel = (filetype: string): string => {
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
  if (!exemptionId.value) return;

  try {
    const response = await apiFetch(
      `/api/exemption/attachments/${exemptionId.value}`,
      {
        method: "GET",
        parseJson: true,
      },
    );

    if (response) {
      attachments.value = response.data || [];
    }
  } catch (error) {
    console.error("Error fetching attachments:", error);
  }
};

const fetchApprovers = async (policyId: number) => {
  if (!policyId) {
    approversData.value = [];
    return;
  }

  isLoadingApprovers.value = true;
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
      },
    );

    const items = response.success ? response.items : response;

    console.log("Fetched approvers in View page", items);

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
    isLoadingApprovers.value = false;
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

const getApproverStatusTextColor = (respond: number) => {
  const colorMap: Record<number, string> = {
    0: "text-yellow-800 dark:text-yellow-400",
    1: "text-green-800 dark:text-green-400",
    2: "text-red-800 dark:text-red-300",
    3: "text-gray-800 dark:text-gray-400",
  };
  return colorMap[respond] || "text-gray-800";
};

const getStatusDisplay = (status: string) => {
  const statusMap: Record<string, string> = {
    O: "Open",
    P: "Open",
    C: "Closed",
    D: "Draft",
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
    3: "Draft",
  };
  return statusMap[respond] || "Pending";
};

const getApproverStatusColor = (respond: number) => {
  const colorMap: Record<number, string> = {
    0: "bg-yellow-100 text-yellow-800 border border-yellow-900",
    1: "bg-green-100 text-green-800 border border-green-900",
    2: "bg-red-100 text-red-800 border border-red-900",
    3: "bg-gray-100 text-gray-800 border border-gray-900",
  };
  return colorMap[respond] || "bg-gray-100 text-gray-800";
};

const groupedApprovers = computed(() => {
  const mainApprovers: any[] = [];
  const proxiesMap: Map<number, any[]> = new Map();

  approversData.value.forEach((approver) => {
    const isMain = approver.lineSequence === 1;

    if (isMain) {
      mainApprovers.push(approver);
      proxiesMap.set(approver.transApproverId, []);
    } else {
      const mainApprover = mainApprovers.find((m) => m.sort === approver.sort);
      if (mainApprover) {
        const proxies = proxiesMap.get(mainApprover.transApproverId) || [];
        proxies.push(approver);
        proxiesMap.set(mainApprover.transApproverId, proxies);
      }
    }
  });

  return { mainApprovers, proxiesMap };
});

const getProxies = (transApproverId: number): any[] => {
  return groupedApprovers.value.proxiesMap.get(transApproverId) || [];
};

const isApprover = computed((): boolean => {
  console.log(
    "Checking if user is approver with data",
    exemptionDetails.value,
    "and userId",
    userId,
  );
  return exemptionDetails.value?.requesterId === userId?.emplId;
});

const isCurrentUser = (requesterId: number): boolean => {
  return userId?.emplId === requesterId;
};

const handleBack = () => {
  router.push("/main/reports/closed");
};

const handleEdit = () => {
  router.push(`/main/exemptions/edit?id=${exemptionId.value}`);
};
const handleApprove = () => {
  router.push(`/main/exemptions/approve?id=${exemptionId.value}`);
};
onMounted(async () => {
  await fetchMenuList();
  canEdit.value = checkCanEdit("/main/exemptions/");
  canDelete.value = checkCanDelete("/main/exemptions/");

  fetchExemptionDetails();
});

// Helper to check if current user is an approver
const isCurrentUserAnApprover = computed((): boolean => {
  if (!userId?.emplId || !approversData.value?.length) return false;

  return approversData.value.some(
    (approver) =>
      approver.transApproverId === userId?.emplId ||
      approver.approverEmail === userId?.email,
  );
});

// Helper to check if current user needs to take action (hasn't responded yet)
const currentUserPendingAction = computed((): boolean => {
  if (!userId?.emplId || !approversData.value?.length) return false;

  const currentUserApprover = approversData.value.find(
    (x) => String(x.employeeId) === String(userStore.profile.emplId),
  );

  // User needs to act if they're an approver and haven't responded (respond === 0 means pending)
  return currentUserApprover?.respond === 0;
});

//
</script>

<template>
  <div class="min-h-[50vh] bg-gray-50 overflow-auto h-100vh dark:bg-gray-900">
    <header class="bg-white dark:bg-gray-800 shadow">
      <div class="max-w-full py-4 px-4 sm:px-6 lg:px-8 flex justify-between items-center">
        <div class="flex items-center">
          <Button variant="ghost-primary" size="lg" icon="left_arrow" iconPosition="center" customClass="rounded-full"
            @onclick="handleBack" />
          <h1 class="text-2xl ps-5 font-bold text-gray-900 dark:text-gray-100">
            View Request
          </h1>
        </div>
        <div class="flex items-center gap-3">
          <!-- <Button variant="outline-info" size="md" @onclick="handleBack">
            Back to List
          </Button> -->
          <Button v-if="
            canEdit &&
            (exemptionDetails?.status === 'D' ||
              (exemptionDetails?.status === 'O' &&
                exemptionDetails?.requesterId === userId?.emplId))
          " variant="primary" size="md" icon="edit" iconPosition="left" @onclick="handleEdit">
            Edit
          </Button>
          <Button v-if="currentUserPendingAction" variant="outline-success" size="md" icon="user_approver"
            iconPosition="left" @onclick="handleApprove">
            Respond
          </Button>
        </div>
      </div>
    </header>

    <main class="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
      <div v-if="loading" class="flex items-center justify-center py-20">
        <svg class="animate-spin h-8 w-8 text-blue-600 dark:text-blue-400" xmlns="http://www.w3.org/2000/svg"
          fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor"
            d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z">
          </path>
        </svg>
        <span class="ml-3 text-gray-600 dark:text-gray-400">Loading exemption details...</span>
      </div>
      <div v-else-if="exemptionDetails" class="space-y-6">
        <div class="grid grid-cols-4 gap-6">
          <div class="sm:col-span-full md:col-span-2 bg-white dark:bg-gray-800 rounded-lg shadow p-6">
            <div class="mb-8">
              <div class="text-xl font-semibold text-gray-800 dark:text-gray-100 mb-4 pb-2 border-b">
                <div class="flex justify-between items-center">
                  <h2>Request Information</h2>
                  <div v-if="exemptionDetails">
                    <span class="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium" :class="{
                      'bg-blue-100 text-blue-800 dark:bg-blue-800/20 dark:text-blue-300 border border-blue-900':
                        exemptionDetails.status === 'O' ||
                        exemptionDetails.status === 'P',
                      'bg-gray-200 text-gray-800 dark:bg-gray-700/20 dark:text-gray-300 border border-gray-900':
                        exemptionDetails.status === 'C',
                      'bg-gray-100 text-gray-800 dark:bg-gray-700/20 dark:text-gray-300 border border-gray-900':
                        exemptionDetails.status === 'D',
                    }">
                      {{ getStatusDisplay(exemptionDetails.status) }}
                    </span>
                  </div>
                </div>
              </div>
              <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div>
                  <label class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1">
                    Transaction Number
                  </label>
                  <p class="text-gray-900 dark:text-gray-100">
                    {{ exemptionDetails.transactionNumber || "N/A" }}
                  </p>
                </div>
                <div>
                  <label class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1">
                    Requestor Name
                  </label>
                  <p class="text-gray-900 dark:text-gray-100">
                    {{ exemptionDetails.requesterName || "N/A" }}
                  </p>
                </div>
              </div>
              <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mt-4">
                <div>
                  <label class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1">
                    Created Date
                  </label>
                  <p class="text-gray-900 dark:text-gray-100">
                    {{ formatDate(exemptionDetails.createdDate) }}
                  </p>
                </div>
              </div>
            </div>

            <div class="mb-8">
              <div class="space-y-6">
                <div class="grid grid-cols-1 gap-6">
                  <div>
                    <label class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1">
                      Policy To Override
                    </label>
                    <p class="text-gray-900 dark:text-gray-100">
                      {{
                        policyValue?.label ||
                        exemptionDetails.policyName ||
                        "N/A"
                      }}
                    </p>
                  </div>
                </div>

                <div>
                  <label class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1">
                    Details
                  </label>
                  <div class="bg-gray-50  rounded-lg p-4 border border-gray-200 dark:border-gray-600 dark:bg-gray-700">
                    <p class="text-gray-900 dark:text-gray-100 whitespace-pre-wrap">
                      {{ exemptionDetails.details || "No details provided" }}
                    </p>
                  </div>
                </div>
              </div>
            </div>

            <div class="mb-8">
              <h2 class="text-xl font-semibold text-gray-800 dark:text-gray-100 mb-4 pb-2 border-b">
                Attachments
              </h2>

              <div v-if="attachments.length > 0" class="space-y-2">
                <div v-for="attachment in attachments" :key="attachment.attachId"
                  class="flex items-center justify-between p-3 bg-gray-50  rounded-lg border border-gray-200 dark:bg-gray-700 dark:border-gray-600">
                  <div class="flex items-center">
                    <svg class="h-5 w-5 text-gray-400 dark:text-gray-500 mr-3" fill="none" viewBox="0 0 24 24"
                      stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                        d="M15.172 7l-6.586 6.586a2 2 0 102.828 2.828l6.414-6.586a4 4 0 00-5.656-5.656l-6.415 6.585a6 6 0 108.486 8.486L20.5 13" />
                    </svg>
                    <div>
                      <p class="text-sm font-medium text-gray-900 dark:text-gray-100">
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
                  <a :href="`${$config.public.apiBase}/uploads/attachments/${attachment.newfilename}`"
                    :download="!isViewableInBrowser(attachment.filetype)" :class="isViewableInBrowser(attachment.filetype)
                      ? 'inline-flex items-center px-3 py-1 rounded-md text-sm font-medium bg-blue-100 text-blue-700 hover:bg-blue-200  dark:bg-blue-800 dark:text-blue-300 dark:border-blue-800 dark:hover:bg-blue-700 dark:hover:text-white'
                      : 'inline-flex items-center px-3 py-1 rounded-md text-sm font-medium bg-green-100 text-green-700 hover:bg-green-200 dark:bg-green-800 dark:text-green-300 dark:hover:bg-green-700 dark:hover:text-white'
                      " target="_blank">
                    {{ getAttachmentButtonLabel(attachment.filetype) }}
                  </a>
                </div>
              </div>
              <p v-else class="text-gray-500 dark:text-gray-400 text-sm">
                No attachments for this request.
              </p>
            </div>
          </div>

          <div class="col-span-2 bg-white dark:bg-gray-800 rounded-lg shadow px-6 py-6">
            <div class="mb-8">
              <div class="border-b flex justify-between">
                <h2 class="text-xl font-semibold text-gray-800 dark:text-gray-100 mb-2">
                  Approvers
                </h2>
                <!-- <div>
                  <span
                    v-if="exemptionDetails.finalDecision"
                    class="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium"
                    :class="{
                      'bg-yellow-100 text-yellow-800 dark:bg-yellow-800/20 dark:text-yellow-300':
                        exemptionDetails.finalDecision === 'Pending',
                      'bg-green-100 text-green-800 dark:bg-green-800/20 dark:text-green-300':
                        exemptionDetails.finalDecision === 'Approved',
                      'bg-red-100 text-red-800 dark:bg-red-800/20 dark:text-red-300':
                        exemptionDetails.finalDecision === 'Disapproved',
                      'bg-gray-100 text-gray-800 dark:bg-gray-700/20 dark:text-gray-300':
                        exemptionDetails.finalDecision === 'Draft',
                    }"
                  >
                    {{
                      getFinalDecisionDisplay(exemptionDetails.finalDecision)
                    }}
                  </span>
                </div> -->
              </div>
            </div>

            <div v-if="approversData && approversData.length > 0" class="space-y-4">
              <template v-for="(approver, index) in groupedApprovers.mainApprovers" :key="approver.transApproverId">
                <div class="bg-gray-50 rounded-lg p-3 border border-gray-200 dark:bg-gray-700 dark:border-gray-600">
                  <div class="flex justify-between items-start mb-2">
                    <div class="flex items-start">
                      <span
                        class="flex items-center justify-center w-6 h-6 rounded-full bg-blue-100 text-blue-600 text-sm font-medium mr-3 mt-1dark:bg-gray-700 dark:border-gray-600">
                        {{ index + 1 }}
                      </span>

                      <div>
                        <p class="text-sm font-medium text-gray-900 dark:text-gray-100">
                          {{ approver.approverName }}
                          <span v-if="isCurrentUser(approver.employeeId)"
                            class="text-xs text-green-500 bg-green-100 px-2 py-1 rounded">You</span>
                        </p>
                        <p class="text-xs text-gray-500 dark:text-gray-400">
                          {{ approver.approverEmail || "" }}
                        </p>
                        <div class="flex items-center gap-2 mt-1">
                          <span v-if="approver.approverRole"
                            class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-600 mt-1">
                            {{ approver.approverRole }}
                          </span>
                        </div>
                        <div v-if="approver.remarks" class="text-sm bg-gray-50 dark:bg-gray-700 rounded-lg px-3">
                          Remark:
                        </div>
                        <div v-if="approver.remarks" class="px-6 pb-2">
                          <span class="text-sm text-gray-900 dark:text-gray-100 whitespace-pre-wrap">
                            {{ approver.remarks }}
                          </span>
                        </div>
                        <div class="text-xs mt-1" :class="getApproverStatusTextColor(approver.respond)">
                          <span v-if="approver.respondDate">
                            Responded on
                            {{ formatDateTime(approver.respondDate).date }} •
                            {{ formatDateTime(approver.respondDate).time }}
                          </span>
                        </div>
                      </div>
                    </div>
                    <div class="flex flex-col items-end gap-1">
                      <span v-if="approver.respond != 3"
                        class="inline-flex items-center px-2 py-1 rounded text-xs font-medium"
                        :class="getApproverStatusColor(approver.respond)">
                        {{ getApproverStatusDisplay(approver.respond) }}
                      </span>
                    </div>
                  </div>

                  <div v-if="getProxies(approver.transApproverId).length > 0" class="ml-8">
                    <div v-for="proxy in getProxies(approver.transApproverId)" :key="proxy.transApproverId"
                      class="flex items-start justify-between py-2 border-b border-gray-200 last:border-0">
                      <div class="flex items-start">
                        <span class="w-2 h-2 rounded-full bg-orange-400 mt-1.5"></span>
                        <div class="ps-3">
                          <p class="text-sm text-gray-700 dark:text-gray-200">
                            {{ proxy.approverName }}
                            <span v-if="isCurrentUser(proxy.employeeId)"
                              class="text-xs text-green-500 bg-green-100 px-2 py-1 rounded">You</span>
                          </p>
                          <p class="text-xs text-gray-500 dark:text-gray-400">
                            {{ proxy.approverEmail || "" }}
                          </p>
                          <span
                            class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-orange-100 text-orange-700 my-1">
                            {{ proxy.approverRole }}
                          </span>
                          <div v-if="approver.respond != 1">
                            <div v-if="proxy.remarks" class="text-sm bg-gray-50 dark:bg-gray-700 rounded-lg px-3">
                              Remark:
                            </div>
                            <div v-if="proxy.remarks" class="px-6 pb-2">
                              <span class="text-sm text-gray-900 dark:text-gray-100 whitespace-pre-wrap">
                                {{ proxy.remarks }}
                              </span>
                            </div>
                            <div class="text-xs" :class="getApproverStatusTextColor(proxy.respond)">
                              <span v-if="proxy.respondDate">
                                Responded on
                                {{ formatDateTime(proxy.respondDate).date }} •
                                {{ formatDateTime(proxy.respondDate).time }}
                              </span>
                            </div>
                          </div>
                        </div>
                      </div>
                      <div class="flex flex-col items-end gap-1">
                        <span v-if="approver.respond != 1"
                          class="inline-flex items-center px-2 py-1 rounded text-xs font-medium"
                          :class="getApproverStatusColor(proxy.respond)">
                          {{ getApproverStatusDisplay(proxy.respond) }}
                        </span>
                      </div>
                    </div>
                  </div>
                </div>
              </template>
            </div>
            <div v-else class="text-sm flex items-center justify-center h-24 text-gray-400 dark:text-gray-500">
              <span>No approvers assigned yet.</span>
            </div>
          </div>

          <div v-if="exemptionDetails?.remarks" class="mb-8">
            <h2 class="text-xl font-semibold text-gray-800 dark:text-gray-100 mb-4 pb-2 border-b">
              Remarks
            </h2>
            <div class="bg-gray-50 dark:bg-gray-700 rounded-lg p-4 border border-gray-200 dark:border-gray-600">
              <p class="text-gray-900 dark:text-gray-100 whitespace-pre-wrap wrap-break-words sm:break-all">
                <span class="inline-block max-w-full bg-gray-100 dark:bg-gray-700 rounded-md px-2 py-1">
                  {{ exemptionDetails.remarks }}
                </span>
              </p>
            </div>
          </div>
        </div>
      </div>

      <div v-else class="bg-white dark:bg-gray-800 rounded-lg shadow px-6 py-12 text-center">
        <p class="text-gray-500 dark:text-gray-400 text-lg mb-4">
          No exemption details found.
        </p>
        <Button variant="primary" size="lg" @onclick="handleBack">
          Back to List
        </Button>
      </div>
    </main>
  </div>
</template>
