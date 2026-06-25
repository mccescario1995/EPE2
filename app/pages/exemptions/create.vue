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
  // referenceType: "",
});

// Add newAttachments (from edit.vue)
const newAttachments = ref<{ file: File /* referenceType: string */ }[]>([]);

const MAX_FILES = 5;
const MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB per file
const MAX_TOTAL_SIZE = 50 * 1024 * 1024; // 50MB total

const ALLOWED_EXTENSIONS = [
  ".pdf",
  ".doc",
  ".docx",
  ".xls",
  ".xlsx",
  ".txt",
  ".jpg",
  ".jpeg",
  ".png",
]; // Allowed file types

const isFileTypeAllowed = (file: File): boolean => {
  const extension = "." + file.name.split(".").pop()?.toLowerCase();
  return ALLOWED_EXTENSIONS.includes(extension);
};

const totalNewAttachmentsSize = computed(() =>
  newAttachments.value.reduce((sum, a) => sum + (a.file?.size || 0), 0),
);
const totalNewAttachmentsSizeMB = computed(() =>
  (totalNewAttachmentsSize.value / 1024 / 1024).toFixed(2),
);

const totalAttachmentsCount = computed(() => newAttachments.value.length);

const remainingAttachmentSlots = computed(() =>
  Math.max(0, MAX_FILES - newAttachments.value.length),
);

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
  // referenceType: "",
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
      label: p.shortname || p.description || p.label || `Policy ${p.policyid}`,
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
      `/api/Maintenance/create/policy/${policyId}/approvers`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ UserId: userId.userId }),
        // credentials: "include",
        parseJson: true,
      },
    );

    const items = response ? response.items : response;

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
    // errors.value.referenceType = ""; // Clear referenceType error when changed
  },
  { deep: true }, // Deep watch for array/object changes
);

// watch(
//   () => newAttachments.value.map(a => a.referenceType),
//   () => {
//     errors.value.referenceType = "";
//   },
//   { deep: true }
// );

// Clear attachment error only when a file is selected
watch(
  () => newAttachments.value.map((a) => a.file),
  (newFiles) => {
    if (newFiles.some((f) => f !== null)) {
      errors.value.attachments = "";
    }
  },
  { deep: true },
);

// Clear referenceType error when any referenceType is selected
// watch(
//   () => newAttachments.value.map(a => a.referenceType),
//   (newTypes) => {
//     if (newTypes.some(t => t !== "")) {
//       errors.value.referenceType = "";
//     }
//   },
//   { deep: true }
// );

//                                                                            orm
const validateForm = (forDraft = false) => {
  let isValid = true;
  errors.value = {
    policyid: "",
    details: "",
    attachments: "",
  };

  if (!forDraft && !formData.value.policyid) {
    errors.value.policyid = "Please select a policy";
    isValid = false;
  }

  if (!formData.value.details || formData.value.details.trim().length < 10) {
    errors.value.details = "Please provide details (minimum 10 characters).";
    isValid = false;
  }

  if (newAttachments.value.length > MAX_FILES) {
    errors.value.attachments = "Maximum 5 attachments allowed.";
    isValid = false;
  }

  if (!forDraft && newAttachments.value.length === 0) {
    errors.value.attachments = "Please attach at least one file.";
    isValid = false;
  }

  if (totalNewAttachmentsSize.value > MAX_TOTAL_SIZE) {
    errors.value.attachments = "Total attachment size exceeds 50MB limit.";
    isValid = false;
  }

  const hasInvalid = newAttachments.value.some((a) => !a.file);
  if (hasInvalid) {
    errors.value.attachments = "Please select a file for all attachments.";
    isValid = false;
  }

  const hasInvalidType = newAttachments.value.some(
    (a) => a.file && !isFileTypeAllowed(a.file),
  );
  if (hasInvalidType) {
    errors.value.attachments =
      "Some attachments have invalid file types. Allowed: PDF, DOC, DOCX, XLS, XLSX, TXT, JPG, JPEG, PNG.";
    isValid = false;
  }

  return isValid;
};

// Computed property to check if form is valid for submission
const isFormValid = computed(() => {
  return (
    !!formData.value.policyid &&
    !!formData.value.details &&
    formData.value.details.trim().length >= 10 &&
    newAttachments.value.length > 0 &&
    newAttachments.value.length <= MAX_FILES &&
    totalNewAttachmentsSize.value <= MAX_TOTAL_SIZE &&
    newAttachments.value.every((att) => att.file && isFileTypeAllowed(att.file))
  );
});

