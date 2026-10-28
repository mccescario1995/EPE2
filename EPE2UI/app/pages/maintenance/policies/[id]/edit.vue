<script setup lang="ts">
definePageMeta({
  middleware: "auth",
});

const route = useRoute();
const policyid = route.params.id as string;

const userStore = useUserStore();
const userId = userStore.profile;
const { showToast } = useToast();

import { usePendingTransactionStore } from "~/stores/usePendingTransactionStore";
import ManageApprovers from "~/components/ManageApprovers.vue";
import BaseInput from "~/components/BaseInput.vue";
import Dropdown from "~/components/Dropdown.vue";
import Button from "~/components/Button.vue";
import Modal from "~/components/Modal.vue";

const pendingStore = usePendingTransactionStore();

// Edit form state
const editForm = reactive({
  userid: userId.userId,
  policyid: policyid,
  description: "",
  shortname: "",
  status: "A",
  managerApprover: "",
});

const editLoading = ref(false);
const editSubmitting = ref(false);
const editError = ref("");
const shortnameExists = ref(false);
const checkingShortname = ref(false);

const approvers = ref<any[]>([]);
const approversLoading = ref(false);
const approversError = ref("");
const manageApproversRef = ref();

const autoSaveTimer = ref<ReturnType<typeof setTimeout> | null>(null);
const AUTO_SAVE_DEBOUNCE_MS = 1500;
const isInitialized = ref(false);
const isDiscarded = ref(false);
const originalData = ref({
  shortname: "",
  description: "",
  status: "A",
  managerApprover: "",
});

const hasUnsavedChanges = computed(() => {
  return (
    editForm.shortname !== originalData.value.shortname ||
    editForm.description !== originalData.value.description ||
    editForm.status !== originalData.value.status ||
    editForm.managerApprover !== originalData.value.managerApprover ||
    approvers.value.length > 0
  );
});

const getPendingApprovers = () => {
  if (!manageApproversRef.value?.getPendingApprovers) return [];
  return manageApproversRef.value.getPendingApprovers();
};

const autoSavePending = () => {
  if (autoSaveTimer.value) {
    clearTimeout(autoSaveTimer.value);
  }

  autoSaveTimer.value = setTimeout(() => {
    if (isDiscarded.value) return;
    if (isInitialized.value && hasUnsavedChanges.value) {
      pendingStore.saveSnapshot(policyid, "/maintenance/policies/edit", "autoSave", {
        description: editForm.description,
        shortname: editForm.shortname,
        status: editForm.status,
        managerApprover: editForm.managerApprover,
        policyid: policyid,
        approvers: getPendingApprovers(),
      });
    }
  }, AUTO_SAVE_DEBOUNCE_MS);
};

// Watch for form changes to auto-save
watch(
  () => editForm.description,
  () => autoSavePending(),
);

watch(
  () => editForm.shortname,
  () => autoSavePending(),
);

watch(
  () => editForm.status,
  () => autoSavePending(),
);

watch(
  () => editForm.managerApprover,
  () => autoSavePending(),
);

const checkShortnameExists = async () => {
  const trimmed = editForm.shortname.trim();
  if (!trimmed) {
    shortnameExists.value = false;
    return;
  }
  if (trimmed === (originalData.value.shortname || "").trim()) {
    shortnameExists.value = false;
    return;
  }
  checkingShortname.value = true;
  try {
    const response: any = await apiFetch("/api/Maintenance/policy", {
      method: "POST",
      body: JSON.stringify({
        UserId: parseInt(userId.emplId),
        page: 1,
        pageSize: 10,
        search: editForm.shortname.trim(),
      }),
      headers: { "Content-Type": "application/json" },
      parseJson: true,
    });
    const currentId = String(policyid);
    const exists = (response.items || []).some(
      (p: any) =>
        p.shortname === editForm.shortname.trim() &&
        p.isdelete === 0 &&
        String(p.policyid) !== currentId,
    );
    shortnameExists.value = exists;
  } catch {
    shortnameExists.value = false;
  } finally {
    checkingShortname.value = false;
  }
};

