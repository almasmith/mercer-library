## Project Plan: Book Library Application (Backend: .NET, Frontend: React + Vite SPA)

This plan integrates all requirements, including authentication (JWT), sorting/filtering, full testing, containerization with Docker, and cloud deployment, as core scope.

## Objectives & Scope

- **Functional coverage**: CRUD for books, stats by genre, sorting/filtering, per-user data isolation, robust validation, error handling, and analytics.
- **Security**: JWT auth, password hashing, authorization on all book operations; bearer tokens (in-memory with localStorage hydration); no BFF/cookies.
- **Quality**: Unit/integration tests (backend and frontend), typed contracts, consistent error shapes, Swagger/OpenAPI.
- **DevEx**: Monorepo, clear layering, hot reload, seed data, environment-driven configuration.
- **Operations**: Dockerfiles for backend/frontend, docker-compose for local orchestration, deployment to Azure (API) and Vercel (frontend).

## High-Level Architecture

- **Monorepo** with separate `backend/` (.NET 8 Web API) and `frontend/` (React 18 + Vite + TypeScript).
- **Database**: SQLite for local development; SQL Server for container and cloud (switchable by `DB_PROVIDER`).
- **Auth**: ASP.NET Core Identity + JWT bearer tokens. SPA attaches `Authorization: Bearer <token>` to requests; no BFF or HttpOnly cookies.
- **Data access**: EF Core (migrations, seeding). Book rows scoped to authenticated user (`OwnerUserId`).
- **Frontend**: React Router (SPA), Tailwind CSS + Radix UI, TanStack Query, React Hook Form + Zod, Recharts (lazy-loaded).

## Repository Structure

```text
backend/
  src/
    Library.Api/
      Controllers/
      Data/
      Domain/
      Dtos/
      Mappings/
      Services/
      Hubs/
      Validation/
      Program.cs
      appsettings.json
    Library.Tests/
  Migrations/
frontend/
  index.html
  src/
    main.tsx
    routes/             (React Router route modules)
    components/
    features/books/
      api/
      components/
      hooks/
      types/
    features/favorites/
      api/
      components/
      hooks/
      types/
    features/analytics/
      api/
      components/
      hooks/
      types/
    lib/                (http client, query client, auth helpers)
    styles/
  vite.config.ts
docs/
  openapi.json
README.md
docker-compose.yml
```

## Backend Plan (.NET 8, ASP.NET Core Web API)

### 1) Scaffolding & Packages

- **Create solution/projects**: `dotnet new sln` + `dotnet new webapi -n Library.Api` + `dotnet new xunit -n Library.Tests`.
- **Packages**:
  - EF Core: `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`.
  - Identity & JWT: `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.AspNetCore.Authentication.JwtBearer`.
  - Swagger: `Swashbuckle.AspNetCore`.
  - Validation & mapping: `FluentValidation.AspNetCore`, `AutoMapper.Extensions.Microsoft.DependencyInjection`.
- **CORS**: allow SPA origins (`http://localhost:5173` for Vite, plus frontend container host). No credentials; tokens are sent via `Authorization` header.
  - Configure via `CORS__AllowedOrigins` (comma-separated). Default in dev: `http://localhost:5173`.

### 2) Domain Models & Schema

- **Entities** (in `Domain/`):
  - `ApplicationUser` (IdentityUser<Guid>): Email (unique), PasswordHash, CreatedAt.
  - `Book`: `Id: Guid`, `Title: string`, `Author: string`, `Genre: string`, `PublishedDate: DateTimeOffset`, `Rating: int (1–5)`, `OwnerUserId: Guid` (FK), `CreatedAt`, `UpdatedAt`, `RowVersion: byte[]` (concurrency token).
  - `Favorite`: `UserId: Guid`, `BookId: Guid`, `CreatedAt`. Composite PK `(UserId, BookId)`. FKs to `ApplicationUser` and `Book` with cascade delete on book.
  - `BookRead`: `Id: Guid`, `BookId: Guid`, `UserId: Guid`, `OccurredAt: DateTimeOffset`. Index on `(UserId, OccurredAt)` and `(BookId, OccurredAt)`.
