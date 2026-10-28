<script setup lang="ts">
import { useMenuService } from "~/services/menuService";
const { fetchMenuList, canAdd: checkCanAdd } = useMenuService();
const router = useRouter();
const { showToast } = useToast();
const userStore = useUserStore();

const userId = userStore.profile;

const pendingStore = usePendingTransactionStore();

// Form data
// Form data (remove attachments from here)
const formData = ref({
  policyid: null as number | { id: number; label: string } | null,
  corporate: null as { id: string; label: string } | null,
  details: "",
  // referenceType: "",
});

const detailsText = computed(() => stripHtml(formData.value.details));

// Add newAttachments (from edit.vue)

const reSelectAttachmentWarning = ref<{ fileNames: string[]; fileCount: number } | null>(null);

// Preserved attachment metadata from pending restore (used until files are re-attached)
const restoredAttachmentMeta = ref<{ name: string; size: number; type: string }[] | null>(null);

// Pending data restore modal state
const showPendingRestoreModal = ref(false);
const pendingRestoreData = ref<{
  action: string;
  data: Record<string, any>;
  attachmentMeta: { name: string; size: number; type: string }[];
  timestamp: string;
} | null>(null);
const pendingRestoreServerData = ref<Record<string, any> | null>(null);

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
const isInitialized = ref(false);

// Transaction ID for pending transaction storage
const transactionIdRef = computed(() => {
  return draftId.value?.toString() ?? userId.userId.toString();
});

// Auto-save debounce timer
const autoSaveTimer = ref<ReturnType<typeof setTimeout> | null>(null);
const AUTO_SAVE_DEBOUNCE_MS = 1500;
const isDiscarded = ref(false);

const getPolicyIdForStorage = () => {
  const policyId = formData.value.policyid;
  if (policyId && typeof policyId === "object") {
    return policyId.id;
  }
  return policyId;
};

const getCorporateIdForStorage = () => {
  const corporate = formData.value.corporate;
  if (corporate && typeof corporate === "object") {
    return corporate.id;
  }
  return corporate;
};

// Check if form has unsaved changes (for create page, any non-empty field counts)
const hasUnsavedChanges = computed(() => {
  const policyId = getPolicyIdForStorage();
  const corporateId = getCorporateIdForStorage();
  return !!(
    policyId ||
    detailsText.value ||
    corporateId ||
    newAttachments.value.length ||
    draftId.value
  );
});

const autoSavePending = () => {
  if (autoSaveTimer.value) {
    clearTimeout(autoSaveTimer.value);
  }

  autoSaveTimer.value = setTimeout(() => {
    if (isDiscarded.value) return;
    if (!isInitialized.value) return;
    const policyId = getPolicyIdForStorage();
    const corporateId = getCorporateIdForStorage();
    const hasChanges = !!(
      policyId ||
      detailsText.value ||
      corporateId ||
      newAttachments.value.length ||
      draftId.value
    );
    if (!hasChanges) return;
    const attachmentMeta = newAttachments.value.length
      ? newAttachments.value.map((a) => ({ name: a.file.name, size: a.file.size, type: a.file.type }))
      : restoredAttachmentMeta.value;
    pendingStore.saveSnapshot(transactionIdRef.value, "/main/create-exemption/create", "autoSave", {
      policyid: policyId,
      details: formData.value.details,
      corporate: corporateId,
      draftId: draftId.value,
    }, attachmentMeta);
  }, AUTO_SAVE_DEBOUNCE_MS);
};

const flushAutoSave = () => {
  if (isDiscarded.value) return;
  if (!isInitialized.value) return;
  const policyId = getPolicyIdForStorage();
  const corporateId = getCorporateIdForStorage();
  const hasChanges = !!(
    policyId ||
    detailsText.value ||
    corporateId ||
    newAttachments.value.length ||
    draftId.value
  );
  if (!hasChanges) return;
  const attachmentMeta = newAttachments.value.length
    ? newAttachments.value.map((a) => ({ name: a.file.name, size: a.file.size, type: a.file.type }))
    : restoredAttachmentMeta.value;
  pendingStore.saveSnapshot(transactionIdRef.value, "/main/create-exemption/create", "autoSave", {
    policyid: policyId,
    details: formData.value.details,
    corporate: corporateId,
    draftId: draftId.value,
  }, attachmentMeta);
};

