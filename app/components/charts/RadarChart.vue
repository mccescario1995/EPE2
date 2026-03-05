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
    default: 400,
  },
  height: {
    type: Number,
    default: 100,
  },
});
</script>

<template>
  <div class="relative h-full w-full flex items-center justify-center">
    <svg
      class="w-96 h-96"
      :viewBox="`0 0 ${width} ${height}`"
      preserveAspectRatio="xMidYMid meet"
    >
      <!-- Radar grid -->
      <polygon
        v-for="(level, i) in 5"
        :key="i"
        :points="`
          200,${50 + i * 40} 
          ${200 + Math.sin(((Math.PI * 2) / 6) * 1) * (i + 1) * 30},${
          50 + Math.cos(((Math.PI * 2) / 6) * 1) * (i + 1) * 30
        }
          ${200 + Math.sin(((Math.PI * 2) / 6) * 2) * (i + 1) * 30},${
          50 + Math.cos(((Math.PI * 2) / 6) * 2) * (i + 1) * 30
        }
          ${200 + Math.sin(((Math.PI * 2) / 6) * 3) * (i + 1) * 30},${
          50 + Math.cos(((Math.PI * 2) / 6) * 3) * (i + 1) * 30
        }
          ${200 + Math.sin(((Math.PI * 2) / 6) * 4) * (i + 1) * 30},${
          50 + Math.cos(((Math.PI * 2) / 6) * 4) * (i + 1) * 30
        }
          ${200 + Math.sin(((Math.PI * 2) / 6) * 5) * (i + 1) * 30},${
          50 + Math.cos(((Math.PI * 2) / 6) * 5) * (i + 1) * 30
        }
        `"
        fill="none"
        stroke="#E5E7EB"
        stroke-width="1"
      />

      <!-- Axes -->
      <line
        v-for="(label, i) in data.labels"
        :key="label"
        :x1="200"
        :y1="50"
        :x2="200 + Math.sin(((Math.PI * 2) / 6) * i) * 150"
        :y2="50 + Math.cos(((Math.PI * 2) / 6) * i) * 150"
        stroke="#D1D5DB"
        stroke-width="1"
      />

      <!-- Axis labels -->
      <text
        v-for="(label, i) in data.labels"
        :key="label"
        :x="200 + Math.sin(((Math.PI * 2) / 6) * i) * 170"
        :y="50 + Math.cos(((Math.PI * 2) / 6) * i) * 170"
        text-anchor="middle"
        class="text-xs fill-gray-500"
      >
        {{ label }}
      </text>

      <!-- Data polygons -->
      <polygon
        v-for="dataset in data.datasets"
        :key="dataset.label"
        :points="`
          ${200 + Math.sin(((Math.PI * 2) / 6) * 0) * dataset.data[0] * 1.5},${
          50 + Math.cos(((Math.PI * 2) / 6) * 0) * dataset.data[0] * 1.5
        } 
          ${200 + Math.sin(((Math.PI * 2) / 6) * 1) * dataset.data[1] * 1.5},${
          50 + Math.cos(((Math.PI * 2) / 6) * 1) * dataset.data[1] * 1.5
        } 
          ${200 + Math.sin(((Math.PI * 2) / 6) * 2) * dataset.data[2] * 1.5},${
          50 + Math.cos(((Math.PI * 2) / 6) * 2) * dataset.data[2] * 1.5
        } 
          ${200 + Math.sin(((Math.PI * 2) / 6) * 3) * dataset.data[3] * 1.5},${
          50 + Math.cos(((Math.PI * 2) / 6) * 3) * dataset.data[3] * 1.5
        } 
          ${200 + Math.sin(((Math.PI * 2) / 6) * 4) * dataset.data[4] * 1.5},${
          50 + Math.cos(((Math.PI * 2) / 6) * 4) * dataset.data[4] * 1.5
        } 
          ${200 + Math.sin(((Math.PI * 2) / 6) * 5) * dataset.data[5] * 1.5},${
          50 + Math.cos(((Math.PI * 2) / 6) * 5) * dataset.data[5] * 1.5
        }
        `"
        :fill="
          dataset.color === 'purple'
            ? 'rgba(124, 58, 237, 0.2)'
            : 'rgba(236, 72, 153, 0.2)'
        "
        :stroke="dataset.color === 'purple' ? '#7C3AED' : '#EC4899'"
        stroke-width="1.5"
      />

      <!-- Data points -->
      <circle
        v-for="(value, i) in data.datasets[0].data"
        :key="i"
        :cx="
          200 +
          Math.sin(((Math.PI * 2) / 6) * i) * data.datasets[0].data[i] * 1.5
        "
        :cy="
          50 +
          Math.cos(((Math.PI * 2) / 6) * i) * data.datasets[0].data[i] * 1.5
        "
        r="3"
        fill="#7C3AED"
        stroke="white"
        stroke-width="1"
      />
      <circle
        v-for="(value, i) in data.datasets[1].data"
        :key="i"
        :cx="
          200 +
          Math.sin(((Math.PI * 2) / 6) * i) * data.datasets[1].data[i] * 1.5
        "
        :cy="
          50 +
          Math.cos(((Math.PI * 2) / 6) * i) * data.datasets[1].data[i] * 1.5
        "
        r="3"
        fill="#EC4899"
        stroke="white"
        stroke-width="1"
      />

      <!-- Legend -->
      <circle cx="300" cy="40" r="4" fill="#7C3AED" />
      <text x="310" y="44" class="text-xs fill-gray-700">
        {{ data.datasets[0].label }}
      </text>
      <circle cx="300" cy="60" r="4" fill="#EC4899" />
      <text x="310" y="64" class="text-xs fill-gray-700">
        {{ data.datasets[1].label }}
      </text>
    </svg>
    <div></div>
  </div>
</template>
