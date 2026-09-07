<script setup lang="ts">
definePageMeta({
  middleware: "auth",
});

import ManageApprovers from "~/components/ManageApprovers.vue";
import { usePendingTransactionStore } from "~/stores/usePendingTransactionStore";
import Modal from "~/components/Modal.vue";
import Button from "~/components/Button.vue";

const userStore = useUserStore();
const userId = userStore.profile;
const pendingStore = usePendingTransactionStore();

// For create pages, use userId as the identifier since no policyid exists yet
const policyIdRef = computed(() => userId.userId.toString());

// Create form state
const createForm = reactive({
  userId: userId.userId,
  description: "",
  shortname: "",
  status: "A",
  managerApprover: "",
});

const createLoading = ref(false);
const createError = ref("");
const createApproverCount = ref(0);
const createdPolicyId = ref<string | null>(null);
const createManageApproversRef = ref();
const shortnameExists = ref(false);
const checkingShortname = ref(false);

// Auto-save
const autoSaveTimer = ref<ReturnType<typeof setTimeout> | null>(null);
const AUTO_SAVE_DEBOUNCE_MS = 1500;
const isInitialized = ref(false);
const isDiscarded = ref(false);

const hasUnsavedChanges = computed(() => {
  return !!(
    createForm.shortname.trim() ||
    createForm.description.trim() ||
    createForm.managerApprover.trim() ||
    createApproverCount.value > 0
  );
});

const getPendingApprovers = () => {
  if (!createManageApproversRef.value?.getPendingApprovers) return [];
  return createManageApproversRef.value.getPendingApprovers();
};

const autoSavePending = () => {
  if (autoSaveTimer.value) {
    clearTimeout(autoSaveTimer.value);
  }

  autoSaveTimer.value = setTimeout(() => {
    if (isDiscarded.value) return;
    if (isInitialized.value && hasUnsavedChanges.value) {
      pendingStore.saveSnapshot(policyIdRef.value, "/maintenance/policies/create", "autoSave", {
        shortname: createForm.shortname,
        description: createForm.description,
        status: createForm.status,
        managerApprover: createForm.managerApprover,
        approvers: getPendingApprovers(),
      });
    }
  }, AUTO_SAVE_DEBOUNCE_MS);
};

// Watch for form changes to auto-save
watch(
  () => createForm.shortname,
  () => autoSavePending(),
);

watch(
  () => createForm.description,
  () => autoSavePending(),
);

watch(
  () => createForm.managerApprover,
  () => autoSavePending(),
);

watch(
  () => createForm.status,
  () => autoSavePending(),
);

watch(
  () => createApproverCount.value,
  () => autoSavePending(),
);

watch(() => createForm.shortname, () => {
  shortnameExists.value = false;
  createError.value = "";
});

const checkShortnameExists = async () => {
  if (!createForm.shortname.trim()) {
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
        search: createForm.shortname.trim(),
      }),
      headers: { "Content-Type": "application/json" },
      parseJson: true,
    });
    const exists = (response.items || []).some(
      (p: any) => p.shortname === createForm.shortname.trim() && p.isdelete === 0
    );
    shortnameExists.value = exists;
  } catch {
    shortnameExists.value = false;
  } finally {
    checkingShortname.value = false;
  }
};

const isCreateFormValid = computed(() => {
  return (
    createForm.shortname.trim().length > 0 &&
    createForm.description.trim().length > 0 &&
    createApproverCount.value > 0 &&
    !shortnameExists.value
  );
});

const submitCreateForm = async () => {
  await checkShortnameExists();
  if (shortnameExists.value) {
    createError.value = "A policy with this name already exists";
    showToast({
      title: "Duplicate Policy Name",
      message: "A policy with this name already exists",
      type: "warning",
      duration: 5000,
    });
    return;
  }
  createLoading.value = true;
  createError.value = "";
  try {
    pendingStore.saveSnapshot(policyIdRef.value, "/maintenance/policies/create", "createPolicy", {
      shortname: createForm.shortname,
      description: createForm.description,
      status: createForm.status,
      managerApprover: createForm.managerApprover,
      approvers: getPendingApprovers(),
    });

    const response: any = await apiFetch("/api/Maintenance/policy/create", {
      method: "POST",
      body: JSON.stringify(createForm),
      headers: {
        "Content-Type": "application/json",
      },
      parseJson: true,
    });
    if (response) {
      // Backend returns { success, message, data: policy }
      const policyid =
        response.data?.policyid ??
        response.data?.Policyid ??
        response.items?.policyid ??
        response.policyid;

      createdPolicyId.value = policyid;

      if (!policyid) {
        console.error("Could not extract policyid from response:", response);
      }

      if (createManageApproversRef.value && policyid) {
        await createManageApproversRef.value.createApprovers(
          policyid,
          userId.userId,
        );
      }

      // Reset form
      createForm.description = "";
      createForm.shortname = "";
      createForm.status = "A";
      createForm.managerApprover = "";
      createdPolicyId.value = null;
      createApproverCount.value = 0;
      isDiscarded.value = true;

      showToast({
        title: "Policy Created",
        message: "The policy has been successfully created.",
        type: "success",
        duration: 5000,
      });

      navigateTo("/maintenance/policies");
      pendingStore.discardPending(policyIdRef.value, "/maintenance/policies/create");
    } else {
      createError.value = response.message || "Failed to create policy";
    }
  } catch (err) {
    createError.value = "An error occurred";
    console.error(err);
  } finally {
    createLoading.value = false;
  }
};

