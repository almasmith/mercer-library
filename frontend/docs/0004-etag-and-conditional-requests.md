# 0004 – ETag and conditional requests

Status: Accepted (Optional optimization)

## Context
Books resources and stats may provide ETags. The client can attach `If-Match` on PUT to avoid lost updates and use `If-None-Match` on GET to leverage 304s.

## Decision
- Store last ETag per resource key; attach `If-Match` on updates.
- Add conditional GET helper to send `If-None-Match` and handle 304.
- Surface 412 as a recoverable `HttpConflictError`.

## Consequences
- Lower bandwidth and flicker for stable resources.
- Extra complexity to manage ETag keys.
