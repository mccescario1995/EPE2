<script setup lang="ts">
import {
  ref,
  computed,
  watch,
  onMounted,
  onBeforeUnmount,
  nextTick,
} from "vue";

type Item = {
  id: string | number;
  label: string;
  [key: string]: any;
};

type FetchFunction = (searchQuery?: string) => Promise<Item[]>;

const props = withDefaults(
  defineProps<{
    modelValue?: Item | Item[] | null | any;
    placeholder?: string;
    searchPlaceholder?: string;
    noResultsText?: string;
    multiple?: boolean;
    closeOnSelect?: boolean;
    maxHeight?: string;
    allowAdd?: boolean;
    addButtonText?: string;
    addInputPlaceholder?: string;
    onSaveItem?: (item: { id: string; label: string }) => Promise<Item>;
    fetchItems?: FetchFunction;
    initialItems?: Item[];
    minSearchChars?: number;
    debounceTime?: number;
    disabled?: boolean;
    objectOutput?: boolean;
  }>(),
  {
    placeholder: "Select an option",
    searchPlaceholder: "Search...",
    noResultsText: "No results found",
    multiple: false,
    closeOnSelect: true,
    maxHeight: "60vh",
    allowAdd: false,
    addButtonText: "Add New Item",
    addInputPlaceholder: "Enter new item and press Enter",
    initialItems: () => [],
    minSearchChars: 0,
    debounceTime: 300,
    disabled: false,
    objectOutput: true,
  }
);

const emit = defineEmits(["update:modelValue", "item-added"]);

const isOpen = ref(false);
const searchQuery = ref("");
const dropdownRef = ref<HTMLElement | null>(null);
const dropdownMenuRef = ref<HTMLElement | null>(null);
const dropdownPosition = ref({
  top: true,
  left: true,
});
const isAddingItem = ref(false);
const newItemInput = ref("");
const isLoading = ref(false);
const isFetching = ref(false);
const items = ref<Item[]>(props.initialItems);
const debounceTimeout = ref<NodeJS.Timeout | null>(null);
const filteredItems = ref<Item[]>([]);

// Calculate dropdown position based on available space
const calculatePosition = () => {
  if (!dropdownRef.value || !dropdownMenuRef.value || !isOpen.value) return;

  const triggerRect = dropdownRef.value.getBoundingClientRect();
  const menuHeight = dropdownMenuRef.value.offsetHeight;
  const menuWidth = dropdownMenuRef.value.offsetWidth;
  const windowHeight = window.innerHeight;
  const windowWidth = window.innerWidth;

  const spaceBelow = windowHeight - triggerRect.bottom;
  const spaceAbove = triggerRect.top;

  dropdownPosition.value.top =
    spaceBelow > menuHeight || spaceBelow > spaceAbove;

  const spaceRight = windowWidth - triggerRect.right;
  const spaceLeft = triggerRect.left;

  dropdownPosition.value.left =
    spaceRight > menuWidth || spaceRight > spaceLeft;
};

watch(isOpen, (val) => {
  if (val) {
    nextTick(() => {
      calculatePosition();
      if (isAddingItem.value) {
        const input = dropdownMenuRef.value?.querySelector(".new-item-input");
        if (input) (input as HTMLInputElement).focus();
      }
    });
  }
});

// Fetch items from server
const fetchItems = async (query = "") => {
  if (!props.fetchItems) {
    filteredItems.value = items.value;
    return;
  }

  try {
    isFetching.value = true;
    const fetchedItems = await props.fetchItems(query);
    items.value = fetchedItems;
    filteredItems.value = fetchedItems;
    // filteredItems.value = fetchedItems;
  } catch (error) {
    console.error("Error fetching items:", error);
  } finally {
    isFetching.value = false;
  }
};

// Close dropdown when clicking outside
const handleClickOutside = (event: MouseEvent) => {
  if (dropdownRef.value && !dropdownRef.value.contains(event.target as Node)) {
    isOpen.value = false;
    cancelAddItem();
  }
};

// Handle window resize
const handleResize = () => {
  if (isOpen.value) {
    calculatePosition();
  }
};

onMounted(() => {
  document.addEventListener("click", handleClickOutside);
  window.addEventListener("resize", handleResize);
});

onBeforeUnmount(() => {
  document.removeEventListener("click", handleClickOutside);
  window.removeEventListener("resize", handleResize);
  if (debounceTimeout.value) clearTimeout(debounceTimeout.value);
});

// Perform local search
const performSearch = () => {
  if (!searchQuery.value) {
    filteredItems.value = items.value;
    return;
  }

  const query = searchQuery.value.toLowerCase();
  filteredItems.value = items.value.filter((item) =>
    item.label.toLowerCase().includes(query)
  );
};

watch(searchQuery, performSearch);

