<template>
  <Button v-if="props.showExport" variant="ghost" size="sm" icon="download" @onclick="openExportModal"
    class="border hover:bg-gray-300 mb-3 dark:bg-gray-700 dark:text-gray-200 dark:hover:bg-gray-600">
    Export Excel
  </Button>

  <div v-if="showExportModal" class="fixed inset-0 z-50 flex items-center justify-center dark:border-gray-50"
    style="background-color: rgba(0, 0, 0, 0.5)" @click.self="closeExportModal">
    <div class="bg-white rounded-xl shadow-xl p-6 w-full max-w-md mx-4 dark:bg-gray-800 dark:*:text-gray-200">
      <h3 class="text-lg font-semibold text-gray-800 mb-4">Export Data</h3>
      <div class="space-y-4">
        <div class="flex flex-col">
          <label class="text-sm font-medium text-gray-700 mb-1 dark:text-gray-300">From Date</label>
          <input type="date" v-model="exportStartDate"
            class="px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:bg-gray-700 dark:text-gray-200 dark:border-gray-600" />
        </div>
        <div class="flex flex-col">
          <label class="text-sm font-medium text-gray-700 mb-1 dark:text-gray-300">To Date</label>
          <input type="date" v-model="exportEndDate"
            class="px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:bg-gray-700 dark:text-gray-200 dark:border-gray-600" />
        </div>
      </div>
      <div class="flex justify-end gap-3 mt-6">
        <button @click="closeExportModal"
          class="px-4 py-2 text-sm text-gray-600 hover:text-gray-800 dark:text-gray-300 dark:hover:text-gray-100 dark:border-gray-600 dark:hover:bg-gray-700 rounded-lg transition-all duration-200">
          Cancel
        </button>
        <Button variant="primary" size="sm" @onclick="confirmExport"
          class="px-4 py-2 text-sm bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 dark:bg-indigo-500 dark:hover:bg-indigo-600 dark:text-gray-200 transition-all duration-200">
          Export
        </Button>
      </div>
    </div>
  </div>

  <div v-if="showFilterControls || props.showlimitfilter || props.showSearch"
    class="flex flex-col lg:flex-row lg:justify-between lg:items-center gap-4 mb-2">
    <!-- Dynamic Filter Controls -->
    <div v-if="showFilterControls" class="flex flex-wrap gap-3">
      <div v-for="key in filterableColumns" :key="key"
        class="flex items-center bg-white rounded-xl border border-gray-200 px-4 py-2 shadow-sm hover:shadow-md transition-shadow duration-200">
        <label class="mr-3 text-sm font-medium text-gray-700 whitespace-nowrap">
          {{ formatHeader(key) }}:
        </label>
        <select v-model="dynamicFilters[key]"
          class="bg-transparent border-0 text-sm font-medium text-gray-600 focus:outline-none focus:text-gray-900 cursor-pointer min-w-20"
          @change="applyDynamicFilters">
          <option value="">All</option>
          <option v-for="option in getFilterOptions(key)" :value="option" class="py-2">
            {{ option }}
          </option>
        </select>
      </div>
    </div>

    <!-- Items per page -->
    <div v-if="props.showlimitfilter" class="flex items-center">
      <div
        class="flex items-center bg-white rounded-xl border border-gray-200 dark:border-gray-600 dark:bg-gray-800 px-4 py-2 shadow-sm hover:shadow-md transition-shadow duration-200">
        <label class="mr-3 text-sm font-medium text-gray-700 whitespace-nowrap dark:text-gray-200">
          Items per page:
        </label>
        <select v-model="limit"
          class="bg-transparent border-0 text-sm font-medium text-gray-600 focus:outline-none f                                                                                                                                                                                       ocus:text-gray-900 cursor-pointer min-w-[60px] dark:text-gray-200 dark:bg-gray-800 dark:border-gray-600"
          @change="handleLimitChange">
          <option v-for="option in props.limitfilter" :value="option" class="py-2 dark:text-gray-200 dark:bg-gray-700">
            {{ option }}
          </option>
        </select>
      </div>
    </div>

    <div v-if="props.policyFilterItems?.length" class="flex items-center">
      <button type="button"
        class="flex items-center gap-2 bg-white rounded-xl border border-gray-200 dark:border-gray-600 dark:bg-gray-800 px-4 py-2 shadow-sm hover:shadow-md transition-shadow duration-200"
        @click="openPolicyFilterModal">
        <span class="text-sm font-medium text-gray-700 dark:text-gray-200">
          {{ props.policyFilterLabel || 'Policy' }}:
        </span>
        <span class="text-sm text-gray-500 dark:text-gray-400">
          {{ selectedPolicyObjects.length ? `${selectedPolicyObjects.length} selected` : 'All' }}
        </span>
      </button>
    </div>

    <Teleport to="body">
      <div v-if="showPolicyFilterModal" class="fixed inset-0 z-50 overflow-y-auto">
        <div class="flex min-h-full items-center justify-center p-4">
          <div class="fixed inset-0 bg-black opacity-50 transition-opacity" @click="closePolicyFilterModal"></div>
          <div class="relative bg-white dark:bg-gray-800 rounded-lg shadow-xl max-w-md w-full p-6">
            <h3 class="text-lg font-medium text-gray-900 dark:text-gray-200 text-center mb-4">
              {{ props.policyFilterLabel || 'Policy' }}
            </h3>
            <!-- Search Input -->
            <div class="mb-4">
              <div class="relative">
                <input type="text" ref="policySearchInput"
                  :value="policySearchTerm"
                  class="block w-full px-4 py-2 text-sm border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 dark:bg-gray-700 dark:border-gray-600 dark:text-gray-200 dark:placeholder-gray-400"
                  placeholder="Search policies..." />
                <button type="button" @click="updateSearchTerm"
                  class="absolute right-2 top-1/2 transform -translate-y-1/2 bg-indigo-600 text-white px-3 py-1 rounded-md text-sm hover:bg-indigo-700 dark:bg-indigo-500 dark:hover:bg-indigo-600">
                  Search
                </button>
              </div>
            </div>
            <!-- Policy Items List -->
            <div class="max-h-64 overflow-y-auto space-y-2 mb-6">
              <label v-for="item in filteredPolicyItems" :key="item.id"
                class="flex items-center gap-3 p-2 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 cursor-pointer">
                <input type="checkbox" :value="item.id" :checked="tempPolicyObjects.some(
                  (p: any) => String(p.id) === String(item.id)
                )
                  " @click="togglePolicySelection(item)"
                  class="h-4 w-4 text-blue-600 rounded focus:ring-blue-500 dark:text-gray-700" />
                <span class="text-sm text-gray-700 dark:text-gray-200">{{ item.label }}</span>
              </label>
            </div>
            <div class="flex gap-3 justify-center">
              <button @click="clearPolicySelection"
                class="px-4 py-2 text-sm text-gray-600 hover:text-gray-800 dark:text-gray-300 dark:hover:text-gray-100 dark:border-gray-600 dark:hover:bg-gray-700 rounded-lg transition-all duration-200">
                Clear
              </button>
              <button @click="applyPolicyFilter"
                class="px-4 py-2 text-sm bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 dark:bg-indigo-500 dark:hover:bg-indigo-600 dark:text-gray-200 transition-all duration-200">
                Apply
              </button>
            </div>
          </div>
        </div>
      </div>
    </Teleport>

    <div v-if="props.showSearch" class="flex">
      <div
        class="relative text-gray-400 focus-within:text-indigo-600 flex-1 bg-white dark:text-gray-200 dark:placeholder-gray-400 dark:border-gray-600 dark:focus-within:text-gray-100 dark:rounded-l-full">
        <div class="absolute inset-y-0 left-0 flex items-center pl-4 pointer-events-none">
          <svg class="w-5 h-5 transition-colors duration-200" viewBox="0 0 20 20" fill="none"
            xmlns="http://www.w3.org/2000/svg">
            <path
              d="M17.5 17.5L15.4167 15.4167M15.8333 9.16667C15.8333 5.48477 12.8486 2.5 9.16667 2.5C5.48477 2.5 2.5 5.48477 2.5 9.16667C2.5 12.8486 5.48477 15.8333 9.16667 15.8333C11.0005 15.8333 12.6614 15.0929 13.8667 13.8947C15.0814 12.6872 15.8333 11.0147 15.8333 9.16667Z"
              stroke="currentColor" stroke-width="1.6" stroke-linecap="round" />
          </svg>
        </div>
        <input type="text" id="default-search"
          class="block bg-white w-full h-12 pr-5 pl-12 py-3 text-base font-medium shadow-sm text-gray-900 border border-gray-200 rounded-l-xl placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition-all duration-200 dark:bg-gray-800 dark:text-gray-200 dark:border-gray-600 dark:placeholder-gray-400 dark:focus:ring-indigo-500 dark:focus:border-transparent"
          v-if="props.showSearch" v-model="globalSearch" placeholder="Search across all data..."
          @keyup.enter="handleGlobalSearch" />
        <!-- @input="handleGlobalSearch" -->
      </div>
      <Button variant="primary" size="sm"
        custom-class="rounded-r-xl rounded-l-none h-12 px-6 bg-linear-to-r from-indigo-600 to-indigo-600  border-0 shadow-sm font-medium transition-all duration-200 dark:bg-indigo-500 dark:text-gray-200"
        @onclick="handleGlobalSearch">
        <div class="flex items-center">
          <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          Search
        </div>
      </Button>
    </div>
  </div>



  <!-- Table -->
  <div class="hidden md:block" :class="[
    'overflow-hidden rounded-2xl border border-gray-200 shadow-lg bg-white dark:bg-gray-800 dark:border-gray-700 dark:text-gray-200',
    tableClass,
  ]">
    <div class="overflow-x-auto">
      <table class="min-w-full divide-y divide-gray-100 dark:*:divide-gray-700">
        <!-- Table Headers -->
        <thead class="bg-linear-to-r from-gray-50 to-gray-100 dark:bg-gray-700 dark:text-gray-200">
          <tr>
            <th v-for="key in resolvedHeaders" :key="key"
              class="px-6 py-4 text-left text-xs font-bold text-gray-600 uppercase tracking-wider border-b border-gray-200 dark:bg-gray-700 dark:text-gray-200">
              <div class="flex flex-col gap-2">
                <div class="flex items-center justify-between">
                  <span class="flex-1">{{ formatHeader(key) }}</span>
                  <!-- Sort button -->
                  <button v-if="isColumnSortable(key)" @click="toggleSort(key)"
                    class="ml-2 p-1.5 text-gray-400 hover:text-indigo-600 hover:bg-indigo-50/50 rounded-lg transition-all duration-200 dark:text-gray-400 dark:hover:text-indigo-400 dark:hover:bg-indigo-600/50">
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none"
                      stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"
                      class="transition-transform duration-200" :class="{ 'text-indigo-600': sortField === key }">
                      <path v-if="sortField === key && sortDirection === 'asc'" d="M12 19V5M5 12l7-7 7 7" />
                      <path v-if="sortField === key && sortDirection === 'desc'" d="M12 5v14M19 12l-7 7-7-7" />
                      <path v-if="sortField !== key" d="M8 9l4-4 4 4M16 15l-4 4-4-4" />
                    </svg>
                  </button>
                </div>
                <!-- Column filter input -->
                <input v-model="columnFilters[key]" v-if="filter && isColumnFilterable(key)" type="text"
                  class="px-3 py-2 text-xs bg-white border border-gray-200 rounded-lg placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition-all duration-200"
                  placeholder="Filter..." @input="handleColumnFilter(key)" />
              </div>
            </th>
            <th v-if="actions?.length"
              class="px-6 py-4 text-left text-xs font-bold text-gray-600 uppercase tracking-wider border-b border-gray-200 dark:bg-gray-700 dark:text-gray-200">
              Actions
            </th>
          </tr>
        </thead>

        <!-- Table Body -->
        <tbody class="bg-white divide-y divide-gray-50 dark:*:divide-gray-700 dark:bg-gray-800">
          <tr v-for="(row, rowIndex) in paginatedData" :key="'row-' + rowIndex" :draggable="draggable"
            @dragstart="onDragStart(rowIndex)" @dragover="onDragOver(rowIndex, $event)" @drop="onDrop"
            @dragleave="onDragLeave" :class="[
              'transition-all duration-200 hover:bg-gray-50 dark:hover:bg-gray-700',
              targetIndex === rowIndex
                ? 'ring-2 ring-indigo-500 bg-indigo-100 '
                : '',
              draggingIndex === rowIndex ? 'opacity-50 scale-[0.98]' : '',
            ]">
            <td v-for="(key, colIndex) in resolvedHeaders" :key="'col-' + colIndex"
              class="px-6 py-4 text-sm text-gray-700 font-medium dark:text-gray-200 dark:border-gray-700">
              <!-- Badge display -->
              <StatusBadge v-if="props.badgeFields?.includes(key)" :value="row[key]" :field="key"
                :field-mappings="props.fieldMappings" :field-colors="props.fieldColors" />
              <div v-else-if="props.dateTimeFields?.includes(key)" class="flex flex-col"
                :title="formatValue(key, row[key])">
                <span>{{ getDateTimeParts(key, row[key])?.date }}</span>
                <span class="text-gray-500  dark:text-gray-400 text-sm">{{
                  getDateTimeParts(key, row[key])?.time
                }}</span>
              </div>
              <span v-else class="block truncate max-w-xs" :title="formatValue(key, row[key])">
                {{ formatValue(key, row[key]) }}
              </span>
            </td>

            <!-- Action buttons column -->
            <td v-if="hasActions" class="px-6 py-4 whitespace-nowrap text-sm text-gray-700 dark:text-gray-200">
              <!-- Grouped actions (dropdown style) -->
              <div v-if="props.groupActions" class="relative">
                <button
                  class="inline-flex items-center justify-center w-8 h-8 text-gray-500 bg-white border border-gray-200 rounded-full hover:bg-gray-50 hover:text-gray-700 hover:shadow-md transition-all duration-200 group">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M12 5v0m0 0v0m0 0v6m0 0H9m5-3V9m4 3v0m0 0v6m0 0H9m5 3V9m4 3v0" />
                  </svg>
                </button>

                <!-- Dropdown menu -->
                <div v-if="openDropdownIndex === rowIndex"
                  class="absolute right-0 mt-2 w-56 bg-white rounded-md shadow-lg py-1 border border-gray-200 z-20 dark:bg-gray-800 dark:border-gray-700">
                  <div class="px-2 py-2">
                    <button v-for="(action, i) in props.actions.filter(a => !a.visible || a.visible(row))"
                      :key="'action-' + i" @click="(action.handler(row), closeDropdown())"
                      :disabled="action.disabled && action.disabled(row)"
                      class="w-full text-left text-sm px-3 py-2 rounded hover:bg-gray-100 dark:hover:bg-gray-600 dark:text-gray-200">
                      <!-- added -->
                      {{
                        typeof action.tooltip === "function"
                          ? action.tooltip(row)
                          : action.tooltip
                      }}
                    </button>
                  </div>
                </div>
              </div>

              <!-- Inline actions -->
              <div v-else class="flex space-x-2">
                <button v-for="(action, i) in props.actions.filter(a => !a.visible || a.visible(row))"
                  :key="'inline-action-' + i" @click="action.handler(row)"
                  :disabled="action.disabled && action.disabled(row)"
                  class="flex items-center justify-center w-8 h-8 text-gray-500 bg-white border border-gray-200 rounded-full hover:bg-indigo-50 hover:text-gray-700 hover:shadow-md transition-all duration-200 group dark:bg-gray-700 dark:border-gray-600 dark:text-gray-200 dark:hover:bg-white-600"
                  :class="action.buttonClass">
                  <component :is="action.label" class="w-4 h-4" :class="action.iconClass" />
                </button>
              </div>
            </td>
          </tr>

          <!-- Empty state -->
          <tr v-if="!paginatedData.length">
            <td :colspan="computedHeaderKeys.length + (hasActions ? 1 : 0)" class="px-6 py-16 text-center">
              <div class="flex flex-col items-center justify-center space-y-3">
                <svg class="w-12 h-12 text-gray-400 dark:text-gray-500" fill="none" stroke="currentColor"
                  viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1"
                    d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" />
                </svg>
                <div>
                  <p class="text-sm font-medium text-gray-500 dark:text-gray-400">
                    No data available
                  </p>
                  <p class="text-xs text-gray-400 dark:text-gray-500 mt-1">
                    Try adjusting your filters or search terms
                  </p>
                </div>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>

  <!-- MOBILE CARD VIEW -->
  <div class="md:hidden space-y-4">
    <div v-for="(row, rowIndex) in paginatedData" :key="'mobile-row-' + rowIndex"
      class="bg-white border border-gray-200 rounded-2xl shadow-sm p-4 space-y-3 dark:bg-gray-800 dark:border-gray-700">
      <!-- Fields -->
      <div v-for="key in resolvedHeaders" :key="'mobile-field-' + key" class="flex justify-between gap-4 text-sm">
        <span class="text-gray-500 font-medium dark:text-gray-400">
          {{ formatHeader(key) }}
        </span>

        <span class="text-gray-800 text-right break-all dark:text-gray-200">
          <StatusBadge v-if="props.badgeFields?.includes(key)" :value="row[key]" :field="key"
            :field-mappings="props.fieldMappings" :field-colors="props.fieldColors" />
          <div v-else-if="props.dateTimeFields?.includes(key)" class="flex flex-col items-end">
            <span>{{ getDateTimeParts(key, row[key])?.date }}</span>
            <span class="text-gray-500 dark:text-gray-400 text-xs">{{
              getDateTimeParts(key, row[key])?.time
            }}</span>
          </div>
          <span v-else>
            {{ formatValue(key, row[key]) }}
          </span>
        </span>
      </div>

      <!-- Actions -->
      <div v-if="hasActions" class="pt-3 border-t border-gray-100 dark:border-gray-700 flex justify-end gap-2">
        <!-- Grouped actions -->
        <div v-if="props.groupActions" class="relative">
          <button @click="toggleDropdown(rowIndex)"
            class="px-3 py-2 text-sm rounded-lg border border-gray-200 bg-gray-50 text-gray-700 dark:bg-gray-700 dark:border-gray-600 dark:text-gray-200 dark:hover:bg-gray-600">
            Actions
          </button>

          <div v-if="openDropdownIndex === rowIndex"
            class="absolute right-0 mt-2 w-48 bg-white border border-gray-200 rounded-xl shadow-lg z-20 dark:bg-gray-800 dark:border-gray-700">
            <button v-for="(action, i) in props.actions.filter(a => !a.visible || a.visible(row))"
              :key="'mobile-action-' + i" @click="(action.handler(row), closeDropdown())"
              :disabled="action.disabled && action.disabled(row)"
              class="w-full px-4 py-3 text-left text-sm hover:bg-gray-100 dark:hover:bg-gray-600" :class="action.class">
              <!-- added -->
              {{
                typeof action.tooltip === "function"
                  ? action.tooltip(row)
                  : action.tooltip
              }}
            </button>
          </div>
        </div>

        <!-- Inline actions -->
        <button v-else v-for="(action, i) in props.actions.filter(a => !a.visible || a.visible(row))"
          :key="'mobile-action-inline-' + i" @click="action.handler(row)"
          :disabled="action.disabled && action.disabled(row)"
          class="px-3 py-2 text-sm rounded-lg border border-gray-200 bg-gray-50 text-gray-700 hover:bg-gray-100 dark:bg-gray-700 dark:border-gray-600 dark:text-gray-200 dark:hover:bg-gray-600"
          :class="action.buttonClass">
          <!-- added -->
          {{
            typeof action.tooltip === "function"
              ? action.tooltip(row)
              : action.tooltip
          }}
        </button>
      </div>
    </div>

    <!-- Empty state -->
    <div v-if="!paginatedData.length">
      class="text-center py-12 text-gray-400 dark:text-gray-500"
      >
      No data available
    </div>
  </div>

  <!-- Pagination -->
  <div class="mt-6">
    <Pagination :current-page="currentPage" :per-page="limit" :total-items="totalItemsInternal ?? 0"
      @page-change="handlePageChange" />
  </div>
