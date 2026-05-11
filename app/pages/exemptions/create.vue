import { user } from '../../composables/useAuth';
<script setup lang="ts">
const router = useRouter();
const { showToast } = useToast();
const userStore = useUserStore();

const userId = userStore.profile;

// Form data
// Form data (remove attachments from here)
const formData = ref({
  policyid: null as number | { id: number; label: string } | null,
  corporate: null as { id: string; label: string } | null,
  details: "",
  referenceType: "",
});

// Add newAttachments (from edit.vue)
const newAttachments = ref<{ file: File; referenceType: string }[]>([]);

// Draft state
const isDraft = ref(false);
const draftId = ref<number | null>(null);
const isDraftMode = ref(false);

// Policy options - will be fetched from API
const policyOptions = ref<{ id: number; label: string }[]>([]);
const isLoadingPolicies = ref(false);

// Approvers for selected policy
const approvers = ref<
  {
    id: number;
    approvername: string;
    approveremail: string;
    approvertype: string;
    sort: number;
    linesequence?: number;
    isMainApprover?: boolean;
    approverRole?: string;
  }[]
>([]);
const isLoadingApprovers = ref(false);

// Computed property to group main approvers with their proxies
// Main approvers: linesequence = 1 (regardless of sort value)
// Proxies: linesequence > 1, grouped under main approvers by matching sort values
const groupedApprovers = computed(() => {
  const mainApprovers: any[] = [];
  const proxiesMap: Map<number, any[]> = new Map();

  approvers.value.forEach((approver) => {
    // Determine if approver is Main (linesequence === 1) or Proxy (linesequence > 1)
    const isMain = approver.linesequence === 1;
    const isIM = approver.approvertype === "IM";

    if (isMain && !isIM) {
      // This is a Main Approver (linesequence=1, not IM)
      mainApprovers.push(approver);
      proxiesMap.set(approver.id, []);
    } else if (isIM) {
      // This is IM - add to mainApprovers but with no proxies
      mainApprovers.push(approver);
      // Don't add to proxiesMap - IM won't have proxies
    } else {
      // This is a Proxy (linesequence > 1) - group under main approver by matching sort
      const mainApprover = mainApprovers.find(
        (m) => m.sort === approver.sort && m.approvertype !== "IM",
      );
      if (mainApprover) {
        const proxies = proxiesMap.get(mainApprover.id) || [];
        proxies.push(approver);
        proxiesMap.set(mainApprover.id, proxies);
      }
    }
  });

  return { mainApprovers, proxiesMap };
});

// Helper function to get proxies for a main approver
const getProxies = (mainId: number): any[] => {
  return groupedApprovers.value.proxiesMap.get(mainId) || [];
};

// Validation errors
const errors = ref({
  policyid: "",
  details: "",
  attachments: "",
  referenceType: "",
});

// Loading state
const isSubmitting = ref(false);
const isSavingDraft = ref(false);

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

// Fetch approvers for selected policy
const fetchApprovers = async (policyId: number) => {
  if (!policyId) {
    approvers.value = [];
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

    console.log("Fetched approvers in Create Page", items);

    if (!items || !Array.isArray(items)) {
      console.error("Invalid approvers response:", items);
      approvers.value = [];
      return;
    }

    approvers.value = items
      .filter((a: any) => a.approvername && a.approveremail && a.approvertype) // Filter out approvers with null name, email, or type
      .map((a: any) => ({
        id: a.id,
        approvername: a.approvername,
        approveremail: a.approveremail,
        approvertype: a.approvertype,
        sort: a.sort,
        linesequence: a.linesequence,
        isMainApprover: a.isMainApprover,
        approverRole: a.approverRole,
      }));
  } catch (error) {
    console.error("Error fetching approvers:", error);
    approvers.value = [];
  } finally {
    isLoadingApprovers.value = false;
  }
};

// Watch for policy changes to fetch approvers
watch(
  () => formData.value.policyid,
  (newPolicyId) => {
    const policyId =
      newPolicyId && typeof newPolicyId === "object"
        ? newPolicyId.id
        : newPolicyId;
    if (policyId) {
      fetchApprovers(policyId as number);
    } else {
      approvers.value = [];
    }
  },
);