- **DbContext** (in `Data/LibraryDbContext.cs`):
  - `DbSet<Book>`; extend `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`.
  - Configure constraints: required fields, string lengths (Title 200, Author 200, Genre 100), rating range check constraint (`CK_Books_RatingRange` ensures 1–5), indexes on `(OwnerUserId, Genre)`, `(OwnerUserId, PublishedDate)`, `Favorite(UserId, BookId)`, and `RowVersion` marked as concurrency token.
  - No uniqueness constraint on books (duplicates allowed to support editions/translations). All ownership queries filter by `OwnerUserId`.
- **Provider switching**: read `DB_PROVIDER` ("sqlite" | "sqlserver"); choose connection accordingly.

### 3) DTOs & Validation (in `Dtos/`, `Validation/`)

- **DTOs**:
  - `BookDto` (response): mirrors Book without `OwnerUserId`.
  - `CreateBookRequest`, `UpdateBookRequest` (request): Title, Author, Genre, PublishedDate (ISO), Rating.
  - `Auth` DTOs: `RegisterRequest` { email, password }, `LoginRequest` { email, password }, `AuthResponse` { accessToken, expiresIn }.
- **FluentValidation** rules: Title/Author/Genre/PublishedDate/Rating required; trim whitespace before validation; reject empty-after-trim for Title/Author/Genre; enforce max lengths (Title ≤ 200, Author ≤ 200, Genre ≤ 100); `Rating ∈ [1,5]`; PublishedDate must be ≤ today + 1 day.
- **ProblemDetails** on validation failure.

### 4) Authentication & Authorization

- **Identity**: configure ASP.NET Core Identity with GUID keys and EF stores.
- **JWT**: issue on successful login; HS256 signing key from `JWT__Secret`, expiry (e.g., 60m), issuer/audience.
- **Endpoints** (`Controllers/AuthController`):
  - `POST /api/auth/register` → create user → 201 (no token) or 200 with token.
  - `POST /api/auth/login` → returns `AuthResponse` (client stores token for subsequent requests).
- **Authorization**: all book endpoints require `[Authorize]`; repository queries must filter by `OwnerUserId == UserId`.
- **Swagger security**: Bearer scheme with "Authorize" button.

### 4a) Real-time (SignalR)

- **Hub**: `Hubs/LibraryHub` with endpoints and groups per user (`Group: user:{UserId}`).
- **Events** (server → client): `bookCreated`, `bookUpdated`, `bookDeleted`, `bookFavorited`, `bookUnfavorited`, `bookRead`, `statsUpdated`.
- **Server publish points**: after successful CRUD/favorite/read operations, publish to the owner's group.
- **Auth**: Hub requires JWT; connection uses `access_token` query param.

### 5) Services Layer (optional but recommended)

- `IBookService` / `BookService`: encapsulate CRUD, stats, and query composition (sorting/filtering/paging) per user.
- `IAuthService`: registration, login, token issuance.
- `IFavoritesService` / `FavoritesService`: toggle/list favorites.
- `IAnalyticsService` / `AnalyticsService`: record reads and compute analytics.

### 6) Controllers & Endpoints (`Controllers/BooksController`)

- **CRUD**:
  - `GET /api/books` → list current user's books with optional query params.
  - `GET /api/books/{id}` → get by id (owned by user) or 404.
  - `POST /api/books` → create (sets OwnerUserId from claims). 201 + Location.
  - `PUT /api/books/{id}` → update if owned; 200 with updated. Route id is source of truth; if body contains `id` and mismatches route, return 400.
  - `DELETE /api/books/{id}` → delete if owned; 204.
- **Stats**:
  - `GET /api/books/stats` → returns `[{ genre: string, count: number }]` scoped to current user.
  - Genre normalization for grouping: trim whitespace and case-insensitive compare; empty/null genres are excluded. Results are sorted by `count desc` then `genre asc`.

