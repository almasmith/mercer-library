# 0003 – Realtime SignalR cache strategy

Status: Accepted

## Context
The server emits CRUD and analytics-related events. The client should reflect changes promptly without full refetch storms.

## Decision
- Central `realtime.ts` manages connection/reconnect with token.
- `useRealtime` registers handlers once and invalidates React Query caches for affected keys (lists, favorites, analytics).
- UI indicates connection state (badge).

## Consequences
- Fresh UI with minimal manual refresh.
- Bounded refetching via targeted invalidations.