</template>

<script setup lang="ts">
import { format, isValid, parseISO } from "date-fns";
import { startLoading, stopLoading } from "~/stores/globalvar";
import * as XLSX from "xlsx";

type TableAction = {
  label: string | Component;
  tooltip?: string | ((row: Record<string, any>) => string);
  handler: (row: Record<string, any>) => void;
  class?: string;
  buttonClass?: string;
  iconClass?: string;
  text?: string;
  showLabelOn?: "all" | "md" | "lg";
  visible?: (row: Record<string, any>) => boolean;
  disabled?: (row: Record<string, any>) => boolean;
};

const props = defineProps<{
  header?: string[];
  headerLabels?: Record<string, string>;
  hiddenFields?: string[];
  tableData?: Array<Record<string, any>> | null;
  actions: TableAction[];
  tableClass?: string;
  fieldMappings?: Record<string, Record<any, string>>;
  badgeFields?: string[];
  fieldColors?: Record<string, Record<any, string>>;
  dateFields?: string[];
  dateFormat?: string;
  dateTimeFields?: string[];
  dateTimeFormat?: string;
  filter?: boolean;
  draggable?: boolean;
  pagination?: boolean;
  perPage?: number;
  showExport?: boolean;
  serverPagination?: boolean;
  totalItems?: number | undefined;
  fetchData?: (params: {
    page: number;
    limit: number;
    search: string;
    filters: Record<string, string>;
    sortField?: string;
    sortDirection?: "asc" | "desc";
    startDate?: string;
    endDate?: string;
    policyNames?: string;
  }) => Promise<{ data: Array<Record<string, any>>; total: number }>;
  exportFetchData?: (params: {
    startDate?: string;
    endDate?: string;
  }) => Promise<{ data: Array<Record<string, any>>; total: number }>;
  showSearch?: boolean;
  groupActions?: boolean;
  filterableColumns?: string[] | "all";
  sortableColumns?: string[] | "all";
  limitfilter?: Number[];
  showlimitfilter?: boolean;
  policyFilterItems?: any[];
  policyFilterLabel?: string;
  policyFilterPlaceholder?: string;
}>();

