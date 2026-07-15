<script setup lang="ts">
definePageMeta({
  middleware: "auth",
});

import ManageApprovers from "~/components/ManageApprovers.vue";

const userStore = useUserStore();
const userId = userStore.profile;

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

const isCreateFormValid = computed(() => {
  return (
    createForm.shortname.trim().length > 0 &&
    createForm.description.trim().length > 0 &&
    createApproverCount.value > 0
  );
});

const submitCreateForm = async () => {
  createLoading.value = true;
  createError.value = "";
  try {
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

      showToast({
        title: "Policy Created",
        message: "The policy has been successfully created.",
        type: "success",
        duration: 5000,
      });

      navigateTo("/main/maintenance/policies");
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
  navigateTo("/main/maintenance/policies");
};
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
            @onclick="cancelCreate"
          />
          <h1 class="text-2xl ps-5 font-bold text-gray-900 dark:text-gray-200">
            Create Policy
          </h1>
        </div>
        <div class="flex justify-end gap-2 pt-2">
              <Button
                variant="ghost"
                @click="cancelCreate"
                :disabled="createLoading"
                class="dark:hover:bg-gray-700 dark:text-gray-200"
              >
                Cancel
              </Button>
              <Button
                variant="primary"
                type="submit"
                form="create-policy-form"
                :disabled="createLoading || !isCreateFormValid"
              >
                {{ createLoading ? "Creating..." : "Create Policy" }}
              </Button>
            </div>
      </div>
    </header>

    <main class="max-w-full mx-auto py-6 sm:px-6">
      <div
        class="grid md:grid-cols-2 gap-6 bg-white dark:bg-gray-800 rounded-lg p-6 border border-gray-200 dark:border-0"
      >
        <!-- Policy Form -->
        <div
          class="sm:border-b-1 sm:pb-6 md:border-b-0 md:pb-0 md:border-r dark:md:border-0 md:border-gray-200"
        >
          <form id="create-policy-form" @submit.prevent="submitCreateForm" class="space-y-4">
            <div>
              <label
                for="create-shortname"
                class="block text-sm font-medium text-gray-700 dark:text-gray-300"
              >
                Short Name <span class="text-red-500 text-md">*</span>
              </label>
              <BaseInput
                id="create-shortname"
                v-model="createForm.shortname"
                type="text"
                required
                class="mt-1 block w-full focus:outline-none focus:ring-blue-500 focus:border-blue-500 bg-white dark:bg-gray-700 border-gray-300 dark:border-gray-600 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500"
                :max-char-count="50"
                :min-char-count="1"
              />
            </div>

            <div>
              <label
                for="create-description"
                class="block text-sm font-medium text-gray-700 dark:text-gray-300"
              >
                Description <span class="text-red-500 text-md">*</span>
              </label>
              <BaseInput
                id="create-description"
                v-model="createForm.description"
                required
                class="mt-1 block w-full focus:outline-none focus:ring-blue-500 focus:border-blue-500 bg-white dark:bg-gray-700 border-gray-300 dark:border-gray-600 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500"
                rows="3"
                is-textarea
                :max-char-count="1000"
                :min-char-count="10"
              ></BaseInput>
            </div>

            <div>
              <label
                for="create-status"
                class="block text-sm font-medium text-gray-700 dark:text-gray-300"
              >
                Status <span class="text-red-500 text-md">*</span>
              </label>
              <select
                id="create-status"
                v-model="createForm.status"
                class="mt-1 block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
              >
                <option value="A">Active</option>
                <option value="I">Inactive</option>
              </select>
            </div>

            <div v-if="createError" class="text-red-500 text-sm">
              {{ createError }}
            </div>

            
          </form>
        </div>

        <!-- Approvers Management (in create mode) -->
        <div
          class="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-0 dark:border-gray-700"
        >
          <ManageApprovers
            ref="createManageApproversRef"
            :show="true"
            :policy="{
              policyid: 'new',
              description: createForm.description,
              shortName: createForm.shortname,
            }"
            inline
            :create-mode="true"
            @approver-count-change="(count) => (createApproverCount = count)"
          />
        </div>
      </div>
    </main>
  </div>
</template>