- **Favorites** (in `Controllers/FavoritesController`):
  - `POST /api/books/{id}/favorite` → 204; idempotent (favoriting an already-favorited book still returns 204).
  - `DELETE /api/books/{id}/favorite` → 204; idempotent.
  - `GET /api/favorites` → 200 paged list of favorited books with optional `search`, `genre`, and sorting same as books.

- **Reads & Analytics** (in `Controllers/AnalyticsController`):
  - `POST /api/books/{id}/read` → 204; records a `BookRead` event for current user.
  - `GET /api/analytics/avg-rating` → 200 `[{ bucket: string, average: number }]` where `bucket` is `YYYY-MM` when `bucket=month`.
  - `GET /api/analytics/most-read-genres` → 200 `[{ genre: string, readCount: number }]` over optional `from`, `to`.

- **ETag/Concurrency (relaxed)**:
  - `Book` uses `RowVersion` internally for optimistic concurrency (server-side). `GET /api/books/{id}` may include an `ETag` header with a base64 of `RowVersion`.
  - `PUT /api/books/{id}` accepts requests without `If-Match` (last-write-wins). If `If-Match` is provided and stale, return 412 Precondition Failed. `If-None-Match` on GET may return 304 when applicable.

### 7) Sorting, Filtering, Paging (core)

- **Query params** on `GET /api/books`:
  - Filtering: `genre`, `minRating`, `maxRating`, `publishedFrom`, `publishedTo`, `search` (title/author contains).
  - Sorting: `sortBy` in { title, author, genre, publishedDate, rating, createdAt }, `sortOrder` in { asc, desc }.
  - Paging: `page` (1+), `pageSize` (default 20, max 100).
- **Response shape**:
  - `{ items: BookDto[], page, pageSize, totalItems, totalPages }`.
 - Default sort: `publishedDate desc`.

### 8) Error Handling & Logging

- Global exception middleware → `ProblemDetails` (500) with correlation id.
- Map known exceptions (404, 400) to ProblemDetails; log at appropriate levels.
- Correlation: accept `X-Correlation-ID` on requests; if absent, generate one. Echo it in responses and include in logs and `ProblemDetails.instance`.

### 8a) Serialization & JSON Conventions

- `System.Text.Json` with `camelCase` property naming.
- Serialize `DateTimeOffset` as ISO 8601 with `Z` (UTC). Accept `YYYY-MM-DD` and interpret as midnight UTC.
- ProblemDetails conforms to RFC 7807 with `errors` for validation issues.

### 8b) Health Checks & Rate Limiting

- Health checks: expose `/health` (liveness) and DB check for readiness; include in Docker/K8s/Cloud probes.
- Rate limiting: fixed-window 100 req/min per IP for authenticated API, 20 req/min for auth endpoints. Return 429 with ProblemDetails.

### 9) Swagger/OpenAPI

- Enable Swagger in Development and Staging.
- Add example payloads, request/response schemas, and bearer auth header.
- Export OpenAPI JSON and commit it to `docs/openapi.json`. (Postman collection omitted; developers can import OpenAPI.)

### 10) Migrations & Seeding

- Initial migration creates Identity tables + `Books`.
- Seed: a test user and 5–10 example books (owned by that user) in Development.
  - Include 2–3 favorites and a handful of `BookRead` events across genres/dates to make analytics charts meaningful.
  - Dev user: `test@example.com` / `Passw0rd!` (development only).

### 11) Testing (xUnit + TestServer)

- **Unit tests**: validators, services (sorting/filtering logic), and API controllers (action-level behavior, status codes, model validation binding).
- **Integration tests**: using `WebApplicationFactory`:
  - `POST /auth/register` → `POST /auth/login` → end-to-end CRUD and stats with auth.
  - Validation failures return 400 with field errors.
  - Authorization enforced: accessing another user's book returns 404.
  - Concurrency (relaxed): If `If-Match` provided and stale → 412; otherwise last-write-wins.
  - Stats normalization: genres with varied casing/whitespace group together; empty genres excluded.
  - Favorites: favoriting/unfavoriting is idempotent; lists reflect toggles.
  - Analytics: read events recorded; avg rating buckets and most-read genres return expected shapes.