// Computed property to check if form is valid for draft
const isDraftValid = computed(() => {
  return (
    !!formData.value.policyid &&
    !!formData.value.details &&
    formData.value.details.trim().length >= 10 &&
    newAttachments.value.length > 0 &&
    newAttachments.value.length <= MAX_FILES &&
    totalNewAttachmentsSize.value <= MAX_TOTAL_SIZE &&
    newAttachments.value.every((att) => att.file && isFileTypeAllowed(att.file))
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

    newAttachments.value.forEach((attachment) => {
      formDataToSend.append("files", attachment.file);
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

    const transactionId =
      response?.item?.transactionId || response?.item?.transactionid;
    console.log("Transaction ID from response:", transactionId);

    const attachmentResponse: any = await apiFetch(
      "/api/exemption/add-attachment/" + transactionId,
      {
        method: "POST",
        body: formDataToSend,
        parseJson: true,
      },
    );

    console.log("Attachment response:", attachmentResponse);
    console.log("Attachment response transaction ID:", transactionId);

    if (attachmentResponse && response) {
      draftId.value = transactionId.toString();
      console.log("Draft ID:", draftId.value);
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

  console.log("Submitting form with data:", {
    policyid: formData.value.policyid,
    details: formData.value.details,
    attachments: newAttachments.value,
  });

  try {
    const formDataToSend = new FormData();

    const policyId = getPolicyId();
    if (policyId != null) {
      formDataToSend.append("policyid", String(policyId));
    }

    console.log("Policy ID to send:", formDataToSend.get("policyid"));

    formDataToSend.append("details", formData.value.details ?? "");
    formDataToSend.append("requestorid", userId.emplId);
    formDataToSend.append("createdby", userId.userId.toString());

    const workstation = parseWorkstation();
    formDataToSend.append("computerhost", workstation);

    console.log("Workstation to send:", formDataToSend.get("computerhost"));

    // ✅ FIXED ATTACHMENTS
    newAttachments.value.forEach((attachment) => {
      // formDataToSend.append(`Attachments[${index}].File`, attachment.file);
      formDataToSend.append("files", attachment.file);
    });

    // console.log("FormData entries:");
    // for (const pair of formDataToSend.entries()) {
    //   console.log(`${pair[0]}:`, pair[1]);
    // }

    const response: any = await apiFetch("/api/exemption/create", {
      method: "POST",
      body: formDataToSend,
      parseJson: true,
    });

    console.log("Create response:", response);

    const attachmentResponse: any = await apiFetch(
      "/api/exemption/add-attachment/" + response.item.transactionid,
      {
        method: "POST",
        body: formDataToSend,
        parseJson: true,
      },
    );

    console.log("Attachment response:", attachmentResponse);

    if (response && attachmentResponse) {
      showToast({
        type: "success",
        title: "Success",
        message: "Exemption request created successfully!",
        duration: 5000,
      });

      router.push("/exemptions/all");
    } else {
      throw new Error(response.message || "Failed to create request");
    }
  } catch (error: any) {
    console.error("Submit error:", error);

    showToast({
      type: "error",
      title: "Error",
      message: error.message || "Submission failed",
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

// Multi-file selection with immediate 10MB + 50MB total validation
const handleMultipleFileSelect = (event: Event) => {
  const target = event.target as HTMLInputElement;
  if (!target.files || target.files.length === 0) return;

  const selectedFiles = Array.from(target.files);
  const validFiles: File[] = [];
  const rejectedOversize: string[] = [];
  const rejectedType: string[] = [];

  for (const file of selectedFiles) {
    if (file.size > MAX_FILE_SIZE) {
      rejectedOversize.push(file.name);
      continue;
    }

    if (!isFileTypeAllowed(file)) {
      rejectedType.push(file.name);
      continue;
    }

    validFiles.push(file);
  }

  if (rejectedType.length > 0) {
    errors.value.attachments = `Rejected (invalid type): ${rejectedType.join(", ")}`;
    showToast({
      type: "error",
      title: "Invalid File Type",
      message: `${rejectedType.length} file(s) have invalid type. Allowed: PDF, DOC, DOCX, XLS, XLSX, TXT, JPG, JPEG, PNG.`,
    });
  }

  if (rejectedOversize.length > 0) {
    errors.value.attachments = `Rejected (exceeds 10MB): ${rejectedOversize.join(", ")}`;
    showToast({
      type: "error",
      title: "File Too Large",
      message: `${rejectedOversize.length} file(s) exceed the 10MB limit and were not added.`,
    });
  }

  const remaining = remainingAttachmentSlots.value;
  const canAddBySize = MAX_TOTAL_SIZE - totalNewAttachmentsSize.value;

  // STRICT REJECTION — no truncation, no partial attach
  if (validFiles.length > remaining) {
    errors.value.attachments = `Maximum ${MAX_FILES} files allowed. You selected ${validFiles.length} but only ${remaining} more can be added.`;
    showToast({
      type: "error",
      title: "Limit Reached",
      message: `You can only add ${remaining} more file(s). You selected ${validFiles.length}. Please select ${remaining} or fewer.`,
    });
    target.value = "";
    return;
  }

  // Size check (only reached if count is valid)
  let addedSize = 0;
  const filesToAdd = validFiles.filter((file) => {
    if (addedSize + file.size <= canAddBySize) {
      addedSize += file.size;
      return true;
    }
    return false;
  });

  if (
    addedSize <
    validFiles.reduce((s, f) => s + f.size, 0) -
      (filesToAdd.reduce((s, f) => s + f.size, 0) || 0)
  ) {
    errors.value.attachments = "Total size would exceed 50MB limit.";
    showToast({
      type: "error",
      title: "Size Limit",
      message: "Adding these files would exceed the 50MB total limit.",
    });
  }

  filesToAdd.forEach((file) => {
    newAttachments.value.push({ file });
  });

  target.value = "";
};

// Remove attachment
const removeAttachment = (index: number) => {
  newAttachments.value.splice(index, 1);
  errors.value.attachments = "";
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
              Attachments <span class="text-red-500">*</span> (Max 5 files, 10MB
              each)
            </label>

            <p
              v-if="errors.attachments"
              class="mt-1 text-xs text-red-500 border border-red-500 rounded px-2 py-3 mb-3"
            >
              {{ errors.attachments }}
            </p>

            <!-- Multi-select input
             <input
               type="file"
               multiple
               accept=".pdf,.doc,.docx,.xls,.xlsx,.txt,.jpg,.jpeg,.png"
               @change="handleMultipleFileSelect"
               :disabled="remainingAttachmentSlots <= 0 || totalNewAttachmentsSize >= MAX_TOTAL_SIZE"
               class="block w-full text-sm text-gray-500 file:mr-4 file:py-2 file:px-4 file:rounded-full file:border-0 file:text-sm file:font-semibold file:bg-blue-50 file:text-blue-700 hover:file:bg-blue-100"
             />
             <p class="text-xs text-gray-500 mt-1 ps-2">
               Select the exact number of files you are allowed to add.
               You can currently add up to {{ remainingAttachmentSlots }} more file(s).
               Maximum 5 files total per exemption (10 MB each, 50 MB combined).
             </p> -->

            <!-- Multi-select input -->

            <!-- Selected files list -->
            <div v-if="newAttachments.length > 0" class="mt-4 space-y-2 mb-3">
              <div
                v-for="(attachment, index) in newAttachments"
                :key="index"
                class="flex items-center justify-between p-3 bg-gray-50 border border-gray-200 rounded-lg"
              >
                <div class="flex items-center gap-3 min-w-0">
                  <span class="text-sm font-medium text-gray-900 truncate">{{
                    attachment.file.name
                  }}</span>
                  <span class="text-xs text-gray-500 whitespace-nowrap">
                    ({{ (attachment.file.size / 1024 / 1024).toFixed(2) }} MB)
                  </span>
                </div>
                <Button
                  variant="outline-danger"
                  size="sm"
                  @onclick="() => removeAttachment(index)"
                  icon="trash"
                  icon-position="center"
                />
              </div>
            </div>

            <input
              v-if="newAttachments.length < MAX_FILES"
              type="file"
              multiple
              accept=".pdf,.doc,.docx,.xls,.xlsx,.txt,.jpg,.jpeg,.png"
              @change="handleMultipleFileSelect"
              :disabled="
                remainingAttachmentSlots <= 0 ||
                totalNewAttachmentsSize >= MAX_TOTAL_SIZE
              "
              class="block w-full text-sm text-gray-500 file:mr-4 file:py-2 file:px-4 file:rounded-full file:border-0 file:text-sm file:font-semibold file:bg-blue-50 file:text-blue-700 hover:file:bg-blue-100"
            />
            <p
              v-if="newAttachments.length < MAX_FILES"
              class="text-xs text-gray-500 mt-1 ps-2 py-2"
            >
              Select multiple files at once. Max 5 files / 50MB total.
            </p>

            <!-- Live usage counter -->
            <div
              v-if="newAttachments.length > 0"
              class="mt-2 text-xs text-gray-600 ps-2"
            >
              {{ totalNewAttachmentsSizeMB }} MB of new files
              <span class="text-gray-400">
                ({{ newAttachments.length }} new •
                {{ totalAttachmentsCount }}/{{ MAX_FILES }} total)
              </span>
            </div>
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