const cancelCreate = () => {
  isDiscarded.value = true;
  if (autoSaveTimer.value) {
    clearTimeout(autoSaveTimer.value);
    autoSaveTimer.value = null;
  }
  pendingStore.discardPending(policyIdRef.value, "/maintenance/policies/create");
  createForm.description = "";
  createForm.shortname = "";
  createForm.status = "A";
  createForm.managerApprover = "";
  navigateTo("/maintenance/policies");
};

const flushAutoSave = () => {
  if (isDiscarded.value) return;
  if (autoSaveTimer.value) {
    clearTimeout(autoSaveTimer.value);
  }
  if (hasUnsavedChanges.value && isInitialized.value) {
    pendingStore.saveSnapshot(policyIdRef.value, "/maintenance/policies/create", "autoSave", {
      shortname: createForm.shortname,
      description: createForm.description,
      status: createForm.status,
      managerApprover: createForm.managerApprover,
      approvers: getPendingApprovers(),
    });
  }
};

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
  createForm.shortname = data.shortname ?? "";
  createForm.description = data.description ?? "";
  createForm.status = data.status ?? "A";
  createForm.managerApprover = data.managerApprover ?? "";
  if (Array.isArray(data.approvers) && createManageApproversRef.value?.loadApprovers) {
    createManageApproversRef.value.loadApprovers(data.approvers);
  }
  isDiscarded.value = true;
  pendingStore.discardPending(policyIdRef.value, "/maintenance/policies/create");
  closePendingRestoreModal();
  nextTick(() => { isDiscarded.value = false; });
};

const handleDiscardPending = () => {
  pendingStore.discardPending(policyIdRef.value, "/maintenance/policies/create");
  closePendingRestoreModal();
};

const closePendingRestoreModal = () => {
  showPendingRestoreModal.value = false;
  pendingRestoreData.value = null;
};

const getActionLabel = (action: string): string => {
  return action === "createPolicy"
    ? "policy"
    : action === "autoSave"
      ? "changes"
      : action;
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
  const pendingData = pendingStore.checkPending(policyIdRef.value, "/maintenance/policies/create");
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
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900 dark:text-gray-200 overflow-auto">
    <header class="bg-white dark:bg-gray-800 shadow">
      <div class="me-auto py-4 px-4 sm:px-6 lg:px-8 flex justify-between items-center">
        <div class="flex items-center">
          <Button variant="ghost-primary" size="lg" icon="left_arrow" iconPosition="center" customClass="rounded-full"
            class="dark:bg-gray-700 dark:text-gray-200 dark:hover:bg-gray-600" @onclick="cancelCreate" />
          <h1 class="text-2xl ps-5 font-bold text-gray-900 dark:text-gray-200">
            Create Policy
          </h1>
        </div>
        <div class="flex justify-end gap-2 pt-2">

          <Button variant="primary" type="submit" form="create-policy-form"
            :disabled="createLoading || !isCreateFormValid" icon="save">
            <p class="hidden sm:inline-block">

              {{ createLoading ? 'Creating...' : 'Create Policy' }}
            </p>

          </Button>

        </div>
      </div>
    </header>

    <main class="max-w-full mx-auto py-6 sm:px-6">
      <div
        class="grid md:grid-cols-2 gap-6 bg-white dark:bg-gray-800 rounded-lg p-6 border border-gray-200 dark:border-0">
        <!-- Policy Form -->
        <div class="sm:border-b-1 sm:pb-6 md:border-b-0 md:pb-0 md:border-r dark:md:border-0 md:border-gray-200">
          <form id="create-policy-form" @submit.prevent="submitCreateForm" class="space-y-4">
            <div>
              <label for="create-shortname" class="block text-sm font-medium text-gray-700 dark:text-gray-300">
                Policy Name <span class="text-red-500 text-md">*</span>
              </label>
              <BaseInput id="create-shortname" v-model="createForm.shortname" type="text" required
                class="mt-1 block w-full focus:outline-none focus:ring-blue-500 focus:border-blue-500 bg-white dark:bg-gray-700 border-gray-300 dark:border-gray-600 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500"
                :max-char-count="50" :min-char-count="1" @blur="checkShortnameExists" />
              <p v-if="shortnameExists" class="mt-1 text-sm text-red-600">Policy name already exists</p>
              <p v-if="checkingShortname" class="mt-1 text-sm text-gray-500">Checking...</p>
            </div>

            <div>
              <label for="create-description" class="block text-sm font-medium text-gray-700 dark:text-gray-300">
                Description <span class="text-red-500 text-md">*</span>
              </label>
              <BaseInput id="create-description" v-model="createForm.description" required
                class="mt-1 block w-full focus:outline-none focus:ring-blue-500 focus:border-blue-500 bg-white dark:bg-gray-700 border-gray-300 dark:border-gray-600 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500"
                rows="3" is-textarea :max-char-count="5000" :min-char-count="10"></BaseInput>
            </div>

            <div>
              <label for="create-status" class="block text-sm font-medium text-gray-700 dark:text-gray-300">
                Status <span class="text-red-500 text-md">*</span>
              </label>
              <!-- <select id="create-status" v-model="createForm.status"
                class="mt-1 block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100">
                <option value="A">Active</option>
                <option value="I">Inactive</option>
              </select> -->

              <Dropdown v-model="createForm.status" :initial-items="statusItems" :object-output="false">
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
          </form>
        </div>

        <!-- Approvers Management (in create mode) -->
        <div class="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-0 dark:border-gray-700">
          <ManageApprovers ref="createManageApproversRef" :show="true" :policy="{
            policyid: 'new',
            description: createForm.description,
            shortName: createForm.shortname,
          }" inline :create-mode="true" @approver-count-change="(count) => (createApproverCount = count)" />
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
              <dt class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Policy Name</dt>
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