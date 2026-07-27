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
  "637120010759985872": "/main/exemptions/create-exemption", // Create Exemption Approval
  "637951116932400418": "/main/exemptions/pending-for-approval", // Pending for Approval
  "637262075600163869": "/maintenance/policies", // Maintenance Policy
  "637951226932433418": "/main/reports/closed", // Closed Request (Reports)
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
  const menuData = ref([]);

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
      menuData.value = data.map((menuGroup) => ({
        title: menuGroup.name,
        isopen: false,
        icon: menuGroup.icon,
        route: resolveGroupRoute(menuGroup.stage),
        isActive: false,
        isClick: menuGroup.isclick,
        canAdd: menuGroup.canadd,
        canEdit: menuGroup.canedit,
        canDelete: menuGroup.candelete,
        submenu: menuGroup.pages.map((page) => ({
          title: page.name,
          isclick: page.isclick,
          route: resolvePageRoute(page.stage, menuGroup.name),
          icon: page.icon,
          canAdd: page.canadd,
          canEdit: page.canedit,
          canDelete: page.candelete,
        })),
      }));

      return menuData.value;
    } catch (error) {
      console.error("Error fetching menu:", error);
      alert("Unauthorized!");

      return navigateTo("/");
    } finally {
      stopLoading();
    }
  };

  const getMenuGroupByRoute = (routePattern) => {
    const lowerPattern = routePattern.toLowerCase();
    return menuData.value.find((group) => {
      const groupRoute = (group.route || "").toLowerCase();
      const pageRoutes = (group.submenu || []).map((page) =>
        (page.route || "").toLowerCase(),
      );
      return (
        groupRoute.includes(lowerPattern) ||
        pageRoutes.some((route) => route.includes(lowerPattern))
      );
    });
  };

  const getMatchingMenuItem = (routePattern) => {
    const lowerPattern = routePattern.toLowerCase();
    for (const group of menuData.value) {
      const groupRoute = (group.route || "").toLowerCase();
      if (groupRoute.includes(lowerPattern)) {
        return { type: "group", item: group };
      }
      const page = (group.submenu || []).find((p) =>
        (p.route || "").toLowerCase().includes(lowerPattern),
      );
      if (page) {
        return { type: "page", item: page, group };
      }
    }
    return null;
  };

  const canAdd = (routePattern) => {
    const match = getMatchingMenuItem(routePattern);
    if (!match) return false;
    if (match.type === "page") {
      return !!match.item.canAdd ?? !!match.group.canAdd;
    }
    return !!match.item.canAdd;
  };

  const canEdit = (routePattern) => {
    const match = getMatchingMenuItem(routePattern);
    if (!match) return false;
    if (match.type === "page") {
      return !!match.item.canEdit ?? !!match.group.canEdit;
    }
    return !!match.item.canEdit;
  };

  const canDelete = (routePattern) => {
    const match = getMatchingMenuItem(routePattern);
    if (!match) return false;
    if (match.type === "page") {
      return !!match.item.canDelete ?? !!match.group.canDelete;
    }
    return !!match.item.canDelete;
  };

  return {
    fetchMenuList,
    menuData,
    getMenuGroupByRoute,
    canAdd,
    canEdit,
    canDelete,
  };
};
