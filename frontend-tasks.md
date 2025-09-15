## Frontend Implementation Tasks (SPA, UI, Realtime)

Note: Frontend-only. Excludes all backend and broader DevOps (Docker/CI/CD) tasks.

### Track A — Project Scaffolding & Tooling

- [F1] Initialize app and configuration
  - Create or validate Vite React-TS app in `frontend/` with Tailwind CSS configured.
  - Enable TypeScript strict mode; set base path aliases.
  - DoD: App runs at `http://localhost:5173` with Tailwind styles applied.

- [F2] Install dependencies
  - `react-router-dom`, `@tanstack/react-query`, `zod`, `react-hook-form`, `@hookform/resolvers`, `@microsoft/signalr`, `recharts`, `clsx`, Radix UI primitives.
  - DoD: `npm install` succeeds; sample imports compile.

- [F3] Linting & formatting
  - Configure ESLint (React/TS plugins) and Prettier; add scripts `lint`, `format`.
  - DoD: `npm run lint` passes; `npm run format` formats files with no diffs after.

- [F4] Application shell & providers
  - Layout with header/nav, container, and toast portal.
  - Providers: `QueryClientProvider`, `BrowserRouter`, theme, and toast provider in `main.tsx`.
  - DoD: Baseline routes render inside the layout.

- [F4a] OpenAPI types/codegen (optional)
  - Use `openapi-typescript` to generate TS types from `docs/openapi.json` into `src/types/api.d.ts`; add `npm run codegen`.
  - Optionally generate a typed client (e.g., orval) or keep typed fetch wrappers.
  - DoD: Types compile and stay in sync; regeneration resolves contract drift.

- [F4b] Error boundaries and 404 route
  - Add route-level ErrorBoundary at the app layout; show ProblemDetails-aware or generic fallback.
  - Add a catch-all `/*` NotFound route with accessible navigation back to `/`.
  - DoD: Errors render fallback; unknown routes show 404 page.

- [F4c] Env typing and validation
  - Type `import.meta.env` via `env.d.ts` and validate required `VITE_*` variables at startup.
  - Fail fast with readable errors in development; add a unit test for env validation.
  - DoD: Misconfiguration yields clear errors; test passes in CI.

### Track B — Authentication & Authorization

- [F5] Auth types & schemas
  - Define `LoginInput`, `RegisterInput` (Zod) and corresponding TS types.
  - DoD: Types compile; invalid shapes rejected in unit tests.

- [F6] Auth API functions
  - Implement `POST /api/auth/login` and `POST /api/auth/register` wrappers.
  - DoD: Happy-path requests succeed against MSW.

- [F7] Auth state & hooks
  - Implement `useAuth` storing token in memory with localStorage hydration.
  - Expose `isAuthenticated`, `login`, `logout`.
  - DoD: Token persists across reload; logout clears state.

- [F8] Route protection & 401 handling
  - `RequireAuth` wrapper to protect `'/'`, `'/books/*'`, `'/stats'`, `'/favorites'`, `'/analytics'`.
  - In HTTP client, on 401: clean logout and redirect to `/login?returnTo=<current>`.
  - DoD: Unauthenticated users are redirected; `returnTo` works post-login.

- [F9] Auth pages
  - Build `/login` and `/register` with RHF + Zod validation and ProblemDetails error display.
  - DoD: Forms validate and show server field errors.

### Track C — HTTP Client & Error Handling

- [F10] HTTP wrapper & error normalization
  - Implement `src/lib/http.ts` with base URL, bearer token, and RFC 7807 ProblemDetails normalization.
  - Add optional `X-Correlation-ID` propagation.
  - DoD: Errors have `{ title, status, detail, instance, errors? }` shape consistently.

- [F11] Conditional requests & concurrency (optional)
  - Store `ETag` from `GET /api/books/{id}`; send `If-Match` on `PUT`.
  - Handle `412 Precondition Failed` by surfacing a non-blocking conflict UI.
  - DoD: Behavior verified via MSW tests.

