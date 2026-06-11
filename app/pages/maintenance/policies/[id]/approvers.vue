<template>
  <div class="container mx-auto p-4">
    <h1 class="text-2xl font-bold mb-4">Manage Approvers for Policy {{ policyId }}</h1>

    <NuxtLink to="/policies" class="text-blue-500 hover:underline mb-4 inline-block">&larr; Back to Policies</NuxtLink>

    <div v-if="loading" class="text-center">Loading...</div>

    <!-- <div v-else>
      <table class="min-w-full bg-white border border-gray-300 mb-4">
        <thead>
          <tr class="bg-gray-100">
            <th class="px-4 py-2 border">Name</th>
            <th class="px-4 py-2 border">Email</th>
            <th class="px-4 py-2 border">Type</th>
            <th class="px-4 py-2 border">Sequence</th>
            <th class="px-4 py-2 border">Role</th>
            <th class="px-4 py-2 border">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="approver in approvers" :key="approver.Id" class="hover:bg-gray-50">
            <td class="px-4 py-2 border">{{ approver.Approvername }}</td>
            <td class="px-4 py-2 border">{{ approver.Approveremail }}</td>
            <td class="px-4 py-2 border">{{ approver.Approvertype }}</td>
            <td class="px-4 py-2 border">
              <input
                v-model.number="approver.Linesequence"
                @blur="updateSequence(approver)"
                type="number"
                min="1"
                class="w-16 px-2 py-1 border border-gray-300 rounded"
              />
            </td>
            <td class="px-4 py-2 border">{{ approver.ApproverRole }}</td>
            <td class="px-4 py-2 border">
              <button @click="deleteApprover(approver.Id)" class="text-red-500 hover:underline">Delete</button>
            </td>
          </tr>
        </tbody>
      </table>

      <h2 class="text-xl font-semibold mb-2">Add New Approver</h2>
      <form @submit.prevent="addApprover" class="space-y-4 max-w-md">
        <div>
          <label for="approverid" class="block text-sm font-medium">Approver ID</label>
          <input
            id="approverid"
            v-model.number="newApprover.approverid"
            type="number"
            class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md"
          />
        </div>
        <div>
          <label for="name" class="block text-sm font-medium">Name</label>
          <input
            id="name"
            v-model="newApprover.name"
            type="text"
            required
            class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md"
          />
        </div>
        <div>
          <label for="email" class="block text-sm font-medium">Email</label>
          <input
            id="email"
            v-model="newApprover.email"
            type="email"
            required
            class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md"
          />
        </div>
        <div>
          <label for="type" class="block text-sm font-medium">Type</label>
          <input
            id="type"
            v-model="newApprover.type"
            type="text"
            class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md"
          />
        </div>
        <div>
          <label for="sequence" class="block text-sm font-medium">Sequence</label>
          <input
            id="sequence"
            v-model.number="newApprover.sequence"
            type="number"
            min="1"
            required
            class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md"
          />
        </div>
        <button
          type="submit"
          :disabled="adding"
          class="px-4 py-2 bg-green-500 text-white rounded-md hover:bg-green-600 disabled:opacity-50"
        >
          {{ adding ? 'Adding...' : 'Add Approver' }}
        </button>
      </form>
    </div> -->

    <div v-if="error" class="mt-4 text-red-500">{{ error }}</div>
  </div>
</template>

<script setup lang="ts">
definePageMeta({
  middleware: "auth",
  alias: "/policies/:id/approvers"
})

const route = useRoute()
const policyId = parseInt(route.params.id as string)

const approvers = ref([])
const loading = ref(true)
const adding = ref(false)
const error = ref('')

const newApprover = reactive({
  approverid: null,
  name: '',
  email: '',
  type: '',
  sequence: 1
})

const fetchApprovers = async () => {
  try {
    const response:any = await $fetch(`/api/Policy/${policyId}/approvers`)
    if (response) {
      approvers.value = response.items
    }
  } catch (err) {
    error.value = 'Failed to load approvers'
    console.error(err)
  } finally {
    loading.value = false
  }
}

const addApprover = async () => {
  adding.value = true
  error.value = ''
  try {
    const response:any = await $fetch(`/api/Policy/${policyId}/approvers`, {
      method: 'POST',
      body: {
        Policyid: policyId,
        Approverid: newApprover.approverid,
        Approvername: newApprover.name,
        Approveremail: newApprover.email,
        Approvertype: newApprover.type,
        Linesequence: newApprover.sequence
      }
    })
    if (response) {
      newApprover.name = ''
      newApprover.email = ''
      newApprover.type = ''
      newApprover.sequence = 1
      await fetchApprovers()
    } else {
      error.value = response.message || 'Failed to add approver'
    }
  } catch (err) {
    error.value = 'An error occurred'
    console.error(err)
  } finally {
    adding.value = false
  }
}


onMounted(() => {
  fetchApprovers()
})
</script>