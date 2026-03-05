<template>
  <div class="p-8 max-w-7xl mx-auto">
    <h1 class="text-3xl font-bold mb-6">BaseTable Component</h1>

    <section class="mb-12">
      <h2 class="text-2xl font-semibold mb-4">Introduction</h2>
      <p class="mb-4">
        BaseTable is a versatile and feature-rich table component for displaying
        tabular data with sorting, filtering, pagination, and custom actions.
      </p>

      <div class="bg-blue-50 border-l-4 border-blue-500 p-4 mb-6">
        <h3 class="font-bold mb-2">Key Features:</h3>
        <ul class="list-disc pl-5 space-y-1">
          <li>Sortable columns (ascending/descending)</li>
          <li>Global and column-specific filtering</li>
          <li>Client-side and server-side pagination</li>
          <li>Customizable date formatting</li>
          <li>Badge displays for status fields</li>
          <li>Action buttons with icons and tooltips</li>
          <li>Responsive design</li>
        </ul>
      </div>
    </section>

    <!-- Basic Usage Example -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold mb-4">Basic Usage</h2>
      <p class="mb-4">
        The simplest way to use BaseTable with default settings:
      </p>

      <div class="bg-gray-50 p-4 rounded-lg mb-4">
        <h4 class="font-mono text-sm mb-2">Example Code:</h4>
        <pre
          class="bg-gray-800 text-gray-100 p-4 rounded text-sm overflow-x-auto"
        >
&lt;base-table :table-data="users" /&gt;</pre
        >
      </div>

      <div class="border rounded-lg overflow-hidden shadow-sm">
        <base-table :table-data="basicExampleData" />
      </div>
    </section>

    <!-- Sorting Example -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold mb-4">Sorting</h2>
      <p class="mb-4">Enable sorting on specific columns:</p>

      <div class="bg-gray-50 p-4 rounded-lg mb-4">
        <h4 class="font-mono text-sm mb-2">Example Code:</h4>
        <pre
          class="bg-gray-800 text-gray-100 p-4 rounded text-sm overflow-x-auto"
        >
&lt;base-table 
  :table-data="products"
  :sortable-columns="['name', 'price', 'stock']"
/&gt;</pre
        >
      </div>

      <div class="border rounded-lg overflow-hidden shadow-sm">
        <base-table
          :table-data="sortingExampleData"
          :sortable-columns="['name', 'price', 'inStock']"
        />
      </div>
    </section>

    <!-- Filtering Example -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold mb-4">Filtering</h2>
      <p class="mb-4">Add global search and column filters:</p>

      <div class="bg-gray-50 p-4 rounded-lg mb-4">
        <h4 class="font-mono text-sm mb-2">Example Code:</h4>
        <pre
          class="bg-gray-800 text-gray-100 p-4 rounded text-sm overflow-x-auto"
        >
&lt;base-table 
  :table-data="orders"
  :show-search="true"
  :filter="true"
  :filterable-columns="['status', 'customer']"
/&gt;</pre
        >
      </div>

      <div class="border rounded-lg overflow-hidden shadow-sm">
        <base-table
          :table-data="filteringExampleData"
          :show-search="true"
          :filter="true"
          :filterable-columns="['status', 'customer']"
        />
      </div>
    </section>

    <!-- Pagination Example -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold mb-4">Pagination</h2>
      <p class="mb-4">Control how many items display per page:</p>

      <div class="bg-gray-50 p-4 rounded-lg mb-4">
        <h4 class="font-mono text-sm mb-2">Example Code:</h4>
        <pre
          class="bg-gray-800 text-gray-100 p-4 rounded text-sm overflow-x-auto"
        >
&lt;base-table 
  :table-data="largeDataset"
  :pagination="true"
  :per-page="5"
  :total-items="paginationExampleData.length"
/&gt;</pre
        >
      </div>

      <div class="border rounded-lg overflow-hidden shadow-sm">
        <base-table
          :table-data="paginationExampleData"
          :pagination="true"
          :per-page="5"
          :total-items="paginationExampleData.length"
        />
      </div>
    </section>

    <!-- Status Badges Example -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold mb-4">Status Badges</h2>
      <p class="mb-4">Display status fields as colored badges:</p>

      <div class="bg-gray-50 p-4 rounded-lg mb-4">
        <h4 class="font-mono text-sm mb-2">Example Code:</h4>
        <pre
          class="bg-gray-800 text-gray-100 p-4 rounded text-sm overflow-x-auto"
        >