// Watch for form changes to auto-save
watch(
  () => formData.value.policyid,
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

watch(
  () => draftId.value,
  () => autoSavePending(),
);

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


// Auto-clear re-select attachment warning when enough files are attached
watch(() => newAttachments.value.length, (newCount) => {
  if (reSelectAttachmentWarning.value && newCount >= reSelectAttachmentWarning.value.fileCount) {
    reSelectAttachmentWarning.value = null;
  }
});
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

  if (detailsText.value.length < 10) {
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
    detailsText.value.length >= 10 &&
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
    detailsText.value.length >= 10 &&
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

    pendingStore.saveSnapshot(transactionIdRef.value, "/main/create-exemption/create", "saveDraft", {
      policyid: getPolicyId(),
      details: formData.value.details,
      corporate: formData.value.corporate,
      draftId: draftId.value,
    }, newAttachments.value.map(a => ({ name: a.file.name, size: a.file.size, type: a.file.type })));

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

    const transactionId =
      response?.item?.transactionId || response?.item?.transactionid;

    const attachmentResponse: any = await apiFetch(
      "/api/exemption/add-attachment/" + transactionId,
      {
        method: "POST",
        body: formDataToSend,
        parseJson: true,
      },
    );

    if (attachmentResponse && response) {
      const previousKey = transactionIdRef.value;
      draftId.value = transactionId.toString();
      isDraft.value = true;

      isDiscarded.value = true;
      if (autoSaveTimer.value) {
        clearTimeout(autoSaveTimer.value);
        autoSaveTimer.value = null;
      }
      pendingStore.discardPending(previousKey, "/main/create-exemption/create");

      showToast({
        type: "success",
        title: "Success",
        message: draftId.value
          ? "Draft updated successfully!"
          : "Draft saved successfully!",
        duration: 5000,
      });


      router.push("/main/create-exemption");

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
    const formDataToSend = new FormData();

    const policyId = getPolicyId();
    if (policyId != null) {
      formDataToSend.append("policyid", String(policyId));
    }

    formDataToSend.append("details", formData.value.details ?? "");
    formDataToSend.append("requestorid", userId.emplId);
    formDataToSend.append("createdby", userId.userId.toString());

    const workstation = parseWorkstation();
    formDataToSend.append("computerhost", workstation);

    // ✅ FIXED ATTACHMENTS
    newAttachments.value.forEach((attachment) => {
      // formDataToSend.append(`Attachments[${index}].File`, attachment.file);
      formDataToSend.append("files", attachment.file);
    });

    // for (const pair of formDataToSend.entries()) {
    // }

    pendingStore.saveSnapshot(transactionIdRef.value, "/main/create-exemption/create", "create", {
      policyid: getPolicyId(),
      details: formData.value.details,
      corporate: formData.value.corporate,
    }, newAttachments.value.map(a => ({ name: a.file.name, size: a.file.size, type: a.file.type })));

    const response: any = await apiFetch("/api/exemption/create", {
      method: "POST",
      body: formDataToSend,
      parseJson: true,
    });

    const attachmentResponse: any = await apiFetch(
      "/api/exemption/add-attachment/" + response.item.transactionid,
      {
        method: "POST",
        body: formDataToSend,
        parseJson: true,
      },
    );

    if (response && attachmentResponse) {
      showToast({
        type: "success",
        title: "Success",
        message: "Exemption request created successfully!",
        duration: 5000,
      });

      isDiscarded.value = true;
      if (autoSaveTimer.value) {
        clearTimeout(autoSaveTimer.value);
        autoSaveTimer.value = null;
      }
      pendingStore.discardPending(transactionIdRef.value, "/main/create-exemption/create");
      router.push("/main/create-exemption");
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
  isDiscarded.value = true;
  if (autoSaveTimer.value) {
    clearTimeout(autoSaveTimer.value);
    autoSaveTimer.value = null;
  }
  pendingStore.discardPending(transactionIdRef.value, "/main/create-exemption/create");
  router.back();
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
  // User re-attached files — clear preserved attachment meta
  restoredAttachmentMeta.value = null;

  target.value = "";
};

// Remove attachment
const removeAttachment = (index: number) => {
  newAttachments.value.splice(index, 1);
  errors.value.attachments = "";
  if (newAttachments.value.length === 0) {
    restoredAttachmentMeta.value = null;
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

const fileInput = ref<HTMLInputElement | null>(null);

const openFilePicker = () => {
  fileInput.value?.click();
};

// Fetch policies on mount and check for draft
const canAdd = ref(false);

// Pending restore modal handlers
const handleRestorePending = () => {
  if (!pendingRestoreData.value?.data) return;

  const pending = pendingRestoreData.value.data;
  const pendingPolicyId = pending.policyid;
  if (pendingPolicyId != null) {
    const matchingOption = policyOptions.value.find(
      (p) => p.id === (typeof pendingPolicyId === "object" ? pendingPolicyId.id : pendingPolicyId),
    );
    formData.value.policyid = matchingOption
      ? { id: matchingOption.id, label: matchingOption.label }
      : null;
  } else {
    formData.value.policyid = null;
  }
  formData.value.details = pending.details ?? "";
  formData.value.corporate = pending.corporate ?? null;
  draftId.value = pending.draftId ?? null;
  if (draftId.value) { isDraft.value = true; isDraftMode.value = true; }
  if (pendingRestoreData.value.attachmentMeta?.length) {
    const names = pendingRestoreData.value.attachmentMeta.map(
      (a) => `${a.name} (${(a.size / 1024 / 1024).toFixed(2)} MB)`,
    );
    reSelectAttachmentWarning.value = { fileNames: names, fileCount: pendingRestoreData.value.attachmentMeta.length };
    restoredAttachmentMeta.value = pendingRestoreData.value.attachmentMeta;
  }
  pendingStore.discardPending(transactionIdRef.value, "/main/create-exemption/create");
  closePendingRestoreModal();
};

const handleDiscardPending = () => {
  pendingStore.discardPending(transactionIdRef.value, "/main/create-exemption/create");
  closePendingRestoreModal();
};

const closePendingRestoreModal = () => {
  showPendingRestoreModal.value = false;
  pendingRestoreData.value = null;
  pendingRestoreServerData.value = null;
};

const getActionLabel = (action: string): string => {
  return action === "saveDraft" ? "draft" : action === "create" ? "submission" : action === "autoSave" ? "changes" : action;
};

const formatTimestamp = (timestamp: string): string => {
  const date = new Date(timestamp);
  return date.toLocaleDateString("en-PH", {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    hour12: true,
  });
};

const policyDisplay = (policyId: any) => {
  if (!policyId) return "—";
  const id = typeof policyId === "object" ? policyId.id : policyId;
  const option = policyOptions.value.find((p) => p.id === id);
  return option ? option.label : `Policy ${id}`;
};

const comparisonFields = computed(() => {
  if (!pendingRestoreData.value?.data || !pendingRestoreServerData.value) return [];

  const pending = pendingRestoreData.value.data;
  const server = pendingRestoreServerData.value;

  const policyLabel = pending.policyId ? policyOptions.value.find((p) => p.id === pending.policyId)?.label || `Policy ${pending.policyId}` : "—";

  const fields = [
    { key: "policyid", label: "Policy To Override", serverValue: policyDisplay(server.policyid), pendingValue: policyDisplay(pending.policyid) },
    { key: "details", label: "Details", serverValue: stripHtml(server.details), pendingValue: stripHtml(pending.details) },
  ];

  return fields.map(field => ({
    ...field,
    changed: field.serverValue !== field.pendingValue,
    serverValue: field.serverValue ?? "—",
    pendingValue: field.pendingValue ?? "—",
  }));
});

onMounted(async () => {
  await fetchMenuList();
  canAdd.value = checkCanAdd("/main/create-exemption");

  if (!canAdd.value) {
    showToast({
      type: "error",
      title: "Access Denied",
      message: "You do not have permission to create exemption requests.",
      duration: 5000,
    });
    router.push("/main/create-exemption");
    return;
  }

  await fetchPolicies();
  const pendingData = pendingStore.checkPending(transactionIdRef.value, "/main/create-exemption/create");


  if (pendingData) {
    pendingRestoreData.value = pendingData;
    showPendingRestoreModal.value = true;
  }


  isInitialized.value = true;
  window.addEventListener("beforeunload", flushAutoSave);
});

onBeforeUnmount(() => {
  window.removeEventListener("beforeunload", flushAutoSave);
  if (autoSaveTimer.value) {
    clearTimeout(autoSaveTimer.value);
  }
  flushAutoSave();
});
</script>

<template>
  <div class="min-h-[50vh] bg-gray-50 overflow-auto h-100vh dark:bg-gray-900">
    <header class="bg-white dark:bg-gray-800 shadow">
      <div class="max-w-7xl me-auto py-6 px-4 sm:px-6 lg:px-8 flex justify-start items-center">
        <Button variant="ghost-primary" size="lg" icon="left_arrow" iconPosition="center" customClass="rounded-full"
          @onclick="handleCancel" class="dark:bg-gray-700 dark:text-gray-200 dark:hover:bg-gray-600" />
        <h1 class="text-3xl ps-5 font-bold text-gray-900 dark:text-gray-100">
          {{ draftId ? "Edit Draft" : "Create Exemption Request" }}
        </h1>
      </div>
    </header>

    <main class="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
      <div class="bg-white dark:bg-gray-800 rounded-lg shadow px-6 py-6">
        <form @submit.prevent="handleSubmit" class="space-y-6">
          <!-- Policy Selection -->
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              Policy To Override
              <span v-if="!isDraftMode" class="text-red-500">*</span>
            </label>
            <Dropdown v-model="formData.policyid" :initialItems="policyOptions" placeholder="Select a policy"
              :disabled="isLoadingPolicies" class="dark:bg-gray-700 dark:text-gray-300" />
            <p v-if="errors.policyid" class="mt-1 text-xs text-red-500 dark:text-red-400">
              {{ errors.policyid }}
            </p>
          </div>

          <!-- Details -->
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              Additional Comment/Detailed Info
              <span class="text-red-500">*</span>
            </label>
            <TipTapEditor v-model="formData.details"
              placeholder="Explain the reason for your exemption request..."
              :errorMessage="errors.details" :rowCount="6"
              :maxCharCount="5000" :minCharCount="10" class="mt-1" />
          </div>

          <!-- Attachments -->
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              Attachments <span class="text-red-500">*</span> (Max 5 files, 10MB
              each)
            </label>

            <!-- Re-select Attachments Banner -->
            <div v-if="reSelectAttachmentWarning"
              class="mt-2 mb-3 p-3 bg-amber-50 border border-amber-200 rounded-lg dark:bg-amber-900/30 dark:border-amber-800">
              <div class="flex items-start gap-3">
                <svg class="h-5 w-5 text-amber-600 dark:text-amber-400 mt-0.5 flex-shrink-0" fill="currentColor"
                  viewBox="0 0 20 20">
                  <path fill-rule="evenodd"
                    d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z"
                    clip-rule="evenodd" />
                </svg>
                <div class="flex-1 min-w-0">
                  <p class="text-sm font-medium text-amber-900 dark:text-amber-100">
                    Attachments need to be re-attached
                  </p>
                  <p class="text-xs text-amber-700 dark:text-amber-300 mt-1">
                    Please re-attach the following {{ reSelectAttachmentWarning.fileCount }} file(s):
                  </p>
                  <ul class="text-xs text-amber-700 dark:text-amber-300 mt-1 ml-4 list-disc space-y-0.5">
                    <li v-for="name in reSelectAttachmentWarning.fileNames" :key="name">{{ name }}</li>
                  </ul>
                  <!-- <p class="text-xs text-amber-600 dark:text-amber-400 mt-2">
                    The banner will disappear once you re-attach all files.
                  </p> -->
                </div>
                <button @click="reSelectAttachmentWarning = null"
                  class="text-amber-500 hover:text-amber-700 dark:text-amber-400 dark:hover:text-amber-300 flex-shrink-0 ml-2"
                  title="Dismiss">
                  <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>
            </div>

            <p v-if="errors.attachments"
              class="mt-1 text-xs text-red-500 dark:text-red-400 border border-red-500 dark:border-red-400 rounded px-2 py-3 mb-3">
              {{ errors.attachments }}
            </p>

            <!-- Multi-select input -->

            <!-- Selected files list -->
            <div v-if="newAttachments.length > 0" class="mt-4 space-y-2 mb-3">
              <div v-for="(attachment, index) in newAttachments" :key="index"
                class="flex items-center justify-between p-3 bg-gray-50 border border-gray-200 rounded-lg dark:border-gray-600 dark:bg-gray-700">
                <div class="flex items-center gap-3 min-w-0">
                  <span class="text-sm font-medium text-gray-900 truncate dark:text-gray-100">{{ attachment.file.name
                  }}</span>
                  <span class="text-xs text-gray-500 whitespace-nowrap dark:text-gray-400">
                    ({{ (attachment.file.size / 1024 / 1024).toFixed(2) }} MB)
                  </span>
                </div>
                <Button variant="outline-danger" size="sm" @onclick="() => removeAttachment(index)" icon="trash"
                  icon-position="center" type="button" />
              </div>
            </div>

            <!-- <input
              v-if="newAttachments.length < MAX_FILES"
              type="file"
              multiple
              accept=".pdf,.doc,.docx,.xls,.xlsx,.txt,.jpg,.jpeg,.png"
              @change="handleMultipleFileSelect"
              :disabled="
                remainingAttachmentSlots <= 0 ||
                totalNewAttachmentsSize >= MAX_TOTAL_SIZE
              "                                                                                                    
            /> -->
            <div>
              <!-- Hidden file input -->
              <input ref="fileInput" v-if="newAttachments.length < MAX_FILES" type="file" multiple
                accept=".pdf,.doc,.docx,.xls,.xlsx,.txt,.jpg,.jpeg,.png" class="hidden"
                @change="handleMultipleFileSelect" :disabled="remainingAttachmentSlots <= 0 ||
                  totalNewAttachmentsSize >= MAX_TOTAL_SIZE
                  " />

              <!-- Custom button -->
              <Button v-if="newAttachments.length < MAX_FILES" variant="outline-primary" type="button"
                @click="openFilePicker()" :disabled="remainingAttachmentSlots <= 0 ||
                  totalNewAttachmentsSize >= MAX_TOTAL_SIZE
                  ">
                Choose Files
              </Button>
            </div>
            <p v-if="newAttachments.length < MAX_FILES" class="text-xs text-gray-500 mt-1 ps-2 py-2">
              Select multiple files at once. Max 5 files / 50MB total.
            </p>

            <!-- Live usage counter -->
            <div v-if="newAttachments.length > 0" class="mt-2 text-xs text-gray-600 ps-2">
              {{ totalNewAttachmentsSizeMB }} MB of new files
              <span class="text-gray-400">
                ({{ newAttachments.length }} new •
                {{ totalAttachmentsCount }}/{{ MAX_FILES }} total)
              </span>
            </div>
          </div>

          <!-- Approvers List -->
          <div v-if="formData.policyid">
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              Approvers
            </label>
            <div v-if="isLoadingApprovers" class="flex items-center justify-center py-4">
              <svg class="animate-spin h-5 w-5 text-blue-600" xmlns="http://www.w3.org/2000/svg" fill="none"
                viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor"
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z">
                </path>
              </svg>
              <span class="ml-2 text-gray-500 dark:text-gray-400">Loading approvers...</span>
            </div>
            <div v-else-if="approvers.length > 0"
              class="bg-gray-50 rounded-lg border border-gray-200 divide-y divide-gray-200 dark:bg-gray-700 pl-8 pr-3 py-2 rounded-b-lg">
              <template v-for="(approver, index) in groupedApprovers.mainApprovers" :key="approver.id">
                <!-- Main Approver (numbered) -->
                <div class="p-3 flex items-center justify-between">
                  <div class="flex items-center">
                    <span
                      class="flex items-center justify-center w-6 h-6 rounded-full bg-blue-100 text-blue-600 text-sm font-medium mr-3">
                      {{ index + 1 }}
                    </span>
                    <div>
                      <p class="text-sm font-medium text-gray-900 dark:text-gray-100">
                        {{ approver.approvername }}
                      </p>
                      <p class="text-sm text-gray-500 dark:text-gray-400">
                        {{ approver.approveremail }}
                      </p>
                    </div>
                  </div>
                  <div class="flex items-center gap-2">
                    <span v-if="approver.approverRole"
                      class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium" :class="{
                        'bg-blue-100 text-blue-800 dark:bg-blue-200 dark:text-blue-700':
                          approver.linesequence === 1,
                        'bg-orange-100 text-orange-800 dark:bg-orange-800/20 dark:text-orange-500':
                          approver.linesequence !== 1,
                      }">
                      {{ approver.approverRole }}
                    </span>
                  </div>
                </div>

                <!-- Proxies (indented bullet points) -->
                <div v-if="getProxies(approver.id).length > 0"
                  class="bg-gray-100 dark:bg-gray-700 pl-8 pr-3 py-2 rounded-b-lg">
                  <div v-for="proxy in getProxies(approver.id)" :key="proxy.id" class="flex items-center py-1">
                    <span class="w-2 h-2 rounded-full bg-orange-500 dark:bg-orange-400 mr-2"></span>
                    <div class="flex-1 flex items-center justify-between">
                      <div>
                        <p class="text-sm text-gray-900 dark:text-gray-100">
                          {{ proxy.approvername }}
                        </p>
                        <p class="text-xs text-gray-500 dark:text-gray-400">
                          {{ proxy.approveremail }}
                        </p>
                      </div>
                      <span v-if="proxy.approverRole"
                        class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium" :class="{
                          'bg-orange-100 text-orange-800 dark:bg-orange-800/20 dark:text-orange-300':
                            proxy.linesequence !== 1,
                        }">
                        {{ proxy.approverRole }}
                      </span>
                    </div>
                  </div>
                </div>

              </template>
            </div>
            <div v-else class="bg-gray-50 rounded-lg border border-gray-200 p-4 text-center text-gray-500 text-sm">
              No approvers found for this policy
            </div>
          </div>

          <!-- Action Buttons -->
          <div class="flex justify-end gap-3 pt-4 border-t border-gray-200">
            <Button variant="outline-info" size="lg" @onclick="handleCancel" :disabled="isSubmitting || isSavingDraft">
              Cancel
            </Button>
            <Button variant="outline-primary" size="lg" @onclick="handleSaveDraft" :loading="isSavingDraft" :disabled="!isDraftValid ||
              isSubmitting ||
              isSavingDraft ||
              approvers.length === 0 ||
              !canAdd
              ">
              {{ draftId ? "Update Draft" : "Save as Draft" }}
            </Button>
            <Button variant="primary" size="lg" type="submit" :loading="isSubmitting" :disabled="!isFormValid ||
              isSavingDraft ||
              isSubmitting ||
              approvers.length === 0 ||
              !canAdd
              ">
              Submit Request
            </Button>
          </div>
        </form>
      </div>
    </main>
  </div>

  <!-- Pending Data Restore Modal -->
  <Modal v-model:isOpen="showPendingRestoreModal" size="lg" :closeOnOverlayClick="false" :closeOnEsc="false"
    :showCloseButton="false">
    <template #header>
      <div class="flex items-center justify-between">
        <h3 class="text-lg font-semibold text-gray-900 dark:text-gray-100">
          Unsaved Changes Detected
        </h3>
      </div>
    </template>

    <div v-if="pendingRestoreData" class="space-y-6">
      <!-- Action label -->
      <div class="p-4 bg-amber-50 dark:bg-amber-900/30 rounded-lg border border-amber-200 dark:border-amber-800">
        <p class="text-sm text-amber-800 dark:text-amber-200">
          We detected an unsaved <strong>{{ getActionLabel(pendingRestoreData.action) }}</strong> from a
          previous session
          ({{ formatTimestamp(pendingRestoreData.timestamp) }}).
          Do you want to restore it?
          <span v-if="pendingRestoreData.attachmentMeta?.length" class="text-amber-600 dark:text-amber-400">
            ({{ pendingRestoreData.attachmentMeta.length }} attachment(s) must be re-attached.)
          </span>
        </p>
      </div>

      <!-- Comparison Table (edit pages) or Pending Data Summary (create page) -->
      <div v-if="pendingRestoreServerData" class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-gray-200 dark:border-gray-700">
              <th class="text-left p-2 font-medium text-gray-500 dark:text-gray-400">Field</th>
              <th class="text-left p-2 font-medium text-gray-500 dark:text-gray-400">Current</th>
              <th class="text-left p-2 font-medium text-gray-500 dark:text-gray-400">Unsaved</th>
              <th class="text-left p-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100 dark:divide-gray-800">
            <tr v-for="field in comparisonFields" :key="field.key" class="hover:bg-gray-50 dark:hover:bg-gray-800/50">
              <td class="p-2 font-medium text-gray-700 dark:text-gray-300">{{ field.label }}</td>
              <td class="p-2 text-gray-900 dark:text-gray-100">{{ field.serverValue || "—" }}</td>
              <td class="p-2 text-gray-900 dark:text-gray-100">{{ field.pendingValue || "—" }}</td>
              <td class="p-2">
                <span
                  :class="field.changed
                    ? 'inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-200'
                    : 'inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200'">
                  {{ field.changed ? "Changed" : "Same" }}
                </span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Pending Data Summary (create page) -->
      <div v-else
        class="space-y-4 p-4 bg-gray-50 dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700">
        <h4 class="text-sm font-medium text-gray-700 dark:text-gray-300">Unsaved Data to Restore</h4>

        <dl class="space-y-3">
          <div>
            <dt class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Policy to Override</dt>
            <dd class="text-sm text-gray-900 dark:text-gray-100">
              {{ policyDisplay(pendingRestoreData.data.policyid)
                ?? '—' }}
            </dd>
          </div>
          <div>
            <dt class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Details</dt>
            <dd class="text-sm text-gray-900 dark:text-gray-100 whitespace-pre-wrap">
              {{ stripHtml(pendingRestoreData.data.details) || '—' }}
            </dd>
          </div>

        </dl>

      </div>

      <!-- Attachment warning -->
      <div v-if="pendingRestoreData.attachmentMeta?.length"
        class="p-4 bg-amber-50 dark:bg-amber-900/30 rounded-lg border border-amber-200 dark:border-amber-800">
        <p class="text-sm text-amber-800 dark:text-amber-200">
          <strong>Attachments ({{ pendingRestoreData.attachmentMeta.length }}):</strong> The following
          files
          were attached in the pending session and must be re-attached:
        </p>
        <ul class="mt-2 list-disc list-inside text-xs text-amber-700 dark:text-amber-300 space-y-1">
          <li v-for="att in pendingRestoreData.attachmentMeta" :key="att.name">
            {{ att.name }} ({{ (att.size / 1024 / 1024).toFixed(2) }} MB)
          </li>
        </ul>
      </div>
    </div>

    <template #footer>
      <div class="flex justify-end gap-3">
        <Button variant="ghost" @click="handleDiscardPending" class="dark:hover:bg-gray-700">
          Discard
        </Button>
        <Button variant="primary" @click="handleRestorePending">
          Restore Changes
        </Button>
      </div>
    </template>
  </Modal>
</template>
