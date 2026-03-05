<template>
  <Dropdown
    v-model="selectedUser"
    :items="users"
    mode="single"
    placeholder="Select a user"
    container-class="max-w-xs"
    button-class="bg-indigo-100 text-indigo-800 border-indigo-300 hover:bg-indigo-200"
    dropdown-panel-class="border-2 border-indigo-300"
  />
  <div v-if="selectedUser">
    Selected: {{ selectedUser.name }} ({{ selectedUser.email }})
  </div>

  <Dropdown
    v-model="selectedProducts"
    :items="products"
    mode="multiple"
    placeholder="Select products"
    container-class="max-w-xs"
    button-class="bg-indigo-100 text-indigo-800 border-indigo-300 hover:bg-indigo-200"
    dropdown-panel-class="border-2 border-indigo-300"
  />
  <div v-if="selectedProducts.length">
    <h3>Selected Products:</h3>
    <ul>
      <li v-for="product in selectedProducts" :key="product.id">
        {{ product.label }} - ${{ product.price }}
      </li>
    </ul>
  </div>
</template>

<script setup>
const users = [
  { id: 1, label: "John Doe", email: "john@example.com", role: "admin" },
  { id: 2, label: "Jane Smith", email: "jane@example.com", role: "user" },
];

const selectedUser = ref(null);

const products = [
  { id: "p1", label: "Laptop", price: 999, category: "electronics" },
  { id: "p2", label: "Phone", price: 699, category: "electronics" },
];

const selectedProducts = ref([]);
</script>
