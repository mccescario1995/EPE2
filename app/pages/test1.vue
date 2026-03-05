<script setup lang="ts">
import { ref } from 'vue'
import { useAuth } from '~/composables/useAuth'

const email = ref('')
const password = ref('')
const { login } = useAuth()

async function submit() {
  try {
    await login(email.value, password.value)
    // redirect
  } catch (err) {
    alert('Invalid credentials')
  }
}
</script>

<template>
  <form @submit.prevent="submit">
    <input class="border bg-gray-50" v-model="email" />
    <input class="border bg-gray-50" type="password" v-model="password" />
    <button type="submit">Login</button>
  </form>
</template>
