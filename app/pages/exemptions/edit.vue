<script setup lang="ts">
const router = useRouter();
const route = useRoute();
const { showToast } = useToast();
const userStore = useUserStore();

const userId = userStore.profile;

const config = useRuntimeConfig();

// Get ID from route query
const exemptionId = computed(() => {
  return Number(route.query.id) || null;
});

// Form data
const formData = ref({
  transactionNumber: "" as string | null,
  requestorName: "" as string | null,
  policyId: null as number | null,
  policyName: "" as string | null,
  corporate: "" as string | null,
  details: "" as string | null,
});

// Exemption details (for status, audit trail, etc.)
const exemptionDetails = ref<any>(null);
const approversData = ref<any[]>([]);
const policyName = ref<string>("");
const policyValue = ref<{ id: number; label: string } | null>(null);
const attachments = ref<any[]>([]);
const newAttachments = ref<{ file: File; referenceType: string }[]>([]);

// Store original data for comparison
const originalData = ref({
  transactionNumber: "" as string | null,
  requestorName: "" as string | null,
  policyId: null as number | null,
  corporate: null as string | null,
  details: "" as string | null,
});

// Policy options - will be fetched from API
const policyOptions = ref<{ id: number; label: string }[]>([]);
const isLoadingPolicies = ref(false);

// Validation errors
const errors = ref({
  policyId: "",
  transactionNumber: "",
  requestorName: "",
  details: "",
});

// Loading state
const isLoading = ref(false);
const isSubmitting = ref(false);

// Fetch policies from API
const fetchPolicies = async () => {
  isLoadingPolicies.value = true;

  try {
    const response = await apiFetch("/api/policy", {
      method: "POST",
      parseJson: true,
    });

    // Handle both success response and direct items array
    const items = response.success ? response.items : response;

    if (!items || !Array.isArray(items)) {
      console.error("Invalid response format:", items);
      policyOptions.value = [];
      return;
    }

    policyOptions.value = items.map((p: any) => ({
      id: p.policyid,
      label: p.description || p.label || `Policy ${p.policyid}`,
    }));
  } catch (error) {
    console.error("Error fetching policies:", error);
    showToast({
      type: "error",
      title: "Error",
      message: "Failed to load policies. Please try again.",
      duration: 5000,
    });
    policyOptions.value = [];
  } finally {
    isLoadingPolicies.value = false;
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

    if (items) {
      const policy = items.find((p: any) => p.policyid === policyId);

      if (policy) {
        policyName.value = policy.description || `Policy ${policyId}`;
      }
    }

    return policyName.value;
  } catch (error) {
    console.error("Error fetching policy name:", error);
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

  isLoading.value = true;
  try {
    const response = await apiFetch(
      `/api/exemption/edit/${exemptionId.value}`,
      {
        method: "POST",
        parseJson: true,
      },
    );

    if (response) {
      const item = response.item;

      // Set form data
      formData.value.policyId = item.policyId;
      formData.value.transactionNumber = item.transactionNumber || "";
      formData.value.corporate = item.corporate;
      formData.value.requestorName = item.requesterName || "";
      formData.value.details = item.details || "";

      // Store original data
      originalData.value = {
        transactionNumber: item.transactionNumber || "",
        requestorName: item.requesterName || "",
        policyId: item.policyId,
        corporate: item.corporate,
        details: item.details || "",
      };

      // Set exemption details for status and audit trail
      exemptionDetails.value = {
        transactionId: item.transactionId,
        status: item.status,
        finalDecision: item.finalDecision,
        createdDate: item.createdDate,
        modifiedDate: item.modifiedDate,
        workstation: item.workstation,
        ipAddress: item.ipAddress,
      };

      // In fetchExemptionDetails, after setting exemptionDetails.value
      if (exemptionDetails.value?.status === "D" && item.policyId) {
        await fetchApprovers(item.policyId);
      } else {
        approversData.value = response.approvers || [];
      }

      // Set approvers data
      // approversData.value = response.approvers || [];

      // Fetch attachments
      await fetchAttachments();

      // If there's a policy ID, fetch the policy name
      if (item.policyId) {
        var policyName = await fetchPolicyName(item.policyId);
      }

      policyValue.value = {
        id: item.policyId,
        label: policyName || `Policy ${item.policyId}`,
      };
    } else {
      showToast({
        type: "error",
        title: "Error",
        message: response.message || "Failed to load exemption details",
        duration: 5000,
      });
      router.push("/exemptions");
    }
  } catch (error) {
    console.error("Error fetching exemption details:", error);
    showToast({
      type: "error",
      title: "Error",
      message: "Failed to load exemption details",
      duration: 5000,
    });
    router.push("/exemptions");
  } finally {
    isLoading.value = false;
  }
};

// Fetch attachments
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

    if (response.success) {
      attachments.value = response.data || [];
    } else {
      console.error("Failed to fetch attachments:", response);
    }
  } catch (error) {
    console.error("Error fetching attachments:", error);
  }
};

