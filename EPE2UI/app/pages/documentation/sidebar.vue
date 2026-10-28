<template>
  <div class="p-8 max-w-7xl mx-auto">
    <h1 class="text-3xl font-bold mb-6">Sidebar Menu Component</h1>

    <section class="mb-12">
      <h2 class="text-2xl font-semibold mb-4">Introduction</h2>
      <p class="mb-4">
        The Sidebar Menu is a responsive navigation component that provides
        hierarchical navigation with collapsible submenus. It supports both
        desktop and mobile views with different interaction patterns for each.
      </p>

      <div class="bg-blue-50 border-l-4 border-blue-500 p-4 mb-6">
        <h3 class="font-bold mb-2">Key Features:</h3>
        <ul class="list-disc pl-5 space-y-1">
          <li>Responsive design (expanded/collapsed states)</li>
          <li>Nested menu support with smooth animations</li>
          <li>Active route highlighting</li>
          <li>Mobile-friendly touch interactions</li>
          <li>Hover interactions for desktop</li>
          <li>Customizable appearance</li>
          <li>Static or API-driven menu configuration</li>
        </ul>
      </div>
    </section>

    <!-- Basic Usage -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold mb-4">Basic Usage</h2>
      <p class="mb-4">
        The simplest way to implement the sidebar in your application:
      </p>

      <div class="bg-gray-50 p-4 rounded-lg mb-4">
        <h4 class="font-mono text-sm mb-2">Example Code:</h4>
        <pre
          class="bg-gray-800 text-gray-100 p-4 rounded text-sm overflow-x-auto"
        >
&lt;template&gt;
  &lt;div class="app-container"&gt;
    &lt;Sidebar /&gt;
    &lt;!-- Your main content here --&gt;
  &lt;/div&gt;
&lt;/template&gt;

&lt;script setup&gt;
import Sidebar from '@/components/Sidebar.vue';
&lt;/script&gt;</pre
        >
      </div>
    </section>

    <!-- Static Menu Configuration -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold mb-4">Static Menu Configuration</h2>
      <p class="mb-4">
        The menu can be configured with a static array in SidebarItems.vue:
      </p>

      <div class="bg-gray-50 p-4 rounded-lg mb-4">
        <h4 class="font-mono text-sm mb-2">Example Structure:</h4>
        <pre
          class="bg-gray-800 text-gray-100 p-4 rounded text-sm overflow-x-auto"
        >
const menulist = ref([
  {
    title: "Activity",
    isopen: false,
    isActive: false,
    submenu: [
      {
        title: "Notification",
        isclick: false,
        route: "/main/notificationpage",
      },
      {
        title: "Modal",
        isclick: false,
        route: "/main/modal",
      }
    ]
  }
]);</pre
        >
      </div>
    </section>

    <!-- Dynamic Menu Configuration -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold mb-4">Dynamic Menu from API</h2>
      <p class="mb-4">
        For API-driven menus, create a service to fetch and map menu data:
      </p>

      <div class="bg-gray-50 p-4 rounded-lg mb-4">
        <h4 class="font-mono text-sm mb-2">menuService.js:</h4>
        <pre
          class="bg-gray-800 text-gray-100 p-4 rounded text-sm overflow-x-auto"
        >
import { ref } from 'vue';

export const useMenuService = () => {
  const fetchMenuList = async () => {
    try {
      const response = await fetch('/api/menus');
      const data = await response.json();
      
      return data.map(menuGroup => ({
        title: menuGroup.name,
        isopen: false,
        isActive: false,
        submenu: menuGroup.items.map(item => ({
          title: item.label,
          isclick: false,
          route: item.path,
          icon: item.icon
        }))
      }));
    } catch (error) {
      console.error('Error fetching menu:', error);
      return [];
    }
  };

  return { fetchMenuList };
};</pre
        >
      </div>

      <div class="bg-gray-50 p-4 rounded-lg mb-4">
        <h4 class="font-mono text-sm mb-2">Integration in SidebarItems.vue:</h4>
        <pre
          class="bg-gray-800 text-gray-100 p-4 rounded text-sm overflow-x-auto"
        >
import { useMenuService } from '@/services/menuService';

