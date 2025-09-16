import type { ReactNode } from "react";
import { ErrorBoundary } from "react-error-boundary";

type ProblemDetails = {
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
};

function isProblemDetails(value: unknown): value is ProblemDetails {
  if (value && typeof value === "object") {
    const v = value as Record<string, unknown>;
    return (
      ("title" in v || "detail" in v || "status" in v || "instance" in v) &&
      (v.title === undefined || typeof v.title === "string")
    );
  }
  return false;
}

function normalizeMessage(error: unknown): { heading: string; body?: string } {
  if (isProblemDetails(error)) {
    const heading = error.title ?? "Something went wrong";
    const body =
      error.detail ??
      (error.status ? `Request failed with status ${error.status}.` : undefined);
    return { heading, body };
  }
  if (error instanceof Error) {
    return { heading: "Unexpected error", body: error.message };
  }
  return { heading: "Unexpected error" };
}

function AppErrorFallback({ error, resetErrorBoundary }: { error: unknown; resetErrorBoundary: () => void }) {
  const { heading, body } = normalizeMessage(error);
  return (
    <div role="alert" className="rounded-md border border-red-200 bg-red-50 p-4 text-red-900">
      <h2 className="font-semibold">{heading}</h2>
      {body ? <p className="mt-1 text-sm">{body}</p> : null}
      <div className="mt-3 flex items-center gap-2">
        <button
          type="button"
          onClick={resetErrorBoundary}
          className="rounded bg-red-600 px-3 py-1.5 text-white hover:bg-red-700"
        >
          Try again
        </button>
        <button
          type="button"
          onClick={() => window.location.assign("/")}
          className="rounded border px-3 py-1.5"
        >
          Go home
        </button>
      </div>
    </div>
  );
}

export function AppErrorBoundary({ children }: { children: ReactNode }) {
  return (
    <ErrorBoundary
      FallbackComponent={AppErrorFallback}
      onReset={() => {
        // Default recovery: simply re-render. Feature areas can enhance this later.
      }}
    >
      {children}
    </ErrorBoundary>
  );
}
