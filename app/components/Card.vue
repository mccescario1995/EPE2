<template>
  <div 
    class="bg-white rounded-xl shadow-lg overflow-hidden transition-all duration-300"
    :class="{
      'hover:shadow-xl hover:-translate-y-1': hoverEffect,
      'max-w-sm': !fullWidth
    }"
  >
    <!-- Image slot (if provided) -->
    <div v-if="$slots.image || image" class="relative h-48 overflow-hidden">
      <slot name="image">
        <img 
          :src="image" 
          class="w-full h-full object-cover transition-transform duration-500 hover:scale-105"
          :alt="imageAlt"
        >
        <div v-if="imageOverlay" class="absolute inset-0 bg-gradient-to-t from-black/30 to-transparent"></div>
      </slot>
    </div>

    <!-- Card content -->
    <div class="p-6">
      <!-- Header section -->
      <div class="flex items-center justify-between mb-4">
        <h3 class="text-xl font-semibold text-gray-800">
          <slot name="title">{{ title }}</slot>
        </h3>
        <span 
          v-if="badge" 
          class="px-3 py-1 text-xs font-medium rounded-full"
          :class="badgeClasses"
        >
          {{ badge }}
        </span>
      </div>

      <!-- Main content -->
      <p class="text-gray-600 leading-relaxed">
        <slot>{{ content }}</slot>
      </p>

      <!-- Footer section - Fixed implementation -->
      <div v-if="$slots.footer || (showActions && (actionText || secondaryAction))" class="mt-6">
        <slot name="footer">
          <div class="flex justify-between items-center">
            <button 
              v-if="actionText" 
              class="text-sm font-medium transition-colors"
              :class="actionTextClasses"
              @click="$emit('action')"
            >
              {{ actionText }} →
            </button>
            <button 
              v-if="secondaryAction" 
              class="px-4 py-2 text-sm font-medium rounded-md transition-colors"
              :class="secondaryActionClasses"
              @click="$emit('secondary-action')"
            >
              {{ secondaryAction }}
            </button>
          </div>
        </slot>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';

const props = defineProps({
  title: {
    type: String,
    default: 'Card Title'
  },
  content: {
    type: String,
    default: 'Card content goes here. You can customize this with props or slots.'
  },
  image: {
    type: String,
    default: ''
  },
  imageAlt: {
    type: String,
    default: 'Card image'
  },
  imageOverlay: {
    type: Boolean,
    default: false
  },
  badge: {
    type: String,
    default: ''
  },
  badgeType: {
    type: String,
    default: 'default', // 'default', 'info', 'success', 'warning', 'danger'
    validator: value => ['default', 'info', 'success', 'warning', 'danger'].includes(value)
  },
  actionText: {
    type: String,
    default: ''
  },
  actionType: {
    type: String,
    default: 'primary', // 'primary', 'secondary', 'text'
    validator: value => ['primary', 'secondary', 'text'].includes(value)
  },
  secondaryAction: {
    type: String,
    default: ''
  },
  hoverEffect: {
    type: Boolean,
    default: true
  },
  fullWidth: {
    type: Boolean,
    default: false
  },
  showActions: {
    type: Boolean,
    default: false
  }
});

const emit = defineEmits(['action', 'secondary-action']);

const badgeClasses = computed(() => {
  const classes = {
    default: 'bg-gray-100 text-gray-800',
    info: 'bg-blue-100 text-blue-800',
    success: 'bg-green-100 text-green-800',
    warning: 'bg-yellow-100 text-yellow-800',
    danger: 'bg-red-100 text-red-800'
  };
  return classes[props.badgeType];
});

const actionTextClasses = computed(() => {
  return {
    primary: 'text-blue-600 hover:text-blue-800',
    secondary: 'text-gray-600 hover:text-gray-800',
    text: 'text-gray-500 hover:text-gray-700'
  }[props.actionType];
});

const secondaryActionClasses = computed(() => {
  return {
    primary: 'bg-blue-600 text-white hover:bg-blue-700',
    secondary: 'bg-gray-100 text-gray-700 hover:bg-gray-200',
    text: 'bg-transparent text-gray-500 hover:text-gray-700'
  }[props.actionType];
});
</script>