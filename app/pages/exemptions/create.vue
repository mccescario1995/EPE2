<script setup lang="ts">
const router = useRouter();
const { showToast } = useToast();

// Form data
const formData = ref({
  policyid: null as number | { id: number; label: string } | null,
  corporate: null as string | null,
  details: "",
});

// Corporate options
const corporateOptions = [
  { id: "FS", label: "FS" },
  { id: "FL", label: "FL" },
  { id: "FTM", label: "FTM" },
];

// Policy options - will be fetched from API
const policyOptions = ref<{ id: number; label: string }[]>([]);
const isLoadingPolicies = ref(false);

// Approvers for selected policy
const approvers = ref<
  { id: number; approvername: string; approvertype: string; sort: number }[]
>([]);
const isLoadingApprovers = ref(false);

// Validation errors
const errors = ref({
  policyid: "",
  details: "",
});

// Loading state
const isSubmitting = ref(false);

// Fetch policies from API
const fetchPolicies = async () => {
  isLoadingPolicies.value = true;
  try {
    // TODO: Replace with actual API endpoint when available
    const response = await apiFetch("/api/policy", {
      method: "POST",
      parseJson: true,
    });

    policyOptions.value = response.items.map((p: any) => ({
      id: p.policyid,
      label: p.description,
    }));

  } catch (error) {
    console.error("Error fetching policies:", error);
    showToast({
      type: "error",
      title: "Error",
      message: "Failed to load policies. Please try again.",
      duration: 5000,
    });
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
    const response = await apiFetch(`/api/policy/approvers/${policyId}`, {
      method: 'POST',
      parseJson: true
    });

    if (response.success) {
      approvers.value = response.items.map((a: any) => ({
        id: a.id,
        approvername: a.approvername,
        approvertype: a.approvertype,
        sort: a.sort
      }));
    }
  } catch (error) {
    console.error('Error fetching approvers:', error);
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

// Validate form
const validateForm = () => {
  let isValid = true;
  errors.value = { policyid: "", details: "" };

  if (!formData.value.policyid) {
    errors.value.policyid = "Please select a policy";
    isValid = false;
  }

  if (!formData.value.details || formData.value.details.trim().length < 10) {
    errors.value.details = "Please provide details (minimum 10 characters)";
    isValid = false;
  }

  return isValid;
};

// Get policy ID from form data (handles both object and number)
const getPolicyId = () => {
  const policyId = formData.value.policyid;
  return policyId && typeof policyId === "object" ? policyId.id : policyId;
};

// Submit form
const handleSubmit = async () => {
  if (!validateForm()) return;

  isSubmitting.value = true;
  try {
    const payload = {
      policyid: getPolicyId(),
      corporate: formData.value.corporate || null,
      details: formData.value.details,
    };

    const response = await apiFetch("/api/exception/create", {
      method: "POST",
      body: JSON.stringify(payload),
      headers: {
        "Content-Type": "application/json",
      },
    });

    if (response.success) {
      showToast({
        type: "success",
        title: "Success",
        message: "Exemption request created successfully!",
        duration: 5000,
      });
      // Redirect to exemptions list after success
      router.push("/exemptions");
    } else {
      showToast({
        type: "error",
        title: "Error",
        message: response.message || "Failed to create exemption request",
        duration: 5000,
      });
    }
  } catch (error: any) {
    console.error("Error creating exemption request:", error);
    showToast({
      type: "error",
      title: "Error",
      message: error.message || "An error occurred while creating the request",
      duration: 5000,
    });
  } finally {
    isSubmitting.value = false;
  }
};

// Handle cancel
const handleCancel = () => {
  router.push("/exemptions");
};

// Fetch policies on mount
onMounted(() => {
  fetchPolicies();
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
          Create Exemption Request
        </h1>
      </div>
    </header>

    <main class="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
      <div class="bg-white rounded-lg shadow px-6 py-6">
        <form @submit.prevent="handleSubmit" class="space-y-6">
          <!-- Policy Selection -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">
              Policy <span class="text-red-500">*</span>
            </label>
            <Dropdown
              v-model="formData.policyid"
              :items="policyOptions"
              placeholder="Select a policy"
              :disabled="isLoadingPolicies"
              objectOutput
            />
            <p v-if="errors.policyid" class="mt-1 text-xs text-red-500">
              {{ errors.policyid }}
            </p>
          </div>

          <!-- Corporate Selection -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">
              Corporate
            </label>
            <Dropdown
              v-model="formData.corporate"
              :items="corporateOptions"
              placeholder="Select corporate (optional)"
              :allowAdd="false"
            />
            <p class="mt-1 text-xs text-gray-500">
              Optional - Leave empty if not applicable
            </p>
          </div>

          <!-- Details -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">
              Details <span class="text-red-500">*</span>
            </label>
            <BaseInput
              v-model="formData.details"
              type="text"
              isTextarea
              :rowCount="5"
              placeholder="Explain the reason for your exemption request..."
              :errorMessage="errors.details"
              :maxCharCount="1000"
            />
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
              <div
                v-for="(approver, index) in approvers"
                :key="approver.id"
                class="p-3 flex items-center justify-between"
              >
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
                    <p class="text-xs text-gray-500">
                      {{ approver.approvertype }}
                    </p>
                  </div>
                </div>
                <span
                  class="px-2 py-1 text-xs font-medium rounded-full"
                  :class="{
                    'bg-purple-100 text-purple-800':
                      approver.approvertype === 'Head',
                    'bg-indigo-100 text-indigo-800':
                      approver.approvertype === 'Manual',
                  }"
                >
                  {{ approver.approvertype }}
                </span>
              </div>
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
              variant="outline-primary"
              size="lg"
              @onclick="handleCancel"
              :disabled="isSubmitting"
            >
              Cancel
            </Button>
            <Button
              variant="primary"
              size="lg"
              type="submit"
              :loading="isSubmitting"
            >
              Submit Request
            </Button>
          </div>
        </form>
      </div>
    </main>
  </div>
</template>
