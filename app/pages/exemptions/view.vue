<script setup lang="ts">
const router = useRouter();
const route = useRoute();
const { showToast } = useToast();

const loading = ref(false);
const exemptionData = ref<any>(null);
const approversData = ref<any[]>([]);
const policyName = ref<string>("");

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
    router.push("/exemptions");
    return;
  }

  loading.value = true;
  try {
    const response = await apiFetch(
      `/api/exception/view/${exemptionId.value}`,
      {
        method: "POST",
        parseJson: true,
      },
    );

    console.log("Fetched exemption view details:", response);

    if (response.success) {
      exemptionData.value = response.item;
      approversData.value = response.approvers || [];
      // If there's a policy ID, fetch the policy name
      if (response.item.policyid) {
        fetchPolicyName(response.item.policyid);
      }

      console.log("Approvers data set:", approversData.value);
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
    P: "Pending Approval",
    C: "Completed",
  };
  return statusMap[status] || status;
};

// Get final decision display
const getFinalDecisionDisplay = (decision: string) => {
  const decisionMap: Record<string, string> = {
    Pending: "Pending",
    Approved: "Approved",
    Disapproved: "Disapproved",
  };
  return decisionMap[decision] || decision;
};

// Get approver status display
const getApproverStatusDisplay = (respond: number) => {
  const statusMap: Record<number, string> = {
    0: "Pending",
    1: "Approved",
    2: "Disapproved",
  };
  return statusMap[respond] || "Pending";
};

// Get approver status color
const getApproverStatusColor = (respond: number) => {
  const colorMap: Record<number, string> = {
    0: "bg-yellow-100 text-yellow-800",
    1: "bg-green-100 text-green-800",
    2: "bg-red-100 text-red-800",
  };
  return colorMap[respond] || "bg-gray-100 text-gray-800";
};