const isEditFormValid = computed(() => {
  const hasShortname = editForm.shortname.trim().length > 0 && !shortnameExists.value;
  const hasDescription = editForm.description.trim().length > 0;
  const hasApprovers = approvers.value.length > 0;
  return hasShortname && hasDescription && hasApprovers;
});

const fetchEditPolicy = async () => {
  editLoading.value = true;
  editError.value = "";
  try {
    const response: any = await apiFetch(
      `/api/Maintenance/policy/${policyid}`,
      {
        method: "GET",
        parseJson: true,
      },
    );
    if (response) {
      editForm.policyid = response.items.policyid || "";
      editForm.description = response.items.description || "";
      editForm.shortname = response.items.shortname || "-";
      editForm.status = response.items.status || "A";
      editForm.managerApprover = response.items.managerApprover || "";

      // Set original data for comparison after fetching
      originalData.value = {
        shortname: editForm.shortname,
        description: editForm.description,
        status: editForm.status,
        managerApprover: editForm.managerApprover,
      };
    } else {
      editError.value = "Policy not found";
    }
  } catch (err) {
    editError.value = "Failed to load policy";
    console.error(err);
  } finally {
    editLoading.value = false;
  }
};

const fetchApproversLocal = async () => {
  approversLoading.value = true;
  try {
    const response = await apiFetch(
      `/api/Maintenance/policy/${policyid}/approvers`,
      {
        method: "POST",
        body: JSON.stringify({ UserId: userId.userId }),
        headers: {
          "Content-Type": "application/json",
        },
        parseJson: true,
      },
    );
    approvers.value = (response.items || []).map((item: any) => ({
      id: item.id || item.approverid,
      policyid: policyid,
      approverid: item.approverid || "",
      approvername: item.approvername || "",
      approveremail: item.approveremail || "",
      approvertype: item.approvertype || "",
      sort: item.sort || 0,
      linesequence: item.linesequence || 0,
      status: item.status || "",
      isdelete: item.isdelete || 0,
    }));
  } finally {
    approversLoading.value = false;
  }
};

const flushAutoSave = () => {
  if (isDiscarded.value) return;
  if (autoSaveTimer.value) {
    clearTimeout(autoSaveTimer.value);
  }
  if (hasUnsavedChanges.value && isInitialized.value) {
    pendingStore.saveSnapshot(policyid, "/maintenance/policies/edit", "autoSave", {
      description: editForm.description,
      shortname: editForm.shortname,
      status: editForm.status,
      managerApprover: editForm.managerApprover,
      policyid: policyid,
      approvers: getPendingApprovers(),
    });
  }
};

const submitEditForm = async () => {
  await checkShortnameExists();
  if (shortnameExists.value) {
    editError.value = "A policy with this name already exists";
    showToast({
      title: "Duplicate Policy Name",
      message: "A policy with this name already exists",
      type: "warning",
      duration: 5000,
    });
    return;
  }
  editSubmitting.value = true;
  editError.value = "";
  try {
    const response: any = await apiFetch(
      `/api/Maintenance/policy/update/${policyid}`,
      {
        method: "POST",
        body: JSON.stringify(editForm),
        headers: {
          "Content-Type": "application/json",
        },
      },
    );
    if (response) {
      if (manageApproversRef.value) {
        await manageApproversRef.value.saveOrder();
      }

      showToast({
        title: "Success",
        message: "The policy successfully updated.",
        type: "success",
        duration: 5000,
      });

      isDiscarded.value = true;
      if (autoSaveTimer.value) {
        clearTimeout(autoSaveTimer.value);
        autoSaveTimer.value = null;
      }
      pendingStore.discardPending(policyid, "/maintenance/policies/edit");
      navigateTo("/maintenance/policies");
    } else {
      editError.value = response.message || "Failed to update policy";
    }
  } catch (err) {
    editError.value = "An error occurred";
    console.error(err);
  } finally {
    editSubmitting.value = false;
  }
};

