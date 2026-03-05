import { API_BASE_URL } from "../services/config";
import { useRoute, useRouter } from "vue-router";
import { startLoading, stopLoading } from "~/stores/globalvar";

export const useMenuService = () => {
  const fetchMenuList = async () => {
    const route = useRoute();
    const router = useRouter();
    const userStore = useUserStore();

    // const token = decrypt(localStorage.getItem("user_lor2"));

    try {
      startLoading();
      const response = await apiFetch(
        "/api/account/get-app-menu-v2/?sysid=17&userid=70002",
        {
          method: "GET",
        },
      );
      const data = response;
      const routeMap = {};
      data.forEach((menuGroup) => {
        (menuGroup.pages || []).forEach((page) => {
          if (page.stage) {
            routeMap[page.stage] = `/main/${menuGroup.name}/${page.stage}`;
          }
        });
      });
      // Map API response to our menu structure
      // return null
      return data.map((menuGroup) => ({
        title: menuGroup.name,
        isopen: false,
        isActive: false,
        submenu: menuGroup.pages.map((page) => ({
          title: page.name,
          isclick: false,
          route: routeMap[page.stage] || `/main/${page.stage}`,
          icon: page.icon, // Optional icon mapping
        })),
      }));
    } catch (error) {
      console.error("Error fetching menu:", error);
      /* alert("Unauthorized!");
     
      return navigateTo("/"); */
    } finally {
      stopLoading();
    }
  };

  return { fetchMenuList };
};
