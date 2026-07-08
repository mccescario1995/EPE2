<script setup>
import '@fortawesome/fontawesome-free/css/all.min.css'
import { ref, computed, onMounted, onBeforeUnmount, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { sidebarOpen } from "../store";
import { useMenuService } from "~/services/menuService";
import { startLoading,stopLoading } from "~/stores/globalvar";
 
 
const { fetchMenuList } = useMenuService();
const route = useRoute();
const router = useRouter();
 
const menulist = ref([]);
const isMobile = ref(false);
const searchQuery = ref("");
const activeQuery = ref('') // the query used for filtering
 
const checkMobile = () => {
  isMobile.value = window.innerWidth < 1024;
};
 
onMounted(async () => {
  const staticMenus = [
    // {
    //   id: 1,
    //   title: "Activity",
    //   isopen: false,
    //   isActive: false,
    //   submenu: [
    //     { id: "A1", title: "Notification", route: "/main/notificationpage" },
    //     { id: "A2", title: "Modal", route: "/main/modal" },
    //     { id: "A3", title: "Buttons", route: "/main/buttons" },
    //     { id: "A4", title: "Table", route: "/main/table" },
    //     { id: "A5", title: "Cards", route: "/main/cards" },
    //     { id: "A6", title: "Forms", route: "/main/forms" },
    //   ],
    // },
    // {
    //   id: 2,
    //   title: "Documentation",
    //   isopen: false,
    //   isActive: false,
    //   submenu: [
    //     { id: "D1", title: "Table", route: "/documentation/table" },
    //     { id: "D2", title: "Sidebar", route: "/documentation/sidebar" },
    //     {
    //       id: "D3",
    //       title: "Notification",
    //       route: "/documentation/notification",
    //     },
    //     { id: "D4", title: "Cards", route: "/documentation/card" },
    //     { id: "D5", title: "Modals", route: "/documentation/modal" },
    //     { id: "D6", title: "Dropdown", route: "/documentation/dropdown" },
    //     { id: "D7", title: "Input", route: "/documentation/input" },
    //   ],
    // },
    // {
    //   id: 3,
    //   title: "Report",
    //   route: "/dashboard",
    //   iswithfunction: true,
    //   isopen: false,
    //   isActive: false,
    // },
  ];
 
  const dynamicmenu = await fetchMenuList();
  menulist.value = [...staticMenus, ...dynamicmenu];
  checkMobile();
  window.addEventListener("resize", checkMobile);
});
 
onBeforeUnmount(() => {
  window.removeEventListener("resize", checkMobile);
});
 
const updateActiveStates = (path) => {
 
  const normalizedPath = path.toLowerCase();
 
  menulist.value.forEach((menu) => {
    menu.isActive = false;
 
    if (menu.submenu && menu.submenu.length > 0 ) {
      menu.submenu.forEach((sub) => {
        const base = sub.route.toLowerCase();
        const isExact = normalizedPath === base;
        const isChild = normalizedPath.startsWith(base + "/");
 
        sub.isclick = isExact || isChild;
      });
 
      // Keep parent menu open if any child is active
      menu.isopen = menu.submenu.some((sub) => sub.isclick);
      menu.isActive = menu.isopen;
      console.log("here",menu.isActive);
    } else {
      const base = menu.route.toLowerCase();
      const isExact = normalizedPath === base;
      const isChild = normalizedPath.startsWith(base + "/");
 
      menu.isActive = isExact || isChild;
      console.log("here",menu.isActive);
    }
  });
};
 
const navigateToMenu = async (menu) => {
  console.log(menu);
  if (!menu.route) return;
  startLoading();
  try {
    await router.push(menu.route);
    updateActiveStates(menu.route);
  } finally {
    stopLoading();
  }
};
 
const toggleMenu = (menu) => {
  // If menu has submenu, toggle open/close
  if (menu.submenu && menu.submenu.length > 0) {
    menu.isopen = !menu.isopen;
    menulist.value.forEach((m) => {
      if (m !== menu) m.isopen = false;
    });
  }
  // If menu has a function or route
  else if (menu.iswithfunction || menu.route) {
    navigateToMenu(menu);
  }
};
 
// ✅ Centralized click handler
const handleClick = (item) => {
  if (item.submenu && item.submenu.length > 0) {
    toggleMenu(item);
  } else if (item.iswithfunction) {
    navigateToMenu(item);
  } else if (item.route) {
    navigateToMenu(item);
  }
};
 
// 🧠 Smart Search
const filteredMenus = computed(() => {
 const query = activeQuery.value.trim().toLowerCase()
  if (!query) return menulist.value
  const matches = [];
 
  menulist.value.forEach((menu) => {
    const mainMatch =
      menu.title.toLowerCase().includes(query) ||
      menu.id?.toString().includes(query);
 
    const subMatches =
      menu.submenu?.filter(
        (sub) =>
          sub.title.toLowerCase().includes(query) ||
          sub.id?.toLowerCase().includes(query)
      ) || [];
 
    // If main menu matches, include it normally
    if (mainMatch) matches.push({ ...menu, isopen: true });
 
    // If submenu matches and main doesn't, flatten them as independent items
    if (!mainMatch && subMatches.length > 0) {
      subMatches.forEach((sub) => {
        matches.push({
          id: sub.id,
          title: sub.title,
          route: sub.route,
          isclick: sub.isclick,
          isSubmenuItem: true,
          parentTitle: menu.title,
        });
      });
    }
  });
 
  return matches;
});
 
const applySearch = () => {
  activeQuery.value = searchQuery.value
}
 
// Clear both fields
const clearSearch = () => {
  searchQuery.value = ''
  activeQuery.value = ''
}
 
 
const handleHover = (menu, isHovering) => {
  if (!sidebarOpen.value) menu.isopen = isHovering;
};
 
updateActiveStates(route.path);
router.afterEach((to) => updateActiveStates(to.path));
 
watch(sidebarOpen, (newVal) => {
  if (!newVal) menulist.value.forEach((m) => (m.isopen = false));
  else updateActiveStates(route.path);
});
watch(sidebarOpen, (newVal) => {
  if (!newVal) searchQuery.value = "";
});
 
watch(searchQuery, (val) => {
  if (!val) {
    menulist.value.forEach((m) => (m.isopen = false));
  }
});
</script>
 
<template>
  <nav class="h-full flex flex-col bg-white dark:bg-gray-800 shadow-sm overflow-y-auto">
    <!-- 🔍 Search -->
    <div v-if="sidebarOpen" class="px-2 py-2 border-b border-gray-100 dark:border-gray-700">
      <div class="relative">
       
        <div class="relative w-full">
          <!-- Search icon (left side) -->
          <svg
            xmlns="http://www.w3.org/2000/svg"
            class="absolute left-2 top-2 h-3.5 w-3.5 text-gray-400 dark:text-gray-500"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            stroke-width="2"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              d="M21 21l-4.35-4.35m0 0A7.5 7.5 0 104.5 4.5a7.5 7.5 0 0012.15 12.15z"
            />
          </svg>
 
          <!-- Search input -->
          <input
            v-model="searchQuery"
            @keyup.enter="applySearch"
            type="text"
            placeholder="Enter to search menu"
            class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-200 placeholder-gray-400 dark:placeholder-gray-500 pl-7 pr-2 py-1.5 text-base focus:border-primary-400 focus:ring-1 focus:ring-primary-300 outline-none"
          />
 
          <!-- Clear (remove) icon (right side) -->
          <button
            v-if="searchQuery"
            @click="clearSearch"
            type="button"
            class="absolute right-1.5 top-1 text-gray-400 dark:text-gray-500 hover:text-gray-600 dark:hover:text-gray-300"
          >
            <svg
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
              stroke-width="1.5"
              stroke="currentColor"
              class="size-4 cursor-pointer text-red-700 dark:text-red-400"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                d="m9.75 9.75 4.5 4.5m0-4.5-4.5 4.5M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"
              />
            </svg>
          </button>
        </div>
      </div>
    </div>
 
    <!-- 🧭 Menu List -->
    <ul class="flex-1 space-y-0.5 px-2 py-2">
      <!-- Dashboard -->
      <li>
        <button
          @click="router.push('/main/dashboard')"
          class="group flex w-full items-start rounded-lg p-2 transition-all duration-200"
          :class="[
            route.path === '/dashboard'
              ? 'bg-primary-100 dark:bg-primary-900/40 text-primary-700 dark:text-primary-300 font-semibold'
              : 'text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700 hover:text-gray-900 dark:hover:text-white',
          ]"
        >
          <span class="text-base">📊</span>
          <span v-if="sidebarOpen" class="ml-2 text-base">Dashboard</span>
        </button>
      </li>
 
   
      <!-- Dynamic Menus -->
      <li
        v-for="(item, index) in filteredMenus"
        :key="index"
        @mouseenter="!item.isSubmenuItem && handleHover(item, true)"
        @mouseleave="!item.isSubmenuItem && handleHover(item, false)"
      >
        <div v-if="item.submenu && item.submenu.length > 0">
          <!-- Parent Menu -->
          <button
            v-if="!item.isSubmenuItem"
            @click="toggleMenu(item)"
            class="group flex w-full items-center justify-between rounded-lg p-2 transition-all duration-200"
            :class="[
              item.isActive
                 ? 'bg-gray-100 dark:bg-gray-700 text-black dark:text-white'
      : 'text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700 hover:text-gray-900 dark:hover:text-white',
            ]"
          >
            <div class="flex items-center">
              <span v-if="item.icon" :class="item.icon" class="text-base flex-shrink-0"></span>
            <span v-else class="text-base flex-shrink-0">📁</span>
 
              <span v-if="sidebarOpen" class="ml-2 text-base">{{ item.title }}</span>
            </div>
 
            <svg
              v-if="sidebarOpen && item.submenu"
              xmlns="http://www.w3.org/2000/svg"
              class="h-3.5 w-3.5 transition-transform duration-200"
              :class="{ 'rotate-180': item.isopen }"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M19 9l-7 7-7-7"
              />
            </svg>
          </button>
 
          <!-- Flattened submenu (search result only) -->
          <button
            v-else
            @click="navigateToMenu(item)"
            class="flex w-full flex-col items-start rounded-lg px-2 py-1.5 text-base transition-colors duration-150"
            :class="[
              item.isclick
                ? 'bg-primary-50 dark:bg-primary-900/30 text-primary-600 dark:text-primary-300 font-medium'
                : 'text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700 hover:text-gray-900 dark:hover:text-white',
            ]"
          >
            <span v-if="sidebarOpen">
              {{ item.title }}
              <span class="text-base text-gray-400 dark:text-gray-500 ml-1">
                ({{ item.parentTitle }})
              </span>
            </span>
            <span v-else>{{ item.title }}</span>
          </button>
        </div>
        <div v-else>
          <!-- If NO submenu or empty submenu, make it clickable -->
       <button
  v-if="!item.isSubmenuItem"
  @click="navigateToMenu(item)"
  class="group flex w-full items-center justify-start text-left rounded-lg p-2 transition-all duration-200"
  :class="[
    item.isActive
      ? 'bg-gray-100 dark:bg-gray-700 text-black dark:text-white'
      : 'text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700 hover:text-gray-900 dark:hover:text-white',
  ]"
