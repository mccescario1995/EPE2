<script setup>
import { ref, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import { sidebarOpen } from "../store"
import SidebarItems from "./SidebarItems.vue"

const router = useRouter()

defineProps({
  mobileOrientation: {
    type: String,
    default: "end",
    validator: (value) => ["start", "end"].includes(value),
  },
})

const menu = ref(null)

// Mobile sidebar handling
const isMobile = ref(false)
const checkMobile = () => {
  isMobile.value = window.innerWidth < 1024
}

onMounted(() => {
  checkMobile()
  window.addEventListener('resize', checkMobile)
  document.addEventListener("click", handleClickOutside)
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', checkMobile)
  document.removeEventListener("click", handleClickOutside)
})

const handleClickOutside = (event) => {
  if (isMobile.value && !sidebarOpen.value && menu.value && !menu.value.contains(event.target)) {
    sidebarOpen.value = false
  }
}

const mobileOrientationClasses = {
  start: "left-0",
  end: "right-0 lg:left-0",
}
</script>

<template>
  <div class="app ">
    <!-- Backdrop for mobile -->
    <Transition name="fade">
      <div 
        v-if="sidebarOpen && isMobile"
        class="fixed inset-0 z-30 bg-black opacity-50 lg:hidden"
        @click="sidebarOpen = false"
      />
    </Transition>

    <aside
      ref="menu"
      class="sidebar bg-white border-r dark:bg-gray-800  shadow-lg flex flex-col h-screen fixed lg:sticky top-0 z-40 transition-all duration-300 ease-in-out"
      :class="[
        mobileOrientationClasses[mobileOrientation],
        sidebarOpen 
          ? 'w-64 translate-x-0' 
          : '-translate-x-full lg:translate-x-0 lg:w-20',
      ]"
    >
      <!-- Logo Section -->
      <div class="px-4 py-5 flex justify-center items-center border-b border-gray-100 dark:border-gray-700">
        <NuxtLink
          to="/main/dashboard"
          class="flex items-center justify-center transition-all duration-300 hover:bg-gray-50 rounded-lg p-2"
        >
          <img
            src="/static/images/epe_logo.png"
            alt="EPE Logo"
            :class="[
              'transition-all duration-300 object-contain',
              sidebarOpen ? 'w-15' : 'w-7',
            ]"
          />
        </NuxtLink>
      </div>

      <!-- Menu Content -->
      <div class="flex-1 flex flex-col overflow-y-auto custom-scrollbar px-3 py-4">
        <!-- Menu Label -->
        <Transition name="fade-slide" mode="out-in">
          <div v-if="sidebarOpen" class="px-3 mb-2">
            <span class="text-xs font-medium text-gray-500 dark:text-gray-100 uppercase tracking-wider">
              Menus
            </span>
          </div>
        </Transition>

        <!-- Menu Items -->
        <SidebarItems />
      </div>
    </aside>
  </div>
</template>

<style scoped>
/* Custom scrollbar */
   .custom-scrollbar::-webkit-scrollbar {
     width: 4px;
     height: 4px;
   }
   
   .custom-scrollbar::-webkit-scrollbar-track {
     background: transparent;
   }
   
   .custom-scrollbar::-webkit-scrollbar-thumb {
     background: #cbd5e1;
     /* Dark mode: background: #4b5563; */
     background: #6b7280; /* Darker gray for dark mode */
   
     border-radius: 2px;
   }
   
   .custom-scrollbar::-webkit-scrollbar-thumb:hover {
     background: #a0aec0;
   }

/* Transitions */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

.fade-slide-enter-active {
  transition: all 0.3s ease-out;
}

.fade-slide-leave-active {
  transition: all 0.2s ease-in;
}

.fade-slide-enter-from,
.fade-slide-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}

.sidebar {
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.05);
}

@media (max-width: 1023px) {
  .sidebar {
    width: 16rem;
  }
}
</style>