# BaseTable URL Query Sync Plan

## Objective
Sync search, pagination, and sort state in BaseTable.vue with URL query parameters so the state persists when the user navigates back (browser back button).

## Current State
- BaseTable.vue uses local refs (globalSearch, currentPage, limit, sortField, sortDirection, columnFilters, dynamicFilters) with no URL synchronization.
- When the user navigates away and presses back, all state resets to defaults.
- Parent pages only pass fetchData / props and do not manage this state.

## Approach
Use Nuxt's useRoute() and useRouter() to read/write query params. Use router.replace() to avoid polluting browser history.

## Parameters to Sync
- search -- global search text
- page -- current page number
- limit -- items per page
- sortField -- active sort column
- sortDirection -- asc / desc
- filters -- comma-separated column:value pairs for columnFilters
- dynamicFilters -- comma-separated key:value pairs for dynamicFilters

## Changes in BaseTable.vue

1. Add imports for useRoute and useRouter (auto-imported in Nuxt).
2. On mount, read route.query and initialize local state from query values.
3. Create helper functions:
   - persistState() -- builds query object from local refs and calls router.replace({ query })
   - restoreFromQuery() -- reads current route.query and updates local refs
4. Watch route.query deeply. When it changes (e.g., user pressed back), call restoreFromQuery() to restore state and trigger fetchTable().
5. After each state-mutating user action (search, page change, limit change, sort change, column filter change, dynamic filter change), call persistState() to persist the new state.
6. Guard against infinite loops by only calling persistState() when the route query actually differs from the intended state.

## Implementation Details

### preserveOtherQueryKeys
When calling router.replace, pass all existing route.query keys plus the new table params to avoid clobbering other query params (e.g., ?id=... on view pages).

### Column Filters serialization
Comma-separated list of `key:value` pairs. Very lightweight and URL-safe for typical usage.

### Infinite loop prevention
- watch(route.query) calls restoreFromQuery() + fetchTable()
- fetchTable() does NOT call persistState()
- onMounted calls restoreFromQuery() (not persistState())
- User actions call persistState() which updates route.query -> triggers watch -> restoreFromQuery + fetchTable. The restore function sets the same values back, so no second persistState call is triggered.

## Files Touched
- C:\Users\Marthen Christ\Documents\My Web Sites\EPEUI\app\components\BaseTable.vue

## Testing
1. Navigate to any exemptions list page.
2. Set search text, page, limit, sort, and column filter.
3. Navigate to a detail page (e.g., view/edit).
4. Press browser back.
5. Verify the table restores the exact search text, page, limit, sort, and filter, and data is refetched.
6. Navigate away and reload the page with query params in the URL; verify initial state matches.
