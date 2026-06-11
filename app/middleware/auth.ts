import { useUserStore } from '~/stores/useUserStore';

export default defineNuxtRouteMiddleware((to) => {
  const userStore = useUserStore();
  
  const isAuthenticated = userStore.isAuthenticated || !!userStore.token;
  
  const publicRoutes = ['/', '/login', '/register', '/index'];
  const isPublicRoute = publicRoutes.includes(to.path);
  
  if (!isAuthenticated && !isPublicRoute) {
    return navigateTo({
      path: '/',
      query: { redirect: to.fullPath },
    });
  }
  
  if (isAuthenticated && publicRoutes.includes(to.path)) {
    return navigateTo('/dashboard');
  }
});