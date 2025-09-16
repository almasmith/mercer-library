import { env } from "./env";
import { dispatchUnauthorized } from "./auth-events";

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
  constructor(status: number, problem?: ProblemDetails) {
    super(problem?.title || `HTTP ${status}`);
    this.status = status;
    this.problem = problem;
  }
}

function toUrl(path: string): string {
  return new URL(path.replace(/^\//, ""), env.apiBaseUrl).toString();
}

export async function httpJson<T>(path: string, init: RequestInit = {}): Promise<T> {
  const res = await fetch(toUrl(path), {
    headers: { "Content-Type": "application/json", ...(init.headers || {}) },
    ...init,
  });

  if (res.status === 204) return undefined as T;
  if (res.ok) return (await res.json()) as T;

  let problem: ProblemDetails | undefined;
  try {
    problem = await res.json();
  } catch {
    // ignore
  }
  if (res.status === 401) {
    const returnTo = window.location.pathname + window.location.search;
    dispatchUnauthorized(returnTo);
  }
  throw new HttpError(res.status, problem);
}