// Watchers to clear errors on input changes for real-time feedback
watch(
  () => formData.value.policyid,
  () => {
    errors.value.policyid = "";
  },
);

watch(
  () => formData.value.details,
  () => {
    errors.value.details = "";
  },
);

watch(
  () => newAttachments.value,
  () => {
    errors.value.attachments = "";
  },
  { deep: true },
);

// Add watchers to clear errors on input changes
watch(
  () => formData.value.policyid,
  () => {
    errors.value.policyid = ""; // Clear policy error when changed
  },
);

watch(
  () => formData.value.details,
  () => {
    errors.value.details = ""; // Clear details error when changed
  },
);

watch(
  () => newAttachments.value,
  () => {
    errors.value.attachments = ""; // Clear attachments error when changed
  },
  { deep: true }, // Deep watch for array/object changes
);

watch(
  () => newAttachments.value.map(a => a.referenceType),
  () => {
    errors.value.referenceType = "";
  },
  { deep: true }
);

// Validate form
const validateForm = (forDraft = false) => {
  let isValid = true;
  errors.value = {
    policyid: "",
    details: "",
    attachments: "",
    referenceType: "",
  };

  // Policy is required only for full submission (not draft)
  if (!forDraft && !formData.value.policyid) {
    errors.value.policyid = "Please select a policy";
    isValid = false;
  }

  // Details required for both draft and submission
  if (!formData.value.details || formData.value.details.trim().length < 10) {
    errors.value.details = "Please provide details (minimum 10 characters).";
    isValid = false;
  }

  // Attachment count limit
  if (newAttachments.value.length > 5) {
    errors.value.attachments = "Maximum 5 attachments allowed.";
    isValid = false;
  }

  // Per-attachment validation
  for (const attachment of newAttachments.value) {
    if (!attachment.file) {
      errors.value.attachments = "Please select a file for all attachments.";
      isValid = false;
      break;
    }

    if (attachment.file.size > 10 * 1024 * 1024) {
      errors.value.attachments = "Each file must be less than 10MB.";
      isValid = false;
      break;
    }

    if (!attachment.referenceType) {
      errors.value.referenceType =
        "Please select a reference type for all attachments.";
      isValid = false;
      break;
    }
  }

  return isValid;
};

// Computed property to check if form is valid for submission
const isFormValid = computed(() => {
  return (
    !!formData.value.policyid &&
    !!formData.value.details &&
    formData.value.details.trim().length >= 10 &&
    newAttachments.value.length <= 5 &&
    newAttachments.value.every(
      (att) =>
        att.file && att.referenceType && att.file.size <= 10 * 1024 * 1024,
    )
  );
});

// Computed property to check if form is valid for draft
const isDraftValid = computed(() => {
  return (
    !!formData.value.details &&
    formData.value.details.trim().length >= 10 &&
    newAttachments.value.length <= 5 &&
    newAttachments.value.every(
      (att) =>
        att.file && att.referenceType && att.file.size <= 10 * 1024 * 1024,
    )
  );
});

// Get policy ID from form data (handles both object and number)
const getPolicyId = () => {
  const policyId = formData.value.policyid;
  return policyId && typeof policyId === "object" ? policyId.id : policyId;
};

const parseWorkstation = (): string => {
  const ua = navigator.userAgent;

  const os = ua.includes("Windows NT 10.0")
    ? "Windows 10/11"
    : ua.includes("Windows NT 6.1")
      ? "Windows 7"
      : ua.includes("Mac")
        ? "MacOS"
        : ua.includes("Linux")
          ? "Linux"
          : ua.includes("Android")
            ? "Android"
            : ua.includes("iPhone")
              ? "iOS"
              : "Unknown OS";

  const edgeMatch = ua.match(/Edg\/([\d]+)/);
  const chromeMatch = ua.match(/Chrome\/([\d]+)/);
  const firefoxMatch = ua.match(/Firefox\/([\d]+)/);
  const safariMatch = ua.match(/Version\/([\d]+)/);

  const browser = edgeMatch
    ? `Edge ${edgeMatch[1]}`
    : chromeMatch
      ? `Chrome ${chromeMatch[1]}`
      : firefoxMatch
        ? `Firefox ${firefoxMatch[1]}`
        : safariMatch
          ? `Safari ${safariMatch[1]}`
          : "Unknown Browser";

  return `${os} / ${browser}`.substring(0, 50);
};

