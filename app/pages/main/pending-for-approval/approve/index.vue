                                                                                                                                                                                                                                                                                                                                                                                                                                                                  <script setup lang="ts">
import CommentFAB from '@/components/CommentFAB.vue'
const router = useRouter();
const route = useRoute();
const { showToast } = useToast();

const loading = ref(false);
const submitting = ref(false);
const exemptionData = ref<any>(null);
const approversData = ref<any[]>([]);
const policyName = ref<string>("");
const currentUser = ref<any>(null);
const userStore = useUserStore();
const userId = userStore.profile;

// Get ID from route query
const exemptionId = computed(() => {
  return Number(route.query.id) || null;
});

// Form data for approval response
const respondRemarks = ref<null | string>(null);
const selectedRespond = ref<number>(0); // 0 = not selected, 1 = approve, 2 = disapprove

// Fetch current user from auth
const fetchCurrentUser = async () => {
  if (userId) {
    try {
      // Store the full user profile object for proper matching
      currentUser.value = userId;
    } catch (e) {
      console.error("Error parsing user data:", e);
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
    if (window.history.length > 1 && document.referrer.startsWith(window.location.origin)) {
      router.back();
    } else {
      router.push('/main/pending-for-approval');
    } // Fallback route
    return;
  }

  loading.value = true;
  try {
    const transactionnumber = exemptionId.value;
    const response = await apiFetch(
      `/api/exemption/view/${transactionnumber}`,
      {
        method: "POST",
        parseJson: true,
      },
    );

    if (response) {
      exemptionData.value = response.item;
      approversData.value = response.approvers || [];

      // Check if current user is an approver for this request
      const currentUserId = currentUser.value?.emplId?.toString();
      const isApprover = approversData.value.some(
        (a) => String(a.employeeId ?? a.EmployeeId) === currentUserId,
      );

      if (!isApprover) {
        // User is not an approver - redirect to exemptions list
        showToast({
          type: "warning",
          title: "Access Denied",
          message: "You are not authorized to approve this request",
          duration: 5000,
        });
        // router.back() || router.push('/main/pending-for-approval'); // Fallback route
        router.back()
        return;
      }

      // If there's a policy ID, fetch the policy name
      if (response.item.policyid) {
        fetchPolicyName(response.item.policyid);
      }
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
  return new Date(date).toLocaleDateString("en-PH", {
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
// const getApproverStatusColor = (respond: number) => {
//   const colorMap: Record<number, string> = {
//     0: "bg-yellow-100 text-yellow-800",
//     1: "bg-green-100 text-green-800",
//     2: "bg-red-100 text-red-800",
//   };
//   return colorMap[respond] || "bg-gray-100 text-gray-800";
// };

// Check if any approver has already disapproved (respond = 2)
const isDisapproved = computed(() => {
  return approversData.value.some((a) => a.respond === 2);
});

const isApproved = computed(() => {
  const currentUserId = currentUser.value?.emplId?.toString();
  const currentUserApprover = approversData.value.find(
    (a) => String(a.employeeId ?? a.EmployeeId) === currentUserId,
  );


  return currentUserApprover && currentUserApprover.respond === 1;
});

// Check if current user can still respond (not disapproved yet)
const canRespond = computed(() => {
  // If already disapproved, no one can respond
  if (isDisapproved.value) {
    return false;
  }

  // If already approved, no one can respond
  if (isApproved.value) {
    return false;
  }

  // Find current user's approver record
  // Use emplId from user profile to match against EmployeeId from API
  const currentUserId = currentUser.value?.emplId?.toString();

  // Try both lowercase and uppercase property names

  const currentUserApprover = approversData.value.find(
    (a) => String(a.employeeId ?? a.EmployeeId) === currentUserId,
  );

  if (!currentUserApprover) {
    return false;
  }

  // If user already responded, they can't respond again
  if (currentUserApprover.respond !== 0) {
    return false;
  }

  // Check if main approver OR proxy in the same group has already approved
  const approversInGroup = approversData.value.filter(
    (a) => a.sort === currentUserApprover.sort,
  );

  // If either main or proxy has approved, this user doesn't need to respond
  const hasApprovedInGroup = approversInGroup.some((a) => a.respond === 1);

  return !hasApprovedInGroup;
});

// Check if current user is satisfied by proxy/main approver's approval
const isSatisfiedByGroup = computed(() => {
  // If already disapproved, doesn't apply
  if (isDisapproved.value) return false;

  // Find current user's approver record
  // Use emplId from user profile to match against EmployeeId from API
  const currentUserId = currentUser.value?.emplId?.toString();
  const currentUserApprover = approversData.value.find(
    (a) => String(a.employeeId ?? a.EmployeeId) === currentUserId,
  );

  if (!currentUserApprover) return false;

  // If user already responded, they're not "satisfied by group"
  if (currentUserApprover.respond !== 0) return false;

  // Check if main approver OR proxy in the same group has already approved
  const approversInGroup = approversData.value.filter(
    (a) => a.sort === currentUserApprover.sort,
  );

  // If another approver in the group has approved, this user is satisfied
  const hasApprovedInGroup = approversInGroup.some(
    (a) => a.respond === 1 && a.employeeId !== currentUserApprover.employeeId,
  );

  return hasApprovedInGroup;
});

// Get the name of the approver who already approved in the group
const satisfiedByApproverName = computed(() => {
  if (!isSatisfiedByGroup.value) return "";

  // Use emplId from user profile to match against EmployeeId from API
  const currentUserId = currentUser.value?.emplId?.toString();
  const currentUserApprover = approversData.value.find(
    (a) => String(a.employeeId ?? a.EmployeeId) === currentUserId,
  );

  if (!currentUserApprover) return "";

  const approversInGroup = approversData.value.filter(
    (a) => a.sort === currentUserApprover.sort,
  );

  const approvedApprover = approversInGroup.find(
    (a) => a.respond === 1 && a.employeeId !== currentUserApprover.employeeId,
  );

  return approvedApprover?.approverName || "another approver";
});

// Check if user is an approver but it's not their turn yet (previous approver groups haven't approved)
const isPendingApproval = computed(() => {
  // Find current user's approver record
  const currentUserId = currentUser.value?.emplId?.toString();
  const currentUserApprover = approversData.value.find(
    (a) => String(a.employeeId ?? a.EmployeeId) === currentUserId,
  );

  // If user is not an approver, return false
  if (!currentUserApprover) {
    return false;
  }

  // If user already responded, this doesn't apply
  if (currentUserApprover.respond !== 0) {
    return false;
  }

  const currentSort = currentUserApprover.sort;

  // Get all unique sort values (approver groups) sorted by their minimum lineSequence
  const groupSorts = [...new Set(approversData.value.map((a) => a.sort))]
    .map((sort) => ({
      sort,
      minSequence: Math.min(...approversData.value.filter((a) => a.sort === sort).map((a) => a.lineSequence)),
    }))
    .sort((a, b) => a.minSequence - b.minSequence);

  // Find the current user's group index
  const currentUserGroupIndex = groupSorts.findIndex((g) => g.sort === currentSort);
  if (currentUserGroupIndex === -1) return false;

  // Get all groups that come before the current user's group
  const previousGroups = groupSorts.slice(0, currentUserGroupIndex);

  // Check if ALL previous groups have at least one approver who approved
  const allPreviousGroupsApproved = previousGroups.every((group) => {
    const groupApprovers = approversData.value.filter((a) => a.sort === group.sort);
    return groupApprovers.some((a) => a.respond === 1);
  });

  // If previous groups haven't all approved, it's not user's turn yet
  return !allPreviousGroupsApproved;
});

// Get the name of the pending previous approver
const pendingApproverName = computed(() => {
  if (!isPendingApproval.value) return "";

  const currentUserId = currentUser.value?.emplId?.toString();
  const currentUserApprover = approversData.value.find(
    (a) => String(a.employeeId ?? a.EmployeeId) === currentUserId,
  );

  if (!currentUserApprover) return "";

  const currentSequence = currentUserApprover.lineSequence;
  const currentSort = currentUserApprover.sort;

  // Find previous approvers with same logic as isPendingApproval
  const previousApprovers = approversData.value
    .filter((a) => {
      // Exclude current user
      if (a.employeeId === currentUserApprover.employeeId) return false;
      // Exclude approvers in the same sort (group)
      if (a.sort === currentSort) return false;
      // Previous if: lower lineSequence OR (same lineSequence AND lower sort)
      return (
        a.lineSequence < currentSequence ||
        (a.lineSequence === currentSequence && a.sort < currentSort)
      );
    })
    .sort((a, b) => {
      // Sort by lineSequence first, then by sort
      if (a.lineSequence !== b.lineSequence) {
        return a.lineSequence - b.lineSequence;
      }
      return a.sort - b.sort;
    });

  // Find the first pending approver (respond === 0)
  const pendingApprover = previousApprovers.find((a) => a.respond === 0);

  // If all previous approved, find the last one who disapproved
  if (!pendingApprover) {
    const lastApprover = previousApprovers[0];
    return lastApprover?.approverName || "previous approver";
  }

  return pendingApprover?.approverName || "previous approver";
});

// Handle approval response
const handleApprove = async () => {
  // Check if already disapproved
  if (isDisapproved.value) {
    showToast({
      type: "warning",
      title: "Warning",
      message:
        "This request has already been disapproved. You can no longer respond.",
      duration: 5000,
    });
    return;
  }

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
  // Use emplId from user profile to match against EmployeeId from API
  const currentUserId = currentUser.value?.emplId?.toString();
  const currentApprover = approversData.value.find(
    (a) =>
      a.respond === 0 && String(a.employeeId ?? a.EmployeeId) === currentUserId,
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
  const approverToRespond =
    currentApprover || approversData.value.find((a) => a.respond === 0);

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
    const payload = {
      transactionId: exemptionId.value,
      transApproverId: approverToRespond.transApproverId,
      employeeId:
        currentUser.value?.emplId?.toString() ??
        approverToRespond.employeeId?.toString(),
      remarks: respondRemarks.value,
      respond: selectedRespond.value,
    };

    console.log("Submitting approval response with payload:", payload);

    const response = await apiFetch("/api/exemption/respond", {
      method: "POST",
      body: JSON.stringify(payload),
      headers: {
        "Content-Type": "application/json",
      },
    });

    if (response) {
      showToast({
        type: "success",
        title: "Success",
        message:
          selectedRespond.value === 1
            ? "Request approved successfully!"
            : "Request disapproved successfully!",
        duration: 5000,
      });

      if (window.history.length > 1 && document.referrer.startsWith(window.location.origin)) {
        router.back();
      } else {
        router.push('/main/pending-for-approval');
      } // Fallback route
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

// Computed property to group main approvers with their proxies
// Main approvers: linesequence = 1 (regardless of sort value)
// Proxies: linesequence > 1, grouped under main approvers by matching sort values
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

// Handle back navigation with fallback
const handleBack = () => {
  if (window.history.length < 1) {
    router.back();
  } else {
    router.push('/main/pending-for-approval'); // Change to your desired fallback route
  }
};

const handleBackClick = () => {
  if (window.history.length < 1 && document.referrer.startsWith(window.location.origin)) {
    router.back();
  } else {
    router.push('/main/pending-for-approval'); // Fallback route
  }
};
let historylength = 0;
// Fetch on mount
onMounted(async () => {
  await fetchCurrentUser();
  await fetchExemptionDetails();

  historylength = window.history.length;
});
</script>

<template>
  <div class="bg-gray-50 dark:bg-gray-900">
    <header class="bg-white dark:bg-gray-800 shadow">
      <div class="max-w-full mx-auto py-4 px-4 sm:px-6 lg:px-8 flex justify-between items-center">
        <div class="flex items-center">
          <Button size="lg" icon="left_arrow" variant="outline-primary" iconPosition="center" customClass="rounded-full"
            @onclick="handleBackClick" />
          <h1 class="text-2xl ps-5 font-bold text-gray-900 dark:text-gray-100">Approve Request</h1>
        </div>
      </div>
    </header>

    <main class="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
      <!-- Loading State -->
      <div v-if="loading" class="flex items-center justify-center py-20">
        <svg class="animate-spin h-8 w-8 text-blue-600" xmlns="http://www.w3.org/2000/svg" fill="none"
          viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor"
            d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z">
          </path>
        </svg>
        <span class="ml-3 text-gray-600">Loading details...</span>
      </div>

      <!-- Content -->
      <div v-else-if="exemptionData">
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <!-- Request Details -->
          <div class="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
            <div class="mb-6">
              <div class="text-xl font-semibold text-gray-800 dark:text-gray-100 mb-4 pb-2 border-b">
                <div class="flex justify-between items-center">
                  <h2>Request Information</h2>
                  <div>
                    <span class="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium" :class="{
                      'bg-blue-100 text-blue-800 dark:bg-blue-800/20 dark:text-blue-300':
                        exemptionData.status === 'O' ||
                        exemptionData.status === 'P',

                      'bg-green-100 text-green-800 dark:bg-green-800/20 dark:text-green-300':
                        exemptionData.status === 'C',
                    }">
                      {{ getStatusDisplay(exemptionData.status) }}
                    </span>
                  </div>
                </div>
              </div>

              <div class="space-y-4">
                <div class="grid grid-cols-2 gap-4">
                  <div>
                    <label class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1">
                      Transaction Number
                    </label>
                    <p class="text-gray-900 dark:text-gray-100 font-medium">
                      {{ exemptionData.transactionNumber || "N/A" }}
                    </p>
                  </div>

                  <div>
                    <label class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1">
                      Requestor Name
                    </label>
                    <p class="text-gray-900 dark:text-gray-100">
                      {{ exemptionData.requesterName || "N/A" }}
                    </p>
                  </div>
                </div>

                <div class="grid grid-cols-2 gap-4">
                  <div>
                    <label class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1">
                      Created Date
                    </label>
                    <p class="text-gray-900 dark:text-gray-100">
                      {{ formatDate(exemptionData.createdDate) }}
                    </p>
                  </div>
                </div>

                <div>
                  <label class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1">
                    Policy To Override
                  </label>
                  <p class="text-gray-900 dark:text-gray-100">
                    {{ exemptionData.policyName }}
                  </p>
                </div>

                <div>
                  <label class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1">
                    Details
                  </label>
                  <div class="bg-gray-50 dark:bg-gray-700 rounded-lg p-4 border border-gray-200 dark:border-gray-600">
                    <TipTapEditor :modelValue="exemptionData.details" readonly
                      placeholder="No details provided" />
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Approval Form -->
          <div class="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
            <div class="mb-6">
              <div class="border-b mb-6 flex justify-between">
                <h2 class="text-xl font-semibold text-gray-800 dark:text-gray-100 pb-2">
                  Your Response
                </h2>
                <!-- <div>
                  <span v-if="exemptionData.finalDecision && isSatisfiedByGroup != true"
                    class="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium" :class="{
                      'bg-yellow-100 text-yellow-800 dark:bg-yellow-800/20 dark:text-yellow-300':
                        exemptionData.finalDecision === 'Pending',
                      'bg-green-100 text-green-800 dark:bg-green-800/20 dark:text-green-300':
                        exemptionData.finalDecision === 'Approved',
                      'bg-red-100 text-red-800 dark:bg-red-800/20 dark:text-red-300':
                        exemptionData.finalDecision === 'Disapproved',
                    }">
                    {{ getFinalDecisionDisplay(exemptionData.finalDecision) }}
                  </span>
                </div> -->
              </div>

              <!-- Check if satisfied by proxy/main approver approval -->
              <div v-if="isSatisfiedByGroup" class="text-center py-8">
                <div
                  class="inline-flex items-center justify-center w-16 h-16 rounded-full mb-4 bg-green-100 dark:bg-green-800/20">
                  <svg class="w-8 h-8 text-green-600 dark:text-green-400" fill="none" stroke="currentColor"
                    viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path>
                  </svg>
                </div>
                <p class="text-lg font-medium text-green-600 dark:text-green-400">
                  Already Approved
                </p>
                <p class="text-gray-500 dark:text-gray-400 mt-2">
                  {{ satisfiedByApproverName }} has already approved this
                  request. Your approval is not required.
                </p>
              </div>

              <!-- Check if user is approver but not their turn yet -->
              <div v-else-if="isPendingApproval" class="text-center py-8">
                <div
                  class="inline-flex items-center justify-center w-16 h-16 rounded-full mb-4 bg-yellow-100 dark:bg-yellow-800/20">
                  <svg class="w-8 h-8 text-yellow-600 dark:text-yellow-400" fill="none" stroke="currentColor"
                    viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                  </svg>
                </div>
                <p class="text-lg font-medium text-yellow-600 dark:text-yellow-400">
                  Waiting for Previous Approvers
                </p>
                <p class="text-gray-500 dark:text-gray-400 mt-2">
                  {{ pendingApproverName }} needs to approve this request first
                  before you can respond. You will be notified when it's your
                  turn.
                </p>
              </div>

              <!-- Check if already responded or disapproved -->
              <div v-else-if="
                (exemptionData.finalDecision &&
                  exemptionData.finalDecision !== 'Pending') ||
                isDisapproved
              " class="text-center py-8">
                <div class="inline-flex items-center justify-center w-16 h-16 rounded-full mb-4" :class="exemptionData.finalDecision === 'Approved'
                  ? 'bg-green-100 dark:bg-green-800/20'
                  : 'bg-red-100 dark:bg-red-800/20'
                  ">
                  <svg v-if="exemptionData.finalDecision === 'Approved'"
                    class="w-8 h-8 text-green-600 dark:text-green-400" fill="none" stroke="currentColor"
                    viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path>
                  </svg>
                  <svg v-else class="w-8 h-8 text-red-600 dark:text-red-400" fill="none" stroke="currentColor"
                    viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12">
                    </path>
                  </svg>
                </div>
                <p class="text-lg font-medium" :class="exemptionData.finalDecision === 'Approved'
                  ? 'text-green-600 dark:text-green-400'
                  : 'text-red-600 dark:text-red-400'
                  ">
                  This request has been {{ exemptionData.finalDecision }}
                </p>
                <p v-if="isDisapproved" class="text-gray-500 dark:text-gray-400 mt-2">
                  The request has been disapproved. Remaining approvers can no
                  longer respond.
                </p>
                <p v-else class="text-gray-500 dark:text-gray-400 mt-2">
                  You can no longer modify your response.
                </p>
              </div>

              <!-- Approval buttons (only show if can respond and not satisfied by group) -->
              <div v-else-if="canRespond">
                <div class="mb-6">
                  <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-3">
                    Select your decision <span class="text-red-500">*</span>
                  </label>
                  <div class="flex gap-4">
                    <button type="button" @click="selectedRespond = 1"
                      class="flex-1 py-3 px-4 rounded-lg border-2 transition-all duration-200 flex items-center justify-center gap-2"
                      :class="selectedRespond === 1
                        ? 'border-green-500 bg-green-50 text-green-700 dark:border-green-400 dark:bg-green-800/20 dark:text-green-300'
                        : 'border-gray-200 dark:border-gray-600 hover:border-green-300 hover:bg-gray-50 dark:hover:bg-gray-700'
                        ">
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path>
                      </svg>
                      <span class="font-medium">Approve</span>
                    </button>
                    <button type="button" @click="selectedRespond = 2"
                      class="flex-1 py-3 px-4 rounded-lg border-2 transition-all duration-200 flex items-center justify-center gap-2"
                      :class="selectedRespond === 2
                        ? 'border-red-500 bg-red-50 text-red-700 dark:border-red-400 dark:bg-red-800/20 dark:text-red-300'
                        : 'border-gray-200 dark:border-gray-600 hover:border-red-300 hover:bg-gray-50 dark:hover:bg-gray-700'
                        ">
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12">
                        </path>
                      </svg>
                      <span class="font-medium">Disapprove</span>
                    </button>
                  </div>
                </div>

                <div class="mb-6">
                  <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                    Remarks (Optional)
                  </label>
                  <BaseInput v-model="respondRemarks" isTextarea :rowCount="4" :maxCharCount="250"
                    class="bg-white dark:bg-gray-700 border-gray-300 dark:border-gray-600 text-gray-900 dark:text-gray-100" />
                </div>

                <div class="flex gap-4">
                  <Button  size="lg" class="flex-1" :loading="submitting" @onclick="handleApprove"
                    :disabled="selectedRespond === 0 || isDisapproved">
                    Submit Response
                  </Button>
                  <Button  size="lg" @onclick="handleBack" :disabled="isDisapproved">
                    Cancel
                  </Button>
                </div>
              </div>

              <!-- Approval Status -->
              <div v-else-if="isApproved"
                class="rounded-lg border-l-4 border-green-500 bg-green-50 dark:bg-green-800/20 dark:border-green-400 p-4 shadow-sm"
                role="alert">
                <div class="flex items-center">
                  <svg class="h-5 w-5 text-green-500 dark:text-green-400 mr-2" fill="currentColor" viewBox="0 0 20 20">
                    <path fill-rule="evenodd"
                      d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.707a1 1 0 00-1.414-1.414L9 10.172 7.707 8.879a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                      clip-rule="evenodd" />
                  </svg>

                  <span class="font-semibold text-green-800 dark:text-green-300"> Approved </span>
                </div>

                <p class="mt-2 text-sm text-green-700 dark:text-green-300">
                  You already approved this exemption request.
                </p>
              </div>

              <!-- Debug fallback - shows when none of the above conditions match -->
              <div v-else class="text-center py-8 bg-gray-50 dark:bg-gray-700 rounded-lg">
                <p class="text-gray-600 dark:text-gray-400 mb-2">
                  You are not assigned as an approver for this request.
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Not Found State -->
      <div v-else class="bg-white dark:bg-gray-800 rounded-lg shadow px-6 py-12 text-center">
        <p class="text-gray-500 dark:text-gray-400 text-lg mb-4">No exemption details found.</p>
        <Button size="lg" @onclick="handleBack">
          Back to List
        </Button>
      </div>
    </main>
  </div>
  <!-- Comment FAB for transaction discussion -->
  <CommentFAB :transactionId="exemptionId" :requester-id="exemptionData?.requesterId" :approvers="approversData.map(a => a.employeeId || a.EmployeeId || a.id)" />

</template>
