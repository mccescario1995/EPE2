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

const generateLinePath = (points) => {
  let path = `M ${points[0].x} ${points[0].y}`
  for (let i = 1; i < points.length; i++) {
    path += ` L ${points[i].x} ${points[i].y}`
  }
  return path
}

const calculateCoordinates = (data, width, height, padding = 40) => {
  const maxValue = Math.max(...data)
  const minValue = Math.min(...data)
  const range = maxValue - minValue
  
  return data.map((value, index) => {
    const x = padding + (index * (width - padding * 2) / (data.length - 1))
    const y = height - padding - ((value - minValue) / range) * (height - padding * 2)
    return { x, y }
  })
}
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
          {{ Math.round(Math.max(...data.datasets.flatMap(d => d.data)) * index / 4).toLocaleString() }}
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
      
      <!-- Line paths -->
      <template v-for="(dataset, i) in data.datasets">
        <path 
          :d="generateLinePath(calculateCoordinates(dataset.data, width, height))" 
          :stroke="dataset.color === 'indigo' ? '#4F46E5' : '#3B82F6'" 
          stroke-width="2" 
          fill="none" 
        />
        
        <!-- Data points -->
        <template v-for="(point, j) in calculateCoordinates(dataset.data, width, height)">
          <circle 
            :cx="point.x" 
            :cy="point.y" 
            r="4" 
            :fill="dataset.color === 'indigo' ? '#4F46E5' : '#3B82F6'" 
            stroke="white" 
            stroke-width="1" 
          />
        </template>
      </template>
      
      <!-- Legend -->
      <template v-for="(dataset, i) in data.datasets">
        <circle 
          :cx="width - 100" 
          :cy="40 + (i * 20)" 
          r="4" 
          :fill="dataset.color === 'indigo' ? '#4F46E5' : '#3B82F6'" 
        />
        <text 
          :x="width - 90" 
          :y="44 + (i * 20)" 
          class="text-xs fill-gray-700"
        >
          {{ dataset.label }}
        </text>
      </template>
    </svg>
  </div>
</template>