// Fetch items when dropdown opens
watch(isOpen, async (val) => {
  if (val) {
    await fetchItems();
    searchQuery.value = "";
  }
});

// Watch for initialItems prop changes
watch(() => props.initialItems, (newItems) => {
  if (newItems && newItems.length > 0) {
    items.value = newItems;
    filteredItems.value = newItems;
  }
}, { immediate: true });

// ✅ Updated: handle objectOutput correctly
const selectItem = (item: Item) => {
  if (props.multiple) {
    const currentValue = Array.isArray(props.modelValue)
      ? props.modelValue
      : [];

    const isSelected = currentValue.some((selected) => selected.id === item.id);

    const newValue = isSelected
      ? currentValue.filter((selected) => selected.id !== item.id)
      : [...currentValue, item];

    emit("update:modelValue", newValue);
  } else {
    const outputValue = props.objectOutput ? item : item.id;
    emit("update:modelValue", outputValue);

    if (props.closeOnSelect) {
      isOpen.value = false;
    }
  }
};

// ✅ Updated: works with both objectOutput true or false
const isSelected = (item: Item) => {
  if (props.multiple) {
    return Array.isArray(props.modelValue)
      ? props.modelValue.some((selected) => selected.id === item.id)
      : false;
  } else {
    if (props.objectOutput) {
      return props.modelValue?.id === item.id;
    } else {
      return props.modelValue === item.id;
    }
  }
};

// ✅ Updated: display label even if only id is stored
const selectedLabels = computed(() => {
  if (props.multiple) {
    return Array.isArray(props.modelValue)
      ? props.modelValue.map((item) => item.label).join(", ")
      : "";
  } else {
    if (props.objectOutput) {
      return props.modelValue?.label || "";
    } else {
      const modelId = props.modelValue;
      const foundItem =
        items.value.find((e) => e.id === modelId) ||
        props.initialItems.find((e) => e.id === modelId);
      return foundItem?.label || "";
    }
  }
});

const clearSelection = () => {
  emit("update:modelValue", props.multiple ? [] : null);
};

const toggleDropdown = () => {
  isOpen.value = !isOpen.value;
  if (isOpen.value) {
    searchQuery.value = "";
    if (props.fetchItems && props.minSearchChars === 0) {
      fetchItems();
    }
  }
};

const startAddItem = () => {
  isAddingItem.value = true;
  newItemInput.value = "";
  nextTick(() => {
    const input = dropdownMenuRef.value?.querySelector(".new-item-input");
    if (input) (input as HTMLInputElement).focus();
  });
};

const cancelAddItem = () => {
  isAddingItem.value = false;
  newItemInput.value = "";
};

const handleAddItem = async () => {
  if (!newItemInput.value.trim()) {
    cancelAddItem();
    return;
  }

  isLoading.value = true;

  try {
    let newItem: Item;
    if (props.onSaveItem) {
      const savedItem = await props.onSaveItem({
        id: newItemInput.value.trim(),
        label: newItemInput.value.trim(),
      });
      newItem = savedItem;
    } else {
      newItem = {
        id: newItemInput.value.trim(),
        label: newItemInput.value.trim(),
      };
    }

    items.value = [...items.value, newItem];
    filteredItems.value = items.value;
    emit("item-added", newItem);
    cancelAddItem();
    searchQuery.value = "";
  } catch (error) {
    console.error("Error adding item:", error);
  } finally {
    isLoading.value = false;
  }
};

const handleKeyDown = (e: KeyboardEvent) => {
  if (e.key === "Enter") handleAddItem();
  else if (e.key === "Escape") cancelAddItem();
};
</script>

