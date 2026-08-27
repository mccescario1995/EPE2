<script setup lang="ts">
import { usePendingTransactionStore } from "~/stores/usePendingTransactionStore";
import { useMenuService } from "~/services/menuService";
const { fetchMenuList, canEdit: checkCanEdit } = useMenuService();
const router = useRouter();
const route = useRoute();
const { showToast } = useToast();
const userStore = useUserStore();

const userId = userStore.profile;

const pendingStore = usePendingTransactionStore();

const canEdit = ref(false);

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
const newAttachments = ref<{ file: File /* referenceType: string */ }[]>([]);

// Auto-save debounce timer
const autoSaveTimer = ref<ReturnType<typeof setTimeout> | null>(null);
const AUTO_SAVE_DEBOUNCE_MS = 1500;

const getPolicyIdForStorage = () => {
  return policyValue.value?.id ?? formData.value.policyId;
};

const autoSavePending = () => {
  if (autoSaveTimer.value) {
    clearTimeout(autoSaveTimer.value);
  }

  autoSaveTimer.value = setTimeout(() => {
    if (isInitialized.value && hasUnsavedChanges.value) {
      pendingStore.saveSnapshot(userId.userId, "/main/pending-for-approval/edit", "autoSave", {
        policyId: getPolicyIdForStorage(),
        details: formData.value.details,
        corporate: formData.value.corporate,
        exemptionId: exemptionId.value,
      }, newAttachments.value.map((a) => ({ name: a.file.name, size: a.file.size, type: a.file.type })));
    }
  }, AUTO_SAVE_DEBOUNCE_MS);
};

// Watch for form changes to auto-save
watch(
  () => formData.value.policyId,
  () => autoSavePending(),
);

watch(
  () => formData.value.details,
  () => autoSavePending(),
);

watch(
  () => formData.value.corporate,
  () => autoSavePending(),
);

watch(
  () => newAttachments.value.map((a) => a.file.name),
  () => autoSavePending(),
  { deep: true },
);


const MAX_FILES = 5;
const MAX_FILE_SIZE = 10 * 1024 * 1024;
const MAX_TOTAL_SIZE = 50 * 1024 * 1024;

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
];

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

const totalAttachmentsCount = computed(
  () => attachments.value.length + newAttachments.value.length,
);

const remainingAttachmentSlots = computed(() =>
  Math.max(0, MAX_FILES - attachments.value.length),
);

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
  attachments: "",
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
        policyName.value =
          policy.shortname || policy.description || `Policy ${policyId}`;
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
    router.push("/main/pending-for-approval");
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
      router.push("/main/pending-for-approval");
    }
  } catch (error) {
    console.error("Error fetching exemption details:", error);
    showToast({
      type: "error",
      title: "Error",
      message: "Failed to load exemption details",
      duration: 5000,
    });
    router.push("/main/pending-for-approval");
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

    if (response) {
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
    attachments: "",
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
      router.push("/main/pending-for-approval");
    }
  } else {
    router.push("/main/pending-for-approval");
  }
};

