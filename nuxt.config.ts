import tailwindcss from "@tailwindcss/vite";

// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: "2025-07-15",
  devtools: { enabled: true },
  ssr: false,
  router: {
    options: {
      hashMode: true,
    },
  },
  nitro: {
    preset: "static",
  },
  modules: ["@pinia/nuxt", "nuxt-charts"],
  css: ["~/assets/css/main.css"],
  plugins: ["~/plugins/pinia-persisted-state.ts"],
  build: {
    transpile: ["gridstack"],
  },
  vite: {
    plugins: [tailwindcss()] as any,
    esbuild: {
      drop: process.env.NODE_ENV === "production" ? ["console"] : [],
    },
  },
  runtimeConfig: {
    public: {
      apiBase: process.env.NUXT_PUBLIC_API || 'https://apps.fastlogistics.com.ph/epe2apidev',
    },
  },
  app: {
    /* head: {
      link: [
        { rel: "icon", type: "image/x-icon", href: "/tbs/favicon.ico" }, 
        // Optional: for better browser support
        { rel: "shortcut icon", href: "/tbs/favicon.ico" },
      ],
      title: "TBS",
    }, */
    baseURL: "/epe2dev/",
  },
});