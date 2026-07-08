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
// const STAGE_ROUTE_MAP = {
//   "637120010759985872": "/exemptions/all",       // Create Exemption Approval
//   "637951116932400418": "/exemptions/pending",   // Pending for Approval
//   "637262075600163869": "/maintenance/policies", // Policy
//   "637951226932433418": "/exemptions/closed",    // All Request (Reports)
// };

// // EPE's own sysid for get-app-menu. Distinct from the "utility" iframe
// // system's systemid (101 is used there too, but that's a coincidence —
// // verify with backend/DB if these apps ever diverge).
// const EPE_SYSID = 101;

// function resolveRoute(item) {
//   if (item.route) return item.route;

//   const key = item.stage ?? item.id;
//   const resolved = STAGE_ROUTE_MAP[key];

//   if (!resolved) {
//     console.warn(
//       `[menuService] No route mapping found for menu item "${item.title}" (stage/id: ${key}). ` +
//       `Add it to STAGE_ROUTE_MAP in menuService.js.`,
//     );
//   }

//   return resolved ?? null;
// }

// function normalizeMenuGroup(group) {
//   return {
//     title: group.title,
//     isopen: false,
//     isActive: false,
//     submenu: (group.submenu || []).map((item) => ({
//       id: item.id,
//       title: item.title,
//       route: resolveRoute(item),
//       icon: item.icon || "",
//       isclick: false,
//       canadd: !!item.canadd,
//       canedit: !!item.canedit,
//       candelete: !!item.candelete,
//     })),
//   };
// }

// export const useMenuService = () => {
//   /**
//    * Fetches the raw menu tree from /api/Account/get-app-menu and
//    * normalizes it into the shape SidebarItems.vue expects (route
//    * resolved client-side, ui-state flags added).
//    */
//   const fetchMenuList = async () => {
//     const userStore = useUserStore();
//     const userId = userStore.profile?.userId;

//     if (!userId) {
//       console.error("[menuService] No userId in profile — cannot fetch menu.");
//       return [];
//     }

//     try {
//       const response = await apiFetch("/api/Account/get-user-menu", {
//         method: "POST",
//         body: JSON.stringify({
//           // sysid: EPE_SYSID,
//           userid: userId,
//         }),
//         headers: {
//           "Content-Type": "application/json",
//         },
//         parseJson: true,
//       });

//       // Controller returns StatusCode(200, parentMenuResult) — a bare array,
//       // NOT wrapped in { items: [...] }. Guard anyway in case that changes.
//       const data = Array.isArray(response) ? response : (response?.items ?? []);

//       return data.map(normalizeMenuGroup);
//     } catch (error) {
//       console.error("Error fetching menu:", error);
//       return [];
//     }
  // };

  export const useMenuService = () => {
  const fetchMenuList = async () => {
    try {
      const userStore = useUserStore();
      const userId = userStore.profile?.userId;
      
      if (!userId) {
        console.error("[menuService] No userId in profile — cannot fetch menu.");
        return [];
      }

      // console.log(token);
      startLoading();
      const response = await apiFetch("/api/Account/get-user-menu", {
        method: "POST",
        body: JSON.stringify({
          // sysid: EPE_SYSID,
          userid: userId,
        }),
        headers: {
          "Content-Type": "application/json",
        },
        parseJson: true,
      });
      
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
      return data.map((menuGroup) => ({
        title: menuGroup.name,
        isopen: false,
            icon: menuGroup.icon, // Optional icon mapping
          route: routeMap[menuGroup.stage] || `/main/${menuGroup.stage}`,
        isActive: false,
        submenu: menuGroup.pages.map((page) => ({
          title: page.name,
          isclick: false,
          route: routeMap[page.stage] || `/main/${page.stage}`,
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

//   return { fetchMenuList, resolveRoute, STAGE_ROUTE_MAP };
// };
