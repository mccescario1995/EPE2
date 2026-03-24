<script setup lang="ts">
const router = useRouter();
const route = useRoute();
const { showToast } = useToast();

const loading = ref(false);
const submitting = ref(false);
const exemptionData = ref<any>(null);
const approversData = ref<any[]>([]);
const policyName = ref<string>("");
const currentUser = ref<any>(null);

// Get ID from route query
const exemptionId = computed(() => {
  return Number(route.query.id) || null;
});

// Form data for approval response
const remarks = ref<string>("");
const selectedRespond = ref<number>(0); // 0 = not selected, 1 = approve, 2 = disapprove

// Fetch current user from auth
const fetchCurrentUser = async () => {
  const authData = useCookie("auth");
  if (authData.value) {
    try {
      currentUser.value = JSON.parse(authData.value);
    } catch (e) {
      console.error("Error parsing auth data:", e);
    }
  }
};

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
    P: "Pending", // Approval in progress (one or more approved, waiting for all)
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

// Handle approval response
const handleApprove = async () => {
  if (selectedRespond.value === 0) {
    showToast({
      type: "warning",
      title: "Warning",
      message: "Please select Approve or Disapprove",
      duration: 5000,
    });
    return;
  }

  // Find the current approver's TransApproverId
  const currentApprover = approversData.value.find(a => 
    a.respond === 0 && a.employeeId === currentUser.value?.employeeId
  );

  if (!currentApprover && !currentUser.value) {
    showToast({
      type: "warning",
      title: "Warning",
      message: "Unable to identify your approver record. Please login first.",
      duration: 5000,
    });
    return;
  }

  // Get the first pending approver if current user not found
  const approverToRespond = currentApprover || approversData.value.find(a => a.respond === 0);

  if (!approverToRespond) {
    showToast({
      type: "warning",
      title: "Warning",
      message: "No pending approver found for this request",
      duration: 5000,
    });
    return;
  }

  submitting.value = true;
  try {
    const response = await apiFetch("/api/exception/respond", {
      method: "POST",
      body: {
        transactionId: exemptionId.value,
        transApproverId: approverToRespond.transApproverId,
        employeeId: currentUser.value?.employeeId,
        remarks: remarks.value,
        respond: selectedRespond.value,
      },
      parseJson: true,
    });

    if (response.success) {
      showToast({
        type: "success",
        title: "Success",
        message: selectedRespond.value === 1 
          ? "Request approved successfully!" 
          : "Request disapproved successfully!",
        duration: 5000,
      });
      
      // Redirect to exemptions list
      router.push("/exemptions");
    } else {
      showToast({
        type: "error",
        title: "Error",
        message: response.message || "Failed to process response",
        duration: 5000,
      });
    }
  } catch (error) {
    console.error("Error processing approval response:", error);
    showToast({
      type: "error",
      title: "Error",
      message: "Failed to process your response",
      duration: 5000,
    });
  } finally {
    submitting.value = false;
  }
};

// Handle back navigation
const handleBack = () => {
  router.push("/exemptions");
};

// Fetch on mount
onMounted(async () => {
  await fetchCurrentUser();
  await fetchExemptionDetails();
});
</script>