const emit = defineEmits<{
  (e: "on-drop", ...args: any[]): void;
  (e: "close"): void;
  (e: "page-change", page: number, limit: number): void;
  (
    e: "on-filtersearch",
    page: number,
    limit: number,
    searchquery?: string,
  ): void;
}>();

const route = useRoute();
const router = useRouter();

const openDropdownIndex = ref<number | null>(null);
const globalSearch = ref("");
const hoveredTooltip = ref<string | null>(null);
const draggingIndex = ref<number | null>(null);
const targetIndex = ref<number | null>(null);
const currentPage = ref(1);
const limit = ref(props.perPage ?? 10);
const searchquery = ref("");
const columnFilters = reactive<Record<string, string>>({});
const internalData = ref<Array<Record<string, any>>>([]);
const totalItemsInternal = ref(props.serverPagination ? 0 : props.totalItems);
const loading = ref(false);
const dynamicFilters = ref<Record<string, string>>({});
const sortField = ref<string>("");
const sortDirection = ref<"asc" | "desc">("asc");
const exportStartDate = ref<string>("");
const exportEndDate = ref<string>("");
const filterStartDate = ref<string>("");
const filterEndDate = ref<string>("");
const showExportModal = ref(false);
const selectedPolicyObjects = ref<any[]>([]);
const showPolicyFilterModal = ref(false);
const tempPolicyObjects = ref<any[]>([]);
const policySearchTerm = ref("");
const policySearchInput = ref<HTMLInputElement | null>(null);