// Save or update draft
const handleSaveDraft = async () => {
  if (!validateForm(true)) return;

  isSavingDraft.value = true;
  try {
    const workstation = parseWorkstation();
    const formDataToSend = new FormData();
    const policyId = getPolicyId();
    if (formData.value.policyid && policyId != null) {
      formDataToSend.append("policyid", policyId.toString());
    }
    formDataToSend.append("details", formData.value.details);
    formDataToSend.append("requestorid", userId.emplId);
    formDataToSend.append("createdby", userId.userId.toString());
    formDataToSend.append("computerhost", workstation);

    newAttachments.value.forEach((attachment, index) => {
      if (attachment.file) {
        formDataToSend.append(`Attachments[${index}].file`, attachment.file);
      }
      formDataToSend.append(
        `Attachments[${index}].ReferenceType`,
        attachment.referenceType,
      );
    });

    let response;
    if (draftId.value) {
      // Update existing draft
      response = await apiFetch(
        `/api/exemption/update-draft/${draftId.value}`,
        {
          method: "POST",
          body: formDataToSend,
          parseJson: true,
        },
      );
    } else {
      // Save new draft
      response = await apiFetch("/api/exemption/save-draft", {
        method: "POST",
        body: formDataToSend,
        parseJson: true,
      });
    }

    console.log("Draft save response:", response);

    if (response) {
      draftId.value = response.item.transactionId;
      isDraft.value = true;
      showToast({
        type: "success",
        title: "Success",
        message: draftId.value
          ? "Draft updated successfully!"
          : "Draft saved successfully!",
        duration: 5000,
      });
      router.push("/exemptions/view?id=" + draftId.value);
    } else {
      showToast({
        type: "error",
        title: "Error",
        message: response.message || "Failed to save draft",
        duration: 5000,
      });
    }
  } catch (error: any) {
    console.error("Error saving draft:", error);
    showToast({
      type: "error",
      title: "Error",
      message: error.message || "An error occurred while saving the draft",
      duration: 5000,
    });
  } finally {
    isSavingDraft.value = false;
  }
};

// Submit form
const handleSubmit = async () => {
  if (!validateForm()) return;

  isSubmitting.value = true;
  try {
    if (draftId.value) {
      // Submit existing draft
      const response = await apiFetch(
        `/api/exemption/submit-draft/${draftId.value}`,
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
        router.push("/exemptions/all");
      } else {
        showToast({
          type: "error",
          title: "Error",
          message: response.message || "Failed to submit draft",
          duration: 5000,
        });
      }
    } else {
      // Create new request
      // Diagnostic logging
      console.log("Submitting exemption request with data:", {
        policyId: getPolicyId(),
        details: formData.value.details,
        requestorId: userId.emplId,
        createdBy: userId.userId,
        attachments: newAttachments.value.map((a) => ({
          fileName: a.file?.name,
          fileSize: a.file?.size,
          referenceType: a.referenceType,
        })),
      });

      const workstation = parseWorkstation();
      const formDataToSend = new FormData();
      const policyId = getPolicyId();
      if (policyId != null) {
        formDataToSend.append("policyid", String(policyId!));
      }
      formDataToSend.append("details", formData.value.details);
      formDataToSend.append("requestorid", userId.emplId);
      formDataToSend.append("createdby", userId.userId.toString());
      formDataToSend.append("computerhost", workstation);

      newAttachments.value.forEach((attachment, index) => {
        if (attachment.file) {
          formDataToSend.append(`Attachments[${index}].file`, attachment.file);
        }
        formDataToSend.append(
          `Attachments[${index}].ReferenceType`,
          attachment.referenceType,
        );
      });

      const response = await apiFetch("/api/exemption/create", {
        method: "POST",
        body: formDataToSend,
        // No Content-Type header - let browser set it for FormData
      });

      if (response) {
        showToast({
          type: "success",
          title: "Success",
          message: "Exemption request created successfully!",
          duration: 5000,
        });
        // Redirect to exemptions list after success
        router.push("/exemptions/all");
      } else {
        showToast({
          type: "error",
          title: "Error",
          message: response.message || "Failed to create exemption request",
          duration: 5000,
        });
      }
    }
  } catch (error: any) {
    console.error("Error submitting request:", error);
    showToast({
      type: "error",
      title: "Error",
      message:
        error.message || "An error occurred while submitting the request",
      duration: 5000,
    });
  } finally {
    isSubmitting.value = false;
  }
};

