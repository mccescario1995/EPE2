<template>
  <div class="p-6 max-w-4xl mx-auto">
    <header class="mb-8">
      <h1 class="text-3xl font-bold text-gray-900 mb-2">Dropdown Component</h1>
      <p class="text-lg text-gray-600">
        A versatile dropdown/select component that supports single selection, multiple selection, and cherry-picking modes.
      </p>
    </header>

    <section class="mb-12">
      <h2 class="text-2xl font-semibold text-gray-800 mb-4">Features</h2>
      <ul class="list-disc pl-6 space-y-2 text-gray-700">
        <li>Supports three selection modes: single, multiple, and cherry-pick</li>
        <li>Returns full selected objects (not just IDs)</li>
        <li>Searchable option for large lists</li>
        <li>Disabled state support</li>
        <li>Keyboard accessible</li>
        <li>Customizable placeholder text</li>
        <li>Clear selection option</li>
        <li>Disabled items support</li>
      </ul>
    </section>

    <section class="mb-12">
      <h2 class="text-2xl font-semibold text-gray-800 mb-4">Props</h2>
      <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200">
          <thead class="bg-gray-50">
            <tr>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Prop</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Type</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Default</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Description</th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-gray-200">
            <tr>
              <td class="px-6 py-4 whitespace-nowrap text-sm font-mono text-gray-900">items</td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">DropdownItem[]</td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">[]</td>
              <td class="px-6 py-4 text-sm text-gray-500">Array of items to display in the dropdown</td>
            </tr>
            <tr>
              <td class="px-6 py-4 whitespace-nowrap text-sm font-mono text-gray-900">modelValue</td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">DropdownItem | DropdownItem[] | null</td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">null</td>
              <td class="px-6 py-4 text-sm text-gray-500">Currently selected item(s)</td>
            </tr>
            <tr>
              <td class="px-6 py-4 whitespace-nowrap text-sm font-mono text-gray-900">placeholder</td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">string</td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">'Select an option'</td>
              <td class="px-6 py-4 text-sm text-gray-500">Placeholder text when nothing is selected</td>
            </tr>
            <tr>
              <td class="px-6 py-4 whitespace-nowrap text-sm font-mono text-gray-900">mode</td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">'single' | 'multiple' </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">'single'</td>
              <td class="px-6 py-4 text-sm text-gray-500">Selection behavior mode</td>
            </tr>
            <tr>
              <td class="px-6 py-4 whitespace-nowrap text-sm font-mono text-gray-900">searchable</td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">boolean</td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">false</td>
              <td class="px-6 py-4 text-sm text-gray-500">Whether to show search input</td>
            </tr>
            <tr>
              <td class="px-6 py-4 whitespace-nowrap text-sm font-mono text-gray-900">disabled</td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">boolean</td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">false</td>
              <td class="px-6 py-4 text-sm text-gray-500">Whether the dropdown is disabled</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <section class="mb-12">
      <h2 class="text-2xl font-semibold text-gray-800 mb-4">Emits</h2>
      <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200">
          <thead class="bg-gray-50">
            <tr>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Event</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Value</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Description</th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-gray-200">
            <tr>
              <td class="px-6 py-4 whitespace-nowrap text-sm font-mono text-gray-900">update:modelValue</td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">DropdownItem | DropdownItem[] | null</td>
              <td class="px-6 py-4 text-sm text-gray-500">Emits when selection changes (for v-model)</td>
            </tr>
            <tr>
              <td class="px-6 py-4 whitespace-nowrap text-sm font-mono text-gray-900">change</td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">DropdownItem | DropdownItem[] | null</td>
              <td class="px-6 py-4 text-sm text-gray-500">Emits when selection changes</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <section class="mb-12">
      <h2 class="text-2xl font-semibold text-gray-800 mb-4">Usage Examples</h2>

      <div class="space-y-8">
        <div class="p-6 border border-gray-200 rounded-lg">
          <h3 class="text-xl font-medium text-gray-800 mb-4">Single Selection</h3>
          <div class="flex flex-col md:flex-row gap-8">
            <div class="flex-1">
              <Dropdown
                v-model="selectedUser"
                :initial-items="users"
                mode="single"
                placeholder="Select a user"
                class="mb-4"
              />
              <div v-if="selectedUser" class="p-4 bg-gray-50 rounded">
                <p class="font-medium">Selected User:</p>
                <pre class="text-sm mt-2">{{ selectedUser }}</pre>
              </div>
            </div>
            <div class="flex-1">
              <pre class="text-sm bg-gray-800 text-gray-100 p-4 rounded overflow-x-auto"><code>{{ singleSelectionCode }}</code></pre>
            </div>
          </div>
        </div>

        <div class="p-6 border border-gray-200 rounded-lg">
          <h3 class="text-xl font-medium text-gray-800 mb-4">Multiple Selection</h3>
          <div class="flex flex-col md:flex-row gap-8">
            <div class="flex-1">
              <Dropdown
                v-model="selectedProducts"
                :initial-items="products"
                :multiple="true"
                placeholder="Select products"
                class="mb-4"
              />
              <div v-if="selectedProducts.length" class="p-4 bg-gray-50 rounded">
                <p class="font-medium">Selected Products:</p>
                <pre class="text-sm mt-2">{{ selectedProducts }}</pre>
              </div>
            </div>
            <div class="flex-1">
              <pre class="text-sm bg-gray-800 text-gray-100 p-4 rounded overflow-x-auto"><code>{{ multipleSelectionCode }}</code></pre>
            </div>
          </div>
        </div>

        <div class="p-6 border border-gray-200 rounded-lg">
          <h3 class="text-xl font-medium text-gray-800 mb-4">Searchable Dropdown</h3>
          <div class="flex flex-col md:flex-row gap-8">
            <div class="flex-1">
              <Dropdown
                v-model="selectedCountry"
                :initial-items="countries"
                mode="single"
                placeholder="Search for a country"
                searchable
                class="mb-4"
              />
              <div v-if="selectedCountry" class="p-4 bg-gray-50 rounded">
                <p class="font-medium">Selected Country:</p>
                <pre class="text-sm mt-2">{{ selectedCountry }}</pre>
              </div>
            </div>
            <div class="flex-1">
              <pre class="text-sm bg-gray-800 text-gray-100 p-4 rounded overflow-x-auto"><code>{{ searchableCode }}</code></pre>
            </div>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'