&lt;base-table 
  :table-data="tasks"
  :badgeFields="['status', 'priority']"
  :fieldMappings="{
    status: {
      0: 'Pending',
      1: 'In Progress',
      2: 'Completed'
    },
    priority: {
      0: 'Low',
      1: 'Medium',
      2: 'High'
    }
  }"
  :fieldColors="{
    status: {
      0: 'bg-yellow-100 text-yellow-800',
      1: 'bg-blue-100 text-blue-800',
      2: 'bg-green-100 text-green-800'
    },
    priority: {
      0: 'bg-gray-100 text-gray-800',
      1: 'bg-orange-100 text-orange-800',
      2: 'bg-red-100 text-red-800'
    }
  }"
/&gt;</pre
        >
      </div>

      <div class="border rounded-lg overflow-hidden shadow-sm">
        <base-table
          :table-data="badgesExampleData"
          :badgeFields="['status', 'priority']"
          :fieldMappings="{
            status: {
              0: 'Pending',
              1: 'In Progress',
              2: 'Completed',
            },
            priority: {
              0: 'Low',
              1: 'Medium',
              2: 'High',
            },
          }"
          :fieldColors="{
            status: {
              0: 'bg-yellow-100 text-yellow-800',
              1: 'bg-blue-100 text-blue-800',
              2: 'bg-green-100 text-green-800',
            },
            priority: {
              0: 'bg-gray-100 text-gray-800',
              1: 'bg-orange-100 text-orange-800',
              2: 'bg-red-100 text-red-800',
            },
          }"
        />
      </div>
    </section>

    <!-- Actions Example -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold mb-4">Action Buttons</h2>
      <p class="mb-4">Add custom action buttons to each row:</p>

      <div class="bg-gray-50 p-4 rounded-lg mb-4">
        <h4 class="font-mono text-sm mb-2">Example Code:</h4>
        <pre
          class="bg-gray-800 text-gray-100 p-4 rounded text-sm overflow-x-auto"
        >
&lt;base-table
  :table-data="products"
  :actions="[
    {
      label: IconEdit,
      tooltip: 'Edit Product',
      handler: editProduct,
      class: 'hover:bg-blue-500',
      iconClass: 'text-blue-500'
    },
    {
      label: IconDelete,
      tooltip: 'Delete Product',
      handler: deleteProduct,
      class: 'hover:bg-red-500',
      iconClass: 'text-red-500',
      disabled: (row) => row.status === 'published'
    }
  ]"