<template>
  <div class="relative w-full" ref="dropdownRef">
    <!-- Trigger Button -->
    <div
      v-if="!disabled"
      class="flex items-center justify-between p-2 border border-gray-300 rounded-md cursor-pointer bg-white hover:border-gray-400 transition-colors"
      @click="toggleDropdown()"
    >
      <div class="truncate max-w-[90%]">
        <span v-if="selectedLabels" class="text-gray-800">
          {{ selectedLabels }}
        </span>
        <span v-else class="text-gray-500">
          {{ placeholder }}
        </span>
      </div>
      <div class="flex items-center space-x-2">
        <button
          v-if="(multiple && modelValue?.length) || (!multiple && modelValue)"
          @click.stop="clearSelection"
          class="text-gray-500 hover:text-gray-700"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            class="h-4 w-4"
            viewBox="0 0 20 20"
            fill="currentColor"
          >
            <path
              fill-rule="evenodd"
              d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"
              clip-rule="evenodd"
            />
          </svg>
        </button>
        <svg
          xmlns="http://www.w3.org/2000/svg"
          class="h-5 w-5 text-gray-500 transition-transform duration-200"
          :class="{ 'rotate-180': isOpen }"
          viewBox="0 0 20 20"
          fill="currentColor"
        >
          <path
            fill-rule="evenodd"
            d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z"
            clip-rule="evenodd"
          />
        </svg>
      </div>
    </div>
    <div
      v-else
      class="flex items-center justify-between p-2 border border-gray-300 rounded-md cursor-pointer bg-white hover:border-gray-400 transition-colors"
    >
      <div class="truncate max-w-[90%]">
        <span v-if="selectedLabels" class="text-gray-800">
          {{ selectedLabels }}
        </span>
        <span v-else class="text-gray-500">
          {{ placeholder }}
        </span>
      </div>
    </div>

    <!-- Dropdown Menu -->
    <div
      v-if="isOpen"
      ref="dropdownMenuRef"
      class="absolute z-50 w-full min-w-max bg-white border border-gray-300 rounded-md shadow-lg overflow-hidden transition-all"
      :class="{
        'right-0': !dropdownPosition.left,
        'left-0': dropdownPosition.left,
      }"
      :style="{ 'max-height': maxHeight }"
    >
      <!-- Search Input -->
      <div class="p-2 border-b border-gray-200 sticky top-0 bg-white z-10">
        <input
          type="text"
          v-model="searchQuery"
          :placeholder="searchPlaceholder"
          class="w-full p-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          @click.stop
        />
      </div>

      <!-- Dropdown Items -->
      <div class="overflow-y-auto max-h-52">
        <!-- Loading state -->
        <div v-if="isFetching" class="p-4 flex justify-center">
          <svg
            class="animate-spin h-5 w-5 text-blue-500"
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
          >
            <circle
              class="opacity-25"
              cx="12"
              cy="12"
              r="10"
              stroke="currentColor"
              stroke-width="4"
            ></circle>
            <path
              class="opacity-75"
              fill="currentColor"
              d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
            ></path>
          </svg>
        </div>

        <!-- Results -->
        <template v-else>
          <template v-if="filteredItems.length > 0">
            <div
              v-for="item in filteredItems"
              :key="item.id"
              class="px-4 py-2 cursor-pointer hover:bg-gray-100 flex items-center"
              :class="{
                'bg-blue-50': isSelected(item),
                'text-blue-600': isSelected(item),
              }"
              @click.stop="selectItem(item)"
            >
              <template v-if="multiple">
                <input
                  type="checkbox"
                  :checked="isSelected(item)"
                  class="mr-2 h-4 w-4 text-blue-600 rounded focus:ring-blue-500"
                  @click="isSelected(item)"
                  @click.stop
                />
              </template>
              <span>{{ item.label }}</span>
            </div>
          </template>
          <div v-else class="px-4 py-2 text-gray-500 text-center">
            {{ noResultsText }}
          </div>
        </template>

        <!-- Add Item Section -->
        <div v-if="allowAdd" class="border-t border-gray-200">
          <div v-if="!isAddingItem" class="px-4 py-2">
            <button
              @click.stop="startAddItem"
              class="w-full text-left text-blue-600 hover:text-blue-800 flex items-center"
            >
              <svg
                xmlns="http://www.w3.org/2000/svg"
                class="h-4 w-4 mr-2"
                viewBox="0 0 20 20"
                fill="currentColor"
              >
                <path
                  fill-rule="evenodd"
                  d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z"
                  clip-rule="evenodd"
                />
              </svg>
              {{ addButtonText }}
            </button>
          </div>
          <div v-else class="p-2 bg-gray-50">
            <input
              type="text"
              v-model="newItemInput"
              @keydown="handleKeyDown"
              :placeholder="addInputPlaceholder"
              class="new-item-input w-full p-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              @click.stop
              :disabled="isLoading"
            />
            <div class="flex justify-end mt-2 space-x-2">
              <button
                @click.stop="cancelAddItem"
                class="px-3 py-1 text-sm text-gray-600 hover:text-gray-800"
                :disabled="isLoading"
              >
                Cancel
              </button>
              <button
                @click.stop="handleAddItem"
                class="px-3 py-1 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
                :disabled="!newItemInput.trim() || isLoading"
              >
                <span v-if="!isLoading">Add</span>
                <span v-else class="inline-flex items-center">
                  <svg
                    class="animate-spin -ml-1 mr-1 h-4 w-4 text-white"
                    xmlns="http://www.w3.org/2000/svg"
                    fill="none"
                    viewBox="0 0 24 24"
                  >
                    <circle
                      class="opacity-25"
                      cx="12"
                      cy="12"
                      r="10"
                      stroke="currentColor"
                      stroke-width="4"
                    ></circle>
                    <path
                      class="opacity-75"
                      fill="currentColor"
                      d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                    ></path>
                  </svg>
                  Adding...
                </span>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
