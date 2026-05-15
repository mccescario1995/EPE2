<script setup lang="ts">
const router = useRouter();
const route = useRoute();
const { showToast } = useToast();

const loading = ref(false);
const exemptionData = ref<any>(null);
const approversData = ref<any[]>([]);
const policyName = ref<string>("");
const attachments = ref<any[]>([]);

const userStore = useUserStore();
const userId = userStore.profile;

const requesterId = computed(() => exemptionData.value?.requesterId);

const exemptStatus = computed(() => exemptionData.value?.status);

const config = useRuntimeConfig();

// Get ID from route query
const exemptionId = computed(() => {
  return Number(route.query.id) || null;
});

// Fetch exemption details
const fetchExemptionDetails = async () => {
  if (!exemptionId.value) {
    showToast({
      type: "error",
      title: "Error",
      message: "Invalid exemption ID",
      duration: 5000,
    });
    router.push("/exemptions/all");
    return;
  }

  loading.value = true;
  try {
    const response = await apiFetch(`/api/exemption/view/${exemptionId.value}`, {
      method: "POST",
      parseJson: true,
    });

    if (response) {
      exemptionData.value = response.item;
      approversData.value = response.approvers || [];

      // If there's a policy ID, fetch the policy name
      if (response.item.policyid) {
        fetchPolicyName(response.item.policyid);
      }
      // Fetch attachments
      await fetchAttachments();
    } else {
      showToast({
        type: "error",
        title: "Error",
        message: response.message || "Failed to load exemption details",
        duration: 5000,
      });
    }
  } catch (error) {
    console.error("Error fetching exemption details:", error);
    showToast({
      type: "error",
      title: "Error",
      message: "Failed to load exemption details",
      duration: 5000,
    });
  } finally {
    loading.value = false;
  }
};

// Fetch attachments
const fetchAttachments = async () => {
  if (!exemptionId.value) return;

  try {
    const response = await apiFetch(`/api/exemption/attachments/${exemptionId.value}`, {
      method: "GET",
      parseJson: true,
    });

    if (response) {
      attachments.value = response.data || [];
    } else {
      console.error("Failed to fetch attachments:", response);
    }
  } catch (error) {
    console.error("Error fetching attachments:", error);
  }
};

// Fetch policy name
const fetchPolicyName = async (policyId: number) => {
  try {
    const response = await apiFetch("/api/policy", {
      method: "POST",
      parseJson: true,
    });
    const items = response.success ? response.items : response;
    if (items && Array.isArray(items)) {
      const policy = items.find((p: any) => p.policyid === policyId);
      if (policy) {
        policyName.value = policy.description || `Policy ${policyId}`;
      }
    }
  } catch (error) {
    console.error("Error fetching policy name:", error);
  }
};

