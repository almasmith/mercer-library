import { env } from "@/lib/env";

export type ProblemDetails = {
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
};

export class HttpError extends Error {
  status: number;
  problem?: ProblemDetails;
  correlationId?: string;
  constructor(status: number, problem?: ProblemDetails, correlationId?: string) {
    super(problem?.title || `HTTP ${status}`);
    this.status = status;
    this.problem = problem;
    this.correlationId = correlationId;
  }
}

export class HttpUnauthorizedError extends HttpError {}
export class HttpConflictError extends HttpError {} // 412 handling added in F11

type AccessTokenProvider = () => string | undefined;
let getAccessToken: AccessTokenProvider | undefined;

/**
 * Allows the auth layer to inject a token provider without creating React hook coupling.
 */
export function setAccessTokenProvider(provider: AccessTokenProvider) {
  getAccessToken = provider;
}

type CorrelationIdProvider = () => string;
let getCorrelationId: CorrelationIdProvider = () =>
  (crypto && "randomUUID" in crypto ? (crypto as any).randomUUID() : Math.random().toString(36).slice(2));

export function setCorrelationIdProvider(provider: CorrelationIdProvider) {
  getCorrelationId = provider;
}

export type HttpOptions = Omit<RequestInit, "headers"> & {
  headers?: Record<string, string>;
  ifMatch?: string;
  ifNoneMatch?: string;
  onEtag?: (etag?: string) => void;
};

function buildUrl(path: string): string {
  const clean = path.startsWith("/") ? path.slice(1) : path;
  return new URL(clean, env.apiBaseUrl).toString();
}

export async function httpJson<T>(path: string, options: HttpOptions = {}): Promise<T> {
  const url = buildUrl(path);
  const token = getAccessToken?.();
  const correlationId = getCorrelationId();

  const headers: Record<string, string> = {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-Correlation-ID": correlationId,
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...(options.headers || {}),
  };
  if (options.ifMatch) headers["If-Match"] = options.ifMatch;
  if (options.ifNoneMatch) headers["If-None-Match"] = options.ifNoneMatch;

  let res: Response;
  try {
    res = await fetch(url, { ...options, headers });
  } catch (networkErr) {
    throw new HttpError(0, { title: "Network error", detail: String(networkErr) }, correlationId);
  }

  const etag = res.headers.get("etag") || undefined;
  if (options.onEtag) options.onEtag(etag);

  if (res.status === 204) return undefined as T;
  const isJson = (res.headers.get("content-type") || "").includes("application/json");
  const parseBody = async () => (isJson ? ((await res.json()) as unknown) : undefined);

  if (res.ok) {
    return (await parseBody()) as T;
  }

  const problem = (await parseBody()) as ProblemDetails | undefined;

  if (res.status === 401) throw new HttpUnauthorizedError(res.status, problem, correlationId);
  if (res.status === 412) throw new HttpConflictError(res.status, problem, correlationId);
  throw new HttpError(res.status, problem, correlationId);
}