// function buildTableQuery() {
//   const q: Record<string, any> = {};

//   if (globalSearch.value) q.search = globalSearch.value;
//   if (currentPage.value > 1) q.page = currentPage.value;
//   if (limit.value !== props.perPage) q.limit = limit.value;
//   if (sortField.value) {
//     q.sortField = sortField.value;
//     q.sortDirection = sortDirection.value;
//   }

//   const activeColumnFilters: string[] = [];
//   Object.entries(columnFilters).forEach(([key, val]) => {
//     if (val) activeColumnFilters.push(`${key}:${val}`);
//   });
//   if (activeColumnFilters.length) q.filters = activeColumnFilters.join(",");

//   const activeDynamicFilters: string[] = [];
//   Object.entries(dynamicFilters.value).forEach(([key, val]) => {
//     if (val) activeDynamicFilters.push(`${key}:${val}`);
//   });
//   if (activeDynamicFilters.length) q.dynamicFilters = activeDynamicFilters.join(",");

//   return q;
// }

function buildTableQuery() {
  const q: Record<string, any> = {};

  // Explicitly allow clearing these values from the URL
  q.search = globalSearch.value || undefined;
  q.page = currentPage.value > 1 ? currentPage.value : undefined;
  q.limit = limit.value !== props.perPage ? limit.value : undefined;

  if (sortField.value) {
    q.sortField = sortField.value;
    q.sortDirection = sortDirection.value;
  } else {
    q.sortField = undefined;
    q.sortDirection = undefined;
  }

  const activeColumnFilters: string[] = [];
  Object.entries(columnFilters).forEach(([key, val]) => {
    if (val) activeColumnFilters.push(`${key}:${val}`);
  });
  q.filters = activeColumnFilters.length ? activeColumnFilters.join(",") : undefined;

  const activeDynamicFilters: string[] = [];
  Object.entries(dynamicFilters.value).forEach(([key, val]) => {
    if (val) activeDynamicFilters.push(`${key}:${val}`);
  });
  q.dynamicFilters = activeDynamicFilters.length ? activeDynamicFilters.join(",") : undefined;

  if (selectedPolicyObjects.value.length > 0) {
    q.policyFilter = selectedPolicyObjects.value.map((p: any) => p.id).join(",");
  } else {
    q.policyFilter = undefined;
  }

  return q;
}

