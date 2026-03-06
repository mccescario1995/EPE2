# BaseTable Component Guide

## Overview

The [`BaseTable`](app/components/BaseTable.vue) is a versatile, feature-rich table component for displaying tabular data. It supports:
- Sorting (ascending/descending)
- Global and column-specific filtering
- Client-side and server-side pagination
- Custom date formatting
- Badge displays for status fields
- Action buttons with icons and tooltips
- Responsive design (mobile card view)

---

## Basic Usage

The simplest way to use BaseTable with default settings:

```vue
<template>
  <BaseTable :table-data="users" />
</template>

<script setup>
const users = [
  { id: 1, name: "John Doe", email: "john@example.com", role: "User" },
  { id: 2, name: "Jane Smith", email: "jane@example.com", role: "Admin" },
  { id: 3, name: "Bob Johnson", email: "bob@example.com", role: "User" },
]
</script>
```

---

## Pagination Options

### 1. Client-Side Pagination

For smaller datasets where all data is loaded at once:

```vue
<template>
  <BaseTable
    :table-data="products"
    :pagination="true"
    :per-page="10"
    :total-items="products.length"
  />
</template>
```

**Key props:**
- [`pagination`](app/components/BaseTable.vue:505) - Enable pagination
- [`perPage`](app/components/BaseTable.vue:506) - Items per page (default: 10)
- [`totalItems`](app/components/BaseTable.vue:508) - Total count for pagination calculation
- [`showlimitfilter`](app/components/BaseTable.vue:522) - Allow users to change items per page
- [`limitfilter`](app/components/BaseTable.vue:521) - Array of options [5, 10, 25, 50]

### 2. Server-Side Pagination (with API)

For large datasets, use server-side pagination:

```vue
<template>
  <BaseTable
    :table-data="tableData"
    :pagination="true"
    :server-pagination="true"
    :per-page="10"
    :total-items="totalItems"
    :fetch-data="fetchTableData"
    :show-search="true"
    :filter="true"
    :filterable-columns="['status', 'category']"
    :sortable-columns="['name', 'createdAt']"
  />
</template>

<script setup lang="ts">
import { apiFetch } from '~/composables/useApiFetch'

const tableData = ref([])
const totalItems = ref(0)

const fetchTableData = async ({ 
  page, 
  limit, 
  search, 
  filters, 
  sortField, 
  sortDirection 
}) => {
  // Build query params
  const params = new URLSearchParams({
    page: String(page),
    limit: String(limit),
  })
  
  if (search) params.append('search', search)
  if (sortField) {
    params.append('sort_by', sortField)
    params.append('sort_dir', sortDirection)
  }
  
  // Add filters
  Object.entries(filters).forEach(([key, value]) => {
    if (value) params.append(key, value)
  })

  const response = await apiFetch(`/api/products?${params}`, {
    parseJson: true
  })

  // Adjust based on your API response structure
  return {
    data: response.data || response.items || [],
    total: response.total || response.meta?.total || 0
  }
}
</script>
```

**Server-side props:**
- [`serverPagination`](app/components/BaseTable.vue:507) - Enable server-side mode
- [`fetchData`](app/components/BaseTable.vue:509) - Function that receives pagination/sort/filter params

The [`fetchData`](app/components/BaseTable.vue:509) function receives:
```ts
{
  page: number,       // Current page (1-based)
  limit: number,      // Items per page
  search: string,     // Global search query
  filters: Record<string, string>,  // Column filters
  sortField?: string, // Field being sorted
  sortDirection?: 'asc' | 'desc'
}
```

And must return:
```ts
{
  data: Array<Record<string, any>>,
  total: number  // Total count for pagination
}
```

---

## Common Props Reference

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `tableData` | Array | [] | Data to display |
| `header` | Array | auto-detect | Custom column headers |
| `hiddenFields` | Array | [] | Fields to hide |
| `pagination` | Boolean | false | Enable pagination |
| `perPage` | Number | 10 | Items per page |
| `serverPagination` | Boolean | false | Use server-side pagination |
| `fetchData` | Function | - | API fetch function |
| `showSearch` | Boolean | false | Show global search |
| `filter` | Boolean | false | Enable column filters |
| `filterableColumns` | Array \| "all" | [] | Filterable columns |
| `sortableColumns` | Array \| "all" | [] | Sortable columns |
| `badgeFields` | Array | [] | Fields as status badges |
| `fieldMappings` | Object | {} | Value mappings |
| `fieldColors` | Object | {} | Badge colors |
| `dateFields` | Array | [] | Date fields to format |
| `dateFormat` | String | "MMM d, yyyy" | Date format |
| `actions` | Array | [] | Action buttons config |
| `groupActions` | Boolean | false | Group actions in dropdown |
| `showlimitfilter` | Boolean | false | Show per-page dropdown |
| `limitfilter` | Number[] | [10,25,50] | Per-page options |