const { fetchMenuList } = useMenuService();
const dynamicMenulist = ref([]);

onMounted(async () => {
  dynamicMenulist.value = await fetchMenuList();
  // Combine with static menus if needed
  menulist.value = [...staticMenulist, ...dynamicMenulist.value];
});</pre
        >
      </div>

      <div class="bg-yellow-50 border-l-4 border-yellow-500 p-4">
        <h4 class="font-bold mb-2">API Response Format:</h4>
        <p class="mb-2">Your API should return data in this format:</p>
        <pre
          class="bg-gray-800 text-gray-100 p-4 rounded text-sm overflow-x-auto"
        >
[
  {
    "name": "Activity",
    "items": [
      {
        "label": "Notification",
        "path": "/main/notificationpage"
      }
    ]
  }
]</pre
        >
      </div>
    </section>

    <!-- Behavior Customization -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold mb-4">Behavior Customization</h2>
      <p class="mb-4">Control how the sidebar behaves in different states:</p>

      <div class="bg-gray-50 p-4 rounded-lg mb-4">
        <h4 class="font-mono text-sm mb-2">Sidebar State Management:</h4>
        <pre
          class="bg-gray-800 text-gray-100 p-4 rounded text-sm overflow-x-auto"
        >
// In your store.js or equivalent
export const sidebarOpen = ref(true);

// Toggle sidebar from anywhere
const toggleSidebar = () => {
  sidebarOpen.value = !sidebarOpen.value;
};</pre
        >
      </div>
    </section>

    <!-- Styling Customization -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold mb-4">Styling Customization</h2>
      <p class="mb-4">
        Customize the appearance by overriding these CSS variables:
      </p>

      <div class="bg-gray-50 p-4 rounded-lg mb-4">
        <h4 class="font-mono text-sm mb-2">CSS Variables:</h4>
        <pre
          class="bg-gray-800 text-gray-100 p-4 rounded text-sm overflow-x-auto"
        >
:root {
  --sidebar-bg: #ffffff;
  --sidebar-text: #4a5568;
  --sidebar-active-bg: #ebf8ff;
  --sidebar-active-text: #3182ce;
  --sidebar-hover-bg: #f7fafc;
  --sidebar-width-open: 16rem;
  --sidebar-width-closed: 5rem;
}</pre
        >
      </div>
    </section>

    <!-- Props Reference -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold mb-4">Props Reference</h2>

      <div class="overflow-x-auto">
        <table class="min-w-full border">
          <thead class="bg-gray-50">
            <tr>
              <th class="px-4 py-2 text-left">Prop</th>
              <th class="px-4 py-2 text-left">Type</th>
              <th class="px-4 py-2 text-left">Default</th>
              <th class="px-4 py-2 text-left">Description</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-200">
            <tr>
              <td class="px-4 py-2 font-mono text-sm">mobileOrientation</td>
              <td class="px-4 py-2">String</td>
              <td class="px-4 py-2 font-mono text-sm">'end'</td>
              <td class="px-4 py-2">
                Position of mobile sidebar ('start' or 'end')
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <!-- Troubleshooting -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold mb-4">Troubleshooting</h2>

      <div class="space-y-4">
        <div class="bg-red-50 border-l-4 border-red-500 p-4">
          <h3 class="font-bold mb-2">
            Issue: Menus stay open when sidebar closes
          </h3>
          <p>
            Solution: Ensure you have the sidebar state watcher implemented:
          </p>
          <pre class="bg-gray-800 text-gray-100 p-4 rounded text-sm mt-2">
watch(sidebarOpen, (newVal) => {
  if (!newVal) {
    menulist.value.forEach(menu => {
      menu.isopen = false;
    });
  }
});</pre
          >
        </div>

        <div class="bg-red-50 border-l-4 border-red-500 p-4">
          <h3 class="font-bold mb-2">Issue: Active state not updating</h3>
          <p>Solution: Verify your route paths match exactly with menu items</p>
        </div>

        <div class="bg-red-50 border-l-4 border-red-500 p-4">
          <h3 class="font-bold mb-2">Issue: Mobile flyout menus appearing</h3>
          <p>Solution: Confirm the isMobile check is properly implemented</p>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup>
// You can add any component-specific script here if needed
</script>