function applyQueryToState() {
  const q = route.query;
  if (q.search != null) globalSearch.value = String(q.search);
  if (q.page != null) currentPage.value = Number(q.page);
  if (q.limit != null) limit.value = Number(q.limit);
  if (q.sortField) sortField.value = String(q.sortField);
  if (q.sortDirection) sortDirection.value = String(q.sortDirection) as "asc" | "desc";

  if (q.filters) {
    Object.keys(columnFilters).forEach((key) => delete columnFilters[key]);
    String(q.filters).split(",").forEach((pair) => {
      const idx = pair.indexOf(":");
      if (idx > -1) {
        const key = pair.slice(0, idx);
        const value = pair.slice(idx + 1);
        if (key && value != null) columnFilters[key] = value;
      }
    });
  }

  if (q.dynamicFilters) {
    dynamicFilters.value = {};
    String(q.dynamicFilters).split(",").forEach((pair) => {
      const idx = pair.indexOf(":");
      if (idx > -1) {
        const key = pair.slice(0, idx);
        const value = pair.slice(idx + 1);
        if (key && value != null) dynamicFilters.value[key] = value;
      }
    });
  }

  if (q.policyFilter) {
    const ids = String(q.policyFilter).split(",").map((id) => id.trim()).filter(Boolean);
    const items = props.policyFilterItems || [];
    selectedPolicyObjects.value = ids
      .map((id) => items.find((it: any) => String(it.id) === String(id)))
      .filter(Boolean);
  } else {
    selectedPolicyObjects.value = [];
  }
}

