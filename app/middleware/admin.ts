import { useUserStore } from '~/stores/useUserStore';

export default defineNuxtRouteMiddleware(() => {
  const userStore = useUserStore();
  
  if (!userStore.isAuthenticated) {
    return navigateTo({
      path: '/',
      query: { redirect: '/main/admin' },
    });
  }
  
  if (!userStore.isAdmin) {
    return navigateTo('/access-denied');
  }
});