// Handle back (go to view page)
const handleBack = () => {
  flushAutoSave();
  router.back();
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

    pendingStore.saveSnapshot(userId.userId, "/main/pending-for-approval/edit", "update", {
      policyId: formData.value.policyId,
      details: formData.value.details,
      corporate: formData.value.corporate,
      exemptionId: exemptionId.value,
    }, newAttachments.value.map(a => ({
      name: a.file.name,
      size: a.file.size,
      type: a.file.type,
    })));

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
      // router.push(`/main/exemptions/view?id=${exemptionId.value}`);
      // router.back();
      router.push("/main/pending-for-approval");
      pendingStore.discardPending(userId.userId, "/main/pending-for-approval/edit");
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
    pendingStore.saveSnapshot(userId.userId, "/main/pending-for-approval/edit", "submitDraft", {
      policyId: formData.value.policyId,
      details: formData.value.details,
      corporate: formData.value.corporate,
      exemptionId: exemptionId.value,
    }, newAttachments.value.map(a => ({
      name: a.file.name,
      size: a.file.size,
      type: a.file.type,
    })));

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
      router.push("/main/pending-for-approval");
      pendingStore.discardPending(userId.userId, "/main/pending-for-approval/edit");
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

    pendingStore.saveSnapshot(userId.userId, "/main/pending-for-approval/edit", "updateDraft", {
      policyId: formData.value.policyId,
      details: formData.value.details,
      corporate: formData.value.corporate,
      exemptionId: exemptionId.value,
    }, newAttachments.value.map(a => ({
      name: a.file.name,
      size: a.file.size,
      type: a.file.type,
    })));

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
      router.push(`/main/pending-for-approval/view?id=${exemptionId.value}`);
      pendingStore.discardPending(userId.userId, "/main/pending-for-approval/edit");
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

watch(
  () => policyValue.value,
  (newVal) => {
    if (newVal && exemptionDetails.value?.status === "D") {
      fetchApprovers(newVal.id);
    }
  },
);

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
const cooldowns = ref<Record<number, number>>({});
const resendIntervals = new Map<number, number>();

function startResendCooldown(transApproverId: number, seconds = 10) {
  const existing = resendIntervals.get(transApproverId);
  if (existing) {
    clearInterval(existing);
    resendIntervals.delete(transApproverId);
  }
  cooldowns.value[transApproverId] = seconds;
  const id = window.setInterval(() => {
    cooldowns.value[transApproverId] -= 1;
    if (cooldowns.value[transApproverId] <= 0) {
      clearInterval(id);
      resendIntervals.delete(transApproverId);
      delete cooldowns.value[transApproverId];
    }
  }, 1000);
  resendIntervals.set(transApproverId, id);
}

onUnmounted(() => {
  resendIntervals.forEach((iv) => clearInterval(iv));
  resendIntervals.clear();
});

const isViewableInBrowser = (fileType: string): boolean => {
  const extension = "." + fileType.toLowerCase();
  return [".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".pdf"].includes(
    extension,
  );
};

const getAttachmentButtonLabel = (filetype: string): string => {
  return isViewableInBrowser(filetype) ? "View" : "Download";
};

const resendToMainApprover = async (transApproverId: number) => {
  if (cooldowns.value[transApproverId] > 0) return;
  isResending.value = transApproverId;
  try {
    await apiFetch(`/api/exemption/resend-main/${exemptionId.value}`, {
      method: "POST",
      body: JSON.stringify({ transApproverId }),
      headers: { "Content-Type": "application/json" },
    });
    showToast({
      type: "success",
      title: "Reminder Sent",
      message: "Reminder email sent to main approver.",
      duration: 5000,
    });
  } catch (error: any) {
    showToast({
      type: "error",
      title: "Error",
      message: error.message || "Failed to send reminder",
      duration: 5000,
    });
  } finally {
    isResending.value = null;
    startResendCooldown(transApproverId, 10);
  }
};

const resendToProxyApprover = async (transApproverId: number) => {
  if (cooldowns.value[transApproverId] > 0) return;
  isResending.value = transApproverId;
  try {
    await apiFetch(`/api/exemption/resend-proxy/${exemptionId.value}`, {
      method: "POST",
      body: JSON.stringify({ transApproverId }),
      headers: { "Content-Type": "application/json" },
    });
    showToast({
      type: "success",
      title: "Reminder Sent",
      message: "Reminder email sent to proxy approver.",
      duration: 5000,
    });
  } catch (error: any) {
    showToast({
      type: "error",
      title: "Error",
      message: error.message || "Failed to send reminder",
      duration: 5000,
    });
  } finally {
    isResending.value = null;
    startResendCooldown(transApproverId, 10);
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
  console.log(" Group approvers for transApproverId", groupedApprovers.value);
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
// const currentApproverId = computed(() => {
//   const { mainApprovers, proxiesMap } = groupedApprovers.value;

//   for (const approver of mainApprovers) {
//     // Get all approvers in this group (main + proxies)
//     const groupApprovers = [
//       approver,
//       ...(proxiesMap.get(approver.transApproverId) || []),
//     ];

//     // Check if ANY in the group has approved
//     const hasApproved = groupApprovers.some((a) => a.respond === 1);

//     // Check if ANY in the group is pending
//     const hasPending = groupApprovers.some((a) => a.respond === 0);

//     // If no one has approved yet and someone is pending, this group is active
//     if (!hasApproved && hasPending) {
//       // Find the first pending one in this group
//       const pending = groupApprovers.find((a) => a.respond === 0);
//       return pending?.transApproverId;
//     }

//     // If someone has approved in this group, skip to next group
//     // Continue to next iteration
//   }

//   return null;
// });

// Strict multi-file selection — rejects the whole batch if it exceeds the global limit.
// User must select the exact (or lower) number of allowed files.
const handleMultipleFileSelect = (event: Event) => {
  const target = event.target as HTMLInputElement;
  if (!target.files || target.files.length === 0) return;

  const selectedFiles = Array.from(target.files);
  const validFiles: File[] = [];
  const rejectedOversize: string[] = [];
  const rejectedType: string[] = [];

  errors.value.attachments = "";

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
    errors.value.attachments = `${rejectedType.length} file(s) have invalid type. Allowed: PDF, DOC, DOCX, XLS, XLSX, TXT, JPG, JPEG, PNG.`;
    showToast({
      type: "error",
      title: "Invalid File Type",
      message: `${rejectedType.length} file(s) have invalid type. Allowed: PDF, DOC, DOCX, XLS, XLSX, TXT, JPG, JPEG, PNG.`,
    });
  }

  if (rejectedOversize.length > 0) {
    errors.value.attachments = `${rejectedOversize.length} file(s) exceed the 10MB limit and were not added.`;
    showToast({
      type: "error",
      title: "File Too Large",
      message: `${rejectedOversize.length} file(s) exceed the 10MB limit and were not added.`,
    });
  }

  const remaining = remainingAttachmentSlots.value;
  const canAddBySize = MAX_TOTAL_SIZE - totalNewAttachmentsSize.value;

  if (validFiles.length > remaining) {
    errors.value.attachments = `You can only add ${remaining} more file(s). You selected ${validFiles.length}. Please select ${remaining} or fewer.`;
    showToast({
      type: "error",
      title: "Attachment Limit Reached",
      message: `You can only add ${remaining} more file(s). You selected ${validFiles.length}. Please select ${remaining} or fewer.`,
    });
    target.value = "";
    return;
  }

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
    errors.value.attachments =
      "Adding these files would exceed the 50MB total limit.";
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

// Remove new attachment
const removeNewAttachment = (index: number) => {
  newAttachments.value.splice(index, 1);
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
      await fetchAttachments();
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

  if (attachments.value.length + newAttachments.value.length > MAX_FILES) {
    showToast({
      type: "error",
      title: "Limit Exceeded",
      message: "Total attachments would exceed the maximum of 5.",
    });
    return;
  }

  const validFiles = newAttachments.value.filter(
    (x) => x.file && isFileTypeAllowed(x.file),
  );

  if (validFiles.length === 0) {
    showToast({
      type: "error",
      title: "Error",
      message: "No valid files selected",
    });
    return;
  }

  const uploadForm = new FormData();

  validFiles.forEach((item) => {
    uploadForm.append("files", item.file);
  });

  try {
    const response = await apiFetch(
      `/api/exemption/add-attachment/${exemptionId.value}`,
      {
        method: "POST",
        body: uploadForm,
      },
    );

    if (response) {
      newAttachments.value = [];
      await fetchAttachments();

      showToast({
        type: "success",
        title: "Success",
        message: "Attachments added successfully!",
        duration: 3000,
      });
    } else {
      showToast({
        type: "error",
        title: "Error",
        message: response?.message || "Failed to add attachments",
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

    // console.log("Fetched approvers in Edit page", items);

    if (!items || !Array.isArray(items)) {
      console.error("Invalid approvers response:", items);
      approversData.value = [];
      return;
    }

    approversData.value = items
      .filter((a: any) => a.approvername && a.approvertype) // Remove approveremail from filter
      .map((a: any) => ({
        transApproverId: a.id,
        approverName: a.approvername,
        approverEmail: a.approveremail,
        approverRole:
          a.approvertype === "IM" ? "Immediate Head" : a.approverRole, // Override for IM
        respond: 3,
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

const activeGroupSort = computed(() => {
  const { mainApprovers, proxiesMap } = groupedApprovers.value;

  for (const approver of mainApprovers) {
    const groupApprovers = [
      approver,
      ...(proxiesMap.get(approver.transApproverId) || []),
    ];

    const hasApproved = groupApprovers.some((a) => a.respond === 1);
    const hasPending = groupApprovers.some((a) => a.respond === 0);

    // This group is active if no one has approved yet but someone is still pending
    if (!hasApproved && hasPending) {
      return approver.sort; // return the sort key of the active group
    }
  }
  return null;
});

// Fetch policies on mount
onMounted(async () => {
  await fetchMenuList();
  canEdit.value = checkCanEdit("/main/pending-for-approval/");

  if (!canEdit.value) {
    showToast({
      type: "error",
      title: "Access Denied",
      message: "You do not have permission to edit exemption requests.",
      duration: 5000,
    });
    router.push("/main/pending-for-approval");
    return;
  }

  await fetchPolicies();
  await fetchExemptionDetails();

  const pendingData = pendingStore.checkPending(userId.userId, "/main/pending-for-approval/edit");
  if (pendingData) {
    const actionLabel = pendingData.action === "saveDraft" ? "draft" : pendingData.action === "create" ? "submission" : pendingData.action === "autoSave" ? "changes" : pendingData.action;
    const restored = confirm(
      `We detected an unsaved ${actionLabel} from a previous session. ` +
      `Do you want to restore it? (Attachments must be re-attached.)`
    );
    if (restored && pendingData.data) {
      formData.value.policyId = pendingData.data.policyId ?? null;
      formData.value.details = pendingData.data.details ?? "";
      formData.value.corporate = pendingData.data.corporate ?? null;
      if (pendingData.attachmentMeta?.length) {
        const names = pendingData.attachmentMeta.map(a => `• ${a.name} (${(a.size / 1024 / 1024).toFixed(2)} MB)`).join("\n");
        showToast({
          type: "warning",
          title: "Re-select attachments",
          message: `Please re-attach these files:\n${names}`,
          duration: 8000,
        });
      }
    } else {
      pendingStore.discardPending(userId.userId, "/main/pending-for-approval/edit");
    }
  }
  isInitialized.value = true;
  window.addEventListener("beforeunload", flushAutoSave);
});

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

const isCurrentUser = (requesterId: number): boolean => {
  return userId?.emplId === requesterId;
};

const isInitialized = ref(false);

const flushAutoSave = () => {
  if (!isInitialized.value || !hasUnsavedChanges.value) return;
  pendingStore.saveSnapshot(userId.userId, "/main/pending-for-approval/edit", "autoSave", {
    policyId: getPolicyIdForStorage(),
    details: formData.value.details,
    corporate: formData.value.corporate,
    exemptionId: exemptionId.value,
  }, newAttachments.value.map((a) => ({ name: a.file.name, size: a.file.size, type: a.file.type })));
};

onBeforeUnmount(() => {
  window.removeEventListener("beforeunload", flushAutoSave);
  if (autoSaveTimer.value) {
    clearTimeout(autoSaveTimer.value);
    autoSaveTimer.value = null;
  }
  flushAutoSave();
});
</script>

<template>
  <div class="min-h-[50vh] bg-gray-50 overflow-auto h-100vh dark:bg-gray-900">
    <header class="bg-white dark:bg-gray-800 shadow">
      <div
        class="max-w-full py-4 px-4 sm:px-6 lg:px-8 flex justify-between items-center"
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
          <h1 class="text-2xl ps-5 font-bold text-gray-900 dark:text-gray-100">
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
              v-if="canEdit"
              variant="outline-info"
              size="md"
              @onclick="handleUpdateDraft"
              :loading="isSubmitting"
            >
              Update Draft
            </Button>
            <Button
              v-if="canEdit"
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
              v-if="canEdit"
              variant="primary"
              size="md"
              icon="save"
              iconPosition="left"
              @onclick="handleSubmit"
              :loading="isSubmitting"
              :disabled="newAttachments.length > 0"
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
          ></path>
        </svg>
        <span class="ml-3 text-gray-600 dark:text-gray-400"
          >Loading exemption details...</span
        >
      </div>

      <!-- Content -->
      <div v-else>
        <!-- Info Notice -->
        <div
          class="bg-blue-50 dark:bg-blue-800/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4 mt-4 mb-3"
        >
          <div class="flex">
            <div class="shrink-0">
              <svg
                class="h-5 w-5 text-blue-400 dark:text-blue-300"
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
              <p class="text-sm text-blue-700 dark:text-blue-300">
                Note: Request Status and Approver Status cannot be edited. These
                are managed automatically.
              </p>
            </div>
          </div>
        </div>
        <div class="grid grid-cols-4 gap-6">
          <!-- Left Column -->
          <div
            class="sm:col-span-full md:col-span-2 bg-white rounded-lg shadow p-6 dark:bg-gray-800"
          >
            <!-- Transaction Info -->
            <div class="mb-8">
              <div
                class="text-xl font-semibold text-gray-800 dark:text-gray-100 mb-4 pb-2 border-b"
              >
                <div class="flex justify-between items-center">
                  <h2 class="dark:text-gray-100">Request Information</h2>
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
                  <label
                    class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                  >
                    Transaction Number
                  </label>
                  <p class="text-gray-900 dark:text-gray-100">
                    {{ formData.transactionNumber || "N/A" }}
                  </p>
                </div>
                <div>
                  <label
                    class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                  >
                    Requestor Name
                  </label>
                  <p class="text-gray-900 dark:text-gray-100">
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
                    <label
                      class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                    >
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
                      class="dark:text-gray-200 dark:bg-gray-700"
                    />
                  </div>

                  <div>
                    <label
                      class="block text-sm font-medium text-gray-500 dark:text-gray-400 mb-1"
                    >
                      Details <span class="text-red-500">*</span>
                    </label>
                    <BaseInput
                      v-model="formData.details"
                      type="textarea"
                      isTextarea
                      :rowCount="5"
                      placeholder="Explain the reason for your exemption request..."
                      :errorMessage="errors.details"
                      :maxCharCount="5000"
                      :minCharCount="10"
                      class="dark:bg-gray-700 dark:border-gray-600 dark:text-gray-200"
                    />
                  </div>
                </div>
              </div>
            </div>

            <!-- Attachments -->
            <div class="mb-8">
              <h2
                class="text-xl font-semibold text-gray-800 mb-4 pb-2 border-b dark:text-gray-100"
              >
                Attachments
              </h2>

              <!-- Existing Attachments -->
              <div v-if="attachments.length > 0" class="mb-6">
                <h3
                  class="text-lg font-medium text-gray-700 dark:text-gray-300 mb-3"
                >
                  Existing Attachments
                </h3>
                <div class="space-y-2">
                  <div
                    v-for="attachment in attachments"
                    :key="attachment.attachId"
                    class="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-700 rounded-lg border border-gray-200 dark:border-gray-600"
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
                          {{ attachment.filetype }} •
                          {{
                            new Date(attachment.createdate).toLocaleDateString()
                          }}
                        </p>
                      </div>
                    </div>
                    <div class="flex items-center gap-2">
                      <a v-if="isViewableInBrowser(attachment.filetype)"
                        :href="`${$config.public.apiBase}/uploads/attachments/${attachment.newfilename}`"
                        target="_blank"
                        class="inline-flex items-center px-3 py-1 rounded-md text-sm font-medium bg-blue-100 text-blue-700 hover:bg-blue-200 dark:bg-blue-800 dark:text-blue-300 dark:border-blue-800 dark:hover:bg-blue-700"
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
                      <button
                        @click="deleteAttachment(attachment.attachId)"
                        class="inline-flex items-center px-3 py-1 rounded-md text-sm font-medium bg-red-100 text-red-700 hover:bg-red-200 dark:bg-red-800/20 dark:text-red-300 dark:hover:bg-red-700"
                      >
                        Delete
                      </button>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Add New Attachments -->
              <div v-if="attachments.length < MAX_FILES">
                <h3
                  class="text-lg font-medium text-gray-700 dark:text-gray-300 mb-3"
                >
                  Add New Attachments
                  <span
                    class="text-sm text-gray-500 dark:text-gray-400 font-normal"
                  >
                    ({{ attachments.length }}/{{ MAX_FILES }} used —
                    {{ remainingAttachmentSlots }} remaining)
                  </span>
                </h3>

                <p
                  v-if="errors.attachments"
                  class="mt-1 text-xs text-red-500 dark:text-red-400 border border-red-500 dark:border-red-400 rounded px-2 py-3 mb-3"
                >
                  {{ errors.attachments }}
                </p>

                <!-- Multi-select input -->
                <input
                  type="file"
                  multiple
                  accept=".pdf,.doc,.docx,.xls,.xlsx,.txt,.jpg,.jpeg,.png"
                  @change="handleMultipleFileSelect"
                  :disabled="
                    remainingAttachmentSlots <= 0 ||
                    totalNewAttachmentsSize >= MAX_TOTAL_SIZE
                  "
                  class="block w-full text-sm text-gray-500 file:mr-4 file:py-2 file:px-4 file:rounded file:border-0 file:text-sm file:font-semibold file:bg-blue-50 file:text-blue-700 hover:file:bg-blue-100 dark:bg-gray-700 dark:file:bg-gray-600 dark:file:text-gray-200 dark:hover:file:bg-gray-500 mb-3"
                />
                <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">
                  Select the exact number of files you are allowed to add. You
                  can currently add up to {{ remainingAttachmentSlots }} more
                  file(s). Maximum 5 files total per exemption (10 MB each, 50
                  MB combined).
                </p>

                <!-- Selected files list -->
                <div v-if="newAttachments.length > 0" class="mt-4 space-y-2">
                  <div
                    v-for="(attachment, index) in newAttachments"
                    :key="index"
                    class="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg"
                  >
                    <div class="flex items-center gap-3 min-w-0 me-2">
                      <span
                        class="text-sm font-medium text-gray-900 dark:text-gray-100 truncate"
                        >{{ attachment.file.name }}</span
                      >
                      <span
                        class="text-xs text-gray-500 dark:text-gray-400 whitespace-nowrap"
                      >
                        ({{ (attachment.file.size / 1024 / 1024).toFixed(2) }}
                        MB)
                      </span>
                    </div>
                    <Button
                      variant="dark:outline-danger"
                      size="sm"
                      @onclick="() => removeNewAttachment(index)"
                      icon="trash"
                      icon-position="center"
                    />
                  </div>
                </div>

                <!-- Live usage counter -->
                <div
                  v-if="newAttachments.length > 0"
                  class="mt-2 text-xs text-gray-600 dark:text-gray-400"
                >
                  {{ totalNewAttachmentsSizeMB }} MB of new files
                  <span class="text-gray-400 dark:text-gray-500">
                    ({{ newAttachments.length }} new •
                    {{ totalAttachmentsCount }}/{{ MAX_FILES }} total)
                  </span>
                </div>

                <div class="flex gap-3 mt-3">
                  <Button
                    v-if="newAttachments.length > 0"
                    variant="primary"
                    size="sm"
                    @click="addNewAttachments"
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
            class="sm:col-span-full md:col-span-2 bg-white rounded-lg shadow px-6 py-6 dark:bg-gray-800"
          >
            <!-- Approvers Status -->
            <div class="mb-8">
              <div class="border-b flex justify-between mb-3">
                <h2
                  class="text-xl font-semibold text-gray-800 mb-2 dark:text-gray-100"
                >
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
                  <div
                    class="bg-gray-50 rounded-lg p-3 border border-gray-200 dark:bg-gray-700 dark:border-gray-600"
                  >
                    <div class="flex justify-between items-start mb-2">
                      <div class="flex items-start">
                        <span
                          class="flex items-center justify-center w-6 h-6 rounded-full bg-blue-100 text-blue-600 text-sm font-medium mr-3 mt-1"
                        >
                          {{ index + 1 }}
                        </span>

                        <div>
                          <p
                            class="text-sm font-medium text-gray-900 dark:text-gray-100"
                          >
                            {{ approver.approverName }}
                            <span
                              v-if="isCurrentUser(approver.employeeId)"
                              class="text-xs text-green-500 bg-green-100 px-2 py-1 rounded border border-green-800"
                              >You</span
                            >
                          </p>
                          <p
                            class="text-xs text-gray-500 dark:text-gray-400 mb-1"
                          >
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
                          <div
                            v-if="approver.remarks"
                            class="text-sm bg-gray-50 dark:bg-gray-700 rounded-lg px-3"
                          >
                            Remark:
                          </div>
                          <div v-if="approver.remarks" class="px-6 pb-2">
                            <span
                              class="text-sm text-gray-900 dark:text-gray-100 whitespace-pre-wrap"
                            >
                              {{ approver.remarks }}
                            </span>
                          </div>
                          <div
                            class="text-xs"
                            :class="
                              getApproverStatusTextColor(approver.respond)
                            "
                          >
                            <span v-if="approver.respondDate">
                              Responded on
                              {{ formatDateTime(approver.respondDate).date }} •
                              {{ formatDateTime(approver.respondDate).time }}
                            </span>
                          </div>
                        </div>
                      </div>
                      <div class="flex flex-col items-end gap-1">
                        <span
                          v-if="approver.respond != 3 && (approver.respond == 1 || approver.respond == 2)"
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
                            approver.sort === activeGroupSort &&
                            !isGroupSatisfied(approver.transApproverId)
                          "
                          @click="
                            resendToMainApprover(approver.transApproverId)
                          "
                          :disabled="
                            isResending === approver.transApproverId ||
                            cooldowns[approver.transApproverId] > 0
                          "
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
                          <span v-if="cooldowns[approver.transApproverId] > 0"
                            >Resend ({{
                              cooldowns[approver.transApproverId]
                            }}s)</span
                          >
                          <span v-else>Resend</span>
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
                            class="w-2 h-2 rounded-full bg-orange-400 mt-1.5"
                          ></span>
                          <div class="ps-3">
                            <p
                              class="text-sm text-gray-700 dark:text-gray-300 mb-1"
                            >
                              {{ proxy.approverName }}
                              <span v-if="isCurrentUser(approver.employeeId)" class="text-xs text-green-500 bg-green-100 px-2 py-1 rounded border-2 border-green-11121w11                                                                                                                                               00">You</span>
                            </p>
                            <p class="text-xs text-gray-500 dark:text-gray-400">
                              {{ proxy.approverEmail || "" }}
                            </p>
                            <span
                              class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-orange-100 text-orange-800 mb-1"
                            >
                              {{ proxy.approverRole }}
                            </span>
                            <div
                              v-if="approver.remarks"
                              class="text-sm bg-gray-50 dark:bg-gray-700 rounded-lg px-3"
                            >
                              Remark:
                            </div>
                            <div v-if="approver.remarks" class="px-6 pb-2">
                              <span
                                class="text-sm text-gray-900 dark:text-gray-100 whitespace-pre-wrap"
                              >
                                {{ approver.remarks }}
                              </span>
                            </div>
                            <div
                              class="text-xs"
                              :class="
                                getApproverStatusTextColor(approver.respond)
                              "
                            >
                              <span v-if="approver.respondDate">
                                Responded on
                                {{
                                  formatDateTime(approver.respondDate).date
                                }}
                                •
                                {{ formatDateTime(approver.respondDate).time }}
                              </span>
                            </div>
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
                              proxy.sort === activeGroupSort &&
                              !isGroupSatisfied(approver.transApproverId)
                            "
                            @click="
                              resendToProxyApprover(proxy.transApproverId)
                            "
                            :disabled="
                              isResending === proxy.transApproverId ||
                              (cooldowns[proxy.transApproverId] ?? 0) > 0
                            "
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
                            <span v-if="cooldowns[proxy.transApproverId] > 0"
                              >Resend ({{
                                cooldowns[proxy.transApproverId]
                              }}s)</span
                            >
                            <span v-else>Resend</span>
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>
                </template>
              </div>
              <div
                v-else
                class="text-sm flex items-center justify-center h-24 text-gray-400 dark:text-gray-500"
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
                class="text-xl font-semibold text-gray-800 dark:text-gray-100 mb-4 pb-2 border-b"
              >
                Remarks
              </h2>
              <div
                class="bg-gray-50 dark:bg-gray-700 rounded-lg p-4 border border-gray-200 dark:border-gray-600"
              >
                <p class="text-gray-900 dark:text-gray-100 whitespace-pre-wrap">
                  {{ exemptionDetails.remarks }}
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </main>
  </div>
</template>