// Validate form
const validateForm = () => {
  let isValid = true;
  errors.value = {
    policyId: "",
    transactionNumber: "",
    requestorName: "",
    details: "",
  };

  const isDraft = exemptionDetails.value?.status === "D";

  if (!isDraft && !formData.value.policyId) {
    errors.value.policyId = "Please select a policy";
    isValid = false;
  }

  if (!formData.value.transactionNumber) {
    errors.value.transactionNumber = "Please enter a transaction number";
    isValid = false;
  }

  if (!formData.value.requestorName) {
    errors.value.requestorName = "Please enter a requestor name";
    isValid = false;
  }

  if (!formData.value.details) {
    errors.value.details = "Please enter details";
    isValid = false;
  }

  return isValid;
};

// Get policy ID from form data (handles both object and number)
const getPolicyId = () => {
  return policyValue.value?.id ?? formData.value.policyId;
};

// Check if there are unsaved changes
const hasUnsavedChanges = computed(() => {
  const currentPolicyId = getPolicyId();
  const currentCorporate = formData.value.corporate || null;

  return (
    currentPolicyId !== originalData.value.policyId ||
    currentCorporate !== originalData.value.corporate ||
    formData.value.transactionNumber !== originalData.value.transactionNumber ||
    formData.value.details?.trim() !== (originalData.value.details || "").trim()
  );
});

// Handle cancel
const handleCancel = () => {
  if (hasUnsavedChanges.value) {
    if (confirm("You have unsaved changes. Are you sure you want to leave?")) {
      router.push("/exemptions");
    }
  } else {
    router.push("/exemptions");
  }
};

// Handle back (go to view page)
const handleBack = () => {
  router.push(`/exemptions/view?id=${exemptionId.value}`);
};

// Submit form
const handleSubmit = async () => {
  if (!validateForm()) return;

  isSubmitting.value = true;
  try {
    const payload = {
      transactionNumber: formData.value.transactionNumber,
      policyid: getPolicyId(),
      corporate: formData.value.corporate || null,
      details: formData.value.details,
      modifiedby: userId.userId,
    };

    const response = await apiFetch(
      `/api/exemption/update/${exemptionId.value}`,
      {
        method: "POST",
        body: JSON.stringify(payload),
        headers: {
          "Content-Type": "application/json",
        },
      },
    );

    if (response) {
      showToast({
        type: "success",
        title: "Success",
        message: "Exemption request updated successfully!",
        duration: 5000,
      });
      // Redirect to exemptions view page after success
      router.push(`/exemptions/view?id=${exemptionId.value}`);
    } else {
      showToast({
        type: "error",
        title: "Error",
        message: response?.message || "Failed to update exemption request",
        duration: 5000,
      });
    }
  } catch (error: any) {
    console.error("Error updating exemption request:", error);
    showToast({
      type: "error",
      title: "Error",
      message: error.message || "An error occurred while updating the request",
      duration: 5000,
    });
  } finally {
    isSubmitting.value = false;
  }
};

