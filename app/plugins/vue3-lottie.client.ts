import { defineNuxtPlugin } from '#app'
import Lottie from 'vue3-lottie'

export default defineNuxtPlugin((nuxtApp) => {
  nuxtApp.vueApp.component('Lottie', Lottie)
})
