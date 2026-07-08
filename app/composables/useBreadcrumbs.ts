// useBreadcrumbs.ts
import { useRoute } from "vue-router";
import { ref, watch } from "vue";
import { useMenuService } from "~/services/menuService";

type Crumb = {
  label: string;
  to?: string;
  clickable: boolean;
};

type MenuNode = {
  title: string;
  route?: string;
  isClick?: boolean;
  submenu?: {
    title: string;
    route: string;
    isclick?: boolean;
  }[];
};

/**
 * Static overrides for known route "words" (not dynamic codes).
 * Keys are lowercase.
 */
const labelMap = ref<Record<string, string>>({
  dashboard: "Dashboard",
  users: "Users",
  settings: "Settings",
  profile: "My Profile",
  edit: "Edit",
  create: "Create",
});

/**
 * A segment counts as a "menucode" if it's a long numeric string, e.g.
 * 637262075600163869. Adjust this if your codes look different
 * (e.g. alphanumeric, GUIDs, etc).
 */
const MENU_CODE_REGEX = /^\d{6,}$/;

/* ------------------------------------------------------------------ *
 * Module-level caches — shared across every component using this
 * composable so we don't refetch the same data twice.
 * ------------------------------------------------------------------ */
type ResolvedMenu = {
  menuName: string;
  parentMenuName?: string;
  clickable: boolean;
};

const menuNameCache = ref<Record<string, ResolvedMenu>>({});
const pendingCodes = new Set<string>();
let menuTreePromise: Promise<MenuNode[]> | null = null;

async function ensureMenuTree(): Promise<MenuNode[]> {
  if (!menuTreePromise) {
    const { fetchMenuList } = useMenuService();

    menuTreePromise = fetchMenuList()
      .then((data) => {
        return data;
      })
      .catch((e) => {
        console.error(e);
        menuTreePromise = null;
        return [];
      });

    menuTreePromise = fetchMenuList().catch((e) => {
      console.error("Failed to load menu tree for breadcrumbs:", e);
      menuTreePromise = null; // allow retry on next call
      return [];
    });
  }
  return menuTreePromise;
}

/**
 * Resolves a menucode -> { menuName, parentMenuName } via the backend.
 * Matches AccountController's POST /api/Account/get-menuname endpoint,
 * which takes { menucode } and returns { menuName, parentMenuName }.
 */
async function resolveMenuCode(code: string) {
  if (menuNameCache.value[code] || pendingCodes.has(code)) return;
  pendingCodes.add(code);
  try {
    const res: any = await apiFetch("/api/Account/get-menu-name", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ menucode: code }),
      parseJson: true,
    });

    if (res?.success && res?.menuName) {
      // Reassign (not mutate) so the watcher below fires.
      menuNameCache.value = {
        ...menuNameCache.value,
        [code]: {
          menuName: res.menuName,
          parentMenuName: res.parentMenuName ?? null,
          clickable: res.clickable ?? false,
        },
      };
    }
  } catch (e) {
    console.error(`Failed to resolve menucode ${code}:`, e);
  } finally {
    pendingCodes.delete(code);
  }
}
/**
 * Finds an exact route match in the dynamic menu tree so we can pull
 * both the item's title AND its parent menu's title.
 */
function findMenuTitleForPath(
  tree: MenuNode[],
  path: string,
): {
  title: string;
  route: string;
  clickable: boolean;

  parentTitle?: string;
  parentRoute?: string;
  parentClickable?: boolean;
} | null {
  const normalized = path.toLowerCase();

  for (const menu of tree) {
    if (menu.submenu?.length) {
      for (const sub of menu.submenu) {
        if (sub.route?.toLowerCase() === normalized) {
          return {
            title: sub.title,
            route: sub.route,
            clickable: sub.isclick ?? false,

            parentTitle: menu.title,
            parentRoute: menu.route,
            parentClickable: menu.isClick ?? false,
          };
        }
      }
    }

    if (menu.route?.toLowerCase() === normalized) {
      return {
        title: menu.title,
        route: menu.route,
        clickable: menu.isClick ?? false,
      };
    }
  }

  return null;
}

export const useBreadcrumbs = () => {
  const route = useRoute();
  const breadcrumbs = ref<Crumb[]>([]);

  const build = async () => {
    const segments = route.path.split("/").filter(Boolean);
    if (segments.length === 0) {
      breadcrumbs.value = [];
      return;
    }

    const tree = await ensureMenuTree();
    // Exact full-path match against the dynamic menu (gives us the real
    // title + parent title without touching the menucode API at all).
    const matched = findMenuTitleForPath(tree, route.path);

const crumbs: Crumb[] = [];

    // Determine which segment index corresponds to the parent menu (if matched)
    // so we can skip adding it as a fallback duplicate.
    let parentSegmentIndex = -1;
    if (matched?.parentRoute) {
      const parentSegments = matched.parentRoute.split("/").filter(Boolean);
      parentSegmentIndex = parentSegments.length - 1;
    }

    segments.forEach((segment, index) => {
      const isLast = index === segments.length - 1;
      const lower = segment.toLowerCase();

      // Static labels
      if (labelMap.value[lower]) {
        crumbs.push({
          label: labelMap.value[lower],
          to: isLast ? undefined : route.path,
          clickable: false,
        });

        return;
      }

      // Dynamic menu match (only on last segment)
      if (isLast && matched) {
        if (matched.parentTitle) {
          crumbs.push({
            label: matched.parentTitle,
            to: undefined,
            clickable: false,
          });
        }

        crumbs.push({
          label: matched.title,
          to: undefined,
          clickable: false,
        });

        return;
      }

      // Skip the segment that corresponds to the parent menu to avoid duplicate
      // (the parent title is already added via matched.parentTitle above)
      if (index === parentSegmentIndex) {
        return;
      }

      // Menu code
      if (MENU_CODE_REGEX.test(segment)) {
        const cached = menuNameCache.value[segment];

        crumbs.push({
          label: cached?.menuName ?? `#${segment}`,
          to: undefined,
          clickable: false,
        });

        if (!cached) {
          resolveMenuCode(segment);
        }

        return;
      }

      // fallback
      crumbs.push({
        label: segment.charAt(0).toUpperCase() + segment.slice(1),
        to: undefined,
        clickable: false,
      });
    });
    breadcrumbs.value = crumbs;
  };
  watch(() => route.path, build, { immediate: true });
  // Rebuild once any pending menucode resolves so the label updates in place.
  watch(menuNameCache, build, { deep: true });

  const currentMenuLabel = computed(() => {
    const lastCrumb = breadcrumbs.value[breadcrumbs.value.length - 1];
    return lastCrumb?.label || "";
  });

  return {
    breadcrumbs,
    labelMap,
    currentMenuLabel,
  };
};