// Handle cancel
const handleCancel = () => {
  router.push("/exemptions/all");
};

// Add attachment
const addAttachment = () => {
  if (newAttachments.value.length < 5) {
    newAttachments.value.push({ file: null as any, referenceType: "" });
  }
};

// Remove attachment
const removeAttachment = (index: number) => {
  newAttachments.value.splice(index, 1);
};

// Handle file selection
const handleFileChange = (index: number, event: Event) => {
  const target = event.target as HTMLInputElement;
  if (target.files && target.files[0] && newAttachments.value[index]) {
    newAttachments.value[index].file = target.files[0];
  }
};

// Load draft if draftId is provided
const loadDraft = async (id: number) => {
  try {
    const response = await apiFetch(`/api/exemption/view/${id}`, {
      method: "POST",
      body: JSON.stringify({ UserId: userId.userId }),
      headers: {
        "Content-Type": "application/json",
      },
      parseJson: true,
    });

    if (response && response.item) {
      const item = response.item;
      if (item.status === "D") {
        draftId.value = id;
        isDraft.value = true;
        isDraftMode.value = true;
        formData.value.policyid = item.policyId
          ? { id: item.policyId, label: item.policyName }
          : null;
        formData.value.details = item.details || "";
        // Load attachments if any
        // For now, assume attachments are handled separately
      }
    }
  } catch (error) {
    console.error("Error loading draft:", error);
  }
};

// Fetch policies on mount and check for draft
onMounted(async () => {
  fetchPolicies();
  const query = new URLSearchParams(window.location.search);
  const draftParam = query.get("draftId");
  if (draftParam) {
    const id = parseInt(draftParam);
    if (id) {
      await loadDraft(id);
    }
  }
});
</script>

