// useBreadcrumbs.ts
import { useRoute } from "vue-router";
import { ref, watch, computed } from "vue";
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
  exemptions: "Excemption",
  policies: "Policies",
  maintenance: "Maintenance",
});

/**
 * A segment counts as a "menucode" if it's a long numeric string, e.g.
 * 637262075600163869. Adjust this if your codes look different
 * (e.g. alphanumeric, GUIDs, etc).
 */
const MENU_CODE_REGEX = /^\d{6,}$/;

/**
 * Converts route segments into readable breadcrumb labels.
 *
 * Examples:
 * userProfile           -> User Profile
 * user-profile          -> User Profile
 * user_profile          -> User Profile
 * user-profile_settings -> User Profile Settings
 * UserProfile           -> User Profile
 * USER_PROFILE          -> User Profile
 */
function formatBreadcrumbLabel(value: string): string {
  if (!value) return "";

  return value
    // Convert kebab-case and snake_case to spaces
    .replace(/[-_]+/g, " ")

    // Add a space before uppercase letters in camelCase / PascalCase
    // userProfile -> user Profile
    // UserProfile -> User Profile
    .replace(/([a-z0-9])([A-Z])/g, "$1 $2")

    // Normalize multiple spaces
    .replace(/\s+/g, " ")

    // Remove leading/trailing spaces
    .trim()

    // Capitalize every word
    .replace(/\b\w/g, (char) => char.toUpperCase());
}

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
const resolvingCodes = ref<Set<string>>(new Set());
let menuTreePromise: Promise<MenuNode[]> | null = null;

async function ensureMenuTree(): Promise<MenuNode[]> {
  if (!menuTreePromise) {
    const { fetchMenuList } = useMenuService();

    menuTreePromise = fetchMenuList()
      .then((data) => data)
      .catch((e) => {
        console.error("Failed to load menu tree for breadcrumbs:", e);
        menuTreePromise = null;
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
  resolvingCodes.value.add(code);
  try {
    const res: any = await apiFetch("/api/Account/get-menu-name", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ menucode: code }),
      parseJson: true,
    });

    if (res?.success && res?.menuName) {
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
    resolvingCodes.value.delete(code);
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
          }
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

function buildKnownRoutesMap(tree: MenuNode[]): Map<string, { title: string; clickable: boolean }> {
  const knownRoutes = new Map<string, { title: string; clickable: boolean }>();
  for (const menu of tree) {
    if (menu.route) {
      knownRoutes.set(menu.route.toLowerCase(), {
        title: menu.title,
        clickable: menu.isClick ?? false,
      });
      if (menu.submenu) {
        for (const sub of menu.submenu) {
          if (sub.route) {
            knownRoutes.set(sub.route.toLowerCase(), {
              title: sub.title,
              clickable: sub.isclick ?? false,
            });
          }
        }
      }
    }
  }
  return knownRoutes;
}

export const useBreadcrumbs = () => {
  const route = useRoute();
  const breadcrumbs = ref<Crumb[]>([]);

  const build = async () => {
    const allSegments = route.path.split("/").filter(Boolean);
    if (allSegments.length === 0) {
      breadcrumbs.value = [];
      return;
    }

    const hasMainPrefix =
      allSegments.length > 1 && allSegments[0].toLowerCase() === "main";
    const mainSkipped = hasMainPrefix ? 1 : 0;
    const segments = hasMainPrefix ? allSegments.slice(1) : allSegments;

    const tree = await ensureMenuTree();
    const matched = findMenuTitleForPath(tree, route.path);
    const knownRoutes = buildKnownRoutesMap(tree);

    const crumbs: Crumb[] = [];

    let parentSegmentIndex = -1;
    if (matched?.parentRoute) {
      const parentSegments = matched.parentRoute.split("/").filter(Boolean);
      parentSegmentIndex = parentSegments.length - 1;
    }

    allSegments.forEach((segment, index) => {
      if (hasMainPrefix && index === 0) return;
      const segIndex = index - mainSkipped;
      const isLast = segIndex === segments.length - 1;
      const lower = segment.toLowerCase();
      const pathUpToHere = "/" + allSegments.slice(0, index + 1).join("/");

      const routeMatch = knownRoutes.get(pathUpToHere.toLowerCase());

      // if (segIndex === parentSegmentIndex) {
      //   return;
      // }

      if (segIndex === parentSegmentIndex) {
        if (isLast && matched) {
          if (matched.parentTitle) {
            crumbs.push({
              label: matched.parentTitle,
              to: matched.parentClickable ? matched.parentRoute : undefined,
              clickable: matched.parentClickable ?? false,
            });
          }
          crumbs.push({
            label: matched.title,
            to: undefined,
            clickable: false,
          });
        }
        return;
      }

      if (labelMap.value[lower]) {
        crumbs.push({
          label: formatBreadcrumbLabel(segment),
          to: !isLast && routeMatch ? pathUpToHere : undefined,
          clickable: !isLast && routeMatch,
        });
        return;
      }

      if (isLast && matched) {
        if (matched.parentTitle) {
          crumbs.push({
            label: matched.parentTitle,
            to: matched.parentClickable ? matched.parentRoute : undefined,
            clickable: matched.parentClickable ?? false,
          });
        }

        crumbs.push({
          label: matched.title,
          to: undefined,
          clickable: false,
        });

        return;
      }

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

      crumbs.push({
        label: formatBreadcrumbLabel(segment),
        to: !isLast && routeMatch ? pathUpToHere : undefined,
        clickable: !isLast && routeMatch,
      });
    });

    breadcrumbs.value = crumbs;
  };

  watch(() => route.path, build, { immediate: true });
  watch(menuNameCache, build, { deep: true });

  const currentMenuLabel = computed(() => {
    const lastCrumb = breadcrumbs.value[breadcrumbs.value.length - 1];
    return lastCrumb?.label || "";
  });

  return {
    breadcrumbs,
    labelMap,
    currentMenuLabel,
    resolvingCodes,
  };
};