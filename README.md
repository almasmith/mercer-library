# Mercer Library – Challenge Deliverables

<!-- BEGIN:ROOT-CHALLENGE-README -->
## Prerequisites
- .NET 9 SDK (or .NET 8 if applicable)
- Node.js 20+ and npm
- SQLite (optional; bundled provider works out of the box)

## Setup
```bash
# From repository root
# Backend
dotnet restore backend/Library.sln
# Frontend
npm --prefix frontend install
```

## Run both apps concurrently
- Terminal A (API):
```bash
dotnet watch run --project backend/src/Library.Api
```
- Terminal B (SPA):
```bash
npm --prefix frontend run dev
```

## Verify
- API Swagger: http://localhost:5000/swagger
- SPA: http://localhost:5173

## Project structure
```
backend/   .NET Web API, EF Core, migrations
frontend/  React + Vite SPA
docs/      OpenAPI (Swagger) JSON and docs
```

## Design decisions / trade-offs
- Layered API with EF Core and DTO validation; errors use RFC 7807 ProblemDetails
- JWT auth and correlation ID middleware respected across endpoints
- React + Vite SPA with hooks and react-query for data fetching/caching
- ETag, conditional requests, and realtime SignalR updates for good UX
- Pagination and filtering designed to scale to larger datasets

## OpenAPI / API testing
- Spec file: `docs/openapi.json`
- Update from a running API (optional):
```bash
curl -fsS http://localhost:5000/swagger/v1/swagger.json -o docs/openapi.json
```
<!-- END:ROOT-CHALLENGE-README -->
