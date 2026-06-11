import { API_BASE_URL } from "../services/config";
import { useRoute, useRouter } from "vue-router";
import { startLoading, stopLoading } from "~/stores/globalvar";

export const useMenuService = () => {
  const fetchMenuList = async () => {
    const route = useRoute();
    const router = useRouter();
    const userStore = useUserStore();

    try {
      startLoading();
      
      const profile = userStore?.profile || {};
      const userid = parseInt(profile?.UserId) || 79023;
      const sysid = parseInt(profile?.SysId) || 101;
      const response = await apiFetch(
        '/api/account/get-app-menu',
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({ sysid, userid }),
        },
      );
      
      const data = await response.json();
      return data.map((menuGroup) => ({
        title: menuGroup.title,
        isopen: false,
        isActive: false,
        submenu: menuGroup.submenu.map((page) => ({
          title: page.title,
          isclick: false,
          route: page.route || '/' + page.stage,
          icon: page.icon || page.ChildIcon || '',
        })),
      }));
    } catch (error) {
      console.error('Error fetching menu:', error);
      return [];
    } finally {
      stopLoading();
    }
  };

  return { fetchMenuList };
};
