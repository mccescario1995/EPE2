  <template>
    <header class="sticky top-0 z-50 bg-white dark:bg-gray-800 shadow-sm">
      <div class="mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex h-16 items-center justify-between">
          <!-- Left section - Logo and menu button -->
          <div class="flex items-center">
            <button type="button" @click="toggleSidebar"
              class="mr-4 rounded-md p-1 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-600 focus:outline-none focus:ring-2 focus:ring-blue-500"
              aria-expanded="false" aria-label="Toggle navigation">
              <svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
              </svg>
            </button>

            <div class="hidden md:flex items-center">
              <span @click="Dashboard()"
                class="cursor-pointer text-xl font-bold text-gray-800 dark:text-gray-200 hover:text-blue-600 transition-colors duration-200">
                Executive Process Exemption
              </span>
            </div>
          </div>

          <!-- Right section - Notification bell and User dropdown -->
          <div class="relative ml-4 flex items-center">

            <div class="mr-4 flex items-center justify-center">
              <ThemeToggle />
            </div>
            <!-- Notification Bell -->
            <div class="mr-4 relative">
              <button type="button" @click="toggleNotificationDropdown"
                class="flex items-center border rounded-full p-1 text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
                id="notification-button" aria-expanded="false" aria-haspopup="true" aria-label="Notifications">
                <BellIcon :unreadCount="notificationStore.unreadCount" />
                <!-- Badge for unread count -->
                <span v-if="notificationStore.unreadCount > 0"
                  class="absolute -top-1 -right-1 flex h-4 w-4 items-center justify-center text-xs font-bold text-white bg-red-500 rounded-full">
                  {{
                    notificationStore.unreadCount > 99
                      ? "99+"
                      : notificationStore.unreadCount
                  }}
                </span>
              </button>

              <!-- Notification Dropdown -->
              <transition enter-active-class="transition ease-out duration-100"
                enter-from-class="transform opacity-0 scale-95" enter-to-class="transform opacity-100 scale-100"
                leave-active-class="transition ease-in duration-75" leave-from-class="transform opacity-100 scale-100"
                leave-to-class="transform opacity-0 scale-95">
                <div v-if="isNotificationDropdownOpen"
                  class="absolute top-10 right-0 mt-2 w-90 origin-top-right rounded-md bg-white dark:bg-gray-700 py-1 shadow-md ring-1 ring-black ring-opacity-5 focus:outline-none"
                  role="menu" aria-orientation="vertical" aria-labelledby="notification-button" tabindex="-1"
                  @click.outside="closeNotificationDropdown">
                  <div class="px-4 py-2 flex items-center justify-between">
                    <span class="block text-sm font-medium text-gray-700 dark:text-gray-300">Notifications
                      <span v-if="notificationStore.unreadCount > 0"
                        class="h-4 w-4 text-xs font-bold text-white bg-red-500 rounded-full px-1">
                        {{
                          notificationStore.unreadCount > 99
                            ? "99+"
                            : notificationStore.unreadCount
                        }}
                      </span></span>
                  </div>

                  <div class="overflow-y-auto max-h-83">
                    <template v-if="notificationStore.notifications.length === 0">
                      <div class="px-4 py-3 text-center text-sm text-gray-500 dark:text-gray-400">
                        No notifications
                      </div>
                    </template>
                    <template v-else>
                      <template v-for="notification in notificationStore.notifications" :key="notification.id">
                        <div class="px-4 py-3 sm:px-4" :class="{
                          'bg-gray-50': !notification.isRead,
                          'bg-gray-300': notification.isRead,
                          'cursor-pointer': true,
                          'hover:bg-gray-200': true,
                          // 'hover:text-gray-700': true,
                          'border-b': true,
                        }" @click="markAsReadAndNavigate(notification)">
                          <div class="flex items-start space-x-3">
                            <div class="flex-1">
                              <h3 class="mb-1 text-sm font-medium text-gray-900 dark:text-gray-300 line-clamp-1">
                                {{ notification.title }}
                              </h3>
                              <p class="mb-1 text-sm text-gray-500 dark:text-gray-400 line-clamp-2">
                                {{ notification.message }}
                              </p>
                              <p class="text-xs text-gray-400 dark:text-gray-500">
                                {{ formatDate(notification.createdDate) }}
                              </p>
                            </div>
                          </div>
                        </div>
                      </template>
                    </template>
                  </div>
                  <div class="px-4 py-3 text-center">
                    <button @click="markAllAsRead"
                      class="w-full text-left text-sm text-gray-600 hover:text-gray-800 dark:text-gray-400 dark:hover:text-gray-200">
                      Mark all as read
                    </button>
                  </div>
                </div>
              </transition>
            </div>

            <!-- User dropdown -->
            <button type="button" @click="toggleDropdown"
              class="flex max-w-xs items-center rounded-full text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
              id="user-menu-button" aria-expanded="false" aria-haspopup="true">
              <span class="sr-only">Open user menu</span>
              <div class="flex items-center">
                <img class="h-8 w-8 rounded-full"
                  :src="`https://apps.fastlogistics.com.ph/fpma2api/api/Access/get-employee-picture?userid=${userId}&empid=${employeeId}`"
                  alt="User profile" />
                <span class="ml-2 hidden text-sm font-medium text-gray-700 dark:text-gray-300 md:block">{{
                  profile.employeename2
                  }}</span>
                <svg class="ml-1 h-4 w-4 text-gray-400" :class="{ 'transform rotate-180': isDropdownOpen }" fill="none"
                  viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                </svg>
              </div>
            </button>

            <!-- Dropdown menu -->
            <transition enter-active-class="transition ease-out duration-100"
              enter-from-class="transform opacity-0 scale-95" enter-to-class="transform opacity-100 scale-100"
              leave-active-class="transition ease-in duration-75" leave-from-class="transform opacity-100 scale-100"
              leave-to-class="transform opacity-0 scale-95">
              <div v-if="isDropdownOpen"
                class="absolute top-10 right-0 mt-2 w-48 origin-top-right rounded-md bg-white dark:bg-gray-700 shadow-lg ring-1 ring-black ring-opacity-5 focus:outline-none"
                role="menu" aria-orientation="vertical" aria-labelledby="user-menu-button" tabindex="-1"
                @click.outside="closeDropdown">

                <button @click="Dashboard"
                  class="w-full text-left px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-600"
                  role="menuitem" tabindex="-1">
                  <div class="flex items-center">
                    <svg class="mr-3 h-4 w-4 text-gray-500 dark:text-gray-400" fill="currentColor" viewBox="0 0 24 24">
                      <path d="M3 13h8V3H3v10zm0 8h8v-6H3v6zm10 0h8V11h-8v10zm0-18v6h8V3h-8z" />
                    </svg>
                    Dashboard
                  </div>
                </button>

                <li @click="
                  storeEncryptedDataInCookie(
                    'https://apps.fastlogistics.com.ph/utility/#/Setting/Account'
                  )
                  "
                  class="py-2.5 px-5 flex items-center hover:bg-gray-100 dark:hover:bg-gray-600 text-[#333] dark:text-gray-300 text-sm cursor-pointer">
                  <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4 mr-3" viewBox="0 0 48 48">
                    <g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round"
                      stroke-width="4">
                      <path d="M18 31h20V5" />
                      <path d="M30 21H10v22m34-32l-6-6l-6 6" />
                      <path d="m16 37l-6 6l-6-6" />
                    </g>
                  </svg>
                  Account Settings
                </li>

                <button @click="Logout"
                  class="w-full text-left px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-600"
                  role="menuitem" tabindex="-1">
                  <div class="flex items-center">
                    <svg class="mr-3 h-4 w-4 text-gray-500 dark:text-gray-400" fill="currentColor" viewBox="0 0 24 24">
                      <path
                        d="M16 17v-3H9v-4h7V7l5 5-5 5M14 2a2 2 0 0 1 2 2v2h-2V4H5v16h9v-2h2v2a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9z" />
                    </svg>
                    Logout
                  </div>
                </button>
              </div>
            </transition>
          </div>
        </div>
      </div>

      <div>
        <!-- Modal -->
        <div v-if="show" class="fixed inset-0 flex items-center justify-center z-50"
          style="background-color: rgba(0, 0, 0, 0.5)">
          <div class="bg-white rounded-xl shadow-lg max-w-4xl w-full p-4 relative dark:bg-gray-800">
            <h2 class="text-lg font-semibold mb-4 text-gray-800 dark:text-gray-200">
              Account Settings
            </h2>
            <!-- Close Button -->
            <button @click="show = false"
              class="absolute top-2 right-2 pr-5 text-gray-600 dark:text-gray-400 hover:text-red-500 dark:hover:text-red-400 text-5xl">
              &times;
            </button>
            <!-- Iframe -->
            <div class="aspect-video">
              <iframe class="w-full h-full" :src="link" frameborder="0" allowfullscreen></iframe>
            </div>
          </div>
        </div>
      </div>
    </header>
  </template>