- [F11a] Conditional GETs with ETag (optional)
  - Use ETag from `GET /api/books/{id}` and `/api/books/stats`; send `If-None-Match` on subsequent GETs.
  - On `304 Not Modified`, serve from React Query cache without refetch to avoid UI flicker.
  - DoD: MSW tests verify 304 path and cache behavior.

### Track D — Books Feature

- [F12] Types & schemas
  - `Book`, `CreateBookInput`, `UpdateBookInput`, `PagedBooks` with Zod schemas (trim, max lengths, rating range, date ≤ today+1d).
  - DoD: Invalid inputs rejected in unit tests.

- [F13] Books API layer
  - List (params), get, create, update (If-Match), delete, stats.
  - DoD: MSW-backed tests cover endpoints and params.

- [F14] Books query hooks
  - `useBooks(params)`, `useBook(id)`, `useCreateBook()`, `useUpdateBook()`, `useDeleteBook()`, `useBookStats()` with cache keys and invalidation.
  - DoD: Mutations invalidate or update caches correctly.

- [F15] BookTable component
  - Columns, sortable headers (asc → desc → default); empty/loading/error states.
  - Filters: search, genre combobox (free text + suggestions from stats), rating range, date range.
  - DoD: Default sort `publishedDate desc`; filters bound to URL.

- [F16] Pagination controls
  - Page size options `[10,20,50,100]` (default 20); Prev/Next buttons and a page number input; show range (e.g., `1–20 of N`). Persist `pageSize` in URL.
  - DoD: Keyboard accessible; Prev/Next and page number input work; state sync with URL.

- [F17] BookForm component
  - RHF + Zod; fields Title, Author, Genre, PublishedDate (`<input type="date">` → ISO `YYYY-MM-DDT00:00:00Z`), Rating 1–5.
  - DoD: Client and server validation errors displayed inline.

- [F18] Books pages
  - `/` List page wired to `useBooks` with URL params.
  - `/books/new` Create page using `useCreateBook`.
  - `/books/:id/edit` Edit page using `useBook` + `useUpdateBook`; handle optional 412.
  - DoD: Navigation flows complete; success toasts shown.

- [F19] Delete flow
  - `ConfirmDialog` and optimistic UI with rollback on failure.
  - DoD: Correct cache updates and error recovery.

### Track E — Stats Feature

- [F20] Stats types and API
  - `BookStats = Array<{ genre: string; count: number }>`; `getBookStats()`.
  - DoD: Shape matches server; MSW test covers normalization.

- [F21] Stats hooks & chart
  - `useBookStats()` and `StatsChart` (dynamic import `recharts`) with Tailwind-based colors; empty state.
  - DoD: Chart renders; dynamic import splits bundle.

- [F22] Stats page
  - `/stats` page with loading/error/empty states.
  - DoD: Page accessible via nav; reflects live data.

### Track F — Favorites Feature

- [F23] Favorites API & hooks
  - API: favorite, unfavorite, list favorites; hooks: `useFavorite`, `useUnfavorite`, `useFavorites`.
  - DoD: Idempotent 204 handling; caches updated.

- [F24] UI components
  - `FavoriteToggle` (optimistic) and `FavoritesFilter`.
  - DoD: Toggle updates star state and favorites list immediately.

- [F25] Favorites page
  - `/favorites` reusing `BookTable` bound to favorites data source.
  - DoD: Filtering/sorting/paging consistent with main list.

### Track G — Analytics Feature

- [F26] Analytics API & hooks
  - API: `recordRead`, `getAvgRating`, `getMostReadGenres`; hooks: `useRecordRead`, `useAvgRating`, `useMostReadGenres`.
  - DoD: MSW tests cover params and shapes.

- [F27] Analytics charts
  - `AvgRatingChart`, `MostReadGenresChart` (dynamic imports) with date range controls.
  - DoD: Charts render; empty/loading/error states handled.

- [F28] Analytics page
  - `/analytics` page composing both charts and controls; optional “Mark as Read” action in list.
  - DoD: Charts reflect seeded data; action records reads.