// Format date
const formatDate = (date: string | Date | null) => {
  if (!date) return "N/A";
  return new Date(date).toLocaleDateString("en-US", {
    year: "numeric",
    month: "long",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
};

// Get status display
const getStatusDisplay = (status: string) => {
  const statusMap: Record<string, string> = {
    O: "Open",
    P: "Open", // Approval in progress (one or more approved, waiting for all)
    C: "Closed",
    D: "Draft",
  };
  return statusMap[status] || status;
};

// Get final decision display
const getFinalDecisionDisplay = (decision: string) => {
  const decisionMap: Record<string, string> = {
    Pending: "Pending",
    Approved: "Approved",
    Disapproved: "Disapproved",
    Draft: "Draft",
  };
  return decisionMap[decision] || decision;
};

// Get approver status display
const getApproverStatusDisplay = (respond: number) => {
  const statusMap: Record<number, string> = {
    0: "Pending",
    1: "Approved",
    2: "Disapproved",
    3: "Draft",
  };
  return statusMap[respond] || "Pending";
};

// Get approver status color
const getApproverStatusColor = (respond: number) => {
  const colorMap: Record<number, string> = {
    0: "bg-yellow-100 text-yellow-800 border border-yellow-900",
    1: "bg-green-100 text-green-800 border border-green-900",
    2: "bg-red-100 text-red-800 border border-red-900",
    3: "bg-gray-100 text-gray-800 border border-gray-900",
  };
  return colorMap[respond] || "bg-gray-100 text-gray-800";
};

// Handle back navigation
const handleBack = () => {
  router.push("/exemptions/all");
};

// Handle edit navigation
const handleEdit = () => {
  router.push(`/exemptions/edit?id=${exemptionId.value}`);
};

// Fetch on mount
onMounted(() => {
  fetchExemptionDetails();
});

// Computed property to group main approvers with their proxies
// Main approvers: linesequence = 1 (regardless of sort value)
// Proxies: linesequence > 1, grouped under main approvers by matching sort values
// IM (Immediate Head) can also have proxies
const groupedApprovers = computed(() => {
  const mainApprovers: any[] = [];
  const proxiesMap: Map<number, any[]> = new Map();

  approversData.value.forEach((approver) => {
    // Determine if approver is Main (linesequence === 1) or Proxy (linesequence > 1)
    const isMain = approver.lineSequence === 1;
    const isIM = approver.approverType === "IM";

    if (isMain) {
      // This is a Main Approver (linesequence=1) - add to main list and create proxy array
      mainApprovers.push(approver);
      proxiesMap.set(approver.transApproverId, []);
    } else {
      // This is a Proxy (linesequence > 1) - group under main approver by matching sort
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

// Helper function to get proxies for a main approver
// Returns the array of proxy approvers associated with a specific main approver's transapproverid
const getProxies = (transApproverId: number): any[] => {
  return groupedApprovers.value.proxiesMap.get(transApproverId) || [];
};

// Helper function to check if a group (main + proxies) is satisfied (someone in group approved)
const isGroupSatisfied = (transApproverId: number): boolean => {
  const { mainApprovers, proxiesMap } = groupedApprovers.value;
  const mainApprover = mainApprovers.find((a) => a.transApproverId === transApproverId);
  if (!mainApprover) return false;

  const groupApprovers = [mainApprover, ...(proxiesMap.get(transApproverId) || [])];
  return groupApprovers.some((a) => a.respond === 1);
};

// Get the current (active) approver's transApproverId - the first group that hasn't been satisfied
const currentApproverId = computed(() => {
  const { mainApprovers, proxiesMap } = groupedApprovers.value;

  for (const approver of mainApprovers) {
    // Get all approvers in this group (main + proxies)
    const groupApprovers = [
      approver,
      ...(proxiesMap.get(approver.transApproverId) || []),
    ];

    // Check if ANY in the group has approved
    const hasApproved = groupApprovers.some((a) => a.respond === 1);

    // Check if ANY in the group is pending
    const hasPending = groupApprovers.some((a) => a.respond === 0);

    // If no one has approved yet and someone is pending, this group is active
    if (!hasApproved && hasPending) {
      // Find the first pending one in this group
      const pending = groupApprovers.find((a) => a.respond === 0);
      return pending?.transApproverId;
    }

    // If someone has approved in this group, skip to next group
    // Continue to next iteration
  }

  return null;
});

// Resend notification to approver
const isResending = ref<number | null>(null);
const resendToApprover = async (transApproverId: number) => {
  isResending.value = transApproverId;
  try {
    const response = await apiFetch(`/api/exemption/resend/${exemptionId.value}`, {
      method: "POST",
      body: JSON.stringify({ transApproverId }),
      headers: {
        "Content-Type": "application/json",
      },
    });

    if (response) {
      showToast({
        type: "success",
        title: "Success",
        message: "Notification sent to approver successfully!",
        duration: 5000,
      });
    } else {
      showToast({
        type: "error",
        title: "Error",
        message: response?.message || "Failed to resend notification",
        duration: 5000,
      });
    }
  } catch (error: any) {
    console.error("Error resending notification:", error);
    showToast({
      type: "error",
      title: "Error",
      message: error.message || "An error occurred while resending notification",
      duration: 5000,
    });
  } finally {
    isResending.value = null;
  }
};
</script>

<template>
  <div class="min-h-[50vh] bg-gray-50 overflow-auto h-100vh">
    <header class="bg-white shadow">
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
          <h1 class="text-2xl ps-5 font-bold text-gray-900">Request Details</h1>
        </div>
        <Button
          v-if="requesterId === userId.emplId && exemptStatus === 'O'"
          variant="primary"
          size="md"
          icon="edit"
          iconPosition="left"
          @onclick="handleEdit"
        >
          Edit Request
        </Button>
      </div>
    </header>

    <main class="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
      <!-- Loading State -->
      <div v-if="loading" class="flex items-center justify-center py-20">
        <svg
          class="animate-spin h-8 w-8 text-blue-600"
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
          ></path>
        </svg>
        <span class="ml-3 text-gray-600">Loading details...</span>
      </div>

      <!-- Content -->
      <div v-else-if="exemptionData">
        <div class="grid grid-cols-4 gap-6">
          <div class="sm:col-span-full md:col-span-2 bg-white rounded-lg shadow p-6">
            <!-- Transaction Info -->
            <div class="mb-8">
              <div class="text-xl font-semibold text-gray-800 mb-4 pb-2 border-b">
                <div class="flex justify-between items-center">
                  <h2>Request Information</h2>
                  <div>
                    <span
                      class="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium"
                      :class="{
                        'bg-blue-100 text-blue-800 border border-blue-900':
                          exemptionData.status === 'O' || exemptionData.status === 'P',
                        'bg-gray-200 text-gray-800 border border-gray-900':
                          exemptionData.status === 'C',
                      }"
                    >
                      {{ getStatusDisplay(exemptionData.status) }}
                    </span>
                  </div>
                </div>
              </div>
              <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div>
                  <label class="block text-sm font-medium text-gray-500 mb-1">
                    Transaction Number
                  </label>
                  <p class="text-gray-900 font-medium">
                    {{ exemptionData.transactionNumber || "N/A" }}
                  </p>
                </div>

                <div>
                  <label class="block text-sm font-medium text-gray-500 mb-1">
                    Requestor Name
                  </label>
                  <p class="text-gray-900">
                    {{ exemptionData.requesterName || "N/A" }}
                  </p>
                </div>
              </div>
            </div>

            <div class="mb-8">
              <div class="space-y-6">
                <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <div>
                    <label class="block text-sm font-medium text-gray-500 mb-1">
                      Created Date
                    </label>
                    <p class="text-gray-900">
                      {{ formatDate(exemptionData.createdDate) }}
                    </p>
                  </div>

                  <div>
                    <!-- <div v-if="exemptionData.modifiedDate"> -->
                    <label class="block text-sm font-medium text-gray-500 mb-1">
                      Modified Date
                    </label>
                    <p class="text-gray-900">
                      <!-- {{ formatDate(exemptionData.createdDate) }} -->
                      {{ formatDate(exemptionData.modifiedDate) }}
                    </p>
                  </div>
                </div>
              </div>
            </div>

            <!-- Policy & Details -->
            <div class="mb-8">
              <div class="space-y-6">
                <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <div>
                    <label class="block text-sm font-medium text-gray-500 mb-1">
                      Policy To Override
                    </label>
                    <p class="text-gray-900">
                      {{ exemptionData.policyName }}
                    </p>
                  </div>

                  <!-- <div>
                    <label class="block text-sm font-medium text-gray-500 mb-1">
                      Request Status
                    </label>
                    <span
                      class="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium"
                      :class="{
                        'bg-yellow-100 text-yellow-800':
                          exemptionData.finalDecision === 'Pending',
                        'bg-green-100 text-green-800':
                          exemptionData.finalDecision === 'Approved',
                        'bg-red-100 text-red-800':
                          exemptionData.finalDecision === 'Disapproved',
                      }"
                    >
                      
                    </span>
                  </div> -->
                </div>
                <div>
                  <label class="block text-sm font-medium text-gray-500 mb-1">
                    Details
                  </label>
                  <div class="bg-gray-50 rounded-lg p-4 border border-gray-200 h-[100px]">
                    <p class="text-gray-900 whitespace-pre-wrap">
                      {{ exemptionData.details || "No details provided" }}
                    </p>
                  </div>
                </div>
              </div>
            </div>

            <!-- Attachments -->
            <div class="mb-8">
              <h2 class="text-xl font-semibold text-gray-800 mb-4 pb-2 border-b">
                Attachments
              </h2>
              <div v-if="attachments.length > 0" class="space-y-2">
                <div
                  v-for="attachment in attachments"
                  :key="attachment.attachId"
                  class="flex items-center justify-between p-3 bg-gray-50 rounded-lg border border-gray-200"
                >
                  <div class="flex items-center">
                    <svg
                      class="h-5 w-5 text-gray-400 mr-3"
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
                      <p class="text-sm font-medium text-gray-900">
                        {{ attachment.uploadFilename }}
                      </p>
                      <p class="text-xs text-gray-500">
                        {{ attachment.referenceType }} • {{ attachment.filetype }} •
                        {{ new Date(attachment.createdate).toLocaleDateString() }}
                      </p>
                    </div>
                  </div>
                  <a
                    :href="`${config.public.apiBase}/uploads/attachments/${attachment.newfilename}`"
                    target="_blank"
                    class="inline-flex items-center px-3 py-1 rounded-md text-sm font-medium bg-blue-100 text-blue-700 hover:bg-blue-200"
                  >
                    View
                  </a>
                </div>
              </div>
              <div v-else class="text-gray-500 text-sm">No attachments found.</div>
            </div>
          </div>

          <div
            class="sm:col-span-full md:col-span-2 bg-white rounded-lg shadow px-6 py-6"
          >
            <!-- Approvers -->
            <div class="mb-8">
              <div class="border-b flex justify-between mb-4">
                <h2 class="text-xl font-semibold text-gray-800 mb-2">Approvers Status</h2>
                <div>
                  <span
                    v-if="exemptionData.finalDecision"
                    class="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium"
                    :class="{
                      'bg-yellow-100 text-yellow-800 border border-yellow-900':
                        exemptionData.finalDecision === 'Pending',
                      'bg-green-100 text-green-800 border border-green-900':
                        exemptionData.finalDecision === 'Approved',
                      'bg-red-100 text-red-800 border border-red-900':
                        exemptionData.finalDecision === 'Disapproved',
                    }"
                  >
                    {{ getFinalDecisionDisplay(exemptionData.finalDecision) }}
                  </span>
                </div>
              </div>

              <div v-if="approversData && approversData.length > 0" class="space-y-4">
                <!-- Loop through main approvers (numbered 1, 2, 3...) -->
                <template
                  v-for="(approver, index) in groupedApprovers.mainApprovers"
                  :key="approver.transApproverId"
                >
                  <!-- Main Approver Card - displays as numbered circle with status badge -->
                  <div class="bg-gray-50 rounded-lg p-3 border border-gray-200">
                    <div class="flex justify-between items-start mb-2">
                      <div class="flex items-start">
                        <!-- Numbered circle indicator (1, 2, 3...) -->
                        <span
                          class="flex items-center justify-center w-6 h-6 rounded-full bg-blue-100 text-blue-600 text-sm font-medium mr-3 mt-1"
                        >
                          {{ index + 1 }}
                        </span>

                        <div>
                          <p class="text-sm font-medium text-gray-900">
                            {{ approver.approverName }}
                          </p>
                          <p class="text-xs text-gray-500">
                            {{ approver.approverEmail || "" }}
                          </p>
                          <div class="flex items-center gap-2 mt-1">
                            <!-- <p class="text-xs text-gray-400">{{ approver.approverType || '' }}</p> -->
                            <span
                              v-if="approver.approverRole"
                              class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-600"
                            >
                              {{ approver.approverRole }}
                            </span>
                          </div>
                          <div v-if="approver.remarks" class="mt-2 mx-2 w-full">
                            <label class="block text-sm font-medium text-gray-500 mb-1">
                              Remarks:
                            </label>
                            <div class="ml-3 w-auto">
                              <p class="text-gray-900 whitespace-pre-wrap">
                                {{ approver.remarks || "No remarks provided" }}
                              </p>
                            </div>
                          </div>
                        </div>
                      </div>
                      <!-- Approver status badge (Pending/Approved/Disapproved) -->
                      <div class="flex flex-col items-end gap-1">
                        <span
                          class="inline-flex items-center px-2 py-1 rounded text-xs font-medium"
                          :class="getApproverStatusColor(approver.respond)"
                        >
                          {{ getApproverStatusDisplay(approver.respond) }}
                        </span>
                        <!-- Decision badge for decided approvers -->
                        <!-- <span
                          v-if="approver.respond === 1"
                          class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-green-200 text-green-800"
                        >
                          Approved
                        </span>
                        <span
                          v-if="approver.respond === 2"
                          class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-red-200 text-red-800"
                        >
                          Disapproved
                        </span> -->
                        <!-- Resend button only for current pending approver (and not satisfied by group) -->
                        <button
                          v-if="
                            approver.respond === 0 &&
                            approver.transApproverId === currentApproverId &&
                            !isGroupSatisfied(approver.transApproverId)
                            // approver.employeeId === userId.emplId // Only show resend button to the pending approver themselves (and not if group is already satisfied by another proxy)
                          "
                          @click="resendToApprover(approver.transApproverId)"
                          :disabled="isResending === approver.transApproverId"
                          class="flex items-center px-2 py-1 rounded text-xs font-medium bg-blue-100 text-blue-600 hover:bg-blue-200 disabled:opacity-50"
                          title="Resend notification to approver"
                        >
                          <svg
                            v-if="isResending === approver.transApproverId"
                            class="animate-spin h-3 w-3 mr-1"
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
                            ></path>
                          </svg>
                          <svg
                            v-else
                            class="h-3 w-3 mr-1"
                            xmlns="http://www.w3.org/2000/svg"
                            width="14"
                            height="14"
                            viewBox="0 0 14 14"
                          >
                            <path
                              fill="currentColor"
                              fill-rule="evenodd"
                              d="M11.688.103a1.724 1.724 0 0 1 2.208 2.208L10.39 12.816a1.72 1.72 0 0 1-1.242 1.137a1.7 1.7 0 0 1-1.619-.462L5.716 11.69l-1.89.976a.625.625 0 0 1-.911-.571l.08-3.132L.5 6.467a1.7 1.7 0 0 1-.466-1.542A1.72 1.72 0 0 1 1.17 3.61l.005-.002zm.69 1.157a.47.47 0 0 0-.286.026L1.572 4.793a.47.47 0 0 0-.31.363l-.003.014a.45.45 0 0 0 .123.412l1.95 1.948l6.33-4.177a.5.5 0 0 1 .616.783L4.225 9.8l-.033 1.27l1.355-.7a.625.625 0 0 1 .728.111l2.138 2.127l.006.006a.45.45 0 0 0 .433.124l.008-.002a.47.47 0 0 0 .342-.312v-.002l3.511-10.516l.007-.02a.47.47 0 0 0-.342-.626"
                              clip-rule="evenodd"
                            />
                          </svg>

                          Resend
                        </button>
                      </div>
                    </div>

                    <div
                      v-if="getProxies(approver.transApproverId).length > 0"
                      class="ml-8"
                    >
                      <!-- Loop through proxy approvers for this main approver -->
                      <div
                        v-for="proxy in getProxies(approver.transApproverId)"
                        :key="proxy.transApproverId"
                        class="flex items-start justify-between py-2 border-b border-gray-200 last:border-0"
                      >
                        <div class="flex items-start">
                          <span class="w-2 h-2 rounded-full bg-blue-400 mt-1.5"></span>
                          <!-- <span class="font-bold">></span> -->
                          <div class="ps-3">
                            <p class="text-sm text-gray-700">
                              {{ proxy.approverName }}
                            </p>
                            <p class="text-xs text-gray-500">
                              {{ proxy.approverEmail || "" }}
                            </p>
                            <span
                              class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-orange-100 text-orange-800 mb-1"
                            >
                              {{ proxy.approverRole }}
                            </span>
                          </div>
                        </div>
                        <div class="flex flex-col items-end gap-1">
                          <!-- Proxy approver status badge -->
                          <span
                            class="inline-flex items-center px-2 py-1 rounded text-xs font-medium"
                            :class="getApproverStatusColor(proxy.respond)"
                          >
                            {{ getApproverStatusDisplay(proxy.respond) }}
                          </span>
                          <!-- Decision badge for decided proxy approvers -->
                          <!-- <span
                            v-if="proxy.respond === 1"
                            class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-green-200 text-green-800"
                          >
                            Approved
                          </span>
                          <span
                            v-if="proxy.respond === 2"
                            class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-red-200 text-red-800"
                          >
                            Disapproved
                          </span> -->
                          <!-- Resend button only for current pending proxy (and not satisfied by group) -->
                          <button
                            v-if="
                              proxy.respond === 0 &&
                              proxy.transApproverId === currentApproverId &&
                              !isGroupSatisfied(approver.transApproverId) &&
                              proxy.employeeId === userId.emplId
                            "
                            @click="resendToApprover(proxy.transApproverId)"
                            :disabled="isResending === proxy.transApproverId"
                            class="inline-flex items-center px-2 py-1 rounded text-xs font-medium bg-blue-100 text-blue-600 hover:bg-blue-200 disabled:opacity-50"
                            title="Resend notification to proxy"
                          >
                            <svg
                              v-if="isResending === proxy.transApproverId"
                              class="animate-spin h-3 w-3 mr-1"
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
                              ></path>
                            </svg>
                            <svg
                              v-else
                              class="h-3 w-3 mr-1"
                              xmlns="http://www.w3.org/2000/svg"
                              fill="none"
                              viewBox="0 0 24 24"
                              stroke="currentColor"
                            >
                              <path
                                stroke-linecap="round"
                                stroke-linejoin="round"
                                stroke-width="2"
                                d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
                              />
                            </svg>
                            Resend
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>
                </template>
              </div>
              <div
                v-else
                class="text-gray-500 text-sm flex items-center justify-center h-24"
              >
                No approvers assigned yet.
              </div>
            </div>

            <!-- Remarks (if any) -->
            <div v-if="exemptionData.remarks" class="mb-8">
              <h2 class="text-xl font-semibold text-gray-800 mb-4 pb-2 border-b">
                Remarks
              </h2>
              <div class="bg-gray-50 rounded-lg p-4 border border-gray-200">
                <p class="text-gray-900 whitespace-pre-wrap">
                  {{ exemptionData.remarks }}
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Not Found State -->
      <div v-else class="bg-white rounded-lg shadow px-6 py-12 text-center">
        <p class="text-gray-500 text-lg mb-4">No exemption details found.</p>
        <Button variant="primary" size="lg" @onclick="handleBack"> Back to List </Button>
      </div>
    </main>
  </div>
</template>
