<template>
  <header class="sticky top-0 z-50 bg-white shadow-sm">
    <div class="mx-auto px-4 sm:px-6 lg:px-8">
      <div class="flex h-16 items-center justify-between">
        <!-- Left section - Logo and menu button -->
        <div class="flex items-center">
          <button
            type="button"
            @click="toggleSidebar"
            class="mr-4 rounded-md p-1 text-gray-700 hover:bg-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
            aria-expanded="false"
            aria-label="Toggle navigation"
          >
            <svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
            </svg>
          </button>
          
          <div class="hidden md:flex items-center">
            <span 
              @click="Dashboard()" 
              class="cursor-pointer text-xl font-bold text-gray-800 hover:text-blue-600 transition-colors duration-200"
            >
              Executive Process Exemption  
            </span>
          </div>
        </div>

        <!-- Right section - User dropdown -->
        <div class="relative ml-4 flex items-center">
          <button
            type="button"
            @click="toggleDropdown"
            class="flex max-w-xs items-center rounded-full text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
            id="user-menu-button"
            aria-expanded="false"
            aria-haspopup="true"
          >
            <span class="sr-only">Open user menu</span>
            <div class="flex items-center">
              <img 
                class="h-8 w-8 rounded-full" 
                src="../static/images/UserIcon.png" 
                alt="User profile"
              >
              <span class="ml-2 hidden text-sm font-medium text-gray-700 md:block">{{ profile.employeename2 }}</span>
              <svg 
                class="ml-1 h-4 w-4 text-gray-400" 
                :class="{ 'transform rotate-180': isDropdownOpen }"
                fill="none" 
                viewBox="0 0 24 24" 
                stroke="currentColor"
              >
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
              </svg>
            </div>
          </button>

          <!-- Dropdown menu -->
          <transition
            enter-active-class="transition ease-out duration-100"
            enter-from-class="transform opacity-0 scale-95"
            enter-to-class="transform opacity-100 scale-100"
            leave-active-class="transition ease-in duration-75"
            leave-from-class="transform opacity-100 scale-100"
            leave-to-class="transform opacity-0 scale-95"
          >
            <div 
              v-if="isDropdownOpen"
              class="absolute top-10 right-0 mt-2 w-48 origin-top-right rounded-md bg-white py-1 shadow-lg ring-1 ring-black ring-opacity-5 focus:outline-none"
              role="menu"
              aria-orientation="vertical"
              aria-labelledby="user-menu-button"
              tabindex="-1"
              @click.outside="closeDropdown"
            >
              <!-- <a 
                href="#" 
                class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                role="menuitem"
                tabindex="-1"
              >
                <div class="flex items-center">
                  <svg class="mr-3 h-4 w-4 text-gray-500" fill="currentColor" viewBox="0 0 24 24">
                    <path d="M12 2a10 10 0 1 0 10 10A10 10 0 0 0 12 2zm0 3a3 3 0 1 1-3 3 3 3 0 0 1 3-3zm0 14.2a7.2 7.2 0 0 1-6-3.22c.03-1.99 4-3.08 6-3.08 1.99 0 5.97 1.09 6 3.08a7.2 7.2 0 0 1-6 3.22z"/>
                  </svg>
                  View Profile
                </div>
              </a> -->
              
              <button
                @click="Dashboard"
                class="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                role="menuitem"
                tabindex="-1"
              >
                <div class="flex items-center">
                  <svg class="mr-3 h-4 w-4 text-gray-500" fill="currentColor" viewBox="0 0 24 24">
                    <path d="M3 13h8V3H3v10zm0 8h8v-6H3v6zm10 0h8V11h-8v10zm0-18v6h8V3h-8z"/>
                  </svg>
                  Dashboard
                </div>
              </button>
              
              <button
                @click="Logout"
                class="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                role="menuitem"
                tabindex="-1"
              >
                <div class="flex items-center">
                  <svg class="mr-3 h-4 w-4 text-gray-500" fill="currentColor" viewBox="0 0 24 24">
                    <path d="M16 17v-3H9v-4h7V7l5 5-5 5M14 2a2 2 0 0 1 2 2v2h-2V4H5v16h9v-2h2v2a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9z"/>
                  </svg>
                  Logout
                </div>
              </button>
            </div>
          </transition>
        </div>
      </div>
    </div>
  </header>
</template>

<script setup>
import { ref } from 'vue';
import { toggleSidebar } from "./store";
import { useUserStore } from '../stores/useUserStore';
const router = useRouter();
const userStore = useUserStore();
const profile = userStore.profile;

const isDropdownOpen = ref(false);

function toggleDropdown() {
  console.log(userStore);
  isDropdownOpen.value = !isDropdownOpen.value;
}

function closeDropdown() {
  isDropdownOpen.value = false;
}

function Logout() {
  closeDropdown();
  router.push('/');
}

function Dashboard() {
  closeDropdown();
  router.push('/dashboard');
}

console.log(user);
</script>