// Example data
const users = [
  { id: 1, label: 'John Doe', email: 'john@example.com', role: 'admin' },
  { id: 2, label: 'Jane Smith', email: 'jane@example.com', role: 'user' },
  { id: 3, label: 'Bob Johnson', email: 'bob@example.com', role: 'user', disabled: true }
]

const products = [
  { id: 'p1', label: 'Laptop', price: 999, category: 'electronics' },
  { id: 'p2', label: 'Phone', price: 699, category: 'electronics' },
  { id: 'p3', label: 'Headphones', price: 199, category: 'electronics' }
]

const countries = [
  { id: 'us', label: 'United States', code: 'US' },
  { id: 'ca', label: 'Canada', code: 'CA' },
  { id: 'mx', label: 'Mexico', code: 'MX' },
  { id: 'br', label: 'Brazil', code: 'BR' },
  { id: 'uk', label: 'United Kingdom', code: 'UK' }
]

// Example state
const selectedUser = ref(null)
const selectedProducts = ref([])
const selectedCountry = ref(null)
const selectedColor = ref(null)

// Example code snippets
const singleSelectionCode = `<template>
  <Dropdown
    v-model="selectedUser"
    :items="users"
    mode="single"
    placeholder="Select a user"
  />
</template>

<script setup>
const users = [
  { id: 1, label: 'John Doe', email: 'john@example.com' },
  { id: 2, label: 'Jane Smith', email: 'jane@example.com' }
]

const selectedUser = ref(null)
<\/script>`

const multipleSelectionCode = `<template>
  <Dropdown
    v-model="selectedProducts"
    :items="products"
    mode="multiple"
    placeholder="Select products"
  />
</template>

<script setup>
const products = [
  { id: 'p1', label: 'Laptop', price: 999 },
  { id: 'p2', label: 'Phone', price: 699 }
]

const selectedProducts = ref([])
<\/script>`

const searchableCode = `<template>
  <Dropdown
    v-model="selectedCountry"
    :items="countries"
    mode="single"
    placeholder="Search for a country"
    searchable
  />
</template>

<script setup>
const countries = [
  { id: 'us', label: 'United States' },
  { id: 'ca', label: 'Canada' }
]

const selectedCountry = ref(null)
<\/script>`

const cherryPickCode = `<template>
  <Dropdown
    v-model="selectedColor"
    :items="colors"
    mode="cherry"
    placeholder="Pick a color"
  />
</template>

<script setup>
const colors = [
  { id: 'red', name: 'Red', hex: '#ff0000' },
  { id: 'green', name: 'Green', hex: '#00ff00' }
]

const selectedColor = ref(null)
<\/script>`
</script>

<style scoped>
pre {
  white-space: pre-wrap;
  word-wrap: break-word;
}
</style>