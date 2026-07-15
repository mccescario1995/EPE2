// services/menuService.js

// /**
//  * The backend's get-app-menu endpoint always returns route: null
//  * (see AccountController.GetAppMenu -> SubMenuDto.route). Route
//  * resolution therefore has to live entirely on the client, keyed
//  * off `stage` (falls back to `id` if stage is somehow empty).
//  *
//  * Add an entry here whenever a new menu item is created in
//  * CoreUserMenuLists, using its Menucode (or MenuId if Menucode is null)
//  * as the key.
//  */
const STAGE_ROUTE_MAP = {
  "637120010759985872": "/main/exemptions/all", // Create Exemption Approval
  "637951116932400418": "/main/exemptions/pending", // Pending for Approval
  "637262075600163869": "/maintenance/policies", // Maintenance Policy
  "637951226932433418": "/main/reports/closed", // All Request (Reports)
};

function resolvePageRoute(stage, menuGroupName) {
  if (!stage) return null;
  if (STAGE_ROUTE_MAP[stage]) return STAGE_ROUTE_MAP[stage];
  return `/main/${menuGroupName}/${stage}`;
}

function resolveGroupRoute(stage) {
  if (!stage) return null;
  if (STAGE_ROUTE_MAP[stage]) return STAGE_ROUTE_MAP[stage];
  return `/main/${stage}`;
}

// menuService.js
export const useMenuService = () => {
  const fetchMenuList = async () => {
    try {
      const userStore = useUserStore();
      const userId = userStore.profile?.userId;

      if (!userId) {
        console.error(
          "[menuService] No userId in profile — cannot fetch menu.",
        );
        return [];
      }

      startLoading();
      const response = await apiFetch("/api/Account/get-user-menu", {
        method: "POST",
        body: JSON.stringify({
          userid: userId,
        }),
        headers: {
          "Content-Type": "application/json",
        },
        parseJson: true,
      });

      const data = response;
      // const routeMap = {};
      // data.forEach((menuGroup) => {
      //   (menuGroup.pages || []).forEach((page) => {
      //     if (page.stage) {
      //       routeMap[page.stage] = `/main/${menuGroup.name}/${page.stage}`;
      //     }
      //   });
      // });
      // Map API response to our menu structure
      // return data.map((menuGroup) => ({
      //   title: menuGroup.name,
      //   isopen: false,
      //   icon: menuGroup.icon, // Optional icon mapping
      //   route: routeMap[menuGroup.stage] || `/main/${menuGroup.stage}`,
      //   isActive: false,
      //   isClick: menuGroup.isclick,
      //   submenu: menuGroup.pages.map((page) => ({
      //     title: page.name,
      //     isclick: page.isclick,
      //     route: routeMap[page.stage] || `/main/${page.stage}`,
      //     icon: page.icon,
      //   })),
      // }));
      return data.map((menuGroup) => ({
        title: menuGroup.name,
        isopen: false,
        icon: menuGroup.icon,
        route: resolveGroupRoute(menuGroup.stage),
        isActive: false,
        isClick: menuGroup.isclick,
        submenu: menuGroup.pages.map((page) => ({
          title: page.name,
          isclick: page.isclick,
          route: resolvePageRoute(page.stage, menuGroup.name),
          icon: page.icon,
        })),
      }));
    } catch (error) {
      console.error("Error fetching menu:", error);
      alert("Unauthorized!");

      return navigateTo("/");
    } finally {
      stopLoading();
    }
  };

  return { fetchMenuList };
};