const cancelEdit = () => {
  isDiscarded.value = true;
  if (autoSaveTimer.value) {
    clearTimeout(autoSaveTimer.value);
    autoSaveTimer.value = null;
  }
  pendingStore.discardPending(policyid, "/maintenance/policies/edit");
  navigateTo("/maintenance/policies");
};

// Pending data restore modal state
const showPendingRestoreModal = ref(false);
const pendingRestoreData = ref<{
  action: string;
  data: Record<string, any>;
  attachmentMeta: { name: string; size: number; type: string }[];
  timestamp: string;
} | null>(null);

const handleRestorePending = () => {
  if (!pendingRestoreData.value?.data) return;
  const data = pendingRestoreData.value.data;
  editForm.policyid = data.policyid ?? policyid;
  editForm.description = data.description ?? "";
  editForm.shortname = data.shortname ?? "-";
  editForm.status = data.status ?? "A";
  editForm.managerApprover = data.managerApprover ?? "";
  if (Array.isArray(data.approvers) && manageApproversRef.value?.loadApprovers) {
    manageApproversRef.value.loadApprovers(data.approvers);
  }
  isDiscarded.value = true;
  pendingStore.discardPending(policyid, "/maintenance/policies/edit");
  closePendingRestoreModal();
  nextTick(() => { isDiscarded.value = false; });
};

const handleDiscardPending = () => {
  pendingStore.discardPending(policyid, "/maintenance/policies/edit");
  closePendingRestoreModal();
};

const closePendingRestoreModal = () => {
  showPendingRestoreModal.value = false;
  pendingRestoreData.value = null;
};