<script setup>
import { ref } from "vue";
import { toggleSidebar } from "./store";
import { useUserStore } from "../stores/useUserStore";
import { useNotificationStore } from "../stores/useNotificationStore";
import { getKey, encryptData } from "../../encrypt";
import { onMounted } from "vue";
import { useRouter } from "vue-router";
import BellIcon from "../components/icon/Bell.vue";
import { useAuth } from "~/composables/useAuth";
import ThemeToggle from "../components/ThemeToggle.vue";
const router = useRouter();
const userStore = useUserStore();
const notificationStore = useNotificationStore();
const auth = useAuth();
const profile = userStore.profile;
const userDATA = userStore.profile;
const employeeId = userStore.profile.emplId;
const userId = userStore.profile.userId;

const isDropdownOpen = ref(false);
const isNotificationDropdownOpen = ref(false);

function toggleDropdown() {
  isDropdownOpen.value = !isDropdownOpen.value;
}

function closeDropdown() {
  isDropdownOpen.value = false;
}

function toggleNotificationDropdown() {
  isNotificationDropdownOpen.value = !isNotificationDropdownOpen.value;
  if (isNotificationDropdownOpen.value) {
    notificationStore.fetchNotifications();
  }
}

function closeNotificationDropdown() {
  isNotificationDropdownOpen.value = false;
}