>
  <span v-if="item.icon" :class="item.icon" class="text-base flex-shrink-0"></span>
  <span v-else class="text-base flex-shrink-0">📊</span>
  <span v-if="sidebarOpen" class="ml-2 text-base">{{ item.title }}</span>
</button>
          <button
            v-else
            @click="navigateToMenu(item)"
            class="flex flex-col w-full justify-start items-start text-left break-words rounded-md px-2 py-1.5 text-base transition-colors duration-150"
            :class="[
              item.isclick
                ? 'bg-gray-100 dark:bg-gray-700 text-primary-600 dark:text-primary-300 font-medium'
                : 'text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700 hover:text-gray-900 dark:hover:text-white',
            ]"
          >
            <span v-if="sidebarOpen">
              {{ item.title }}
              <span class="text-base text-gray-400 dark:text-gray-500 ml-1">
                ({{ item.parentTitle }})
              </span>
            </span>
            <span v-else>{{ item.title }}</span>
          </button>
        </div>
 
        <!-- Submenu (only visible when expanded) -->
        <Transition
          enter-active-class="transition-all duration-200 ease-out"
          enter-from-class="opacity-0 max-h-0"
          enter-to-class="opacity-100 max-h-96"
          leave-active-class="transition-all duration-150 ease-in"
          leave-from-class="opacity-100 max-h-96"
          leave-to-class="opacity-0 max-h-0"
        >
          <ul
            v-if="item.isopen"
            :class="[
              isMobile || sidebarOpen
                ? 'ml-6 mt-1 space-y-0.5 border-l border-gray-200 dark:border-gray-700'
                : 'absolute left-full ml-1 w-44 rounded-md bg-white dark:bg-gray-800 py-1 shadow-lg ring-1 ring-black dark:ring-gray-700 ring-opacity-5 z-10',
            ]"
          >
            <li v-for="(sub, i2) in item.submenu" :key="i2">
              <button
                @click="navigateToMenu(sub)"
              class="group flex flex-col w-full justify-start items-start text-left break-words rounded-md px-2 py-1.5 text-base transition-colors duration-150"
                :class="[
                  sub.isclick
                     ? 'bg-gray-100 dark:bg-gray-700 text-black dark:text-white'
      : 'text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700 hover:text-gray-900 dark:hover:text-white',
                ]"
              >
              <div class="flex items-center">
                  <span v-if="sub.icon" :class="sub.icon" class="text-base flex-shrink-0"></span>
                <span class="ml-1">{{ sub.title }} </span>
               
                <span
                  v-if="searchQuery != ''"
                  class="text-base text-gray-400 dark:text-gray-500 ml-1"
                >
                  ({{ item.title }})
                </span>
                </div>
              </button>
            </li>
          </ul>
        </Transition>
      </li>
       
    </ul>
  </nav>
</template>
 
<style scoped>
nav {
  transition: all 0.3s ease;
}
nav::-webkit-scrollbar {
  width: 4px;
}
nav::-webkit-scrollbar-thumb {
  background: #cbd5e0;
  border-radius: 2px;
}
nav::-webkit-scrollbar-thumb:hover {
  background: #a0aec0;
}
 
@media (prefers-color-scheme: dark) {
  nav::-webkit-scrollbar-thumb {
    background: #4a5568;
  }
  nav::-webkit-scrollbar-thumb:hover {
    background: #718096;
  }
}
</style>