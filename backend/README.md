# Backend – Library API (.NET)

## Prerequisites
- .NET 9 SDK (or .NET 8 if applicable)
- SQLite (optional; bundled provider works out of the box)

## Setup
```bash
# From repository root
dotnet restore backend/Library.sln
dotnet build backend/Library.sln
```

## Run
```bash
dotnet run --project backend/src/Library.Api/Library.Api.csproj --no-build --launch-profile https
```

## Verify
- API Swagger: http://localhost:7186/swagger

## OpenAPI / API testing
- The OpenAPI spec is stored at `docs/openapi.json`.

## Structure
```
backend/   .NET Web API, EF Core, migrations
frontend/  React + Vite SPA
docs/      OpenAPI (Swagger) JSON and docs
```