### 12) Configuration & Environments

- `appsettings.Development.json` defaults to SQLite.
- `appsettings.Docker.json` / environment variables for SQL Server.
- Keys:
  - `DB_PROVIDER`, `ConnectionStrings__Sqlite`, `ConnectionStrings__SqlServer`.
  - `JWT__Secret`, `JWT__Issuer`, `JWT__Audience`, `JWT__ExpiresMinutes`.
  - `CORS__AllowedOrigins` (comma-separated origin list).
 - Ports and URLs via `launchSettings.json`: HTTP 5000, HTTPS 5001.

### 13) Docker (core)

- **Backend Dockerfile**: multi-stage build (sdk → runtime), ARGs for provider, healthcheck.
- **Compose service**: `backend` exposes 5000, depends on `sqlserver` when provider=sqlserver. Volume mount for SQLite when provider=sqlite.
- **SQL Server service**: `mcr.microsoft.com/mssql/server:2022-latest` with persisted volume.
- **Entrypoint**: run EF migrations on startup.

### 14) Deployment (core)

- **Azure App Service** for API:
  - Build & publish via GitHub Actions or `dotnet publish` artifact.
  - Configure `DB_PROVIDER=sqlserver` and connection string to Azure SQL.
  - Set JWT secrets as app settings; enable HTTPS only.
- **Monitoring**: enable App Insights logs; structured logging.

### 15) CI/CD (core)

- GitHub Actions pipeline:
  - On PR/merge: restore, build, run tests (backend + frontend), lint frontend, run dotnet format check.
  - Build and push Docker images for backend and frontend with tags `:sha` and `:latest`.
  - Deploy backend to Azure App Service; deploy frontend to Azure Static Web Apps (or Vercel) on main branch updates.

## Frontend Plan (React 18 + Vite, TypeScript)

### 1) Scaffolding & Packages

- **Create app**: `npm create vite@latest frontend -- --template react-ts`.
- **UI**: `tailwindcss`, Radix UI (optional).
- **Routing**: `react-router-dom`.
- **Data**: `@tanstack/react-query`, `zod`, `react-hook-form`, `@hookform/resolvers`.
- **Charts**: `recharts` (lazy-loaded with `React.lazy`/dynamic import).
- **HTTP**: `axios` (optional) or `fetch` with a small wrapper.
- **Testing**: `vitest`, `@testing-library/react`, `@testing-library/user-event`, `msw`.
 - **Code quality**: `eslint` + React/TS plugins and `prettier`; enable TypeScript strict mode in `tsconfig.json` (`"strict": true`).

### 2) Architecture & Data Flow

- **SPA pattern**:
  - UI calls the backend API directly at `import.meta.env.VITE_API_BASE_URL`.
  - A lightweight HTTP client attaches `Authorization: Bearer <token>` from in-memory state (with localStorage fallback) and maps errors to a normalized shape.
- **React Query** for client caching; retry/backoff on transient errors; query keys for lists/records/stats.

### 3) Routing & Pages (React Router)

- `/` → Book List (filters/sort bound to URL search params).
- `/books/new` → Create Book form.
- `/books/:id/edit` → Edit Book form.
- `/stats` → Stats view (chart dynamically imported).
- `/favorites` → Favorites list (same table component with `favoritesOnly` preset).
- `/login` & `/register` → Auth pages.
 - Protected routes: `'/'`, `'/books/*'`, and `'/stats'` require auth via `RequireAuth`. Auth pages redirect to `'/'` if already authenticated. On 401, users are redirected to `'/login?returnTo=<current>'`.

### 4) Types & Schemas

- `features/books/types/book.ts`: `Book`, `CreateBookInput`, `UpdateBookInput`, `PagedBooks`, `BookStats`.
- Zod schemas mirror backend validation (shared constants for rating range, max lengths). Date is handled as ISO UTC at midnight (`YYYY-MM-DDT00:00:00Z`).
 - `BookStats` is `Array<{ genre: string; count: number }>`.

