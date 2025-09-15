## Backend Implementation Tasks (API, Data, Realtime)

Note: Backend-only. Excludes all frontend and broader DevOps (Docker/CI/CD) tasks.

### Track A — Project Scaffolding & Configuration

- [B1] Initialize solution and projects
  - Create .NET solution, Web API project `Library.Api`, and test project `Library.Tests`.
  - Add basic `Program.cs` with controllers and Swagger enabled in Development.
  - DoD: Projects build and run; Swagger UI reachable.

- [B2] Add core NuGet packages
  - EF Core (Sqlite, SqlServer, Design), Identity EF, JWT Bearer, Swashbuckle, FluentValidation, AutoMapper, SignalR.
  - DoD: `dotnet restore` succeeds; packages referenced in `.csproj` files.

- [B3] App configuration & options binding
  - Bind `DB_PROVIDER`, connection strings, JWT settings, CORS allowed origins, rate limit options.
  - DoD: Options validated on startup; invalid config fails fast with clear log.

- [B4] JSON serialization conventions
  - Configure `System.Text.Json` camelCase, ISO 8601; ensure `DateTimeOffset` UTC.
  - DoD: Sample endpoint returns camelCased JSON; dates are ISO with Z.

- [B5] CORS policy
  - Configure named policy from `CORS__AllowedOrigins` (comma-separated).
  - DoD: Preflight passes from configured origin; blocked from others.

### Track B — Domain Model & Persistence

- [B6] Entity: ApplicationUser
  - Extend `IdentityUser<Guid>`; configure Identity tables.
  - DoD: Identity migrations compile.

- [B7] Entity: Book
  - Properties: Id, Title, Author, Genre, PublishedDate (DateTimeOffset), Rating (1–5), OwnerUserId, CreatedAt, UpdatedAt, RowVersion.
  - DoD: Fluent configuration applies required/max lengths, check constraint on rating, concurrency token.

- [B8] Entity: Favorite (many-to-many)
  - Composite PK (UserId, BookId); FKs to User/Book; CreatedAt.
  - DoD: Unique favorite per user/book enforced by PK.

- [B9] Entity: BookRead (event)
  - Id, BookId, UserId, OccurredAt; indexes on (UserId, OccurredAt) and (BookId, OccurredAt).
  - DoD: Mapped with required fields and indexes.

- [B10] DbContext & provider switching
  - Inherit from `IdentityDbContext`; add DbSets; provider switch by `DB_PROVIDER`.
  - Configure indexes on (OwnerUserId, Genre), (OwnerUserId, PublishedDate).
  - DoD: Startup connects to chosen provider; migrations compatible with both.

- [B11] Initial migration
  - Create migration for Identity + Books + Favorites + BookReads.
  - DoD: `dotnet ef database update` creates schema from empty DB.

- [B12] Development seeding
  - Seed test user, 5–10 books, 2–3 favorites, and some BookRead events across dates/genres.
  - DoD: Dev DB contains seeded data; credentials documented.

### Track C — Security & Auth

- [B13] Identity setup & password policy
  - Configure Identity options, GUID keys, password complexity for dev.
  - DoD: Can create user via UserManager in seeding.

- [B14] JWT authentication
  - Configure JWT bearer options, token issuance service (HS256), expiry, issuer/audience.
  - DoD: Valid token authorizes API requests.

- [B15] Auth endpoints
  - `POST /api/auth/register`, `POST /api/auth/login` returning `{ accessToken, expiresIn }`.
  - DoD: Happy-path register/login e2e works; validation errors return ProblemDetails.

- [B16] Authorization & user scoping
  - Enforce `[Authorize]` on protected controllers; repository/service queries filter by `OwnerUserId`.
  - DoD: Accessing another user’s resources yields 404/Forbidden as designed.

- [B55] Register response semantics
  - Decide and implement response for `POST /api/auth/register`: either 201 (no token) or 200 with token; document in Swagger.
  - DoD: Integration tests assert chosen status/body; Swagger examples updated.

### Track D — Cross-Cutting Concerns

- [B17] ProblemDetails & exception handling
  - Global exception middleware mapping known errors; include correlation id.
  - DoD: Consistent RFC7807 responses for 4xx/5xx.

