# 0002 – Auth token storage and route guarding

Status: Accepted

## Context
The SPA uses bearer tokens (no cookies). Tokens should live in memory to reduce exposure and hydrate from localStorage for reloads. Unauthenticated users are redirected to `/login?returnTo=...`.

## Decision
- `useAuth` holds token in memory and mirrors minimal state to localStorage.
- 401 responses trigger logout and redirect via a global unauthorized event.
- `RequireAuth` protects app routes.

## Consequences
- Safe default (in-memory) with reload persistence.
- Clear redirect semantics for 401 and deep links.
