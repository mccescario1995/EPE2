<template>
  <span class="px-2 py-1 text-xs font-medium rounded-full" :class="badgeClass">
    {{ displayLabel }}
  </span>
</template>

<script setup lang="ts">
const props = defineProps<{
  value: any;
  field: string;
  fieldMappings?: Record<string, Record<any, string>>;
  fieldColors?: Record<string, Record<any, string>>;
}>();

const displayLabel = computed(() => {
  return props.fieldMappings?.[props.field]?.[props.value] ?? props.value;
});

const badgeClass = computed(() => {
  return (
    props.fieldColors?.[props.field]?.[props.value] ??
    "bg-gray-100 text-gray-800"
  );
});
</script>
