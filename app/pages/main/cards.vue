<template>
  <div class="grid grid-cols-2 gap-2">
    <card
      title="Basic Card"
      content="This is a simple card with default styling."
    />

    <card
      title="Image Card"
      content="This card includes an image with hover effect."
      image="/path/to/image.jpg"
      imageAlt="Description of image"
      imageOverlay
    />

    <!-- Action Card -->
    <card>
      <template #footer>
        <div class="flex justify-end">
          <button class="px-4 py-2 bg-purple-600 text-white rounded-md">
            Custom Action
          </button>
        </div>
      </template>
    </card>
    <!-- Customer Card -->
    <card>
      <template #title>Custom Title</template>
      <template #image>
        <div
          class="h-48 bg-gradient-to-r from-blue-500 to-purple-600 flex items-center justify-center"
        >
          <span class="text-white text-xl font-bold">Custom Image Content</span>
        </div>
      </template>
      <p>Custom content using slots instead of props</p>
      <template #footer>
        <!-- this is the footer -->
        <button class="text-sm text-purple-600">Custom Footer Action</button>
      </template>
    </card>
  </div>
</template>

<script setup>
// You can add any necessary JavaScript here
</script>

<style scoped>
/* Custom transitions for smoother hover effects */
.transition-all {
  transition-property: all;
  transition-timing-function: cubic-bezier(0.4, 0, 0.2, 1);
}

/* Ensure images maintain aspect ratio */
img {
  object-fit: cover;
}
</style>