- [B18] Correlation IDs
  - Accept `X-Correlation-ID`, generate if missing; echo in responses/logs.
  - DoD: ID present in responses and logs.

- [B19] Rate limiting
  - Fixed window: 100 req/min per IP for API; 20 req/min for auth endpoints.
  - DoD: 429 returned with ProblemDetails when limits exceeded.

- [B20] Health checks
  - `/health` liveness and DB readiness probe.
  - DoD: Health endpoints return expected statuses.

- [B21] Logging
  - Structured logging configuration; sensible levels for dev.
  - DoD: Requests and key events logged; secrets not logged.

- [B54] ProblemDetails shape guarantees
  - Ensure validation failures return `ValidationProblemDetails` with `errors` populated; include correlation id in `ProblemDetails.instance`.
  - DoD: Integration tests assert RFC7807 fields and correlation id propagation.

### Track E — Validation, Mapping, DTOs

- [B22] DTOs
  - `BookDto`, `CreateBookRequest`, `UpdateBookRequest` (no OwnerUserId), auth DTOs.
  - DoD: DTOs used by controllers; AutoMapper profiles compiled.

- [B23] AutoMapper profiles
  - Map between entities and DTOs; handle date conversions.
  - DoD: Unit tests cover basic mappings.

- [B24] FluentValidation rules
  - Trim Title/Author/Genre; required; max lengths; Rating in [1,5]; PublishedDate ≤ today + 1 day.
  - DoD: Invalid payloads return 400 with field errors.

- [B52] Date-only parsing (YYYY-MM-DD)
  - Accept `YYYY-MM-DD` inputs and coerce to `YYYY-MM-DDT00:00:00Z` (UTC) via custom JsonConverter/model binder.
  - DoD: Unit tests verify parsing and timezone safety; validators accept coerced inputs.

### Track F — Books Feature

- [B25] BooksService
  - Implement create/read/update/delete; list with filtering (genre, rating range, date range, search), sorting, paging; default sort `publishedDate desc`.
  - DoD: Service unit-tested for query composition and correctness.

- [B26] BooksController
  - Endpoints: `GET /api/books`, `GET /api/books/{id}`, `POST /api/books`, `PUT /api/books/{id}`, `DELETE /api/books/{id}`; enforce route/body id match.
  - DoD: Integration tests pass; correct status codes.

- [B27] Stats endpoint
  - `GET /api/books/stats` → `[{ genre, count }]` (normalized: trim + case-insensitive; exclude empty/null genres; sorted by count desc then genre asc).
  - DoD: Integration tests validate grouping/normalization and exclusion of empty/null genres.

- [B50] Conditional GET & ETag (Book detail)
  - Emit strong `ETag` on `GET /api/books/{id}` using base64 of `RowVersion`.
  - Honor `If-None-Match`: return `304 Not Modified` when ETag matches.
  - DoD: Integration tests cover 200 vs 304 behavior; ETag changes after update/delete.

- [B51] Conditional GET & ETag (Stats)
  - Include `ETag` on `GET /api/books/stats`; honor `If-None-Match` → 304 when unchanged.
  - Maintain a per-user stats version that bumps on book/favorite/read changes.
  - DoD: Integration tests cover stats 200 vs 304; version bumps on relevant mutations.

### Track G — Favorites Feature

- [B28] FavoritesService
  - Toggle favorite/unfavorite (idempotent); list favorited books with filters/sort/paging.
  - DoD: Service unit tests cover idempotency and listing.

- [B29] FavoritesController
  - Endpoints: `POST /api/books/{id}/favorite`, `DELETE /api/books/{id}/favorite`, `GET /api/favorites`.
  - DoD: Integration tests pass; idempotent 204 responses.

### Track H — Analytics & Reads

- [B30] AnalyticsService
  - Record read event; compute average rating buckets (by month); compute most-read genres over optional date range.
  - DoD: Unit tests verify aggregation logic and edge cases.

- [B31] AnalyticsController
  - Endpoints: `POST /api/books/{id}/read`, `GET /api/analytics/avg-rating`, `GET /api/analytics/most-read-genres`.
  - DoD: Integration tests validate shapes and filtering.

