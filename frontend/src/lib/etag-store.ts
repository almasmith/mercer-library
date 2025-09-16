/**
 * In-memory ETag store keyed by a logical resource key (e.g., "book:{id}").
 * Replace with a more robust cache or React Query meta if desired later.
 */
const etags = new Map<string, string>();

export function getEtag(key: string): string | undefined {
  return etags.get(key);
}
export function setEtag(key: string, etag?: string) {
  if (!etag) return;
  etags.set(key, etag);
}
export function clearEtag(key: string) {
  etags.delete(key);
}


