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

const autoSavePending = () => {
  if (autoSaveTimer.value) {
    clearTimeout(autoSaveTimer.value);
  }

  autoSaveTimer.value = setTimeout(() => {
    if (isInitialized.value && hasUnsavedChanges.value) {
      pendingStore.saveSnapshot(policyid, "/maintenance/policies/edit", "autoSave", {
        description: editForm.description,
        shortname: editForm.shortname,
        status: editForm.status,
        managerApprover: editForm.managerApprover,
        policyid: policyid,
      }, []);
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
  if (!editForm.shortname.trim()) {
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
    const exists = (response.items || []).some(
      (p: any) => p.shortname === editForm.shortname.trim() && p.isdelete === 0 && p.policyid != policyid
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
    }, []);
  }
};

const submitEditForm = async () => {
  await checkShortnameExists();
  if (shortnameExists.value) {
    editError.value = "A policy with this shortname already exists";
    showToast({
      title: "Duplicate Shortname",
      message: "A policy with this shortname already exists",
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
  pendingStore.discardPending(policyid, "/maintenance/policies/edit");
  navigateTo("/maintenance/policies");
};

onMounted(async () => {
  await fetchEditPolicy();
  await fetchApproversLocal();

  const pendingData = pendingStore.checkPending(policyid, "/maintenance/policies/edit");
  if (pendingData) {
    const restored = confirm("We detected an unsaved policy from a previous session. Do you want to restore it?");
    if (restored && pendingData.data) {
      editForm.policyid = pendingData.data.policyid ?? policyid;
      editForm.description = pendingData.data.description ?? "";
      editForm.shortname = pendingData.data.shortname ?? "-";
      editForm.status = pendingData.data.status ?? "A";
      editForm.managerApprover = pendingData.data.managerApprover ?? "";
    } else {
      pendingStore.discardPending(policyid, "/maintenance/policies/edit");
    }
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
  <div
    class="min-h-screen bg-gray-50 dark:bg-gray-900 dark:text-gray-200 overflow-auto"
  >
    <header class="bg-white dark:bg-gray-800 shadow">
      <div
        class="me-auto py-4 px-4 sm:px-6 lg:px-8 flex justify-between items-center"
      >
        <div class="flex items-center">
          <Button
            variant="ghost-primary"
            size="lg"
            icon="left_arrow"
            iconPosition="center"
            customClass="rounded-full"
            class="dark:bg-gray-700 dark:text-gray-200 dark:hover:bg-gray-600"
            @onclick="cancelEdit"
          />
          <h1 class="text-2xl ps-5 font-bold text-gray-900 dark:text-gray-200">
            Edit Policy {{ editForm.policyid }}
          </h1>
        </div>

        <div class="flex justify-end gap-2 pt-2">
          <Button
            variant="ghost"
            @onclick="cancelEdit"
            :disabled="editSubmitting"
            class="dark:hover:bg-gray-700 dark:text-gray-200"
          >
            Cancel
          </Button>
          <Button
            variant="primary"
            type="submit"
            form="edit-policy-form"
            :disabled="editSubmitting || editLoading || !isEditFormValid"
          >
            {{ editSubmitting ? "Updating..." : "Update Policy" }}
          </Button>
        </div>
      </div>
    </header>

    <main class="max-w-full mx-auto py-6 sm:px-6">
      <div
        class="grid grid-cols-2 gap-6 bg-white dark:bg-gray-800 rounded-lg p-6 border border-gray-200 dark:border-0"
      >
        <div
          v-if="editLoading"
          class="text-center py-4 text-gray-500 dark:text-gray-400"
        >
          Loading...
        </div>

        <form
          id="edit-policy-form"
          v-else
          @submit.prevent="submitEditForm"
          class="space-y-4"
        >
          <div>
            <label
              for="edit-shortname"
              class="block text-sm font-medium text-gray-700 dark:text-gray-300"
            >
              Shortname
            </label>
            <input
              id="edit-shortname"
              v-model="editForm.shortname"
              type="text"
              required
              class="mt-1 block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
              @blur="checkShortnameExists"
            />
            <p v-if="shortnameExists" class="mt-1 text-sm text-red-600">Shortname already exists</p>
            <p v-if="checkingShortname" class="mt-1 text-sm text-gray-500">Checking...</p>
          </div>

          <div>
            <label
              for="edit-description"
              class="block text-sm font-medium text-gray-700 dark:text-gray-300"
            >
              Description
            </label>
            <textarea
              id="edit-description"
              v-model="editForm.description"
              required
              class="mt-1 block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
              rows="3"
            ></textarea>
          </div>

          <div>
            <label
              for="edit-status"
              class="block text-sm font-medium text-gray-700 dark:text-gray-300"
            >
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
                  <span class="inline-flex items-center rounded px-2.5 py-0.5 text-xs font-medium"
                   :class="item === 'A'
                      ? 'bg-green-100 text-green-800 border border-green-200'
                      : 'bg-red-100 text-red-800 border border-red-200'
                    ">
                    {{ item === 'A' ? 'Active' : 'Inactive' }}
                  </span>
                </template>

                <template #option="{ item }">
                  <span class="inline-flex items-center rounded px-2.5 py-0.5 text-xs font-medium"
                   :class="item.id === 'A'
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

        <div
          class="border-t border-l md:border-l-0 md:border-t-0 pt-4 md:pt-0 md:border-l-gray-200 dark:md:border-l-gray-700 md:border-t-gray-200 dark:md:border-t-gray-700"
        >
          <ManageApprovers
            ref="manageApproversRef"
            :key="policyid"
            :show="true"
            :policy="{ policyid }"
            :initial-approvers="approvers"
            inline
            @saved="fetchApproversLocal"
          />
        </div>
      </div>
    </main>
  </div>
</template>
