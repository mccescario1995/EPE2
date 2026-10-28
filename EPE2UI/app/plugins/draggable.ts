// plugins/draggable.ts
import { VueDraggable } from "vue-draggable-plus";

export default defineNuxtPlugin((nuxtApp) => {
  nuxtApp.vueApp.component("VueDraggable", VueDraggable);
});