// Submit draft
const handleSubmitDraft = async () => {
  if (!validateForm()) return;

  isSubmitting.value = true;
  try {
    const response = await apiFetch(
      `/api/exemption/submit-draft/${exemptionId.value}`,
      {
        method: "POST",
      },
    );

    if (response) {
      showToast({
        type: "success",
        title: "Success",
        message: "Draft submitted successfully!",
        duration: 5000,
      });
      // Redirect to exemptions list after success
      router.push("/exemptions");
    } else {
      showToast({
        type: "error",
        title: "Error",
        message: response?.message || "Failed to submit draft",
        duration: 5000,
      });
    }
  } catch (error: any) {
    console.error("Error submitting draft:", error);
    showToast({
      type: "error",
      title: "Error",
      message: error.message || "An error occurred while submitting the draft",
      duration: 5000,
    });
  } finally {
    isSubmitting.value = false;
  }
};

// Submit form
const handleUpdateDraft = async () => {
  if (!validateForm()) return;

  isSubmitting.value = true;
  try {
    const payload = {
      transactionNumber: formData.value.transactionNumber,
      policyid: getPolicyId(),
      corporate: formData.value.corporate || null,
      details: formData.value.details,
      modifiedby: userId.userId,
    };

    const response = await apiFetch(
      `/api/exemption/update-draft/${exemptionId.value}`,
      {
        method: "POST",
        body: JSON.stringify(payload),
        headers: {
          "Content-Type": "application/json",
        },
      },
    );

    if (response) {
      showToast({
        type: "success",
        title: "Success",
        message: "Draft updated successfully!",
        duration: 5000,
      });
      // Redirect to exemptions view page after success
      router.push(`/exemptions/view?id=${exemptionId.value}`);
    } else {
      showToast({
        type: "error",
        title: "Error",
        message: response?.message || "Failed to update draft",
        duration: 5000,
      });
    }
  } catch (error: any) {
    console.error("Error updating draft:", error);
    showToast({
      type: "error",
      title: "Error",
      message: error.message || "An error occurred while updating the request",
      duration: 5000,
    });
  } finally {
    isSubmitting.value = false;
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

// Resend notification to approver
const isResending = ref<number | null>(null);
const resendToApprover = async (transApproverId: number) => {
  isResending.value = transApproverId;
  try {
    const response = await apiFetch(
      `/api/exemption/resend/${exemptionId.value}`,
      {
        method: "POST",
        body: JSON.stringify({ transApproverId }),
        headers: {
          "Content-Type": "application/json",
        },
      },
    );

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
      message:
        error.message || "An error occurred while resending notification",
      duration: 5000,
    });
  } finally {
    isResending.value = null;
  }
};

// Computed property to group main approvers with their proxies
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

// Helper function to get proxies for a main approver
const getProxies = (transApproverId: number): any[] => {
  return groupedApprovers.value.proxiesMap.get(transApproverId) || [];
};