---

## Status Badges Example

```vue
<template>
  <BaseTable
    :table-data="tasks"
    :badge-fields="['status', 'priority']"
    :field-mappings="{
      status: { 0: 'Pending', 1: 'In Progress', 2: 'Completed' },
      priority: { 0: 'Low', 1: 'Medium', 2: 'High' }
    }"
    :field-colors="{
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
  />
</template>
```

---

## Action Buttons Example

```vue
<template>
  <BaseTable
    :table-data="products"
    :actions="[
      {
        label: IconView,
        tooltip: 'View Details',
        handler: viewProduct,
        iconClass: 'text-indigo-500'
      },
      {
        label: IconEdit,
        tooltip: 'Edit Product',
        handler: editProduct,
        iconClass: 'text-blue-500'
      },
      {
        label: IconDelete,
        tooltip: 'Delete Product',
        handler: deleteProduct,
        iconClass: 'text-red-500',
        disabled: (row) => row.status === 'published' // Optional disable logic
      }
    ]"
  />
</template>

<script setup>
import { IconView, IconEdit, IconDelete } from '#components'

const viewProduct = (row) => console.log('View:', row)
const editProduct = (row) => console.log('Edit:', row)
const deleteProduct = (row) => console.log('Delete:', row)
</script>
```

---

## Using with API - Complete Example

Here's a complete page component that fetches data from an API with server-side pagination:

```vue
<template>
  <div class="p-6">
    <h1 class="text-2xl font-bold mb-6">Products</h1>
    
    <BaseTable
      :table-data="products"
      :pagination="true"
      :server-pagination="true"
      :per-page="perPage"
      :total-items="totalProducts"
      :fetch-data="fetchProducts"
      :show-search="true"
      :filter="true"
      :filterable-columns="['status', 'category']"
      :sortable-columns="['name', 'price', 'createdAt']"
      :show-limit-filter="true"
      :limit-filter="[5, 10, 25, 50]"
      :header="['name', 'category', 'price', 'status', 'createdAt']"
      :badge-fields="['status']"
      :field-mappings="{
        status: { 1: 'Active', 0: 'Inactive' }
      }"
      :date-fields="['createdAt']"
      :date-format="'MMM d, yyyy HH:mm'"
      :actions="[
        {
          label: 'View',
          tooltip: 'View Product',
          handler: viewProduct
        }
      ]"
    />
  </div>
</template>

<script setup lang="ts">
import { apiFetch } from '~/composables/useApiFetch'

const products = ref([])
const perPage = ref(10)
const totalProducts = ref(0)

const fetchProducts = async ({ page, limit, search, filters, sortField, sortDirection }) => {
  const queryParams = new URLSearchParams({
    page: String(page),
    limit: String(limit),
  })
  
  if (search) queryParams.append('search', search)
  if (sortField) {
    queryParams.append('sort_by', sortField)
    queryParams.append('sort_dir', sortDirection)
  }
  
  // Add filters
  Object.entries(filters).forEach(([key, value]) => {
    if (value) queryParams.append(key, value)
  })

  try {
    const response = await apiFetch(`/api/products?${queryParams}`, {
      parseJson: true
    })
    
    products.value = response.data || []
    totalProducts.value = response.total || 0
    
    return {
      data: products.value,
      total: totalProducts.value
    }
  } catch (error) {
    console.error('Failed to fetch products:', error)
    return { data: [], total: 0 }
  }
}

const viewProduct = (row) => {
  // Navigate to product detail page
  navigateTo(`/products/${row.id}`)
}
</script>
```

---

## API Response Format Expectation

Your API should return data in this format:

```json
{
  "data": [
    { "id": 1, "name": "Product 1", "price": 99.99, "status": 1, "createdAt": "2024-01-15T10:30:00Z" }
  ],
  "total": 150,
  "meta": {
    "current_page": 1,
    "per_page": 10,
    "total": 150
  }
}
```

Or configure the return in your fetch function to match:

```js
return {
  data: response.data || [],
  total: response.total || response.meta?.total || 0
}
```

---

## Events

The component emits these events:

| Event | Payload | Description |
|-------|---------|-------------|
| `page-change` | `(page: number, limit: number)` | Page changed |
| `on-filtersearch` | `(page, limit, search?)` | Filter or search triggered |
| `on-drop` | `(draggedRow, targetRow)` | Rows reordered (drag & drop) |

---

## Documentation

For more examples, see [`app/pages/documentation/table.vue`](app/pages/documentation/table.vue)