### Track H — Realtime (SignalR)

- [F29] SignalR client setup
  - Connect to `/hubs/library` with `accessTokenFactory`; configure reconnect with backoff.
  - DoD: Connection succeeds when authenticated; unauthorized rejected.
  - Reinitialize connection when the token changes; stop/dispose cleanly on logout/auth changes to avoid stale connections; re-subscribe on login.

- [F30] Realtime cache updates
  - `useRealtime` hook to handle `bookCreated/updated/deleted`, `bookFavorited/unfavorited`, `bookRead`, `statsUpdated`.
  - DoD: Relevant React Query caches updated/refetched accordingly.

- [F31] Realtime UI badge
  - `RealtimeBadge` component shows connection status in the header.
  - DoD: Status reflects actual SignalR state.
  - Badge reflects `connecting/reconnecting/connected/disconnected` states, including auth-change transitions.

### Track I — Accessibility & UX Polish

- [F32] A11y pass
  - Labels/ARIA for inputs, combobox, dialogs, pagination; keyboard navigation.
  - DoD: Keyboard-only flows work; basic axe checks pass.

- [F33] Focus management & skip links
  - Manage focus on route change, dialog open/close, and validation errors.
  - DoD: Visible focus, skip to content works.

- [F34] Toasts & error messages
  - Consistent toast variants; map ProblemDetails to user-friendly messages.
  - DoD: Error toasts show actionable info.

### Track J — Testing

- [F35] Test harness & MSW
  - Vitest + RTL + user-event; MSW handlers for all API routes.
  - DoD: Tests run in CI locally; handlers cover error/success.

- [F36] Unit tests: utilities & schemas
  - HTTP normalization, date helpers, Zod schemas.
  - DoD: Tests pass with coverage on edge cases.

- [F37] Unit tests: components
  - `BookForm` (validation and submission), `BookTable` (sorting/pagination behavior), `FavoriteToggle`.
  - DoD: Tests pass; accessibility assertions where sensible.

- [F38] Integration tests: flows
  - Auth (login/register + guards), Books CRUD + list filters/sort/paging, Stats, Favorites, Analytics.
  - DoD: Tests pass with MSW simulating backend.

- [F39] Realtime tests
  - Mock SignalR client; simulate hub events; verify cache updates.
  - DoD: Cache reflects events without manual refetch.

### Track K — Performance & Code Splitting

- [F40] Bundle optimization
  - Dynamic imports for charts and heavy UI; analyze bundle.
  - DoD: Initial bundle size reduced; code-split chunks verified.

- [F40a] Route-level code splitting (optional)
  - Lazy-load major routes (`/stats`, `/favorites`, `/analytics`, `/login`, `/register`) with suspense fallbacks.
  - DoD: Initial route bundle shrinks; route chunks verified in build output.

- [F41] React Query tuning & memoization
  - Configure sensible `staleTime`/retries; memoize heavy rows/filters.
  - DoD: No obvious unnecessary re-renders; smooth interactions.

### Track L — Documentation

- [F42] Frontend README
  - Setup, env vars, scripts, routes, architectural overview (api/hooks/components), error handling, date & ETag conventions, realtime.
  - DoD: README complete and up-to-date.

- [F43] ADRs / design notes
  - Document key decisions (ProblemDetails normalization, token storage, route guarding, SignalR cache strategy).
  - DoD: ADRs committed under `docs/` or `frontend/docs/`.

### Parallelization Notes

- Track A must begin first; after [F4], Tracks B–D can proceed in parallel.
- Books (Track D), Stats (Track E), Favorites (Track F), Analytics (Track G) can run concurrently once HTTP/Auth foundations ([F5]–[F11]) are stable.
- Realtime (Track H) can start after minimal endpoints exist; can be integrated progressively.
- Testing (Track J) should be authored alongside each feature track; MSW enables parallel test creation.
- Performance (Track K) and Documentation (Track L) run continuously and finalize near code-freeze.