async function persistTableState() {
  const tableQuery = buildTableQuery();
  const merged = { ...route.query, ...tableQuery };
  const current = JSON.stringify(route.query);
  const next = JSON.stringify(merged);
  if (current !== next) {
    await router.replace({ query: merged });
  }
}

watch(
  () => route.query,
  () => {
    applyQueryToState();
    if (props.serverPagination && props.fetchData) {
      if (route.query.policyFilter && !props.policyFilterItems?.length) {
        return;
      }
      fetchTable();
    }
  },
  { deep: true },
);

watch(
  () => props.policyFilterItems,
  (items) => {
    if (items?.length && route.query.policyFilter && !selectedPolicyObjects.value.length) {
      applyQueryToState();
      if (props.serverPagination && props.fetchData) {
        fetchTable();
      }
    }
  },
);

// Default props
const defaultProps = {
  filterableColumns: [] as string[] | "all",
  sortableColumns: [] as string[] | "all",
};

// Merge default props with actual props
const mergedProps = {
  ...defaultProps,
  ...props,
};

const refresh = () => {
  fetchTable();
};

const setFilterDates = (start: string, end: string) => {
  filterStartDate.value = start;
  filterEndDate.value = end;
  currentPage.value = 1;
};

defineExpose({
  refresh,
  setFilterDates,
});

function openExportModal() {
  showExportModal.value = true;
  exportStartDate.value = "";
  exportEndDate.value = "";
}

function closeExportModal() {
  showExportModal.value = false;
}

async function confirmExport() {
  await exportToExcel();
  closeExportModal();
}

function openPolicyFilterModal() {
  showPolicyFilterModal.value = true;
  tempPolicyObjects.value = [...selectedPolicyObjects.value];
  policySearchTerm.value = ""; // Clear search term when opening
}

function closePolicyFilterModal() {
  showPolicyFilterModal.value = false;
}

function applyPolicyFilter() {
  selectedPolicyObjects.value = [...tempPolicyObjects.value];
  policySearchTerm.value = ""; // Clear search term when applying
  showPolicyFilterModal.value = false;
  currentPage.value = 1;
  if (props.serverPagination && props.fetchData) {
    persistTableState();
    fetchTable();
  }
}

function togglePolicySelection(item: any) {
  const idx = tempPolicyObjects.value.findIndex((p: any) => String(p.id) === String(item.id));
  if (idx > -1) {
    tempPolicyObjects.value.splice(idx, 1);
  } else {
    tempPolicyObjects.value.push(item);
  }
}

function clearPolicySelection() {
  tempPolicyObjects.value = [];
  selectedPolicyObjects.value = [];
  policySearchTerm.value = ""; // Clear search term
  showPolicyFilterModal.value = false;
  currentPage.value = 1;
  if (props.serverPagination && props.fetchData) {
    persistTableState();
    fetchTable();
  }
}

async function exportToExcel() {
  let data: Array<Record<string, any>> = [];
  if (props.exportFetchData) {
    try {
      const res = await props.exportFetchData({
        startDate: exportStartDate.value,
        endDate: exportEndDate.value,
      });
      data = res.data;
    } catch (error) {
      console.error("Export error:", error);
      return;
    }
  } else {
    data = filteredData.value;
  }

  if (!data || data.length === 0) return;

  const headers = resolvedHeaders.value;

  // Prepare data for Excel (include headers as first row)
  const excelData = data.map((row) => {
    const excelRow: any = {};
    headers.forEach((key) => {
      excelRow[formatHeader(key)] = formatValue(key, row[key]);
    });
    return excelRow;
  });

  // Create worksheet
  const worksheet = XLSX.utils.json_to_sheet(excelData, {
    header: headers.map(formatHeader),
  });

  const colWidths = headers.map((header) => {
    const headerText = formatHeader(header);
    const headerLen = headerText.length;
    const maxDataLen = Math.max(
      ...data.map((row) => String(formatValue(header, row[header])).length),
      0,
    );
    return { wch: Math.max(headerLen, maxDataLen) + 2 }; // +2 for padding
  });

  worksheet["!cols"] = colWidths;

  // Create workbook
  const workbook = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(workbook, worksheet, "Exported Requests");

  // Download the file
  XLSX.writeFile(workbook, "exported_requests.xlsx");
}

// Computed properties
const computedHeaderKeys = computed(() => {
  if (props.header && props.header.length) {
    return props.header;
  }
  if (props.tableData && props.tableData.length > 0 && props.tableData[0]) {
    return Object.keys(props.tableData[0]);
  }
  return [];
});

const hasActions = computed(() => props.actions && props.actions.length > 0);

const displayData = computed(() => {
  if (props.serverPagination) return internalData.value;
  return props.tableData || [];
});

const resolvedHeaders = computed(() => {
  if (props.header && props.header.length > 0) {
    return props.header.filter((key) => !props.hiddenFields?.includes(key));
  }
  // Use displayData so it reacts to internalData changes
  const sampleRow = displayData.value?.[0];
  if (sampleRow) {
    return Object.keys(sampleRow).filter(
      (key) => !props.hiddenFields?.includes(key),
    );
  }
  return [];
});

const filterableColumns = computed(() => {
  if (props.filterableColumns === "all") return resolvedHeaders.value;
  if (props.filterableColumns?.length) {
    return resolvedHeaders.value.filter(
      (key) =>
        props.filterableColumns?.includes(key) &&
        !props.badgeFields?.includes(key) &&
        !props.dateFields?.includes(key) &&
        !props.dateTimeFields?.includes(key),
    );
  }
  return [];
});