const getActionLabel = (action: string): string => {
  return action === "autoSave" ? "changes" : action;
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

const statusLabel = (status: string): string => {
  return status === "A" ? "Active" : status === "I" ? "Inactive" : status || "—";
};

onMounted(async () => {
  await fetchEditPolicy();
  await fetchApproversLocal();

  const pendingData = pendingStore.checkPending(policyid, "/maintenance/policies/edit");
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

const statusItems = [
  {
    id: "A",
    label: "Active"
  },
  {
    id: "I",
    label: "Inactive"
  }
]
</script>

<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900 dark:text-gray-200 overflow-auto">
    <header class="bg-white dark:bg-gray-800 shadow">
      <div class="py-4 px-4 sm:px-6 lg:px-8 flex sm:flex-row justify-between items-center gap-3">

        <div class="flex items-center">
          <Button variant="ghost-primary" size="lg" icon="left_arrow" iconPosition="center" customClass="rounded-full"
            class="dark:bg-gray-700 dark:text-gray-200 dark:hover:bg-gray-600" @onclick="cancelEdit" />
          <h1 class="text-xl sm:text-2xl ps-2 sm:ps-5 font-bold text-gray-900 dark:text-gray-200">
            Edit Policy {{ editForm.policyid }}
          </h1>
        </div>

        <div class="flex gap-2 pt-2">

          <Button variant="primary" type="submit" form="edit-policy-form"
            :disabled="editSubmitting || editLoading || !isEditFormValid" icon="save" icon-position="left">
            <p class="hidden sm:inline-block">
              {{ editSubmitting ? 'Updating...' : 'Update Policy' }}
            </p>
          </Button>
        </div>
      </div>
    </header>

    <main class="max-w-full mx-auto py-6 sm:px-6">
      <div
        class="grid grid-cols-1 lg:grid-cols-2 gap-4 sm:gap-6 bg-white dark:bg-gray-800 rounded-lg p-4 sm:p-6 border border-gray-200 dark:border-0">
        <div v-if="editLoading" class="text-center py-4 text-gray-500 dark:text-gray-400 lg:col-span-2">
          Loading...
        </div>

        <form id="edit-policy-form" v-else @submit.prevent="submitEditForm" class="space-y-4">
          <div>
            <label for="edit-shortname" class="block text-sm font-medium text-gray-700 dark:text-gray-300">
              Policy Name
            </label>
            <input id="edit-shortname" v-model="editForm.shortname" type="text" required
              class="mt-1 block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
              @blur="checkShortnameExists" />
            <p v-if="shortnameExists" class="mt-1 text-sm text-red-600">Policy name already exists</p>
            <p v-if="checkingShortname" class="mt-1 text-sm text-gray-500">Checking...</p>
          </div>

          <div>
            <label for="edit-description" class="block text-sm font-medium text-gray-700 dark:text-gray-300">
              Description
            </label>
            <textarea id="edit-description" v-model="editForm.description" required
              class="mt-1 block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
              rows="3"></textarea>
          </div>

          <div>
            <label for="edit-status" class="block text-sm font-medium text-gray-700 dark:text-gray-300">
              Status
            </label>
            <!-- <select
              id="edit-status"
              v-model="editForm.status"
              class="mt-1 block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
            >
              <option value="A">Active</option>
              <option value="I">Inactive</option>
            </select> -->
            <Dropdown v-model="editForm.status" :initial-items="statusItems" :object-output="false">
              <template #selected="{ item }">
                <span class="inline-flex items-center rounded px-2.5 py-0.5 text-xs font-medium" :class="item === 'A'
                  ? 'bg-green-100 text-green-800 border border-green-200'
                  : 'bg-red-100 text-red-800 border border-red-200'
                  ">
                  {{ item === 'A' ? 'Active' : 'Inactive' }}
                </span>
              </template>

              <template #option="{ item }">
                <span class="inline-flex items-center rounded px-2.5 py-0.5 text-xs font-medium" :class="item.id === 'A'
                  ? 'bg-green-100 text-green-800 border border-green-200'
                  : 'bg-red-100 text-red-800 border border-red-200'
                  ">
                  {{ item.label }}
                </span>
              </template>
            </Dropdown>
          </div>

          <div v-if="editError" class="text-red-500 text-sm">
            {{ editError }}
          </div>
        </form>

        <div class="">
          <ManageApprovers ref="manageApproversRef" :key="policyid" :show="true" :policy="{ policyid }"
            :initial-approvers="approvers" inline @saved="fetchApproversLocal" />
        </div>
      </div>
    </main>

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
        <div class="p-4 bg-amber-50 dark:bg-amber-900/30 rounded-lg border border-amber-200 dark:border-amber-800">
          <p class="text-sm text-amber-800 dark:text-amber-200">
            We detected an unsaved <strong>{{ getActionLabel(pendingRestoreData.action) }}</strong> from a
            previous session
            ({{ formatTimestamp(pendingRestoreData.timestamp) }}).
            Do you want to restore it?
          </p>
        </div>

        <div class="space-y-4 p-4 bg-gray-50 dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700">
          <h4 class="text-sm font-medium text-gray-700 dark:text-gray-300">Unsaved Data to Restore</h4>

          <dl class="space-y-3">
            <div>
              <dt class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Shortname</dt>
              <dd class="text-sm text-gray-900 dark:text-gray-100">
                {{ pendingRestoreData.data.shortname || '—' }}
              </dd>
            </div>
            <div>
              <dt class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Description</dt>
              <dd class="text-sm text-gray-900 dark:text-gray-100 whitespace-pre-wrap">
                {{ pendingRestoreData.data.description || '—' }}
              </dd>
            </div>
            <div>
              <dt class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Status</dt>
              <dd class="text-sm text-gray-900 dark:text-gray-100">
                {{ statusLabel(pendingRestoreData.data.status) }}
              </dd>
            </div>
            <div v-if="pendingRestoreData.data.managerApprover">
              <dt class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Manager Approver</dt>
              <dd class="text-sm text-gray-900 dark:text-gray-100">
                {{ pendingRestoreData.data.managerApprover }}
              </dd>
            </div>
            <div v-if="Array.isArray(pendingRestoreData.data.approvers) && pendingRestoreData.data.approvers.length">
              <dt class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Approvers</dt>
              <dd class="text-sm text-gray-900 dark:text-gray-100">
                {{ pendingRestoreData.data.approvers.length }} approver(s) configured
              </dd>
            </div>
          </dl>
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
  </div>
</template>
