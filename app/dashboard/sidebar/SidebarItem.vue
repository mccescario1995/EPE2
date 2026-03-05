<script setup>
import { useRoute } from "vue-router";
import { sidebarOpen } from "../store";

defineProps({
  to: {
    type: String,
    default: "",
  },
  title: {
    type: String,
    default: "",
  },
});
const route = useRoute();
</script>

<template>
  <li>
    <router-link :to="to">
      <span
        class="my-2 flex w-full items-center text-black-950 justify-start p-4 font-thin uppercase transition-colors duration-200 lg:hover:text-black-950"
        :class="route.path === to &&
          'bg-gradient-to-r border-r-4 border-black-950 from-black-800 to-black-100'
          ">
        <span>
          <slot />
        </span>
        <span class="mx-4 text-sm font-normal whitespace-pre" v-show="sidebarOpen">
          {{ title }}
        </span>
      </span>
    </router-link>
  </li>
</template>
