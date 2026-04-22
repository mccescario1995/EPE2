<script setup>
import { useRoute } from "vue-router";
import { onMounted, watch } from "vue";
import TopBar from "../dashboard/TopBar.vue";
import Overlay from "../dashboard/Overlay.vue";
import Sidebar from "../dashboard/sidebar/Sidebar.vue";
import Footer from "../dashboard/Footer.vue";
import { closeSidebar, sidebarOpen } from "../dashboard/store";

const route = useRoute();
const { isOpen, message, title, type, confirm, close } = useConfirmModal();

onMounted(() => {
  document.documentElement.style.overflow = "hidden";
});

watch(route, () => {
  if (sidebarOpen && window.innerWidth < 1024) {
    closeSidebar();
  }
});
</script>
<template>
  <div class="font-medium">
    <div class="flex items-start">
      <Overlay />
      <Sidebar mobile-orientation="start" />
      <div
        class="flex flex-col min-h-screen w-full overflow-x-auto bg-linear-to-br from-blue-50 to-indigo-100"
      >
        <TopBar />
        <Breadcrumbs class="m-4" />
        <main class="flex-1 overflow-auto md:pb-8 lg:px-4 relative">
          <!-- Added relative here -->
          <div
            class="relative h-[85vh] overflow-auto shadow-md rounded-xl bg-white"
          >
            <!-- Wrapper div for slot content -->
            <ClientOnly>
              <slot />
            </ClientOnly>
            <LoadingIndicator />
            <!-- Moved inside the slot wrapper -->
            <Footer class="hidden md:block" />
          </div>
          <Toast />
          <ConfirmationModal
            :is-open="isOpen"
            :message="message"
            :title="title"
            :type="type"
            @confirm="confirm"
            @close="close"
          />
        </main>
      </div>
    </div>
  </div>
</template>
