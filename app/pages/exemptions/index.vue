<script setup lang="ts">
const router = useRouter();

import { IconView, IconEdit, IconDelete } from "#components";

const loading = ref(false);
const exemptionData = ref(null);

const viewProduct = (row: any) => console.log("View:", row);
const editProduct = (row: any) => console.log("Edit:", row);
const deleteProduct = (row: any) => console.log("Delete:", row);

const fetchExemptionRequests = async () => {
  loading.value = true;
  try {
    const responseExemptions = await apiFetch("/api/exception", {
      method: "POST",
      parseJson: true,
    });

    exemptionData.value = responseExemptions.items;
  } catch (error) {
    console.error("Error fetching exemption requests:", error);
  } finally {
    loading.value = false;
  }
};

watchEffect(() => {
  fetchExemptionRequests();
});
</script>

<template>
  <div class="min-h-[50vh] bg-gray-50 overflow-auto h-100vh">
    <header class="bg-white shadow">
      <div
        class="max-w-7xl me-auto py-6  sm:px-6 lg:px-8 flex justify-start items-center"
      >
        <Button
          variant="ghost-primary"
          size="lg"
          icon="left_arrow"
          iconPosition="center"
          customClass="rounded-full"
          @onclick="$router.back()"
        />
        <h1 class="text-3xl ps-5 font-bold text-gray-900">My Requests</h1>
      </div>
    </header>
    <main class="max-w-7xl mx-auto py-6 sm:px-6">
      <!-- Exemption Requests List -->
      <div class="mb-6 px-2 rounded-lg">
        <base-table
          :table-data="exemptionData"
          :header="[
            'transactionId',
            'transactionNumber',
            'requesterName',
            'corporate',
            'policyName',
            'status',
            'createdDate',
          ]"
          :badge-fields="['status', 'finalDecision']"
          :field-mappings="{
            status: {
              O: 'Open',
              P: 'Pending Approval',
              C: 'Completed',
            },
            finalDecision: {
              Pending: 'Pending',
              Approved: 'Approved',
              Disapproved: 'Disapproved',
            },
          }"
          :field-colors="{
            status: {
              O: 'bg-blue-100 text-blue-800',
              P: 'bg-yellow-100 text-yellow-800',
              C: 'bg-green-100 text-green-800',
            },
            finalDecision: {
              Pending: 'bg-yellow-100 text-yellow-800',
              Approved: 'bg-green-100 text-green-800',
              Disapproved: 'bg-red-100 text-red-800',
            },
          }"
          :date-fields="['createdDate']"
          :pagination="true"
          :per-page="1"
          :show-search="true"
          :actions="[
            {
              label: IconView,
              tooltip: 'View Details',
              handler: viewProduct,
              iconClass: 'text-indigo-500',
            },
            {
              label: IconEdit,
              tooltip: 'Edit Product',
              handler: editProduct,
              iconClass: 'text-blue-500',
            },
            {
              label: IconDelete,
              tooltip: 'Delete Product',
              handler: deleteProduct,
              iconClass: 'text-red-500',
              disabled: (row) => row.status === 'published', // Optional disable logic
            },
          ]"
        />
      </div>
    </main>
  </div>
</template>