### 5) API Layer

- **HTTP utilities** (`lib/http.ts`): base URL from `VITE_API_BASE_URL`, attach bearer token, normalize errors to a unified shape based on RFC 7807 `ProblemDetails` (`{ title, status, detail, instance, errors?: Record<string,string[]> }`). Handle `ETag`/`If-Match` for book updates: store last ETag from GET and send as `If-Match` on PUT.
- **Books API** (`features/books/api/books.ts`): get/list (with params), create, update, delete, stats.
- **Favorites API** (`features/favorites/api/favorites.ts`): favorite, unfavorite, list favorites.
- **Analytics API** (`features/analytics/api/analytics.ts`): record read, fetch avg rating buckets, fetch most-read genres.

### 6) Hooks (React Query)

- `useBooks(params)`, `useBook(id)`, `useCreateBook()`, `useUpdateBook()`, `useDeleteBook()`, `useBookStats()`.
- Cache keys: `['books', params]`, `['book', id]`, `['stats']`.
- Invalidate caches on mutations.
- Favorites: `useFavorite(id)`, `useUnfavorite(id)`, `useFavorites(params)` with keys `['favorites', params]`.
- Analytics: `useRecordRead(id)`, `useAvgRating(params)`, `useMostReadGenres(params)`.

### 7) UI Components

- **BookTable**: responsive table with sort headers, filter controls (genre combobox/select, rating range, date pickers, search), and pagination controls (page size: 10, 20, 50; default 20). Prev/Next and page number input with accessible labels. Show total results and current range (e.g., "1–20 of 132").
- **BookForm**: RHF + Zod; fields Title, Author, Genre (combobox with free text allowed; suggested options derived from stats keys), PublishedDate (date input mapped to ISO `YYYY-MM-DDT00:00:00Z`), Rating (1–5). Reusable for create/edit.
- **StatsChart**: bar/pie chart (dynamic import) from `recharts`. If no data, render an empty state with guidance to add books. Use a consistent color palette mapped from Tailwind theme tokens. Expects `BookStats` array.
- **Feedback**: LoadingSpinner, ErrorState, EmptyState, ConfirmDialog, Toasts.
- **FavoriteToggle**: star icon button in list/cards; optimistic update with rollback on error.
- **FavoritesFilter**: toggle to filter list to favorites-only.
- **RealtimeBadge**: indicates live updates; listens to SignalR connection state.

### 8) Auth Flow (core)

- Registration and login pages call backend `/api/auth/*` directly.
- Successful login returns `accessToken` with `expiresIn`; store token in memory (fallback to `localStorage` on reload) and attach to subsequent requests. On expiry or 401 responses, perform a clean logout and redirect to `'/login?returnTo=<last>'` with a toast notification.
- `useAuth` hook exposes `isAuthenticated`, `login`, `logout`, and persists minimal user info.

### 9) Sorting & Filtering UI (core)

- Query params bound to UI controls; URL state preserved with `useSearchParams`. Default sort is `publishedDate desc`. Column header toggles: first click asc, second desc, third clears to default.
- React Query prefetches on mount and on URL param changes (no SSR in Vite SPA); client transitions update results without full reload.

### 10) Validation & UX

- Inline field errors from Zod; disable submit while pending; optimistic UI on delete with rollback. Map backend validation errors (ProblemDetails `errors`) to RHF field errors.
- Accessible labels, keyboard navigation, focus management.

### 11) Performance

- Dynamic import `recharts` and heavy components.
- Image optimization (if any assets); memoization where needed.
- React Query caching and sensible `staleTime`.
 - Charts are dynamically imported (`recharts`) to reduce initial bundle size; color palette aligns with Tailwind theme tokens.

### 12) Testing (Vitest + RTL + MSW)