<template>
  <div class="min-h-screen bg-gray-50">
    <header class="bg-white shadow">
      <div class="max-w-7xl mx-auto py-4 px-4 sm:px-6 lg:px-8 flex justify-between items-center">
        <div class="flex items-center">
          <Button
            variant="ghost-primary"
            size="lg"
            icon="left_arrow"
            iconPosition="center"
            customClass="rounded-full"
            @onclick="handleBack"
          />
          <h1 class="text-2xl ps-5 font-bold text-gray-900">Approve Request</h1>
        </div>
      </div>
    </header>

    <main class="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
      <!-- Loading State -->
      <div v-if="loading" class="flex items-center justify-center py-20">
        <svg class="animate-spin h-8 w-8 text-blue-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        <span class="ml-3 text-gray-600">Loading details...</span>
      </div>

      <!-- Content -->
      <div v-else-if="exemptionData">
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <!-- Request Details -->
          <div class="bg-white rounded-lg shadow p-6">
            <div class="mb-6">
              <div class="text-xl font-semibold text-gray-800 mb-4 pb-2 border-b">
                <div class="flex justify-between items-center">
                  <h2>Request Information</h2>
                  <div>
                    <span
                      class="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium"
                      :class="{
                        'bg-blue-100 text-blue-800': exemptionData.status === 'O',
                        'bg-yellow-100 text-yellow-800': exemptionData.status === 'P',
                        'bg-green-100 text-green-800': exemptionData.status === 'C',
                      }"
                    >
                      {{ getStatusDisplay(exemptionData.status) }}
                    </span>
                  </div>
                </div>
              </div>

              <div class="space-y-4">
                <div class="grid grid-cols-2 gap-4">
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

                <div class="grid grid-cols-2 gap-4">
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
                      Final Decision
                    </label>
                    <span
                      v-if="exemptionData.finalDecision"
                      class="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium"
                      :class="{
                        'bg-yellow-100 text-yellow-800': exemptionData.finalDecision === 'Pending',
                        'bg-green-100 text-green-800': exemptionData.finalDecision === 'Approved',
                        'bg-red-100 text-red-800': exemptionData.finalDecision === 'Disapproved',
                      }"
                    >
                      {{ getFinalDecisionDisplay(exemptionData.finalDecision) }}
                    </span>
                  </div>
                </div>

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
          </div>

          <!-- Approval Form -->
          <div class="bg-white rounded-lg shadow p-6">
            <div class="mb-6">
              <h2 class="text-xl font-semibold text-gray-800 mb-4 pb-2 border-b">
                Your Response
              </h2>

              <!-- Check if already responded -->
              <div v-if="exemptionData.finalDecision && exemptionData.finalDecision !== 'Pending'" class="text-center py-8">
                <div class="inline-flex items-center justify-center w-16 h-16 rounded-full mb-4"
                  :class="exemptionData.finalDecision === 'Approved' ? 'bg-green-100' : 'bg-red-100'">
                  <svg v-if="exemptionData.finalDecision === 'Approved'" class="w-8 h-8 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path>
                  </svg>
                  <svg v-else class="w-8 h-8 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
                  </svg>
                </div>
                <p class="text-lg font-medium" :class="exemptionData.finalDecision === 'Approved' ? 'text-green-600' : 'text-red-600'">
                  This request has been {{ exemptionData.finalDecision }}
                </p>
                <p class="text-gray-500 mt-2">You can no longer modify your response.</p>
              </div>

              <!-- Approval buttons -->
              <div v-else>
                <div class="mb-6">
                  <label class="block text-sm font-medium text-gray-700 mb-3">
                    Select your decision <span class="text-red-500">*</span>
                  </label>
                  <div class="flex gap-4">
                    <button
                      type="button"
                      @click="selectedRespond = 1"
                      class="flex-1 py-3 px-4 rounded-lg border-2 transition-all duration-200 flex items-center justify-center gap-2"
                      :class="selectedRespond === 1 
                        ? 'border-green-500 bg-green-50 text-green-700' 
                        : 'border-gray-200 hover:border-green-300 hover:bg-gray-50'"
                    >
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path>
                      </svg>
                      <span class="font-medium">Approve</span>
                    </button>
                    <button
                      type="button"
                      @click="selectedRespond = 2"
                      class="flex-1 py-3 px-4 rounded-lg border-2 transition-all duration-200 flex items-center justify-center gap-2"
                      :class="selectedRespond === 2 
                        ? 'border-red-500 bg-red-50 text-red-700' 
                        : 'border-gray-200 hover:border-red-300 hover:bg-gray-50'"
                    >
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
                      </svg>
                      <span class="font-medium">Disapprove</span>
                    </button>
                  </div>
                </div>

                <div class="mb-6">
                  <label class="block text-sm font-medium text-gray-700 mb-2">
                    Remarks (Optional)
                  </label>
                  <textarea
                    v-model="remarks"
                    rows="4"
                    class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                    placeholder="Enter your remarks or comments..."
                  ></textarea>
                </div>

                <div class="flex gap-4">
                  <Button
                    variant="primary"
                    size="lg"
                    class="flex-1"
                    :loading="submitting"
                    @onclick="handleApprove"
                    :disabled="selectedRespond === 0"
                  >
                    Submit Response
                  </Button>
                  <Button
                    variant="outline"
                    size="lg"
                    @onclick="handleBack"
                  >
                    Cancel
                  </Button>
                </div>
              </div>
            </div>
          </div>
        </div>
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
