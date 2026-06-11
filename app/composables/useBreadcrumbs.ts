import { useRoute } from "vue-router";
import { computed, ref } from "vue";
// import { resolve } from "path";

type Crumb = {
  label: string;
  to: string;
  clickable?: boolean;
};

// Optional: allows manual overrides for label mapping
const labelMap = ref<Record<string, string>>({
  dashboard: "Dashboard",
  users: "Users",
  settings: "Settings",
  profile: "My Profile",
  edit: "Edit",
  create: "Create",
  "email-response": "Email Response",
});

// Base path for the pages directory (adjust based on your project structure)
// const PAGES_DIR = resolve("./app/pages");

const PAGE_INDEX_PATHS = new Set([
  "",           // root index
  "/dashboard",
  "/access-denied",
  "/activity",
  "/documentation",
  "/drodown",
  "/exemptions",
  "/exemptions/all",
  "/exemptions/closed",
  "/exemptions/pending",
  "/main",
  "/main/maintenance",
  "/maintenance",
  "/maintenance/policies",
  // Add more as needed
]);

export const useBreadcrumbs = () => {
  const route = useRoute();

  const breadcrumbs = computed<Crumb[]>(() => {
    // const pathSegments = route.path.split("/").filter(Boolean).filter(segment => segment !== "exemptions");
    const pathSegments = route.path.split("/").filter(Boolean);

    const crumbs: Crumb[] = [];
    let fullPath = "";

    pathSegments.forEach((segment, index) => {
      fullPath += "/#/" + segment;
      const isParam = segment.startsWith(":") || Number.isInteger(+segment);

      // Check if this path has an index.vue file
      // const checkPathHasIndex = (path: string): boolean => {
      //   // Skip check for route parameters
      //   if (isParam) return false;
        
      //   try {
      //     const indexPath = resolve(PAGES_DIR, path, "index.vue");
      //     return existsSync(indexPath);
      //   } catch {
      //     return false;
      //   }
      // };

      // const isClickable = checkPathHasIndex(fullPath);

      const isClickable = !isParam && PAGE_INDEX_PATHS.has(fullPath);

      crumbs.push({
        label:
          labelMap.value[segment] ||
          (isParam
            ? `#${segment.replace(/\[|\]/g, "")}`
            : segment.charAt(0).toUpperCase() + segment.slice(1)),
        to: fullPath,
        clickable: isClickable,
      });
    });

    return crumbs;
  });

  return {
    breadcrumbs,
    labelMap,
  };
};