// Filtered policy items for search functionality
const filteredPolicyItems = computed(() => {
  if (!props.policyFilterItems || !props.policyFilterItems.length) {
    return [];
  }

  const searchTerm = policySearchTerm.value.trim().toLowerCase();
  if (!searchTerm) {
    return props.policyFilterItems;
  }

  return props.policyFilterItems.filter(item =>
    item.label.toLowerCase().includes(searchTerm)
  );
});

const showFilterControls = computed(
  () => props.filter && resolvedHeaders.value.length > 0,
);
const paginatedData = computed(() => {
  if (props.serverPagination) return filteredData.value; // ← already paginated
  if (!props.pagination || !filteredData.value) return filteredData.value || [];
  const start = (currentPage.value - 1) * limit.value;
  const end = start + limit.value;
  return filteredData.value.slice(start, end);
});

const filteredData = computed(() => {
  // When server handles pagination, just return internalData as-is
  // No client-side filtering/sorting — server already did it
  let data = props.serverPagination
    ? internalData.value
    : props.tableData || [];

  if (props.serverPagination) return data; // ← skip all client-side logic

  const search = globalSearch.value?.toLowerCase() ?? "";

  data = data.filter((row) => {
    const matchesSearch =
      !search ||
      Object.entries(row).some(([key, val]) => {
        const displayValue = formatValue(key, val);
        return String(displayValue).toLowerCase().includes(search);
      });

    const matchesColumnFilters = resolvedHeaders.value.every((key) => {
      const filterValue = columnFilters[key]?.toLowerCase?.() ?? "";
      const displayValue = formatValue(key, row[key]);
      return (
        filterValue === "" ||
        String(displayValue).toLowerCase().includes(filterValue)
      );
    });

    const matchesDynamicFilters = Object.entries(dynamicFilters.value).every(
      ([key, value]) => {
        if (!value) return true;
        const displayValue = formatValue(key, row[key]);
        return String(displayValue) === value;
      },
    );

    return matchesSearch && matchesColumnFilters && matchesDynamicFilters;
  });

  if (sortField.value) {
    data = [...data].sort((a, b) => {
      const aValue = formatValue(sortField.value, a[sortField.value]);
      const bValue = formatValue(sortField.value, b[sortField.value]);
      if (aValue < bValue) return sortDirection.value === "asc" ? -1 : 1;
      if (aValue > bValue) return sortDirection.value === "asc" ? 1 : -1;
      return 0;
    });
  }

  return data;
});

// Watchers
watch(
  resolvedHeaders,
  (headers) => {
    headers.forEach((key) => {
      if (!(key in columnFilters)) columnFilters[key] = "";
      if (!(key in dynamicFilters.value)) dynamicFilters.value[key] = "";
    });
  },
  { immediate: true },
);

//watch([currentPage, globalSearch], () => {
//  if (props.serverPagination && props.fetchData) {
//    fetchTable();
//  }
//});

// Replace the existing watch
// Removed: watch on page/search changes is now handled through route query sync

watch(
  globalSearch,
  (val) => {
    if (!val) {
      handleGlobalSearch();
    }
  }
);

watch(
  () => props.totalItems,
  (val) => {
    if (!props.serverPagination) {
      totalItemsInternal.value = val;
    }
  },
  { immediate: true },
);

// Methods
function formatHeader(headerKey: string): string {

  // Custom label takes priority
  if (props.headerLabels?.[headerKey]) {
    return props.headerLabels[headerKey];
  }

  // First, handle common acronyms and patterns
  let result = headerKey
    // Replace underscores with spaces
    .replace(/_/g, " ")
    // Insert space before uppercase letters (camelCase handling)
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    // Handle consecutive lowercase letters followed by uppercase (e.g.,Policyname -> Policy name)
    .replace(/([a-z]{2,})([A-Z])/g, "$1 $2")
    // Capitalize first letter of each word
    .replace(/\b\w/g, (char) => char.toUpperCase());

  return result;
}

function formatValue(key: string, value: any): string {
  if (value == null || value === "") return "-";

  if (props.fieldMappings?.[key] && props.fieldMappings[key][value] != null) {
    return props.fieldMappings[key][value];
  }

  // Handle Date objects directly
  if (value instanceof Date) {
    return isValid(value)
      ? format(value, props.dateFormat || "MMM d, yyyy")
      : value.toString();
  }

  // Explicit date-time field handling
  if (props.dateTimeFields?.includes(key)) {
    // <-- add this block
    try {
      const parsed =
        typeof value === "string" ? parseISO(value) : new Date(value);
      return isValid(parsed)
        ? format(parsed, props.dateTimeFormat || "MMM d, yyyy h:mm a")
        : value.toString();
    } catch (e) {
      return value.toString();
    }
  }

  // Explicit date field handling
  if (props.dateFields?.includes(key)) {
    try {
      const parsed =
        typeof value === "string" ? parseISO(value) : new Date(value);
      return isValid(parsed)
        ? format(parsed, props.dateFormat || "MMM d, yyyy")
        : value.toString();
    } catch (e) {
      return value.toString();
    }
  }

  // Auto-detect ISO 8601 strings
  if (typeof value === "string" && /^\d{4}-\d{2}-\d{2}T/.test(value)) {
    try {
      const parsed = parseISO(value);
      return isValid(parsed)
        ? format(parsed, props.dateFormat || "MMM d, yyyy")
        : value;
    } catch (e) {
      return value;
    }
  }

  return value.toString();
}

