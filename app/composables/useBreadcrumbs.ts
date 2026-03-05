import { useRoute } from "vue-router";
import { computed, ref } from "vue";

type Crumb = {
  label: string;
  to: string;
};

// Optional: allows manual overrides for label mapping
const labelMap = ref<Record<string, string>>({
  dashboard: "Dashboard",
  users: "Users",
  settings: "Settings",
  profile: "My Profile",
  edit: "Edit",
  create: "Create",
});

export const useBreadcrumbs = () => {
  const route = useRoute();

  const breadcrumbs = computed<Crumb[]>(() => {
    const pathSegments = route.path.split("/").filter(Boolean);

    const crumbs: Crumb[] = [];
    let fullPath = "";

    pathSegments.forEach((segment, index) => {
      fullPath += "/" + segment;
      const isParam = segment.startsWith(":") || Number.isInteger(+segment);

      crumbs.push({
        label:
          labelMap.value[segment] ||
          (isParam
            ? `#${segment}`
            : segment.charAt(0).toUpperCase() + segment.slice(1)),
        to: fullPath,
      });
    });

    return crumbs;
  });

  return {
    breadcrumbs,
    labelMap,
  };
};