// Handle back navigation
const handleBack = () => {
  router.push("/exemptions");
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
    const isIM = approver.approverType === 'IM';
    
    if (isMain) {
      // This is a Main Approver (linesequence=1) - add to main list and create proxy array
      mainApprovers.push(approver);
      proxiesMap.set(approver.transApproverId, []);
    } else {
      // This is a Proxy (linesequence > 1) - group under main approver by matching sort
      const mainApprover = mainApprovers.find(
        (m) => m.sort === approver.sort
      );
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

</script>

<template>
  <div class="min-h-[50vh] bg-gray-50 overflow-auto h-100vh">
    <header class="bg-white shadow">
      <div
        class="max-w-7xl me-auto py-4 px-4 sm:px-6 lg:px-8 flex justify-between items-center"
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
          <h1 class="text-3xl ps-5 font-bold text-gray-900">Request Details</h1>
        </div>
        <Button
          v-if="exemptionData"
          variant="primary"
          size="lg"
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
          <div
            class="sm:col-span-full md:col-span-2 bg-white rounded-lg shadow p-6"
          >
            <!-- Transaction Info -->
            <div class="mb-8">
              <div
                class="text-xl font-semibold text-gray-800 mb-4 pb-2 border-b"
              >
                <div class="flex justify-between items-center">
                  <h2>Transaction Information</h2>
                  <div>
                    <span
                      class="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium"
                      :class="{
                        'bg-blue-100 text-blue-800':
                          exemptionData.status === 'O',
                        'bg-yellow-100 text-yellow-800':
                          exemptionData.status === 'P',
                        'bg-green-100 text-green-800':
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
                    Transaction ID
                  </label>
                  <p class="text-gray-900 font-medium">
                    {{ exemptionData.transactionId }}
                  </p>
                </div>
                <div>
                  <label class="block text-sm font-medium text-gray-500 mb-1">
                    Transaction Number
                  </label>
                  <p class="text-gray-900 font-medium">
                    {{ exemptionData.transactionNumber || "N/A" }}
                  </p>
                </div>
              </div>
            </div>

            <!-- Requester Info -->
            <div class="mb-8">
              <h2
                class="text-xl font-semibold text-gray-800 mb-4 pb-2 border-b"
              >
                Requester Information
              </h2>
              <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div>
                  <label class="block text-sm font-medium text-gray-500 mb-1">
                    Requestor Name
                  </label>
                  <p class="text-gray-900">
                    {{ exemptionData.requesterName || "N/A" }}
                  </p>
                </div>
                <div>
                  <label class="block text-sm font-medium text-gray-500 mb-1">
                    Corporate
                  </label>
                  <p class="text-gray-900">
                    {{ exemptionData.corporate || "N/A" }}
                  </p>
                </div>
              </div>
            </div>

            <!-- Policy & Details -->
            <div class="mb-8">
              <h2
                class="text-xl font-semibold text-gray-800 mb-4 pb-2 border-b"
              >
                Request Details
              </h2>
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

                  <div>
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
                      {{ getFinalDecisionDisplay(exemptionData.finalDecision) }}
                    </span>
                  </div>
                </div>
                <div>
                  <label class="block text-sm font-medium text-gray-500 mb-1">
                    Details
                  </label>
                  <div class="bg-gray-50 rounded-lg p-4 border border-gray-200">
                    <p class="text-gray-900 whitespace-pre-wrap">
                      {{ exemptionData.details || "No details provided" }}
                    </p>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div
            class="sm:col-span-full md:col-span-2 bg-white rounded-lg shadow px-6 py-6"
          >
            <!-- Approvers -->
            <div class="mb-8">
              <h2
                class="text-xl font-semibold text-gray-800 mb-4 pb-2 border-b"
              >
                Approvers Status
              </h2>
              <div
                v-if="approversData && approversData.length > 0"
                class="space-y-4"
              >
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
                        </div>
                      </div>
                      <!-- Approver status badge (Pending/Approved/Disapproved) -->
                      <span
                        class="inline-flex items-center px-2 py-1 rounded text-xs font-medium"
                        :class="getApproverStatusColor(approver.respond)"
                      >
                        {{ getApproverStatusDisplay(approver.respond) }}
                      </span>
                    </div>
                    <!-- <div v-if="approver.responddate" class="mt-2">
                      <p class="text-xs text-gray-500">Response Date: <span class="text-gray-700">{{ formatDate(approver.responddate) }}</span></p>
                    </div>
                    <div v-if="approver.remarks" class="mt-2">
                      <p class="text-xs text-gray-500">Remarks:</p>
                      <p class="text-gray-700 text-sm">{{ approver.remarks }}</p>
                    </div> -->


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
                        <span
                          class="w-2 h-2 rounded-full bg-blue-400 mt-1.5"
                        ></span>
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
                      <div class="flex flex-col items-end">
                        <!-- Proxy approver status badge -->
                        <span
                          class="inline-flex items-center px-2 py-1 rounded text-xs font-medium"
                          :class="getApproverStatusColor(proxy.respond)"
                        >
                          {{ getApproverStatusDisplay(proxy.respond) }}
                        </span>
                      </div>
                    </div>
                  </div>



                  </div>
                  
                </template>
              </div>
              <div v-else class="text-gray-500 text-sm">
                No approvers assigned yet.
              </div>
            </div>

            <!-- Remarks (if any) -->
            <div v-if="exemptionData.remarks" class="mb-8">
              <h2
                class="text-xl font-semibold text-gray-800 mb-4 pb-2 border-b"
              >
                Remarks
              </h2>
              <div class="bg-gray-50 rounded-lg p-4 border border-gray-200">
                <p class="text-gray-900 whitespace-pre-wrap">
                  {{ exemptionData.remarks }}
                </p>
              </div>
            </div>

            <!-- Timestamps -->
            <div class="mb-8">
              <h2
                class="text-xl font-semibold text-gray-800 mb-4 pb-2 border-b"
              >
                Audit Trail
              </h2>
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
                  <label class="block text-sm font-medium text-gray-500 mb-1">
                    Modified Date
                  </label>
                  <p class="text-gray-900">
                    {{ formatDate(exemptionData.modifiedDate) }}
                  </p>
                </div>
                <div>
                  <label class="block text-sm font-medium text-gray-500 mb-1">
                    Workstation
                  </label>
                  <p class="text-gray-900">
                    {{ exemptionData.workstation || "N/A" }}
                  </p>
                </div>
                <div>
                  <label class="block text-sm font-medium text-gray-500 mb-1">
                    IP Address
                  </label>
                  <p class="text-gray-900">
                    {{ exemptionData.ipAddress || "N/A" }}
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Action Buttons -->
        <!-- <div class="flex justify-end gap-3 pt-4 border-t border-gray-200">
          <Button variant="outline-primary" size="lg" @onclick="handleBack">
            Back to List
          </Button>
          <Button variant="primary" size="lg" @onclick="handleEdit">
            Edit Request
          </Button>
        </div> -->
      </div>

      <!-- Not Found State -->
      <div v-else class="bg-white rounded-lg shadow px-6 py-12 text-center">
        <p class="text-gray-500 text-lg mb-4">No exemption details found.</p>
        <Button variant="primary" size="lg" @onclick="handleBack">
          Back to List
        </Button>
      </div>
    </main>
  </div>
</template>
