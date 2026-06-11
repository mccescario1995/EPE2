<template>
  <div class="min-h-screen bg-linear-to-br from-blue-50 to-indigo-100 flex items-center justify-center p-4">
    <div class="max-w-md w-full bg-white rounded-2xl shadow-xl p-8 text-center">
      <div class="w-20 h-20 mx-auto mb-6 bg-red-100 rounded-full flex items-center justify-center">
        <svg class="w-10 h-10 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.654-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
        </svg>
      </div>
      <h1 class="text-2xl font-bold text-gray-900 mb-2">Access Denied</h1>
      <p class="text-gray-600 mb-6">
        You do not have permission to access this page.
        Please contact your administrator if you believe this is an error.
      </p>
      <button
        @click="goBack"
        class="w-full bg-indigo-600 text-white py-2 px-4 rounded-lg font-semibold hover:bg-indigo-700 transition-colors"
      >
        Go Back
      </button>
    </div>
  </div>
</template>

<script setup>
definePageMeta({
  layout: 'empty'
});

const router = useRouter();

function goBack() {
  router.back();
}
</script>