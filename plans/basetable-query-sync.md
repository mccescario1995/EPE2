# BaseTable URL Query Sync Plan

## Objective
Sync search, pagination, and sort state in BaseTable.vue with URL query parameters so the state persists when the user navigates back (browser back button).

## Current State
- BaseTable.vue uses local refs (globalSearch, currentPage, limit, sortField, sortDirection, columnFilters, dynamicFilters) with no URL synchronization.
- When the user navigates away and presses back, all state resets to defaults.
- Parent pages only pass etchData / props and do not manage this state.

## Approach
Use Nuxt's useRoute() and useRouter() to read/write query params. Use outer.replace() to avoid polluting browser history.

## Parameters to Sync
- search — global search text
- page — current page number
- limit — items per page
- sortField — active sort column
- sortDirection — asc / desc
- ilters — comma-separated column:value pairs for columnFilters
- dynamicFilters — comma-separated key:value pairs for dynamicFilters

## Changes in BaseTable.vue

1. Add imports for useRoute and useRouter (auto-imported in Nuxt).
2. On mount, read oute.query and initialize local state from query values.
3. Create helper functions:
   - syncQuery(params: Record<string, string | number | undefined>) — uses outer.replace({ query }) to update URL.
   - eadQuery() — reads current query and updates local state.
4. Watch oute.query deeply. When it changes (e.g., user pressed back), call eadQuery() to restore state and trigger etchTable().
5. After each state-mutating user action (search, page change, limit change, sort change, column filter change, dynamic filter change), call syncQuery() to persist the new state.
6. Guard against infinite loops by only calling syncQuery() when the route query actually differs from the intended state.

## Example Implementation Sketch

`	s
const route = useRoute();
const router = useRouter();

function stateToQuery() {
  return {
    search: globalSearch.value || undefined,
    page: currentPage.value > 1 ? currentPage.value : undefined,
    limit: limit.value !== props.perPage ? limit.value : undefined,
    sortField: sortField.value || undefined,
    sortDirection: sortDirection.value === 'desc' ? 'desc' : undefined,
    filters: Object.entries(columnFilters).filter(([,v]) => v).map(([k,v]) => ${k}:).join(',') || undefined,
    dynamicFilters: Object.entries(dynamicFilters.value).filter(([,v]) => v).map(([k,v]) => ${k}:).join(',') || undefined,
  };
}

function queryToState() {
  if (route.query.search != null) globalSearch.value = String(route.query.search);
  if (route.query.page != null) currentPage.value = Number(route.query.page);
  if (route.query.limit != null) limit.value = Number(route.query.limit);
  if (route.query.sortField) sortField.value = String(route.query.sortField);
  if (route.query.sortDirection) sortDirection.value = String(route.query.sortDirection);
  if (route.query.filters) {
    columnFilters = reactive<Record<string, string>>({});
    String(route.query.filters).split(',').forEach(pair => {
      const [k, v] = pair.split(':');
      if (k && v != null) columnFilters[k] = v;
    });
  }
  if (route.query.dynamicFilters) {
    dynamicFilters.value = {};
    String(route.query.dynamicFilters).split(',').forEach(pair => {
      const [k, v] = pair.split(':');
      if (k && v != null) dynamicFilters.value[k] = v;
    });
  }
}

watch(() => route.query, (newQ, oldQ) => {
  if (JSON.stringify(newQ) === JSON.stringify(oldQ)) return;
  queryToState();
  if (props.serverPagination && props.fetchData) fetchTable();
}, { deep: true });

function persistState() {
  const q = stateToQuery();
  if (JSON.stringify(route.query) !== JSON.stringify(q)) {
    router.replace({ query: q });
  }
}
`

## Integration Points
- Call persistState() inside handlePageChange, handleLimitChange, handleGlobalSearch, 	oggleSort, handleColumnFilter, pplyDynamicFilters.
- Call queryToState() inside onMounted before initial etchTable() so the table restores from the URL on first load.
- Use eplace (not push) to avoid adding a new history entry every time the user types in search or changes page.

## Risks & Mitigations
- **Infinite loops**: watch(route.query) triggers queryToState + etchTable; etchTable does not call persistState, so no loop. onMounted should call queryToState instead of persistState.
- **Parent pages using other query params (e.g., ?id=...)**: Preserve existing non-table query keys in outer.replace({ query: { ...route.query, ...q } }).
- **Large filter payloads**: Query string is URL-encoded; comma-separated lists are acceptable for typical use cases.

## Files Touched
- C:\Users\Marthen Christ\Documents\My Web Sites\EPEUI\app\components\BaseTable.vue

## Testing
1. Navigate to any exemptions list page.
2. Set search text, page, limit, sort, and column filter.
3. Navigate to a detail page (e.g., view/edit).
4. Press browser back.
5. Verify the table restores the exact search text, page, limit, sort, and filter, and data is refetched.
6. Navigate away and reload the page with query params in the URL; verify initial state matches.
