import { Suspense, lazy } from "react";
import type { BookStats } from "@/features/stats/types/stats";

const LazyChart = lazy(() => import("./stats-chart.impl"));

export function StatsChart({ data }: { data: BookStats }) {
  if (!data || data.length === 0) {
    return <div className="text-sm text-slate-600">No data to display yet. Add books to see stats.</div>;
  }
  return (
    <Suspense fallback={<div className="h-80 animate-pulse rounded bg-slate-100" />}>
      <LazyChart data={data} />
    </Suspense>
  );
}