/&gt;</pre
        >
      </div>

      <div class="border rounded-lg overflow-hidden shadow-sm">
        <base-table
          :table-data="actionsExampleData"
          :actions="[
            {
              label: IconView,
              tooltip: 'View Details',
              handler: viewDetails,
              class: 'hover:bg-indigo-500',
              iconClass: 'text-indigo-500',
            },
            {
              label: IconEdit,
              tooltip: 'Edit Item',
              handler: editItem,
              class: 'hover:bg-blue-500',
              iconClass: 'text-blue-500',
            },
          ]"
        />
      </div>
    </section>

    <!-- Props Reference -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold mb-4">Props Reference</h2>

      <div class="overflow-x-auto">
        <table class="min-w-full border">
          <thead class="bg-gray-50">
            <tr>
              <th class="px-4 py-2 text-left">Prop</th>
              <th class="px-4 py-2 text-left">Type</th>
              <th class="px-4 py-2 text-left">Default</th>
              <th class="px-4 py-2 text-left">Description</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-200">
            <tr v-for="prop in propsReference" :key="prop.name">
              <td class="px-4 py-2 font-mono text-sm">{{ prop.name }}</td>
              <td class="px-4 py-2">{{ prop.type }}</td>
              <td class="px-4 py-2 font-mono text-sm">{{ prop.default }}</td>
              <td class="px-4 py-2">{{ prop.description }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </div>
</template>

<script setup>
import { IconView, IconEdit, IconDelete } from "#components";

// Sample data for examples
const basicExampleData = [
  { id: 1, name: "John Doe", email: "john@example.com", role: "User" },
  { id: 2, name: "Jane Smith", email: "jane@example.com", role: "Admin" },
  { id: 3, name: "Bob Johnson", email: "bob@example.com", role: "User" },
];

const sortingExampleData = [
  { id: 1, name: "Laptop", price: 999.99, inStock: true },
  { id: 2, name: "Smartphone", price: 699.99, inStock: false },
  { id: 3, name: "Tablet", price: 399.99, inStock: true },
];

const filteringExampleData = [
  { id: 1, customer: "John Doe", status: "Pending", amount: 150.0 },
  { id: 2, customer: "Jane Smith", status: "Shipped", amount: 225.5 },
  { id: 3, customer: "Bob Johnson", status: "Delivered", amount: 99.99 },
  { id: 4, customer: "Bob Johnson", status: "Delivered", amount: 99.99 },
  { id: 5, customer: "Bob Johnson", status: "Delivered", amount: 99.99 },
  { id: 6, customer: "Bob Johnson", status: "Delivered", amount: 99.99 },
];

const paginationExampleData = [
  { id: 1, product: "Product 1", category: "Electronics", price: 199.99 },
  { id: 2, product: "Product 2", category: "Clothing", price: 49.99 },
  { id: 3, product: "Product 3", category: "Home", price: 129.99 },
  { id: 4, product: "Product 4", category: "Electronics", price: 299.99 },
  { id: 5, product: "Product 5", category: "Clothing", price: 39.99 },
  { id: 6, product: "Product 6", category: "Home", price: 89.99 },
];

const badgesExampleData = [
  { id: 1, task: "Design homepage", status: 1, priority: 2 },
  { id: 2, task: "API integration", status: 0, priority: 1 },
  { id: 3, task: "User testing", status: 2, priority: 0 },
];

const actionsExampleData = [
  { id: 1, item: "Document 1", type: "PDF", date: new Date() },
  { id: 2, item: "Spreadsheet 1", type: "Excel", date: new Date() },
  { id: 3, item: "Presentation 1", type: "PowerPoint", date: new Date() },
];

// Mock action handlers
const viewDetails = (row) => alert(`Viewing details for: ${row.item}`);
const editItem = (row) => alert(`Editing: ${row.item}`);

// Props reference data
const propsReference = [
  {
    name: "tableData",
    type: "Array",
    default: "[]",
    description: "The data to display in the table",
  },
  {
    name: "header",
    type: "Array",
    default: "[]",
    description: "Custom header labels (optional)",
  },
  {
    name: "hiddenFields",
    type: "Array",
    default: "[]",
    description: "Fields to hide from the table",
  },
  {
    name: "actions",
    type: "Array",
    default: "[]",
    description: "Action buttons configuration",
  },
  {
    name: "badgeFields",
    type: "Array",
    default: "[]",
    description: "Fields to display as badges",
  },
  {
    name: "fieldMappings",
    type: "Object",
    default: "{}",
    description: "Mapping for field values (e.g., status codes)",
  },
  {
    name: "fieldColors",
    type: "Object",
    default: "{}",
    description: "Color mappings for badge fields",
  },
  {
    name: "sortableColumns",
    type: 'Array | "all"',
    default: "[]",
    description: "Columns that should be sortable",
  },
  {
    name: "filterableColumns",
    type: 'Array | "all"',
    default: "[]",
    description: "Columns that should show filter controls",
  },
  {
    name: "pagination",
    type: "Boolean",
    default: "false",
    description: "Enable pagination",
  },
  {
    name: "perPage",
    type: "Number",
    default: "10",
    description: "Items per page",
  },
  {
    name: "showSearch",
    type: "Boolean",
    default: "false",
    description: "Show global search input",
  },
  {
    name: "filter",
    type: "Boolean",
    default: "false",
    description: "Enable column filtering",
  },
  {
    name: "dateFields",
    type: "Array",
    default: "[]",
    description: "Fields to format as dates",
  },
  {
    name: "dateFormat",
    type: "String",
    default: '"MMM d, yyyy"',
    description: "Format for date fields",
  },
];
</script>