- **Unit**: form validation, UI components.
- **Integration**: hooks with MSW simulating backend; auth flows; list sorting/filtering.
- **E2E (optional)**: Playwright smoke tests for CRUD path.
- **Realtime**: mock SignalR client; verify cache updates on simulated hub events.

### 2a) Realtime Client (SignalR)

- Use `@microsoft/signalr` to connect to `/hubs/library` with the bearer token via `accessTokenFactory`.
- Reconnect with exponential backoff. Expose a small `useRealtime` hook that binds events and updates React Query caches:
  - On `bookCreated` → prepend to paged cache if matches current filters.
  - On `bookUpdated` → update list/detail caches.
  - On `bookDeleted` → remove from caches.
  - On `bookFavorited/bookUnfavorited` → update favorites and list caches.
  - On `bookRead`/`statsUpdated` → refetch stats/analytics queries.
 - Show a lightweight connection status indicator (`RealtimeBadge`).

### 13) Docker (core)

- **Frontend Dockerfile**: multi-stage (install → build) to static assets served by `nginx:alpine`.
- **Compose**: `frontend` depends_on `backend`; pass build arg/env `VITE_API_BASE_URL=http://backend:5000` so the SPA calls the API inside the compose network.

### 14) Deployment (core)

- **Azure Static Web Apps** (or Netlify): build Vite SPA and set `VITE_API_BASE_URL` to the cloud API URL. Ensure cache headers for static assets and disable caching on `index.html` for safe deployments.
- **Auth**: SPA uses bearer tokens; ensure HTTPS; no cookies required.

## Local Development Workflow

### Prerequisites

- .NET 8 SDK, Node 20+, Docker Desktop, Azure account (for deployment), SQL Server (optional local container).

### Environment variables (dev defaults)

```bash
# backend
ASPNETCORE_ENVIRONMENT=Development
DB_PROVIDER=sqlite # or sqlserver
ConnectionStrings__Sqlite=Data Source=./data/library.db
ConnectionStrings__SqlServer=Server=sqlserver;Database=Library;User Id=sa;Password=Your_strong_password123;TrustServerCertificate=True
JWT__Secret=dev_super_secret_key_change_me
JWT__Issuer=library-api
JWT__Audience=library-client
CORS__AllowedOrigins=http://localhost:5173

# frontend (Vite SPA)
VITE_API_BASE_URL=http://localhost:5000
VITE_DEFAULT_PAGE_SIZE=20
VITE_MAX_PAGE_SIZE=100
```

### Common commands

```bash
# Backend: first-time setup
dotnet restore ./backend
dotnet tool install --global dotnet-ef
dotnet ef database update -p backend/src/Library.Api -s backend/src/Library.Api

# Backend: run with hot reload
dotnet watch --project backend/src/Library.Api

# Frontend: setup and run
cd frontend && npm install && npm run dev

# Frontend: lint & format
cd frontend && npm run lint && npm run format

# Docker: full stack
docker compose up --build
```

### Run both apps concurrently (non-Docker)

```bash
# using concurrently from a root package.json (recommended)
# package.json scripts:
#   "dev": "concurrently \"dotnet watch --project backend/src/Library.Api\" \"npm --prefix frontend run dev\""
npm run dev
```

### Maintainers only: creating/updating migrations

```bash
dotnet ef migrations add InitialCreate -p backend/src/Library.Api -s backend/src/Library.Api
dotnet ef database update -p backend/src/Library.Api -s backend/src/Library.Api
```

## Acceptance Criteria

