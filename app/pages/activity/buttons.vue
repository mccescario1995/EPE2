<template>
  <div class="bg-white rounded-xl p-6 shadow-sm">
    <h2 class="text-2xl font-semibold text-gray-800 mb-6">Button Components</h2>

    <div class="space-y-8">
      <!-- Primary Buttons -->
      <div>
        <h3
          class="text-sm font-medium text-gray-500 uppercase tracking-wider mb-4"
        >
          Primary Variants
        </h3>
        <div class="flex flex-wrap items-center gap-4">
          <Button variant="primary" size="xs">Primary</Button>
          <Button variant="primary" size="sm">Primary</Button>
          <Button variant="primary" size="md">Primary</Button>
          <Button variant="primary" size="lg">Primary</Button>
          <Button variant="primary" size="xl">Primary</Button>
        </div>
      </div>

      <!-- Success Buttons -->
      <div>
        <h3
          class="text-sm font-medium text-gray-500 uppercase tracking-wider mb-4"
        >
          Success Variants
        </h3>
        <div class="flex flex-wrap items-center gap-4">
          <Button variant="success" size="xs">Success</Button>
          <Button variant="success" size="sm">Success</Button>
          <Button variant="success" size="md">Success</Button>
          <Button variant="success" size="lg">Success</Button>
          <Button variant="success" size="xl">Success</Button>
        </div>
      </div>

      <!-- Danger Buttons -->
      <div>
        <h3
          class="text-sm font-medium text-gray-500 uppercase tracking-wider mb-4"
        >
          Danger Variants
        </h3>
        <div class="flex flex-wrap items-center gap-4">
          <Button variant="danger" size="xs">Danger</Button>
          <Button variant="danger" size="sm">Danger</Button>
          <Button variant="danger" size="md">Danger</Button>
          <Button variant="danger" size="lg">Danger</Button>
          <Button variant="danger" size="xl">Danger</Button>
        </div>
      </div>

      <!-- Warning Buttons -->
      <div>
        <h3
          class="text-sm font-medium text-gray-500 uppercase tracking-wider mb-4"
        >
          Warning Variants
        </h3>
        <div class="flex flex-wrap items-center gap-4">
          <Button variant="warning" size="xs">Warning</Button>
          <Button variant="warning" size="sm">Warning</Button>
          <Button variant="warning" size="md">Warning</Button>
          <Button variant="warning" size="lg">Warning</Button>
          <Button variant="warning" size="xl">Warning</Button>
        </div>
      </div>

      <!-- Info Buttons -->
      <div>
        <h3
          class="text-sm font-medium text-gray-500 uppercase tracking-wider mb-4"
        >
          Info Variants
        </h3>
        <div class="flex flex-wrap items-center gap-4">
          <Button variant="info" size="xs">Info</Button>
          <Button variant="info" size="sm">Info</Button>
          <Button variant="info" size="md">Info</Button>
          <Button variant="info" size="lg">Info</Button>
          <Button variant="info" size="xl">Info</Button>
        </div>
      </div>

      <!-- Outline Buttons -->
      <div>
        <h3
          class="text-sm font-medium text-gray-500 uppercase tracking-wider mb-4"
        >
          Outline Variants
        </h3>
        <div class="flex flex-wrap items-center gap-4">
          <Button variant="outline-primary" size="md">Outline</Button>
          <Button variant="outline-success" size="md">Outline</Button>
          <Button variant="outline-danger" size="md">Outline</Button>
          <Button variant="outline-warning" size="md">Outline</Button>
          <Button variant="outline-info" size="md">Outline</Button>
        </div>
      </div>

      <!-- Icon Buttons -->
      <div>
        <h3
          class="text-sm font-medium text-gray-500 uppercase tracking-wider mb-4"
        >
          With Icons
        </h3>
        <div class="flex flex-wrap items-center gap-4">
          <Button variant="primary" size="md" icon="upload">Upload</Button>
          <Button variant="success" size="md" icon="check">Confirm</Button>
          <Button variant="danger" size="md" icon="trash">Delete</Button>
          <Button variant="outline-primary" size="md" icon="download"
            >Download</Button
          >
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { IconView } from "#components";
import { confirmModal } from "~/stores/globalvar";

const deleteItem = async () => {
  const confirmed = await confirmModal.open(
    "Delete this item?",
    "delete",
    "Delete Confirmation"
  );

  const confirmed1 = await confirmModal.open({
    message: "awjdiawuhd",
    title: "aiuwydhaiwudh",
    type: "warning",
  });
  console.log("Modal result:", confirmed);
  if (confirmed) {
    console.log("User confirmed delete");
    // proceed with delete
  } else {
    console.log("User canceled");
  }
};
</script>

<style scoped>
/* Add any custom styles here if needed */
</style>