### Track I — Realtime (SignalR)

- [B32] LibraryHub
  - Create `Hubs/LibraryHub` with per-user groups (`user:{UserId}`); JWT auth on hub.
  - DoD: Hub connects with valid token; unauthorized rejected.

- [B33] Event publishing
  - Publish `bookCreated`, `bookUpdated`, `bookDeleted`, `bookFavorited`, `bookUnfavorited`, `bookRead`, `statsUpdated` after successful operations to owner group.
  - DoD: Manual verification with a simple hub client; events received as expected.

- [B34] Program integration
  - Map `/hubs/library`; configure SignalR services and authentication handshake.
  - DoD: Hub available under configured path.

- [B53] JWT via access_token for WebSockets
  - Configure `JwtBearerEvents.OnMessageReceived` to read `access_token` from query string when `Request.Path` starts with `/hubs/library`.
  - DoD: Integration test connects to hub providing `?access_token=...`.

### Track J — API Documentation

- [B35] Swagger configuration
  - Add Bearer security scheme; annotate endpoints; include examples.
  - DoD: Swagger UI shows secured endpoints with models and examples.

- [B36] OpenAPI export
  - Ensure `docs/openapi.json` is generated/exported and committed.
  - DoD: File exists and matches live API.

- [B56] Swagger environment gating
  - Enable Swagger only in Development and Staging; disable in Production.
  - DoD: Environment toggling verified; no Swagger in Production.

### Track K — Testing

- [B37] Unit tests: validators
  - Cover all FluentValidation rules; invalid/edge cases.
  - DoD: Tests pass; coverage for rules.

- [B38] Unit tests: AutoMapper
  - Verify mappings for entities ⇄ DTOs.
  - DoD: Tests pass.

- [B39] Unit tests: services (Books)
  - CRUD behavior; filtering/sorting/paging correctness.
  - DoD: Tests pass.

- [B40] Unit tests: services (Favorites)
  - Toggle idempotency; listing.
  - DoD: Tests pass.

- [B41] Unit tests: services (Analytics)
  - Aggregations for avg rating buckets and most-read genres.
  - DoD: Tests pass.

- [B42] Integration: auth flow
  - Register → login → authenticated requests.
  - DoD: Tests pass; tokens authorize endpoints.

- [B43] Integration: books CRUD + list
  - End-to-end create/edit/delete; list sorting/filtering/paging.
  - DoD: Tests pass; correct status codes.

- [B44] Integration: stats
  - Per-genre counts with normalization and sort order.
  - DoD: Tests pass.

- [B45] Integration: favorites
  - Favorite/unfavorite idempotency; favorites listing.
  - DoD: Tests pass.

- [B46] Integration: reads & analytics
  - Record read events; fetch avg rating buckets; fetch most-read genres w/ ranges.
  - DoD: Tests pass.

- [B47] Integration: authorization boundaries
  - Cross-user access blocked; returns 404/Forbidden as designed.
  - DoD: Tests pass.

- [B48] Integration: concurrency (relaxed)
  - PUT without If-Match allowed; If-Match stale → 412.
  - DoD: Tests pass.

- [B49] Integration: realtime smoke
  - Connect to hub; receive a subset of events (e.g., on create/delete/favorite/read).
  - DoD: Tests pass.

- [B57] Integration: conditional GET 304
  - Books detail and stats endpoints return 304 when `If-None-Match` matches; 200 with updated ETag after mutations.
  - DoD: Tests pass.

- [B58] Integration: rate limiting policies
  - Verify 20 rpm policy for auth endpoints and 100 rpm for authenticated API; responses return 429 with ProblemDetails.
  - DoD: Tests pass for both policy scopes.

- [B59] Integration: SignalR access_token handshake
  - Connect to `/hubs/library` using `?access_token=` without Authorization header; connection authorized.
  - DoD: Tests pass.

### Parallelization Notes

- Tracks A–D can proceed largely in parallel once B1 is done.
- Tracks F, G, H depend on completion of domain model (B6–B11) and auth (C track) but can proceed in parallel with each other.
- Track I depends on controllers/services wiring but can stub publish points early.
- Track K tests can be authored iteratively per feature track and run in parallel.
