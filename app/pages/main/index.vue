<script setup>
// Chart data
const lineChartData = {
  labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul'],
  datasets: [
    {
      label: 'Revenue',
      data: [5000, 8000, 6500, 12000, 9500, 14000, 16000],
      color: 'indigo'
    },
    {
      label: 'Users',
      data: [3000, 5000, 4000, 7000, 6000, 9000, 11000],
      color: 'blue'
    }
  ]
}

const barChartData = {
  labels: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
  datasets: [
    {
      label: 'Sales',
      data: [12, 19, 3, 5, 2, 3, 9],
      color: 'green'
    }
  ]
}

const pieChartData = {
  labels: ['Desktop', 'Mobile', 'Tablet'],
  datasets: [
    {
      data: [60, 30, 10],
      colors: ['#4F46E5', '#3B82F6', '#10B981']
    }
  ]
}

const radarChartData = {
  labels: ['Speed', 'Reliability', 'UX', 'Features', 'Support', 'Security'],
  datasets: [
    {
      label: 'Current',
      data: [80, 90, 70, 85, 75, 95],
      color: 'purple'
    },
    {
      label: 'Target',
      data: [70, 82, 95, 75, 90, 80],
      color: 'pink'
    }
  ]
}

// User selected charts
const availableCharts = [
  { type: 'line', title: 'Revenue vs Users' },
  { type: 'bar', title: 'Weekly Sales' },
  { type: 'pie', title: 'Device Distribution' },
  { type: 'radar', title: 'Performance Metrics' }
]

const selectedCharts = ref([
  { type: 'line', title: 'Revenue vs Users' },
  { type: 'bar', title: 'Weekly Sales' }
])

const addChart = (chartType) => {
  if (selectedCharts.value.length >= 4) return
  const chartToAdd = availableCharts.find(c => c.type === chartType)
  if (chartToAdd && !selectedCharts.value.some(c => c.type === chartType)) {
    selectedCharts.value.push(chartToAdd)
  }
}

const removeChart = (chartType) => {
  selectedCharts.value = selectedCharts.value.filter(c => c.type !== chartType)
}

const getChartData = (type) => {
  switch (type) {
    case 'line': return lineChartData
    case 'bar': return barChartData
    case 'pie': return pieChartData
    case 'radar': return radarChartData
    default: return null
  }
}
</script>

<template>
  <div class="min-h-screen bg-gray-50 overflow-auto h-[75vh]">
    <header class="bg-white shadow">
      <div class="max-w-full mx-auto py-6 px-4 sm:px-6 lg:px-8 flex justify-between items-center">
        <h1 class="text-3xl font-bold text-gray-900">Dashboard</h1>
        <div class="relative">
          <button class="bg-indigo-600 text-white px-4 py-2 rounded-md">
            Add Chart
          </button>
          <div class="absolute right-0 mt-2 w-48 bg-white rounded-md shadow-lg z-10 hidden">
            <div class="py-1">
              <template v-for="chart in availableCharts" :key="chart.type">
                <button 
                  v-if="!selectedCharts.some(c => c.type === chart.type)"
                  @click="addChart(chart.type)"
                  class="block w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                >
                  Add {{ chart.title }}
                </button>
              </template>
            </div>
          </div>
        </div>
      </div>
    </header>
    <main class="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
      <!-- Chart Selection Controls -->
      <div class="mb-6 bg-white p-4 rounded-lg shadow">
        <h2 class="text-lg font-medium text-gray-900 mb-2">Select Charts</h2>
        <div class="flex flex-wrap gap-2">
          <template v-for="chart in availableCharts" :key="chart.type">
            <button 
              @click="selectedCharts.some(c => c.type === chart.type) ? removeChart(chart.type) : addChart(chart.type)"
              :class="[
                selectedCharts.some(c => c.type === chart.type) 
                  ? 'bg-indigo-600 text-white' 
                  : 'bg-gray-200 text-gray-800',
                'px-3 py-1 rounded-md text-sm'
              ]"
            >
              {{ chart.title }}
            </button>
          </template>
        </div>
      </div>

      <!-- Dynamic Charts Grid -->
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-8">
        <template v-for="chart in selectedCharts" :key="chart.type">
          <chart-container 
            :type="chart.type" 
            :data="getChartData(chart.type)" 
            :title="chart.title" 
          />
        </template>
      </div>
    </main>
  </div>
</template>