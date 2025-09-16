import { Suspense, lazy } from "react";
import type { AvgRatingPoint } from "@/features/analytics/types/analytics";

const Lazy = lazy(() => import("./avg-rating-chart.impl"));

export function AvgRatingChart({ data }: { data: AvgRatingPoint[] }) {
  if (!data || data.length === 0) return <div className="text-sm text-slate-600">No rating data.</div>;
  return (
    <Suspense fallback={<div className="h-80 animate-pulse rounded bg-slate-100" />}>
      <Lazy data={data} />
    </Suspense>
  );
}