// Helper function to check if a group (main + proxies) is satisfied (someone in group approved)
const isGroupSatisfied = (transApproverId: number): boolean => {
  const { mainApprovers, proxiesMap } = groupedApprovers.value;
  const mainApprover = mainApprovers.find(
    (a) => a.transApproverId === transApproverId,
  );
  if (!mainApprover) return false;

  const groupApprovers = [
    mainApprover,
    ...(proxiesMap.get(transApproverId) || []),
  ];
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

// Add new attachment
const addNewAttachment = () => {
  if (newAttachments.value.length < 5) {
    newAttachments.value.push({ file: null as any, referenceType: "" });
  }
};

// Remove new attachment
const removeNewAttachment = (index: number) => {
  newAttachments.value.splice(index, 1);
};

// Handle file change for new attachments
const handleNewFileChange = (index: number, event: Event) => {
  const target = event.target as HTMLInputElement;

  if (target.files && target.files[0] && newAttachments.value[index]) {
    newAttachments.value[index].file = target.files[0];
  }
};

// Delete existing attachment
const deleteAttachment = async (attachId: number) => {
  if (!confirm("Are you sure you want to delete this attachment?")) return;

  try {
    const response = await apiFetch(
      `/api/exemption/delete-attachment/${attachId}`,
      {
        method: "POST",
      },
    );

    if (response) {
      showToast({
        type: "success",
        title: "Success",
        message: "Attachment deleted successfully!",
        duration: 3000,
      });
      await handleBack();
      // await fetchAttachments();
    } else {
      showToast({
        type: "error",
        title: "Error",
        message: response.message || "Failed to delete attachment",
        duration: 3000,
      });
    }
  } catch (error) {
    console.error("Error deleting attachment:", error);
    showToast({
      type: "error",
      title: "Error",
      message: "Failed to delete attachment",
      duration: 3000,
    });
  }
};

// Add new attachments
const addNewAttachments = async () => {
  if (newAttachments.value.length === 0) return;

  const formData = new FormData();
  newAttachments.value.forEach((attachment, index) => {
    formData.append(`attachments[${index}].file`, attachment.file);
    formData.append(
      `attachments[${index}].referenceType`,
      attachment.referenceType,
    );
  });

  try {
    const response = await apiFetch(
      `/api/exemption/add-attachment/${exemptionId.value}`,
      {
        method: "POST",
        body: formData,
      },
    );

    if (response) {
      showToast({
        type: "success",
        title: "Success",
        message: "Attachments added successfully!",
        duration: 3000,
      });
      newAttachments.value = [];
      await handleBack();
      // await fetchAttachments();
    } else {
      showToast({
        type: "error",
        title: "Error",
        message: response.message || "Failed to add attachments",
        duration: 3000,
      });
    }
  } catch (error) {
    console.error("Error adding attachments:", error);
    showToast({
      type: "error",
      title: "Error",
      message: "Failed to add attachments",
      duration: 3000,
    });
  }
};

// Add to the reactive variables section
const isLoadingApprovers = ref(false);

// Add this function (adapted from create.vue)
const fetchApprovers = async (policyId: number) => {
  if (!policyId) {
    approversData.value = [];
    return;
  }

  isLoadingApprovers.value = true;
  try {
    const response = await apiFetch(`/api/policy/approvers/${policyId}`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ UserId: userId.userId }),
      credentials: "include",
      parseJson: true,
    });

    const items = response.success ? response.items : response;
    if (!items || !Array.isArray(items)) {
      console.error("Invalid approvers response:", items);
      approversData.value = [];
      return;
    }

    approversData.value = items
      .filter((a: any) => a.approvername && a.approveremail && a.approvertype)
      .map((a: any) => ({
        transApproverId: a.id, // Map to match existing structure
        approverName: a.approvername,
        approverEmail: a.approveremail,
        approverRole: a.approverRole,
        respond: 3, // Set to draft status
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

// Fetch policies on mount
onMounted(() => {
  fetchPolicies();
  fetchExemptionDetails();
});
</script>

<template>
  <div class="min-h-[50vh] bg-gray-50 overflow-auto h-100vh">
    <header class="bg-white shadow">
      <div
        class="max-w-7xl me-auto py-4 px-4 sm:px-6 lg:px-8 flex justify-between items-center"
      >
        <div class="flex items-center">
          <!-- v-if="row.requesterId === userId.emplId && exemptionDetails.status !== 'C'" -->
          <Button
            variant="ghost-primary"
            size="lg"
            icon="left_arrow"
            iconPosition="center"
            customClass="rounded-full"
            @onclick="handleBack"
          />
          <h1 class="text-2xl ps-5 font-bold text-gray-900">
            {{
              exemptionDetails?.status === "D" ? "Edit Draft" : "Edit Request"
            }}
          </h1>
        </div>
        <div class="flex items-center gap-3">
          <Button
            variant="outline-primary"
            size="md"
            @onclick="handleBack"
            :disabled="isSubmitting"
          >
            Cancel
          </Button>
          <template v-if="exemptionDetails?.status === 'D'">
            <Button
              variant="outline-info"
              size="md"
              @onclick="handleUpdateDraft"
              :loading="isSubmitting"
            >
              Update Draft
            </Button>
            <Button
              variant="primary"
              size="md"
              @onclick="handleSubmitDraft"
              :loading="isSubmitting"
            >
              Submit Request
            </Button>
          </template>
          <template v-else>
            <Button
              variant="primary"
              size="md"
              icon="save"
              iconPosition="left"
              @onclick="handleSubmit"
              :loading="isSubmitting"
            >
              Save Changes
            </Button>
          </template>
        </div>
      </div>
    </header>

    <main class="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
      <!-- Loading State -->
      <div v-if="isLoading" class="flex items-center justify-center py-20">
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
        <span class="ml-3 text-gray-600">Loading exemption details...</span>
      </div>

      <!-- Content -->
      <div v-else>
        <!-- Info Notice -->
        <div class="bg-blue-50 border border-blue-200 rounded-lg p-4 mt-4 mb-3">
          <div class="flex">
            <div class="shrink-0">
              <svg
                class="h-5 w-5 text-blue-400"
                xmlns="http://www.w3.org/2000/svg"
                viewBox="0 0 20 20"
                fill="currentColor"
              >
                <path
                  fill-rule="evenodd"
                  d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z"
                  clip-rule="evenodd"
                />
              </svg>
            </div>
            <div class="ml-3">
              <p class="text-sm text-blue-700">
                Note: Request Status and Approver Status cannot be edited. These
                are managed automatically.
              </p>
            </div>
          </div>
        </div>
        <div class="grid grid-cols-4 gap-6">
          <!-- Left Column -->
          <div
            class="sm:col-span-full md:col-span-2 bg-white rounded-lg shadow p-6"
          >
            <!-- Transaction Info -->
            <div class="mb-8">
              <div
                class="text-xl font-semibold text-gray-800 mb-4 pb-2 border-b"
              >
                <div class="flex justify-between items-center">
                  <h2>Request Information</h2>
                  <div v-if="exemptionDetails">
                    <span
                      class="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium"
                      :class="{
                        'bg-blue-100 text-blue-800 border border-blue-900':
                          exemptionDetails.status === 'O' ||
                          exemptionDetails.status === 'P',
                        'bg-gray-200 text-gray-800 border border-gray-900':
                          exemptionDetails.status === 'C',
                        'bg-gray-100 text-gray-800 border border-gray-900':
                          exemptionDetails.status === 'D',
                      }"
                    >
                      {{ getStatusDisplay(exemptionDetails.status) }}
                    </span>
                  </div>
                </div>
              </div>
              <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div>
                  <label class="block text-sm font-medium text-gray-500 mb-1">
                    Transaction Number
                  </label>
                  <p class="text-gray-900">
                    {{ formData.transactionNumber || "N/A" }}
                  </p>
                </div>
                <div>
                  <label class="block text-sm font-medium text-gray-500 mb-1">
                    Requestor Name
                  </label>
                  <p class="text-gray-900">
                    {{ formData.requestorName || "N/A" }}
                  </p>
                </div>
              </div>
            </div>

            <!-- Request Details -->
            <div class="mb-8">
              <div class="space-y-6">
                <div class="grid grid-cols-1 gap-6">
                  <!-- md:grid-cols-2 -->
                  <div>
                    <label class="block text-sm font-medium text-gray-500 mb-1">
                      Policy To Override
                      <span
                        v-if="exemptionDetails?.status !== 'D'"
                        class="text-red-500"
                        >*</span
                      >
                    </label>
                    <Dropdown
                      v-model="policyValue"
                      :initialItems="policyOptions"
                      placeholder="Select a policy"
                      :disabled="isLoadingPolicies"
                      :errorMessage="errors.policyId"
                    />
                  </div>
                </div>

                <div>
                  <label class="block text-sm font-medium text-gray-500 mb-1">
                    Details <span class="text-red-500">*</span>
                  </label>
                  <BaseInput
                    v-model="formData.details"
                    type="textarea"
                    isTextarea
                    :rowCount="5"
                    placeholder="Explain the reason for your exemption request..."
                    :errorMessage="errors.details"
                    :maxCharCount="1000"
                  />
                </div>
              </div>
            </div>

            <!-- Attachments -->
            <div class="mb-8">
              <h2
                class="text-xl font-semibold text-gray-800 mb-4 pb-2 border-b"
              >
                Attachments
              </h2>

              <!-- Existing Attachments -->
              <div v-if="attachments.length > 0" class="mb-6">
                <h3 class="text-lg font-medium text-gray-700 mb-3">
                  Existing Attachments
                </h3>
                <div class="space-y-2">
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
                          {{ attachment.referenceType }} •
                          {{ attachment.filetype }} •
                          {{
                            new Date(attachment.createdate).toLocaleDateString()
                          }}
                        </p>
                      </div>
                    </div>
                    <div class="flex items-center gap-2">
                      <a
                        :href="`${config.public.apiBase}/uploads/attachments/${attachment.newfilename}`"
                        target="_blank"
                        class="inline-flex items-center px-3 py-1 rounded-md text-sm font-medium bg-blue-100 text-blue-700 hover:bg-blue-200"
                      >
                        View
                      </a>
                      <button
                        @click="deleteAttachment(attachment.attachId)"
                        class="inline-flex items-center px-3 py-1 rounded-md text-sm font-medium bg-red-100 text-red-700 hover:bg-red-200"
                      >
                        Delete
                      </button>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Add New Attachments -->
              <div>
                <h3 class="text-lg font-medium text-gray-700 mb-3">
                  Add New Attachments (Max 5 files, 10MB each)
                </h3>
                <div
                  v-for="(attachment, index) in newAttachments"
                  :key="index"
                  class="mb-4 p-4 border border-gray-200 rounded-lg"
                >
                  <div class="flex items-center gap-4">
                    <div class="flex-1">
                      <input
                        type="file"
                        @change="(e) => handleNewFileChange(index, e)"
                        accept=".pdf,.doc,.docx,.xls,.xlsx,.txt,.jpg,.jpeg,.png"
                        class="block w-full text-sm text-gray-500 file:mr-4 file:py-2 file:px-4 file:rounded-full file:border-0 file:text-sm file:font-semibold file:bg-blue-50 file:text-blue-700 hover:file:bg-blue-100"
                      />
                      <p
                        v-if="attachment.file"
                        class="mt-1 text-xs text-gray-500"
                      >
                        Selected: {{ attachment.file.name }} ({{
                          (attachment.file.size / 1024 / 1024).toFixed(2)
                        }}
                        MB)
                      </p>
                    </div>
                    <div class="w-48">
                      <select
                        v-model="attachment.referenceType"
                        class="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                      >
                        <option value="">Select type</option>
                        <option value="documentation">Documentation</option>
                        <option value="email">Email</option>
                        <option value="supporting">Supporting Document</option>
                        <option value="other">Other</option>
                      </select>
                    </div>
                    <Button
                      variant="outline-danger"
                      size="sm"
                      @onclick="removeNewAttachment(index)"
                      icon="trash"
                      icon-position="center"
                    >
                    </Button>
                  </div>
                </div>
                <div class="flex gap-3">
                  <Button
                    v-if="newAttachments.length < 5"
                    variant="outline-primary"
                    size="sm"
                    @onclick="addNewAttachment"
                    icon="plus"
                  >
                    Add Attachment
                  </Button>
                  <Button
                    v-if="newAttachments.length > 0"
                    variant="primary"
                    size="sm"
                    @onclick="addNewAttachments"
                    :loading="isSubmitting"
                  >
                    Save Attachments
                  </Button>
                </div>
              </div>
            </div>
          </div>

          <!-- Right Column -->
          <div
            class="sm:col-span-full md:col-span-2 bg-white rounded-lg shadow px-6 py-6"
          >
            <!-- Approvers Status -->
            <div class="mb-8">
              <div class="border-b flex justify-between">
                <h2 class="text-xl font-semibold text-gray-800 mb-2">
                  Approvers Status
                </h2>
                <div>
                  <span
                    v-if="exemptionDetails"
                    class="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium"
                    :class="{
                      'bg-yellow-100 text-yellow-800 border border-yellow-900':
                        exemptionDetails.finalDecision === 'Pending',
                      'bg-green-100 text-green-800 border border-green-900':
                        exemptionDetails.finalDecision === 'Approved',
                      'bg-red-100 text-red-800 border border-red-900':
                        exemptionDetails.finalDecision === 'Disapproved',
                      'bg-gray-100 text-gray-800 border border-gray-900':
                        exemptionDetails.finalDecision === 'Draft',
                    }"
                  >
                    {{
                      getFinalDecisionDisplay(exemptionDetails.finalDecision)
                    }}
                  </span>
                </div>
              </div>

              <div
                v-if="approversData && approversData.length > 0"
                class="space-y-4"
              >
                <template
                  v-for="(approver, index) in groupedApprovers.mainApprovers"
                  :key="approver.transApproverId"
                >
                  <div class="bg-gray-50 rounded-lg p-3 border border-gray-200">
                    <div class="flex justify-between items-start mb-2">
                      <div class="flex items-start">
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
                            <span
                              v-if="approver.approverRole"
                              class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-600"
                            >
                              {{ approver.approverRole }}
                            </span>
                          </div>
                        </div>
                      </div>
                      <div class="flex flex-col items-end gap-1">
                        <span
                          v-if="approver.respond != 3"
                          class="inline-flex items-center px-2 py-1 rounded text-xs font-medium"
                          :class="getApproverStatusColor(approver.respond)"
                        >
                          {{ getApproverStatusDisplay(approver.respond) }}
                        </span>
                        <!-- Decision badge for decided approvers -->

                        <!-- Resend button only for current pending approver (and not satisfied by group) -->
                        <button
                          v-if="
                            approver.respond === 0 &&
                            approver.transApproverId === currentApproverId &&
                            !isGroupSatisfied(approver.transApproverId)
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

                    <div
                      v-if="getProxies(approver.transApproverId).length > 0"
                      class="ml-8"
                    >
                      <div
                        v-for="proxy in getProxies(approver.transApproverId)"
                        :key="proxy.transApproverId"
                        class="flex items-start justify-between py-2 border-b border-gray-200 last:border-0"
                      >
                        <div class="flex items-start">
                          <span
                            class="w-2 h-2 rounded-full bg-blue-400 mt-1.5"
                          ></span>
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
                          <span
                            v-if="approver.respond != 3"
                            class="inline-flex items-center px-2 py-1 rounded text-xs font-medium"
                            :class="getApproverStatusColor(proxy.respond)"
                          >
                            {{ getApproverStatusDisplay(proxy.respond) }}
                          </span>
                          <!-- Decision badge for decided proxy approvers -->

                          <!-- Resend button only for current pending proxy (and not satisfied by group) -->
                          <button
                            v-if="
                              proxy.respond === 0 &&
                              proxy.transApproverId === currentApproverId &&
                              !isGroupSatisfied(approver.transApproverId)
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
                class="text-sm flex items-center justify-center h-24 text-gray-400"
              >
                <span
                  v-if="exemptionDetails?.status === 'D'"
                  class="text-black bg-amber-200 p-2 rounded-md"
                  >Please submit request to send email notifications to
                  approvers.</span
                >
                <span v-else>No approvers assigned yet.</span>
              </div>
            </div>

            <!-- Remarks (if any) -->
            <div v-if="exemptionDetails?.remarks" class="mb-8">
              <h2
                class="text-xl font-semibold text-gray-800 mb-4 pb-2 border-b"
              >
                Remarks
              </h2>
              <div class="bg-gray-50 rounded-lg p-4 border border-gray-200">
                <p class="text-gray-900 whitespace-pre-wrap">
                  {{ exemptionDetails.remarks }}
                </p>
              </div>
            </div>

            <!-- Audit Trail -->
            <!-- <div class="mb-8">
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
                    {{ formatDate(exemptionDetails?.createdDate) }}
                  </p>
                </div>
                <div>
                  <label class="block text-sm font-medium text-gray-500 mb-1">
                    Modified Date
                  </label>
                  <p class="text-gray-900">
                    {{ formatDate(exemptionDetails?.modifiedDate) }}
                  </p>
                </div>
                <div>
                  <label class="block text-sm font-medium text-gray-500 mb-1">
                    Workstation
                  </label>
                  <p class="text-gray-900">
                    {{ exemptionDetails?.workstation || "N/A" }}
                  </p>
                </div>
                <div>
                  <label class="block text-sm font-medium text-gray-500 mb-1">
                    IP Address
                  </label>
                  <p class="text-gray-900">
                    {{ exemptionDetails?.ipAddress || "N/A" }}
                  </p>
                </div>
              </div>
            </div> -->
          </div>
        </div>
      </div>
    </main>
  </div>
</template>
