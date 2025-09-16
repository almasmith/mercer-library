import { getEtag, setEtag } from "@/lib/etag-store";
import { httpConditionalGet, type ConditionalGetResult, httpJson } from "@/lib/http";

export async function getWithEtag<T>(key: string, path: string): Promise<ConditionalGetResult<T>> {
  const etag = getEtag(key);
  const result = await httpConditionalGet<T>(path, etag);
  if (result.etag) setEtag(key, result.etag);
  return result;
}

export async function getAndStoreEtag<T>(key: string, path: string): Promise<T> {
  return await httpJson<T>(path, {
    onEtag: (etag) => setEtag(key, etag),
  });
  // For non-conditional reads where we just want to capture the latest ETag.
}


