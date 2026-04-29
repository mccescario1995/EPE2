<template>
  <div class="container mx-auto p-4 max-w-md">
    <h1 class="text-2xl font-bold mb-4">Edit Policy</h1>

    <div v-if="loading" class="text-center">Loading...</div>

    <form v-else @submit.prevent="submitForm" class="space-y-4">
      <div>
        <label for="shortname" class="block text-sm font-medium text-gray-700"
          >Shortname</label
        >
        <input
          id="shortname"
          v-model="form.shortname"
          type="text"
          required
          class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
        />
      </div>

      <div>
        <label for="description" class="block text-sm font-medium text-gray-700"
          >Description</label
        >
        <textarea
          id="description"
          v-model="form.description"
          required
          class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
          rows="3"
        ></textarea>
      </div>

      <div>
        <label for="status" class="block text-sm font-medium text-gray-700"
          >Status</label
        >
        <select
          id="status"
          v-model="form.status"
          class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
        >
          <option value="A">Active</option>
          <option value="I">Inactive</option>
        </select>
      </div>

      <div class="flex space-x-2">
        <Button
          type="submit"
          :disabled="submitting"
          class="px-4 py-2 bg-blue-500 text-white rounded-md hover:bg-blue-600 disabled:opacity-50"
        >
          {{ submitting ? "Updating..." : "Update Policy" }}
        </Button>
        <NuxtLink
          to="/maintenance/policies"
          class="px-4 py-2 bg-gray-500 text-white rounded-md hover:bg-gray-600"
        >
          Cancel
        </NuxtLink>
      </div>
    </form>

    <div v-if="error" class="mt-4 text-red-500">{{ error }}</div>
  </div>
</template>

<script setup lang="ts">
const route = useRoute();
const policyId = parseInt(route.params.id as string);

const form = reactive({
  description: "",
  shortname: "",
  status: "A",
});

const loading = ref(true);
const submitting = ref(false);
const error = ref("");

const fetchPolicy = async () => {
  try {
    const response: any = await $fetch(`/api/Policy/${policyId}`);
    if (response) {
      form.description = response.Description || "";
      form.shortname = response.Shortname || "";
      form.status = response.Status || "A";
    } else {
      error.value = "Policy not found";
    }

    console.log("Fetched policy data:", response);
  } catch (err) {
    error.value = "Failed to load policy";
    console.error(err);
  } finally {
    loading.value = false;
  }
};

const submitForm = async () => {
  submitting.value = true;
  error.value = "";
  try {
    const response: any = await $fetch(`/api/Policy/${policyId}`, {
      method: "PUT",
      body: form,
    });
    if (response) {
      await navigateTo("/maintenance/policies");
    } else {
      error.value = response.message || "Failed to update policy";
    }
  } catch (err) {
    error.value = "An error occurred";
    console.error(err);
  } finally {
    submitting.value = false;
  }
};

onMounted(() => {
  fetchPolicy();
});
</script>
