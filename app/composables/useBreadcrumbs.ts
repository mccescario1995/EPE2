import { useRoute } from "vue-router";
import { ref, watch } from "vue";
import { useMenuService } from "~/services/menuService";

type Crumb = {
  label: string;
  to: string;
  clickable: boolean;
};

type MenuNode = {
  title: string;
  route?: string;
  submenu?: { title: string; route: string }[];
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
type ResolvedMenu = { menuName: string; parentMenuName?: string | null };

const menuNameCache = ref<Record<string, ResolvedMenu>>({});
const pendingCodes = new Set<string>();
let menuTreePromise: Promise<MenuNode[]> | null = null;

async function ensureMenuTree(): Promise<MenuNode[]> {
  if (!menuTreePromise) {
    const { fetchMenuList } = useMenuService();
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
    const res: any = await apiFetch("/api/Account/get-menuname", {
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
): { title: string; parentTitle?: string } | null {
  const normalized = path.toLowerCase();

  for (const menu of tree) {
    if (menu.submenu?.length) {
      for (const sub of menu.submenu) {
        if (sub.route && sub.route.toLowerCase() === normalized) {
          return { title: sub.title, parentTitle: menu.title };
        }
      }
    } else if (menu.route && menu.route.toLowerCase() === normalized) {
      return { title: menu.title };
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
    let fullPath = "";

    segments.forEach((segment, index) => {
      fullPath += "/" + segment;
      const isLast = index === segments.length - 1;
      const lower = segment.toLowerCase();

      // 1) Known static word
      if (labelMap.value[lower]) {
        crumbs.push({
          label: labelMap.value[lower],
          to: fullPath,
          clickable: !isLast,
        });
        return;
      }

      // 2) Last segment resolved via the dynamic menu tree match above.
      //    Inject the parent menu title right before it if we haven't
      //    already shown it via a static segment.
      if (isLast && matched) {
        if (
          matched.parentTitle &&
          !crumbs.some((c) => c.label === matched.parentTitle)
        ) {
          crumbs.push({
            label: matched.parentTitle,
            to: fullPath,
            clickable: false,
          });
        }
        crumbs.push({ label: matched.title, to: fullPath, clickable: false });
        return;
      }

      // 3) Looks like a menucode -> resolve via the backend
      if (MENU_CODE_REGEX.test(segment)) {
        const cached = menuNameCache.value[segment];

        if (
          cached?.parentMenuName &&
          !crumbs.some((c) => c.label === cached.parentMenuName)
        ) {
          crumbs.push({
            label: cached.parentMenuName,
            to: fullPath,
            clickable: false,
          });
        }

        crumbs.push({
          label: cached?.menuName ?? `#${segment}`,
          to: fullPath,
          clickable: false,
        });

        if (!cached) {
          // Fire and forget; menuNameCache watcher below rebuilds once it lands.
          resolveMenuCode(segment);
        }
        return;
      }

      // 4) Fallback: Title Case the raw segment
      crumbs.push({
        label: segment.charAt(0).toUpperCase() + segment.slice(1),
        to: fullPath,
        clickable: !isLast,
      });
    });

    breadcrumbs.value = crumbs;
  };

  watch(() => route.path, build, { immediate: true });
  // Rebuild once any pending menucode resolves so the label updates in place.
  watch(menuNameCache, build, { deep: true });

  return {
    breadcrumbs,
    labelMap,
  };
};