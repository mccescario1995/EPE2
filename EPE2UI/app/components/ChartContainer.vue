<script setup>
import LineChart from './charts/LineChart.vue'
import BarChart from './charts/BarChart.vue'
import PieChart from './charts/PieChart.vue'
import RadarChart from './charts/RadarChart.vue'

const props = defineProps({
  type: {
    type: String,
    required: true,
    validator: (value) => ['line', 'bar', 'pie', 'radar'].includes(value)
  },
  data: {
    type: Object,
    required: true
  },
  title: {
    type: String,
    default: ''
  }
})

const components = {
  line: LineChart,
  bar: BarChart,
  pie: PieChart,
  radar: RadarChart
}
</script>

<template>
  <div class="bg-white p-6 rounded-lg shadow h-full">
    <h2 v-if="title" class="text-lg font-medium text-gray-900 mb-4">{{ title }}</h2>
    <component :is="components[type]" :data="data" />
  </div>
</template>