function markAsReadAndNavigate(notification) {
  console.log("Notification clicked:", notification);
  // Mark as read
  notificationStore.markAsRead(notification.id);
  // Update the notification's isRead property locally to reflect the change immediately
  var response = apiFetch(`/api/notification/${notification.id}/read`, {
    method: "POST",
  });

  console.log("Mark as read response:", response);

  // Close dropdown
  closeNotificationDropdown();
  // Navigate to the related URL if it exists
  if (notification.relatedUrl) {
    router.push(notification.relatedUrl);
  }
}

function markAllAsRead() {
  notificationStore.markAllAsRead();
  var response = apiFetch("/api/notification/read-all", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ UserId: parseInt(profile.userId) }),
    credentials: "include",
    parseJson: true,
  });
  console.log("Mark all as read response:", response);
  closeNotificationDropdown();
}

async function Logout() {
  try {
    await apiFetch("/api/access/logout", { method: "POST", credentials: "include" });
  } catch (error) {
    console.error("Logout API error:", error);
  }

  const utilityCookie = document.cookie
    .split("; ")
    .find((row) => row.startsWith("utility="));
  if (utilityCookie) {
    document.cookie = "utility=; path=/; secure; samesite=strict; expires=Thu, 01 Jan 1970 00:00:00 GMT";
  }

  notificationStore.clearNotifications();
  userStore.logout();  // Handles state clear, localStorage, and navigation
  auth.logout();
  closeDropdown();
  closeNotificationDropdown();

  showToast({
    title: "Logout",
    message: "Logged out successfully",
    type: "success",
    duration: 3000,
  });
}

function Dashboard() {
  closeDropdown();
  router.push("/main/dashboard");
}
const show = ref(false);
const link = ref("");
async function storeEncryptedDataInCookie(iframeUrl) {
  const payload = {
    systemid: 101,
    userhashcode: userDATA.hashcode,
    token: userStore.token,
    userid: userDATA.userid ?? userDATA.userId, //userId
    employeename: userDATA.requestorname ?? userDATA.employeename2,
    employeeid: userDATA.empid ?? userDATA.emplId, // emplId
    position: userDATA.position ?? userDATA.positionname,
    emailadd: userDATA.email ?? userDATA.username, //username
    department: userDATA.department ?? userDATA.departmentname,
    sex: userDATA.sex,
  };

  console.log("Payload for encryption:", payload); // Debug log for payload

  const key = await getKey(); // You can share the password securely across systems
  const encrypted = await encryptData(payload, key);

  // Calculate expiration time (30 minutes from now)
  const expirationDate = new Date();
  expirationDate.setMinutes(expirationDate.getMinutes() + 30);

  // Set cookie (secure, strict, and expires in 30 minutes)
  document.cookie = `utility=${encrypted}; path=/; secure; samesite=strict; expires=${expirationDate.toUTCString()}`;
  link.value = iframeUrl;
  show.value = true;
}

// Helper function to get notification icon path based on type
function getNotificationIconPath(type) {
  switch (type) {
    case "success":
      return "M5 13l4 4L19 7"; // Checkmark
    case "error":
      return "M18 10l-5-5m0 5L13 8m0 5L8 13"; // X
    case "warning":
      return "M12 9v2m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"; // Triangle with exclamation
    case "info":
    default:
      return "M13 2H6a2 2 0 00-2 2v10a2 2 0 002 2h7"; // Info symbol
  }
}

// Helper function to format date
function formatDate(dateString) {
  const options = {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  };
  return new Date(dateString).toLocaleDateString(undefined, options);
}

onMounted(() => {
  // Fetch notifications when component mounts
  notificationStore.fetchNotifications();
});
</script>
