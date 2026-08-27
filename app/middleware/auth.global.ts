import { useUserStore } from '~/stores/useUserStore';
import { useMenuService } from '~/services/menuService';
import { showToast } from '~/stores/globalvar';

export default defineNuxtRouteMiddleware(async (to) => {
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

  if (isAuthenticated && !isPublicRoute) {
    const { getMatchingMenuItem, fetchMenuList, menuData } = useMenuService();

    const alwaysAllowed = ['/main/dashboard'];

    if (!alwaysAllowed.includes(to.path)) {
      if (menuData.value.length === 0) {
        try {
          await fetchMenuList();
        } catch (e) {
          console.error('Failed to load menu for authorization:', e);
          showToast({
            title: 'Unauthorized Access!',
            message: 'You do not have permission to access this page.',
            type: 'error',
            duration: 4000,
          });
          return navigateTo('/main/dashboard');
        }
      }

      if (menuData.value.length > 0) {
        const allowed = getMatchingMenuItem(to.path);
        if (!allowed) {
          showToast({
            title: 'Unauthorized Access!',
            message: 'You do not have permission to access this page.',
            type: 'error',
            duration: 4000,
          });
          return navigateTo('/main/dashboard');
        }
      }
    }
  }
});