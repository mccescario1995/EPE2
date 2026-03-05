<script setup>
import { useRoute } from "vue-router";
import { onMounted, watch } from "vue";

import TopBar from "./TopBar.vue";
import Overlay from "./Overlay.vue";
import Sidebar from "./sidebar/Sidebar.vue";
import { closeSidebar, sidebarOpen } from "./store";

const route = useRoute();

onMounted(() => {
  // set the html tag attribute overflow to hidden when component is mounted
  document.documentElement.style.overflow = "hidden";
});

watch(route, () => {
  // close sidebar on route changes when viewport is less than 1024px
  if (sidebarOpen && window.innerWidth < 1024) {
    closeSidebar();
  }
});
</script>
<template>
  <div class="relative h-screen overflow-hidden bg-[#F4F9FB]">
    <div class="flex items-start">
      <Overlay />
      <Sidebar mobile-orientation="end" />
      <div class="flex h-screen w-full flex-col pl-0 lg:space-y-4">
        <TopBar />
        <main class="h-screen overflow-auto px-2 pb-36 pt-4 md:pb-8 lg:px-4">
          <slot />
        </main>
      </div>
    </div>
  </div>
</template>