<template>
  <div class="min-h-[50vh] bg-gray-50 overflow-auto h-100vh">
    <header class="bg-white shadow">
      <div
        class="max-w-7xl me-auto py-6 px-4 sm:px-6 lg:px-8 flex justify-start items-center"
      >
        <Button
          variant="ghost-primary"
          size="lg"
          icon="left_arrow"
          iconPosition="center"
          customClass="rounded-full"
          @onclick="handleCancel"
        />
        <h1 class="text-3xl ps-5 font-bold text-gray-900">
          {{ draftId ? "Edit Draft" : "Create Exemption Request" }}
        </h1>
      </div>
    </header>

    <main class="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
      <div class="bg-white rounded-lg shadow px-6 py-6">
        <form @submit.prevent="handleSubmit" class="space-y-6">
          <!-- Policy Selection -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">
              Policy To Override
              <span v-if="!isDraftMode" class="text-red-500">*</span>
            </label>
            <Dropdown
              v-model="formData.policyid"
              :initialItems="policyOptions"
              placeholder="Select a policy"
              :disabled="isLoadingPolicies"
            />
            <p v-if="errors.policyid" class="mt-1 text-xs text-red-500">
              {{ errors.policyid }}
            </p>
          </div>

          <!-- Details -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">
              Additional Comment/Detailed Info
              <span class="text-red-500">*</span>
            </label>
            <BaseInput
              v-model="formData.details"
              type="text"
              isTextarea
              :rowCount="5"
              placeholder="Explain the reason for your exemption request..."
              :error="errors.details"
              :maxCharCount="1000"
              :minCharCount="10"
            />
          </div>

          <!-- Attachments -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">
              Attachments (Optional - Max 5 files, 10MB each)
            </label>
            <div
              v-for="(attachment, index) in newAttachments"
              :key="index"
              class="mb-4 p-4 border border-gray-200 rounded-lg"
            >
              <div class="flex items-center gap-4">
                <div class="flex-1">
                  <input
                    type="file"
                    @change="(e) => handleFileChange(index, e)"
                    accept=".pdf,.doc,.docx,.xls,.xlsx,.txt,.jpg,.jpeg,.png"
                    class="block w-full text-sm text-gray-500 file:mr-4 file:py-2 file:px-4 file:rounded-full file:border-0 file:text-sm file:font-semibold file:bg-blue-50 file:text-blue-700 hover:file:bg-blue-100"
                  />
                  <p v-if="attachment.file" class="mt-1 text-xs text-gray-500">
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
                  @onclick="removeAttachment(index)"
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
                @onclick="addAttachment"
                icon="plus"
              >
                Add Attachment
              </Button>
            </div>
            <p v-if="errors.attachments" class="mt-1 text-xs text-red-500">
              {{ errors.attachments }}
            </p>
            <p v-if="errors.referenceType" class="mt-1 text-xs text-red-500">
              {{ errors.referenceType }}
            </p>
          </div>

          <!-- Approvers List -->
          <div v-if="formData.policyid">
            <label class="block text-sm font-medium text-gray-700 mb-2">
              Approvers
            </label>
            <div
              v-if="isLoadingApprovers"
              class="flex items-center justify-center py-4"
            >
              <svg
                class="animate-spin h-5 w-5 text-blue-600"
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
              <span class="ml-2 text-gray-500">Loading approvers...</span>
            </div>
            <div
              v-else-if="approvers.length > 0"
              class="bg-gray-50 rounded-lg border border-gray-200 divide-y divide-gray-200"
            >
              <template
                v-for="(approver, index) in groupedApprovers.mainApprovers"
                :key="approver.id"
              >
                <!-- Main Approver (numbered) -->
                <div class="p-3 flex items-center justify-between">
                  <div class="flex items-center">
                    <span
                      class="flex items-center justify-center w-6 h-6 rounded-full bg-blue-100 text-blue-600 text-sm font-medium mr-3"
                    >
                      {{ index + 1 }}
                    </span>
                    <div>
                      <p class="text-sm font-medium text-gray-900">
                        {{ approver.approvername }}
                      </p>
                      <p class="text-sm text-gray-500">
                        {{ approver.approveremail }}
                      </p>
                    </div>
                  </div>
                  <div class="flex items-center gap-2">
                    <span
                      v-if="approver.approverRole"
                      class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium"
                      :class="{
                        'bg-blue-100 text-blue-800':
                          approver.linesequence === 1,
                        'bg-orange-100 text-orange-800':
                          approver.linesequence !== 1,
                      }"
                    >
                      {{ approver.approverRole }}
                    </span>
                  </div>
                </div>

                <!-- Proxies (indented bullet points) -->
                <div
                  v-if="getProxies(approver.id).length > 0"
                  class="bg-gray-100 pl-8 pr-3 py-2"
                >
                  <div
                    v-for="proxy in getProxies(approver.id)"
                    :key="proxy.id"
                    class="flex items-center py-1"
                  >
                    <span class="w-2 h-2 rounded-full bg-blue-500 mr-2"></span>
                    <div class="flex-1 flex items-center justify-between">
                      <div>
                        <p class="text-sm text-gray-700">
                          {{ proxy.approvername }}
                        </p>
                        <p class="text-xs text-gray-500">
                          {{ proxy.approveremail }}
                        </p>
                      </div>
                      <span
                        v-if="proxy.approverRole"
                        class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-orange-100 text-orange-800"
                      >
                        {{ proxy.approverRole }}
                      </span>
                    </div>
                  </div>
                </div>
              </template>
            </div>
            <div
              v-else
              class="bg-gray-50 rounded-lg border border-gray-200 p-4 text-center text-gray-500 text-sm"
            >
              No approvers found for this policy
            </div>
          </div>

          <!-- Action Buttons -->
          <div class="flex justify-end gap-3 pt-4 border-t border-gray-200">
            <Button
              variant="outline-info"
              size="lg"
              @onclick="handleCancel"
              :disabled="isSubmitting || isSavingDraft"
            >
              Cancel
            </Button>
            <Button
              variant="outline-primary"
              size="lg"
              @onclick="handleSaveDraft"
              :loading="isSavingDraft"
              :disabled="!isDraftValid"
            >
              {{ draftId ? "Update Draft" : "Save as Draft" }}
            </Button>
            <Button
              variant="primary"
              size="lg"
              type="submit"
              :loading="isSubmitting"
              :disabled="!isFormValid"
            >
              Submit Request
            </Button>
          </div>
        </form>
      </div>
    </main>
  </div>
</template>
