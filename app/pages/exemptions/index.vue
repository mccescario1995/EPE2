<script setup>
const router = useRouter();
import { IconView, IconEdit, IconDelete } from "#components";

const viewProduct = (row) => console.log("View:", row);
const editProduct = (row) => console.log("Edit:", row);
const deleteProduct = (row) => console.log("Delete:", row);

const users = [
  { id: 1, name: "John Doe", email: "john@example.com", role: "User" },
  { id: 2, name: "Jane Smith", email: "jane@example.com", role: "Admin" },
  { id: 3, name: "Bob Johnson", email: "bob@example.com", role: "User" },
];
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
          @onclick="$router.back()"
        />
        <h1 class="text-3xl ps-5 font-bold text-gray-900">My Requests</h1>
      </div>
    </header>
    <main class="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
      <!-- Exemption Requests List -->
      <div class="mb-6 bg-white p-2 rounded-lg shadow">
        <base-table
          :table-data="users"
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
