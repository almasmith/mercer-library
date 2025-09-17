# Backend – Library API (.NET)

<!-- BEGIN:BACKEND-README -->
## Prerequisites
- .NET 9 SDK (or .NET 8 if applicable)
- SQLite (optional; bundled provider works out of the box)

## Setup
```bash
# From repository root
dotnet restore backend/Library.sln
dotnet build backend/Library.sln
```

## Run (concurrently with the frontend)
- Terminal A (API):
```bash
dotnet watch run --project backend/src/Library.Api
```
- Terminal B (SPA):
```bash
npm --prefix frontend install
npm --prefix frontend run dev
```

## Verify
- API Swagger: http://localhost:5000/swagger
- SPA: http://localhost:5173

## OpenAPI / API testing
- The OpenAPI spec is stored at `docs/openapi.json`.
- Update from a running API (optional):
```bash
curl -fsS http://localhost:5000/swagger/v1/swagger.json -o docs/openapi.json
```

## Structure
```
backend/   .NET Web API, EF Core, migrations
frontend/  React + Vite SPA
docs/      OpenAPI (Swagger) JSON and docs
```

## Design decisions / trade-offs
- Simple layered API with DTO validation and ProblemDetails errors
- React SPA with hooks for data fetching and clear separation of concerns
- Pagination/sorting/filtering designed to scale
<!-- END:BACKEND-README -->


