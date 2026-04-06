<script setup>
import { ref, computed, onMounted, onBeforeUnmount, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { sidebarOpen } from "../store";
import { useMenuService } from "~/services/menuService";
import { sub } from "date-fns";

const { fetchMenuList } = useMenuService();
const route = useRoute();
const router = useRouter();

const menulist = ref([]);
const isMobile = ref(false);
const searchQuery = ref("");

const checkMobile = () => {
  isMobile.value = window.innerWidth < 1024;
};

const refreshing = ref(false);

async function refreshAll() {
  const { data, refresh } = useAsyncData(
    "dataKey",
    async () => await fetchMenuList(),
  );

  refreshing.value = true;
  try {
    console.log("refreshing");

    await refreshNuxtData();
  } finally {
    console.log("refreshed");

    refreshing.value = false;
  }
}

onMounted(async () => {
  const staticMenus = [
    // {
    //   id: 1,
    //   title: "Activity",
    //   isopen: false,
    //   isActive: false,
    //   submenu: [
    //     {
    //       id: "A1",
    //       title: "Notification",
    //       route: "/activity/notificationpage",
    //     },
    //     { id: "A2", title: "Modal", route: "/activity/modal" },
    //     { id: "A3", title: "Buttons", route: "/activity/buttons" },
    //     { id: "A4", title: "Table", route: "/activity/table" },
    //     { id: "A5", title: "Cards", route: "/activity/cards" },
    //     { id: "A6", title: "Forms", route: "/activity/forms" },
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
    //   isActive: true,
    // },
    {
      id: 3,
      title: "Requests",
      isopen: true,
      submenu: [
        {
          id: "E1",
          title: "All",
          route: "/exemptions/",
        },
        // {
        //   id: "E2",
        //   title: "Create Request",
        //   route: "/exemptions/create",
        // },
        {
          id: "E3",
          title: "Pending",
          route: "/exemptions/pending",
        },
        {
          id: "E4",
          title: "Closed",
          route: "/exemptions/closed",
        },
      ],
    },

    // =============================================================

    // {
    //   id: 4,
    //   title: "Excemption Requests",
    //   route: "/exemptions/",
    // },
    // {
    //   id: 5,
    //   title: "Create Request",
    //   route: "/exemptions/create",
    // },
    // {
    //   id: 5,
    //   title: "Pending Approvals",
    //   route: "/exemptions/pending",
    // },
    // {
    //   id: 5,
    //   title: "Closed Requests",
    //   route: "/exemptions/closed",
    // },
  ];

  const dynamicmenu = await fetchMenuList();
  menulist.value = [...staticMenus];
  checkMobile();
  window.addEventListener("resize", checkMobile);
});

onBeforeUnmount(() => {
  window.removeEventListener("resize", checkMobile);
});

const updateActiveStates = (path) => {
  menulist.value.forEach((menu) => {
    menu.isActive = false;
    menu.isopen = false;
    if (menu.submenu) {
      // Check if any submenu route matches current path (exact match)
      const hasActiveSubmenu = menu.submenu.some((sub) => sub.route === path);
      
      // Set isclick for each submenu item
      menu.submenu.forEach((sub) => (sub.isclick = sub.route === path));
      
      // Open dropdown if sidebar is open and has active submenu
      menu.isopen = sidebarOpen.value && hasActiveSubmenu;
      
      // Parent is active when any child is active (for highlighting)
      menu.isActive = hasActiveSubmenu;
    } else {
      menu.isActive = path === menu.route;
    }
  });
};

const navigateToMenu = (menu) => {
  if (menu.route) {
    router.push(menu.route);
    updateActiveStates(menu.route);
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
  const query = searchQuery.value.trim().toLowerCase();
  if (!query) return menulist.value;

  const matches = [];

  menulist.value.forEach((menu) => {
    const mainMatch =
      menu.title.toLowerCase().includes(query) ||
      menu.id?.toString().includes(query);

    const subMatches =
      menu.submenu?.filter(
        (sub) =>
          sub.title.toLowerCase().includes(query) ||
          sub.id?.toLowerCase().includes(query),
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
  <nav class="h-full flex flex-col bg-white shadow-sm overflow-y-auto">
    <!-- 🔍 Search -->
    <div v-if="sidebarOpen" class="px-3 py-3 border-b border-gray-100">
      <div class="relative">
        <svg
          xmlns="http://www.w3.org/2000/svg"
          class="absolute left-3 top-2.5 h-4 w-4 text-gray-400"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M21 21l-4.35-4.35M10 18a8 8 0 100-16 8 8 0 000 16z"
          />
        </svg>
        <input
          v-model="searchQuery"
          type="text"
          placeholder="Search menu or ID..."
          class="w-full rounded-lg border border-gray-200 pl-9 pr-3 py-2 text-sm focus:border-primary-400 focus:ring-1 focus:ring-primary-300 outline-none"
        />
      </div>
    </div>

    <!-- Refresh -->
    <!-- <div>
      <button class="border p-2" @click="refreshAll">
        Refetch All Data
      </button>
    </div> -->

    <!-- 🧭 Menu List -->
    <ul class="flex-1 space-y-1 px-2 py-4">
      <!-- Dashboard -->
      <li>
        <button
          @click="router.push('/dashboard')"
          class="group flex w-full items-center rounded-lg p-3 transition-all duration-200"
          :class="[
            route.path === '/dashboard'
              ? 'bg-gray-200 text-primary-700 font-semibold'
              : 'text-gray-700 hover:bg-gray-100 hover:text-gray-900',
          ]"
        >
          <span class="text-lg">📊</span>
          <span v-if="sidebarOpen" class="ml-3">Dashboard</span>
        </button>
      </li>

      <!-- Dynamic Menus -->
      <li
        v-for="(item, index) in filteredMenus"
        :key="index"
        @mouseenter="!item.isSubmenuItem && handleHover(item, true)"
        @mouseleave="!item.isSubmenuItem && handleHover(item, false)"
      >
        <!-- Parent Menu -->
        <button
          v-if="!item.isSubmenuItem"
          @click="toggleMenu(item)"
          class="group flex w-full items-center justify-between rounded-lg p-3 transition-all duration-200"
          :class="[
            item.isActive || (item.submenu && item.submenu.some(sub => sub.route === route.path))
              ? 'bg-gray-200 text-primary-700 font-semibold'
              : 'text-gray-700 hover:bg-gray-100 hover:text-gray-900',
          ]"
        >
          <div class="flex items-center">
            <span class="text-lg shrink-0">📁</span>
            <span v-if="sidebarOpen" class="ml-3">{{ item.title }}</span>
          </div>

          <svg
            v-if="sidebarOpen && item.submenu"
            xmlns="http://www.w3.org/2000/svg"
            class="h-4 w-4 transition-transform duration-200"
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
          class="flex w-full flex-col items-start rounded-lg px-3 py-2 text-sm transition-colors duration-150"
          :class="[
            item.isclick
              ? 'bg-primary-50 text-primary-600 font-medium'
              : 'text-gray-700 hover:bg-gray-100 hover:text-gray-900',
          ]"
        >
          <span v-if="sidebarOpen">
            {{ item.title }}
            <span class="text-xs text-gray-400 ml-1">
              ({{ item.parentTitle }})
            </span>
          </span>
          <span v-else>{{ item.title }}</span>
        </button>

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
                ? 'ml-8 mt-1 space-y-1 border-l border-gray-200 rounded'
                : 'absolute left-full ml-1 w-56 rounded-md bg-white py-1 shadow-lg ring-1 ring-black ring-opacity-5 z-10',
            ]"
          >
            <li v-for="(sub, i2) in item.submenu" :key="i2">
              <button
                @click="navigateToMenu(sub)"
                class="flex flex-col w-full items-start rounded-md px-3 py-2 text-sm transition-colors duration-150"
                :class="[
                  sub.route === route.path
                    ? 'bg-gray-200 text-primary-700 font-semibold'
                    : 'text-gray-700 hover:bg-gray-100 hover:text-gray-900',
                ]"
              >
                <span>{{ sub.title }}</span>
                <span
                  v-if="searchQuery != ''"
                  class="text-xs text-gray-400 ml-1"
                >
                  ({{ item.title }})
                </span>
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
</style>
