import { Suspense, lazy } from "react";
import type { MostReadGenre } from "@/features/analytics/types/analytics";

const Lazy = lazy(() => import("./most-read-genres-chart.impl"));

export function MostReadGenresChart({ data }: { data: MostReadGenre[] }) {
  if (!data || data.length === 0) return <div className="text-sm text-slate-600">No genre data.</div>;
  return (
    <Suspense fallback={<div className="h-80 animate-pulse rounded bg-slate-100" />}>
      <Lazy data={data} />
    </Suspense>
  );
}


