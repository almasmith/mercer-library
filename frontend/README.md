# Library Frontend (React + Vite + TypeScript)

<!-- BEGIN:LIBRARY-FRONTEND-README -->
A Vite React SPA for the Book Library application. It implements authentication, books CRUD with sorting/filtering/paging, favorites, stats/analytics, and realtime updates over SignalR.

## Requirements
- Node.js 20+ and npm 10+
- Backend API running locally (default `http://localhost:5000`)
- Optional: Docker for full-stack orchestration

## Quickstart
```bash
npm --prefix frontend install
npm --prefix frontend run dev
# open http://localhost:5173
```

## Environment variables
Create `frontend/.env` (or `.env.local`) with:
```bash
VITE_API_BASE_URL=http://localhost:5000
VITE_DEFAULT_PAGE_SIZE=20
VITE_MAX_PAGE_SIZE=100
```
- `VITE_API_BASE_URL`: Base URL to the backend API.
- Page size variables drive list/pagination defaults and limits.

## Scripts
```bash
npm --prefix frontend run dev            # start Vite dev server
npm --prefix frontend run build          # production build
npm --prefix frontend run preview        # preview build
npm --prefix frontend run lint           # ESLint
npm --prefix frontend run lint:fix       # ESLint (fix)
npm --prefix frontend run format         # Prettier write
npm --prefix frontend run format:check   # Prettier check
npm --prefix frontend run codegen        # (optional) OpenAPI → TS types
```
If present, OpenAPI is read from `docs/openapi.json`.

## Project structure
```
frontend/
  src/
    app.tsx, app-layout.tsx, main.tsx
    features/
      auth/ (types, api, hooks, pages)
      books/ (types, api, hooks, components, pages)
      favorites/ (api, hooks, components)
      stats/ (api, hooks, components)
      analytics/ (api, hooks, components)
      realtime/ (hooks, badge)
    lib/ (env, http, etag, realtime, errors)
    components/ (shared UI like ConfirmDialog, Toast)
    types/ (generated OpenAPI d.ts, optional)
```

## Architecture & data flow
- Routing: React Router.
- Data: TanStack Query; keys per feature; mutations invalidate or update caches.
- HTTP: `src/lib/http.ts` adds `Authorization`, `X-Correlation-ID`, and normalizes RFC 7807 ProblemDetails.
- Auth: In-memory token with localStorage hydration (`useAuth`); 401 triggers clean logout and redirect to `/login?returnTo=...`.
- Realtime: `@microsoft/signalr` hub at `/hubs/library`; lifecycle tied to auth token; cache invalidation on server events.
- Codegen (optional): `openapi-typescript` outputs `src/types/api.d.ts`.

## Error handling
- Server errors use ProblemDetails `{ title, status, detail, instance, errors? }`.
- UI displays form field errors from `errors` and shows toasts for general failures.
- A route-level ErrorBoundary provides a user-friendly fallback.

## Dates, sorting & filtering
- Dates are ISO 8601 (UTC). User-entered `YYYY-MM-DD` is normalized to midnight UTC (`YYYY-MM-DDT00:00:00Z`).
- Lists support URL-bound filters and sort; default sort is `publishedDate desc`.

## ETag & conditional requests
- Optional: capture ETag on GET; send `If-Match` on PUT.
- Optional: conditional GETs with `If-None-Match` to leverage `304 Not Modified`.

## Realtime
- Events: `bookCreated/updated/deleted`, `bookFavorited/unfavorited`, `bookRead`, `statsUpdated`.
- UI badge reflects `connecting/reconnecting/connected/disconnected`.

## Accessibility & UX
- Keyboard-accessible table sorting, pagination, dialogs.
- Skip link and focus management on route changes.
- Error messages use `role="alert"`, busy states mark `aria-busy="true"`.

## Performance
- Code splitting: charts and selected routes lazily loaded.
- React Query `staleTime` tuning to reduce refetching.

## Testing
- Unit/integration tests (Vitest + RTL + MSW) cover utilities, components, and flows.

## Deployment
- Build static assets and host on a static host (e.g., Vercel/Netlify).
- Set `VITE_API_BASE_URL` to the deployed API URL.

## API reference (optional)
- If `docs/openapi.json` is present: generate types via `npm --prefix frontend run codegen`.

<!-- END:LIBRARY-FRONTEND-README -->