- **API**: All CRUD endpoints secure by JWT; correct status codes; `/api/books/stats` returns `[{ genre, count }]` for authenticated user (normalized, sorted); sorting/filtering/paging work; Swagger documents endpoints with auth.
- **DB**: EF Core migrations apply cleanly; SQLite works locally; SQL Server works in Docker/cloud.
- **Frontend**: Book list with sort/filter and pagination controls; create/edit forms validate and submit (including ProblemDetails field errors); stats page renders chart with empty state; login/register work; protected routes redirect unauthenticated to login with `returnTo`; robust loading/error states; optional ETag/If-Match supported.
- **Security**: SPA stores token in memory/localStorage; CORS restricted to SPA origins; HTTPS enforced in cloud.
- **Reliability**: Health checks exposed; rate limiting enforced; correlation ID propagated via `X-Correlation-ID`.
- **Tests**: Backend unit/integration including concurrency + conditional requests; Frontend unit/integration; basic coverage on critical paths.
- **Operations**: Docker images build/run; compose brings up the stack; deployment instructions succeed.
- **Favorites**: Users can favorite/unfavorite; favorites list works; idempotent behavior validated.
- **Realtime**: SignalR hub broadcasts CRUD/favorite/read events; clients receive and update lists/stats without manual refresh.
- **Analytics**: Avg rating over time and most-read genres endpoints return correct aggregates; UI charts render with seeded data.

## Risks & Mitigations

- **Date/time handling**: Use ISO UTC end-to-end; parse carefully in UI.
- **Validation mismatch**: Mirror rules in Zod; contract tests via MSW; e2e happy path.
- **Token handling**: Use in-memory tokens (hydrated from localStorage) with bearer auth; on 401 or token expiry, perform clean logout and redirect to login; avoid cookies to minimize CSRF surface in this challenge.
- **Provider drift**: Test both SQLite and SQL Server in CI to avoid provider-specific issues.

## Milestones & Timeline

- **M1**: Backend scaffold, Identity + JWT, models/DbContext, migrations, seed, Swagger.
- **M2**: Books controller with CRUD + sorting/filtering + stats; global error handling.
- **M3**: Frontend scaffold, React Router setup, React Query + HTTP client, auth pages.
- **M4**: Book List with sorting/filtering; Book Form create/edit with validation.
- **M5**: Stats chart page; UX polish (toasts, empty/error states).
- **M6**: Tests (backend + frontend), documentation.
- **M7**: Dockerfiles + docker-compose; deploy API to Azure and frontend to Vercel.

## Appendix A: API Spec Outline

- **Auth**
  - `POST /api/auth/register` → 201/200; errors 400.
  - `POST /api/auth/login` → 200 `{ accessToken, expiresIn }`; errors 401.
- **Books** (all require `Authorization: Bearer`)
  - `GET /api/books?genre=&minRating=&maxRating=&publishedFrom=&publishedTo=&search=&sortBy=&sortOrder=&page=&pageSize=` → 200 paged result (default sort `publishedDate desc`).
  - `GET /api/books/{id}` → 200 or 404 if not owned. May include `ETag`. Honors `If-None-Match` → 304 when provided.
  - `POST /api/books` → 201 with Location.
  - `PUT /api/books/{id}` → 200 updated, 404 if not owned. If `If-Match` provided and stale → 412.
  - `DELETE /api/books/{id}` → 204.
  - `GET /api/books/stats` → 200 `[{ genre, count }]`. Includes `ETag`; honors `If-None-Match`.
- **Favorites**
  - `POST /api/books/{id}/favorite` → 204.
  - `DELETE /api/books/{id}/favorite` → 204.
  - `GET /api/favorites?page=&pageSize=&genre=&search=&sortBy=&sortOrder=` → 200 paged result.
- **Analytics**
  - `POST /api/books/{id}/read` → 204.
  - `GET /api/analytics/avg-rating?bucket=month&from=&to=` → 200 `[{ bucket, average }]`.
  - `GET /api/analytics/most-read-genres?from=&to=` → 200 `[{ genre, readCount }]`.

## Appendix B: Deployment Notes

- **Azure API**: set app settings (connection string, JWT secrets), turn on HTTPS only, configure health check endpoint.
- **SPA hosting**: Azure Static Web Apps or Netlify. Set `VITE_API_BASE_URL` to API URL at build time.

## Appendix C: Documentation & Tooling

- Root `README.md` includes setup, migrations, running dev/prod, Docker, deployment, and design decisions.
- Swagger UI at `/swagger`; export OpenAPI JSON and commit it to `docs/openapi.json`. (Postman collection omitted; developers can import OpenAPI.)


