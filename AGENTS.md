# Repository Guidelines

## Environment Notes
This repository is currently in a Git work tree. Treat any instructions that reference `/Users/jason/Projects/mercer`, the project root directory, as operations to be performed directly within this work tree.

## Project Structure & Module Organization
The backend solution lives in `backend/Library.sln`. Application code sits in `backend/src/Library.Api`, organized by concern: `Controllers/` for HTTP endpoints, `Domain/` for aggregates and value objects, `Data/` and `Migrations/` for EF Core persistence, plus `Configuration/`, `Infrastructure/`, and `Services/` for cross-cutting layers. Automated tests belong in `backend/src/Library.Tests`, mirroring the API namespace. Documentation and task briefs remain in the repository root.

## Build, Test, and Development Commands
Run `dotnet restore backend/Library.sln` after pulling to sync dependencies. Use `dotnet build backend/Library.sln` for CI-equivalent compilation and analyzers. Local iteration is fastest with `dotnet watch run --project backend/src/Library.Api`, which hot-reloads the API on port 5000. Execute the suite with `dotnet test backend/src/Library.sln --collect:"XPlat Code Coverage"` to capture coverlet output, and `dotnet ef migrations add Name --project backend/src/Library.Api --startup-project backend/src/Library.Api` when evolving the schema.

## Coding Style & Naming Conventions
C# projects target .NET 9 with nullable reference types enabled; use async/await and Task-returning methods by default. Follow four-space indentation, file-scoped namespaces, and expression-bodied members when concise. Prefer explicit access modifiers, PascalCase for classes, camelCase for private fields, and pluralized controller names (e.g., `BooksController`). Keep DTOs in `Domain` or `Services` namespaces that match their usage. Run `dotnet format backend/Library.sln` before opening a PR to enforce standard analyzers and ordering.

## Testing Guidelines
Write xUnit tests beside the behavior they cover inside `Library.Tests`, naming files `<TypeUnderTest>Tests.cs` and methods `Method_State_ExpectedResult`. Seed integration-style tests with in-memory SQLite. Every non-trivial controller or service change needs test coverage; aim to keep overall coverage trending upward and justify any gaps in the PR description. Include reproduction tests for reported defects where possible.

## Commit & Pull Request Guidelines
Follow the existing history by prefixing commit subjects with the tracker key, e.g., `B58:` or `[B58]`, followed by an imperative summary under 72 characters. Squash local work before pushing. Pull requests should describe intent, list primary changes, note new migrations or config keys, link to the associated issue, and attach screenshots or curl examples for API-visible updates. Document manual test steps and expected outcomes so reviewers can reproduce results quickly.

## Security & Configuration Tips
Keep secrets out of `appsettings.json`; use environment variables or ASP.NET User Secrets during development. Validate that new endpoints respect the correlation ID middleware and JWT policies defined in `Program.cs`. When adding health checks or logging fields, ensure sensitive data such as tokens or passwords never reaches structured logs.
