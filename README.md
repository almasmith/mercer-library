# Mercer Library – Challenge Deliverables

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
# Frontend env
cp frontend/env.example frontend/.env
```

## Database
- **Development:** Migrations are applied automatically on first run and dev data is seeded; no manual step required.
- **Non-development:** Apply migrations manually:
```bash
dotnet ef database update --project backend/src/Library.Api --startup-project backend/src/Library.Api
```

## Run both apps concurrently
- Terminal A (API):
```bash
dotnet run --project backend/src/Library.Api/Library.Api.csproj --launch-profile https
```
- Terminal B (SPA):
```bash
# From repository root
npm --prefix frontend run dev
```

## Verify
- API Swagger: https://localhost:7186/swagger
- SPA: http://localhost:5173

## Auth
- Login with the seed credentials: test@example.com Passw0rd!
- With Swagger, click authorize and paste in the auth token from the login result

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

## OpenAPI 
- Spec file: `docs/openapi.json`
