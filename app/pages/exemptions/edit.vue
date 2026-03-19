<script setup lang="ts">
const router = useRouter();
const route = useRoute();
const { showToast } = useToast();
const userStore = useUserStore();

const userId = userStore.profile;

// Get ID from route query
const exemptionId = computed(() => {
  return Number(route.query.id) || null;
});

// Form data
const formData = ref({
  policyid: null as number | { id: number; label: string } | null,
  corporate: null as { id: string; label: string } | null,
  details: '',
});

// Store original data for comparison
const originalData = ref({
  policyid: null as number | null,
  corporate: null as string | null,
  details: '',
});

// Corporate options
const corporateOptions = [
  { id: 'FS', label: 'FS' },
  { id: 'FL', label: 'FL' },
  { id: 'FTM', label: 'FTM' },
];

// Policy options - will be fetched from API
const policyOptions = ref<{ id: number; label: string }[]>([]);
const isLoadingPolicies = ref(false);

// Validation errors
const errors = ref({
  policyid: '',
  details: '',
});

// Loading state
const isLoading = ref(false);
const isSubmitting = ref(false);

// Fetch policies from API
const fetchPolicies = async () => {
  isLoadingPolicies.value = true;

  try {
    const response = await apiFetch('/api/policy', {
      method: 'POST',
      parseJson: true,
    });

    // Handle both success response and direct items array
    const items = response.success ? response.items : response;

    if (!items || !Array.isArray(items)) {
      console.error('Invalid response format:', items);
      policyOptions.value = [];
      return;
    }

    policyOptions.value = items.map((p: any) => ({
      id: p.policyid,
      label: p.description || p.label || `Policy ${p.policyid}`,
    }));
  } catch (error) {
    console.error('Error fetching policies:', error);
    showToast({
      type: 'error',
      title: 'Error',
      message: 'Failed to load policies. Please try again.',
      duration: 5000,
    });
    policyOptions.value = [];
  } finally {
    isLoadingPolicies.value = false;
  }
};

// Fetch exemption details
const fetchExemptionDetails = async () => {
  if (!exemptionId.value) {
    showToast({
      type: 'error',
      title: 'Error',
      message: 'Invalid exemption ID',
      duration: 5000,
    });
    router.push('/exemptions');
    return;
  }

  

  isLoading.value = true;
  try {

    const response = await apiFetch(`/api/exception/edit/${exemptionId.value}`, {
      method: 'POST',
      parseJson: true,
    });

    console.log('Fetched exemption edit details:', response);

    if (response.success) {
      const item = response.item;
      
      // Set form data
      formData.value.policyid = item.policyid;
      formData.value.corporate = item.corporate 
        ? { id: item.corporate, label: item.corporate } 
        : null;
      formData.value.details = item.details || '';

      // Store original data
      originalData.value = {
        policyid: item.policyid,
        corporate: item.corporate,
        details: item.details || '',
      };
    } else {
      showToast({
        type: 'error',
        title: 'Error',
        message: response.message || 'Failed to load exemption details',
        duration: 5000,
      });
      router.push('/exemptions');
    }
  } catch (error) {
    console.error('Error fetching exemption details:', error);
    showToast({
      type: 'error',
      title: 'Error',
      message: 'Failed to load exemption details',
      duration: 5000,
    });
    router.push('/exemptions');
  } finally {
    isLoading.value = false;
  }
};

// Validate form
const validateForm = () => {
  let isValid = true;
  errors.value = { policyid: '', details: '' };

  if (!formData.value.policyid) {
    errors.value.policyid = 'Please select a policy';
    isValid = false;
  }

  if (!formData.value.details || formData.value.details.trim().length < 10) {
    errors.value.details = 'Please provide details (minimum 10 characters)';
    isValid = false;
  }

  return isValid;
};

// Get policy ID from form data (handles both object and number)
const getPolicyId = () => {
  const policyId = formData.value.policyid;
  return policyId && typeof policyId === 'object' ? policyId.id : policyId;
};

// Check if there are unsaved changes
const hasUnsavedChanges = computed(() => {
  const currentPolicyId = getPolicyId();
  const currentCorporate = formData.value.corporate?.id || null;
  
  return (
    currentPolicyId !== originalData.value.policyid ||
    currentCorporate !== originalData.value.corporate ||
    formData.value.details.trim() !== (originalData.value.details || '').trim()
  );
});

// Handle cancel
const handleCancel = () => {
  if (hasUnsavedChanges.value) {
    if (confirm('You have unsaved changes. Are you sure you want to leave?')) {
      router.push('/exemptions');
    }
  } else {
    router.push('/exemptions');
  }
};

// Submit form
const handleSubmit = async () => {
  if (!validateForm()) return;

  isSubmitting.value = true;
  try {
    const payload = {
      policyid: getPolicyId(),
      corporate: formData.value.corporate?.id || null,
      details: formData.value.details,
      modifiedby: userId.userId,
    };

    console.log('Updating exemption request with payload:', payload);

    const response = await apiFetch(`/api/exception/${exemptionId.value}`, {
      method: 'PUT',
      body: JSON.stringify(payload),
      headers: {
        'Content-Type': 'application/json',
      },
    });
    console.log('Update exemption response:', response);

    if (response && response.success) {
      showToast({
        type: 'success',
        title: 'Success',
        message: 'Exemption request updated successfully!',
        duration: 5000,
      });
      // Redirect to exemptions list after success
      router.push('/exemptions');
    } else {
      showToast({
        type: 'error',
        title: 'Error',
        message: response?.message || 'Failed to update exemption request',
        duration: 5000,
      });
    }
  } catch (error: any) {
    console.error('Error updating exemption request:', error);
    showToast({
      type: 'error',
      title: 'Error',
      message: error.message || 'An error occurred while updating the request',
      duration: 5000,
    });
  } finally {
    isSubmitting.value = false;
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
          Edit Exemption Request
        </h1>
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

      <!-- Form -->
      <div v-else class="bg-white rounded-lg shadow px-6 py-6">
        <form @submit.prevent="handleSubmit" class="space-y-6">
          <!-- Policy Selection -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">
              Policy <span class="text-red-500">*</span>
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

          <!-- Corporate Selection -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">
              Corporate
            </label>
            <Dropdown
              v-model="formData.corporate"
              :initialItems="corporateOptions"
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

          <!-- Info Notice -->
          <div class="bg-blue-50 border border-blue-200 rounded-lg p-4">
            <div class="flex">
              <div class="flex-shrink-0">
                <svg class="h-5 w-5 text-blue-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
                </svg>
              </div>
              <div class="ml-3">
                <p class="text-sm text-blue-700">
                  Note: Status and Final Decision cannot be edited. These are managed by approvers.
                </p>
              </div>
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
              Update Request
            </Button>
          </div>
        </form>
      </div>
    </main>
  </div>
</template>
