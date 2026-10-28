<script setup>
const props = defineProps({
  data: {
    type: Object,
    required: true,
    default: () => ({
      labels: [],
      datasets: [],
    }),
  },
  width: {
    type: Number,
    default: 100,
  },
  height: {
    type: Number,
    default: 100,
  },
});
</script>

<template>
  <div class="relative h-full w-full flex items-center justify-center flex-col">
    <svg class="w-72 h-7w-72" :viewBox="`0 0 ${width} ${height}`">
      <template v-for="(value, index) in data.datasets[0].data" :key="index">
        <circle
          r="40"
          cx="50"
          cy="50"
          :stroke="data.datasets[0].colors[index]"
          stroke-width="40"
          :stroke-dasharray="`${value} ${100 - value}`"
          stroke-dashoffset="-25"
          fill="none"
          transform="rotate(-90) translate(-100)"
        />
      </template>

      <text
        x="50"
        y="50"
        text-anchor="middle"
        dominant-baseline="middle"
        class="text-sm font-medium fill-gray-700"
      >
        Total
      </text>
    </svg>
    <!-- Legend - now using flex layout instead of SVG text -->
    <div class="flex flex-wrap justify-center gap-4 mt-2 w-full px-4">
      <div
        v-for="(label, index) in data.labels"
        :key="label"
        class="flex items-center"
      >
        <div
          class="w-3 h-3 rounded-sm mr-2"
          :style="{ backgroundColor: data.datasets[0].colors[index] }"
        ></div>
        <span class="text-xs text-gray-600">
          {{ label }} ({{ data.datasets[0].data[index] }}%)
        </span>
      </div>
    </div>
  </div>
</template>