function getDateTimeParts(
  key: string,
  value: any,
): { date: string; time: string } | null {
  if (!props.dateTimeFields?.includes(key)) return null;
  if (value == null || value === "") return { date: "-", time: "" };

  try {
    const parsed =
      typeof value === "string" ? parseISO(value) : new Date(value);
    if (!isValid(parsed)) return { date: value.toString(), time: "" };

    return {
      date: format(parsed, "MMMM d, yyyy"),
      time: format(parsed, "h:mm a"),
    };
  } catch (e) {
    return { date: value.toString(), time: "" };
  }
}

function updateSearchTerm() {
  const input = policySearchInput.value;
  if (input) {
    policySearchTerm.value = input.value;
    input.focus();
  } else {
    policySearchTerm.value = '';
  }
}

function getFilterOptions(key: string) {
  if (!props.tableData) return [];
  const uniqueValues = new Set();
  props.tableData.forEach((row) => {
    const value = formatValue(key, row[key]);
    if (value) uniqueValues.add(value);
  });
  return Array.from(uniqueValues).sort();
}

function toggleDropdown(index: number) {
  openDropdownIndex.value = openDropdownIndex.value === index ? null : index;
}

function closeDropdown() {
  openDropdownIndex.value = null;
}

function onDragStart(index: number) {
  draggingIndex.value = index;
}

function onDragOver(index: number, event: Event) {
  event.preventDefault();
  targetIndex.value = index;
}

function onDrop() {
  if (
    draggingIndex.value === null ||
    targetIndex.value === null ||
    !props.tableData ||
    draggingIndex.value >= props.tableData.length ||
    targetIndex.value >= props.tableData.length
  ) {
    draggingIndex.value = null;
    targetIndex.value = null;
    return;
  }

  if (draggingIndex.value !== targetIndex.value) {
    console.log("drag row: ", props.tableData[draggingIndex.value]);

    console.log("target row: ", props.tableData[targetIndex.value]);

    [props.tableData[draggingIndex.value], props.tableData[targetIndex.value]] =
      [
        props.tableData[targetIndex.value],
        props.tableData[draggingIndex.value],
      ];

    console.log("done drag row: ", props.tableData[draggingIndex.value]);

    console.log("done target row: ", props.tableData[targetIndex.value]);

    emit(
      "on-drop",
      props.tableData[draggingIndex.value],
      props.tableData[targetIndex.value],
    );

    console.log("Dropping");
  }

  draggingIndex.value = null;
  targetIndex.value = null;
}

function onDragLeave() {
  targetIndex.value = null;
}

function handlePageChange(page: number) {
  closeDropdown();
  currentPage.value = page;
  persistTableState();
}

function applyDynamicFilters() {
  currentPage.value = 1;
  persistTableState();
}

function handleGlobalSearch() {
  currentPage.value = 1;
  persistTableState();
}

function handleLimitChange() {
  currentPage.value = 1;
  persistTableState();
}

function handleColumnFilter(key: string) {
  currentPage.value = 1;
  persistTableState();
}

function isColumnSortable(key: string) {
  if (props.sortableColumns === "all") return true;
  return props.sortableColumns?.includes(key) ?? false;
}

function isColumnFilterable(key: string) {
  if (!props.filter) return false;
  if (props.filterableColumns === "all") return true;
  return props.filterableColumns?.includes(key) ?? false;
}

function toggleSort(key: string) {
  if (sortField.value === key) {
    sortDirection.value = sortDirection.value === "asc" ? "desc" : "asc";
  } else {
    sortField.value = key;
    sortDirection.value = "asc";
  }

  currentPage.value = 1;
  persistTableState();
}

//async function fetchTable() {
//  if (!props.fetchData) return;
//
//  loading.value = true;
//  try {
//    startLoading();
//    const res = await props.fetchData({
//      page: currentPage.value,
//      limit: limit.value,
//      search: globalSearch.value,
//      filters: {},
//      sortField: sortField.value,
//      sortDirection: sortDirection.value,
//    });
//
//    internalData.value = res.data;
//    totalItemsInternal.value = res.total;
//  } catch (e) {
//    console.error("Error fetching data:", e);
//  } finally {
//    loading.value = false;
//    stopLoading();
//  }
//}

// In BaseTable.vue — replace the existing fetchTable
const fetchTable = async () => {
  if (!props.fetchData) return;

  loading.value = true;
  try {
    startLoading();

    // Build active column + dynamic filters
    const activeFilters: Record<string, string> = {};
    Object.entries(columnFilters).forEach(([key, val]) => {
      if (val) activeFilters[key] = val;
    });
    Object.entries(dynamicFilters.value).forEach(([key, val]) => {
      if (val) activeFilters[key] = val as string;
    });

    const res = await props.fetchData({
      page: currentPage.value,
      limit: limit.value,
      search: globalSearch.value,
      filters: activeFilters,
      sortField: sortField.value || undefined,
      sortDirection: sortField.value ? sortDirection.value : undefined,
      startDate: filterStartDate.value || undefined,
      endDate: filterEndDate.value || undefined,
      policyNames:
        selectedPolicyObjects.value.length > 0
          ? selectedPolicyObjects.value.map((p: any) => p.id).join(",")
          : undefined,
    });

    internalData.value = res.data;
    totalItemsInternal.value = res.total;
  } catch (e) {
    console.error("Error fetching data:", e);
  } finally {
    loading.value = false;
    stopLoading();
  }
};

// Lifecycle hooks
onMounted(() => {
  applyQueryToState();
  if (props.serverPagination && props.fetchData) {
    fetchTable();
  }
});

</script>

<style scoped>
/* Scoped styles if needed */
</style>
