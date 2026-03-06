<script setup>
const router = useRouter();
</script>

<template>
  <div class="min-h-screen bg-gray-50 overflow-auto h-[75vh]">
    <header class="bg-white shadow">
      <div
        class="max-w-7xl me-auto py-6 px-4 sm:px-6 lg:px-8 flex justify-start items-center"
      >
        <Button
          variant="primary"
          size="md"
          icon="left_arrow"
          iconPosition="center"
          customClass="rounded-full"
          @onclick="$router.back()"
        />
        <h1 class="text-3xl ps-5 font-bold text-gray-900">
          Closed Requests
        </h1>
      </div>
    </header>
    <main class="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
      <!-- Chart Selection Controls -->
      <div class="mb-6 bg-white p-4 rounded-lg shadow"></div>

      <!-- Dynamic Charts Grid -->
    </main>
  </div>
</template>
