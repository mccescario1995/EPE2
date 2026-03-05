<script setup>
const props = defineProps({
  data: {
    type: Object,
    required: true,
    default: () => ({
      labels: [],
      datasets: []
    })
  },
  width: {
    type: Number,
    default: 400
  },
  height: {
    type: Number,
    default: 300
  }
})
</script>

<template>
  <div class="relative h-full w-full">
    <svg class="w-full h-full" :viewBox="`0 0 ${width} ${height}`" preserveAspectRatio="xMidYMid meet">
      <!-- X-axis -->
      <line x1="40" :y1="height - 40" :x2="width - 40" :y2="height - 40" stroke="#D1D5DB" stroke-width="1" />
      <!-- Y-axis -->
      <line x1="40" y1="40" x2="40" :y2="height - 40" stroke="#D1D5DB" stroke-width="1" />
      
      <!-- X-axis labels -->
      <template v-for="(label, index) in data.labels">
        <text 
          :x="40 + (index * (width - 80) / (data.labels.length - 1))" 
          :y="height - 20" 
          text-anchor="middle" 
          class="text-xs fill-gray-500"
        >
          {{ label }}
        </text>
      </template>
      
      <!-- Y-axis labels -->
      <template v-for="(tick, index) in 5">
        <text 
          x="30" 
          :y="height - 40 - (index * (height - 80) / 4)" 
          text-anchor="end" 
          class="text-xs fill-gray-500"
        >
          {{ Math.round(Math.max(...data.datasets.flatMap(d => d.data)) * index / 4) }}
        </text>
        <line 
          x1="40" 
          :y1="height - 40 - (index * (height - 80) / 4)" 
          :x2="width - 40" 
          :y2="height - 40 - (index * (height - 80) / 4)" 
          stroke="#E5E7EB" 
          stroke-width="1" 
          stroke-dasharray="2,2" 
        />
      </template>
      
      <!-- Bars -->
      <template v-for="(dataset, i) in data.datasets">
        <template v-for="(value, index) in dataset.data">
          <rect 
            :x="40 + (index * (width - 80) / data.labels.length) + 10" 
            :y="height - 40 - (value * (height - 80) / Math.max(...dataset.data))" 
            :width="(width - 80) / data.labels.length - 20" 
            :height="value * (height - 80) / Math.max(...dataset.data)" 
            :fill="dataset.color === 'green' ? '#10B981' : '#3B82F6'" 
            rx="2" 
          />
        </template>
      </template>
    </svg>
  </div>
</template>