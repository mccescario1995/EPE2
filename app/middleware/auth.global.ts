import { useUserStore } from '~/stores/useUserStore';

export default defineNuxtRouteMiddleware((to) => {
  const userStore = useUserStore();
  
  if (!userStore.token && process.client) {
    userStore.loadUserFromStorage();
  }
  
  const isAuthenticated = userStore.isAuthenticated;
  
  const publicRoutes = ['/', '/login', '/register', '/index'];
  const isPublicRoute = publicRoutes.includes(to.path);
  
  if (!isAuthenticated && !isPublicRoute) {
    return navigateTo({
      path: '/',
      query: { redirect: to.fullPath },
    });
  }
  
  if (isAuthenticated && isPublicRoute) {
    const redirectTo = to.query.redirect as string || '/main/dashboard';
    return navigateTo(redirectTo);
  }
});