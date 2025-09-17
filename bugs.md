## Bug Tracker

| ID | Title | Area | Severity | Status |
| --- | --- | --- | --- | --- |
| BUG-001 | Favorite star highlight does not persist | Frontend → Favorites | High | Open |
| BUG-002 | Overuse of `any`/`unknown` in TypeScript | Frontend → Types | Medium | Open |
| BUG-003 | Filtering blanks entire page; only table should hide | Frontend → Books List | High | Open |
| BUG-004 | Genre filter does not match partials (e.g., "fan" ≠ "Fantasy") | Frontend → Books Filters | Medium | Open |

---

### BUG-001: Favorite star highlight does not persist

- Area: Frontend → Favorites (Book row actions, Favorites hooks)
- Severity: High (incorrect state, poor UX)

Description
- After clicking the star to favorite a book, the star appears highlighted momentarily but reverts after a re-render (e.g., navigating, refetch, or realtime event). The underlying favorite state on the server may be set, but UI does not reflect it consistently.

Reproduction Steps
1. Navigate to `/` and favorite any book via the star icon.
2. Change route (e.g., navigate to `/favorites` then back) or trigger a refetch (filter, sort, or page change).
3. Observe the star highlight state for the same book.

Expected
- Star remains highlighted for favorited books across re-renders, route changes, and refetches.
- State remains correct after optimistic updates, error rollbacks, and realtime events.

Actual
- Star highlight reverts (appears unfavorited) even when the server state is favorited.

Suspected Causes
- Favorite state not derived from cached data; star driven only by local `useState` without initial value from list/favorites data.
- Cache invalidation updates the list but the star component does not reconcile with latest server data.
- Missing consolidation of favorite state across `books` and `favorites` caches or lack of selector for `isFavorited`.

Impact
- Users cannot trust the visual state; may re-click and cause unnecessary API calls.

Acceptance Criteria
- Star reflects `isFavorited` based on canonical cached data (books list and/or favorites list) and persists through navigation/refetch.
- Optimistic toggle updates caches immediately; rollback on error restores prior state.
- Realtime `bookFavorited`/`bookUnfavorited` events update both lists consistently.

---

### BUG-002: Overuse of `any`/`unknown` in TypeScript

- Area: Frontend → Type system, utilities (HTTP, realtime, hooks)
- Severity: Medium (type-safety, maintainability)

Description
- Several values are typed as `any` or `unknown`, reducing type-safety and increasing the risk of runtime errors. These should be replaced with explicit types, generics, or discriminated unions consistent with our domain types and OpenAPI-generated declarations.

Examples (non-exhaustive)
- Event payload handlers in realtime utilities.
- HTTP error surfaces that do not leverage `ProblemDetails` or custom error types (`HttpError`, `HttpUnauthorizedError`, `HttpConflictError`).

Expected
- No `any` in `src/` (except in third-party type shims where unavoidable).
- `unknown` only as a boundary type with immediate refinement/narrowing.

Actual
- Usage of `any`/`unknown` in app code paths without narrowing leads to implicit-`any` and weak type checks.

Acceptance Criteria
- ESLint/TS config enforces: `noImplicitAny`, ban `any` rule (allow explicit escape hatches only in shims), and prefer precise types.
- Replace `any`/`unknown` with concrete types or generics, using OpenAPI-generated types where applicable.
- Type-check passes with stricter rules and zero `any` in `src/`.

---

### BUG-003: Filtering blanks entire page; only the table should hide

- Area: Frontend → Books List (filters, table, empty state)
- Severity: High (blocking UX)

Description
- Applying filters that yield zero results causes the entire content area to disappear (including filters and controls). The only recovery is to manually edit the URL query string. The expected behavior is that the filters remain visible and only the table area shows an empty state.

Reproduction Steps
1. Navigate to `/` (Books list).
2. Apply a filter combination that returns zero results (e.g., genre + rating range outside available data).
3. Observe that both the controls and the table disappear, leaving a blank page.

Expected
- Filters and paging controls remain visible and interactive.
- Only the results region swaps to an empty state message (e.g., “No matching books”).

Actual
- The entire section goes blank; filters are not visible and cannot be adjusted.

Suspected Causes
- Early return in the `BookTable` component when `!data` or `items.length === 0`, which bypasses rendering the filters container.
- Empty state implemented at the component root rather than scoped to the table/results region.

Acceptance Criteria
- Filters and pagination controls render regardless of result count.
- Empty state replaces only the table body area; controls remain to adjust filters.
- No full-page blank state when queries return zero items.

---

### BUG-004: Genre filter does not match partials (e.g., "fan" should match "Fantasy")

- Area: Frontend → Books Filters (search/genre input, query serialization)
- Severity: Medium (discoverability/UX)

Description
- The genre filter requires an exact match. Typing a partial string like `fan` does not return books with `Fantasy` genre. Users expect substring/contains matching for quick filtering.

Reproduction Steps
1. Navigate to `/` (Books list) with data containing a `Fantasy` book.
2. In the Genre filter, type `fan`.
3. Observe that the list does not include `Fantasy` items.

Expected
- Genre filter performs case-insensitive substring matching (e.g., `fan` → `Fantasy`).
- Optionally, suggestions indicate matched genres while allowing free text.

Actual
- Only exact (or case-sensitive) matches are returned; partial inputs yield zero results.

Suspected Causes
- Client passes `genre` query param expecting server-side contains, but API interprets as equality.
- Or client-side pre-filtering (if any) uses strict equality instead of `includes`/normalized match.

Acceptance Criteria
- Typing a partial string (e.g., `fan`) returns books whose normalized genre includes the substring, case-insensitive.
- URL query remains `genre=fan`; server or client logic ensures contains behavior.
- Existing exact-match behavior remains supported when the full genre is entered.
