# 0001 – ProblemDetails and error handling

Status: Accepted

## Context
The API standardizes errors as RFC 7807 ProblemDetails. The client must surface field errors for forms and concise toasts for general failures, preserving correlation IDs for support.

## Decision
- Normalize all non-2xx responses to `ProblemDetails` in `src/lib/http.ts`.
- Provide `HttpError` with `{ status, problem, correlationId }`.
- Map `problem.errors[field]` to form field messages; show toasts for non-field failures.
- Add a route-level ErrorBoundary fallback for unexpected errors.

## Consequences
- Predictable error UX across features.
- Easier diagnostics via correlation IDs.
- Slight overhead to parse JSON and maintain shapes.

## Alternatives considered
- Per-feature error shapes (rejected for inconsistency).
- Silent failures (rejected).
EOF,
