## DevOps Tasks (Docker, CI/CD, Deployment, Observability)

Note: Cross-cutting for both backend and frontend per project plan.

### Track D1 — Containerization

- [D1] Backend Dockerfile
  - Multi-stage build (sdk → runtime), cache restores, ARG `DB_PROVIDER`.
  - Add `HEALTHCHECK` hitting `/health`.
  - DoD: Image builds locally; runs with both SQLite and SQL Server.

- [D2] Frontend Dockerfile
  - Multi-stage (install → build) producing static assets; serve via `nginx:alpine`.
  - DoD: Image builds and serves SPA; `VITE_API_BASE_URL` configurable at build.

- [D3] docker-compose.yml
  - Services: `backend`, `frontend`, `sqlserver` (for provider=sqlserver).
  - Wire env vars (DB_PROVIDER, connection strings, JWT, CORS origins).
  - Volumes: persist SQL Server; mount SQLite file when provider=sqlite.
  - DoD: `docker compose up --build` brings up full stack; backend healthcheck passes.

- [D4] EF migrations on startup
  - Backend entrypoint runs `dotnet ef database update` (or applies migrations programmatically).
  - DoD: Fresh container initializes schema automatically.

### Track D2 — CI/CD

- [D5] GitHub Actions: Build & Test
  - On PR/merge: restore, build, run backend unit/integration tests; frontend unit/integration; lint frontend; dotnet format check.
  - DoD: Workflow green on sample PR.

- [D6] Provider matrix tests
  - Matrix jobs for `sqlite` and `sqlserver` using compose for SQL Server job.
  - DoD: Both providers tested; migrations validated.

- [D7] Build & push Docker images
  - Build backend and frontend images tagged with `:sha` and `:latest`; push to registry.
  - DoD: Images published on main branch.

- [D8] OpenAPI export artifact
  - Step to hit Swagger/OpenAPI and export `docs/openapi.json`; commit or upload artifact.
  - DoD: File present and matches API; artifact attached to workflow run.

- [D9] Deploy backend to Azure App Service
  - Use official action to deploy; set app settings (DB_PROVIDER=sqlserver, connection string, JWT secrets); HTTPS only.
  - DoD: Successful deploy; healthcheck green.

- [D10] Deploy frontend (Vercel or Azure Static Web Apps)
  - Build with `VITE_API_BASE_URL` pointing to cloud API; cache headers for static assets; disable cache for index.html.
  - DoD: SPA live and connected to API.

### Track D3 — Operational Hardening

- [D11] Swagger environment gating verification
  - Ensure Swagger enabled only in Development/Staging; disabled in Production.
  - DoD: Environment checks validated on deployment slots.

- [D12] Monitoring & logs
  - Enable App Insights; structured logging; capture correlation id; set sampling.
  - DoD: Traces visible in Azure; correlation id propagated end-to-end.

- [D13] Secrets management
  - Store secrets in GitHub Actions secrets and Azure App Service app settings; do not commit secrets.
  - DoD: Pipelines reference secrets securely; no secrets in repo.

- [D14] Rollback plan
  - Maintain last known-good image tags; document rollback steps for API and SPA.
  - DoD: Runbook documented in README.


