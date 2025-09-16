import { useSearchParams } from "react-router-dom";
import { AvgRatingChart } from "@/features/analytics/components/avg-rating-chart";
import { MostReadGenresChart } from "@/features/analytics/components/most-read-genres-chart";
import { DateRangeControls } from "@/features/analytics/components/date-range-controls";
import { useAvgRating, useMostReadGenres, useRecordRead } from "@/features/analytics/hooks/use-analytics";

export default function AnalyticsPage() {
  const [params, setParams] = useSearchParams();
  const from = params.get("from") || undefined;
  const to = params.get("to") || undefined;

  const { data: ratings, isLoading: rL, isError: rE } = useAvgRating({ bucket: "month", from, to });
  const { data: genres, isLoading: gL, isError: gE } = useMostReadGenres({ from, to });

  const rec = useRecordRead();

  const onRangeChange = (next: { from?: string; to?: string }) => {
    const usp = new URLSearchParams(params);
    if (next.from) usp.set("from", next.from); else usp.delete("from");
    if (next.to) usp.set("to", next.to); else usp.delete("to");
    setParams(usp, { replace: true });
  };

  return (
    <section className="space-y-6">
      <div className="flex items-end justify-between gap-3">
        <h1 className="text-xl font-semibold">Analytics</h1>
        <DateRangeControls from={from ?? undefined} to={to ?? undefined} onChange={onRangeChange} />
      </div>

      <div className="space-y-3">
        <h2 className="text-lg font-medium">Average rating over time</h2>
        {rL ? <div className="h-80 animate-pulse rounded bg-slate-100" /> : rE ? <div role="alert">Failed to load average rating.</div> : <AvgRatingChart data={ratings ?? []} />}
      </div>

      <div className="space-y-3">
        <h2 className="text-lg font-medium">Most read genres</h2>
        {gL ? <div className="h-80 animate-pulse rounded bg-slate-100" /> : gE ? <div role="alert">Failed to load genres.</div> : <MostReadGenresChart data={genres ?? []} />}
      </div>

      {/* Optional: quick action to record a read by ID for demo purposes */}
      <div className="flex items-center gap-2">
        <input id="markReadId" placeholder="Book ID" className="w-64 rounded border px-2 py-1" />
        <button
          className="rounded border px-3 py-1.5"
          onClick={() => {
            const id = (document.getElementById("markReadId") as HTMLInputElement)?.value?.trim();
            if (id) rec.mutate(id);
          }}
        >
          Mark as Read
        </button>
      </div>
    </section>
  );
}


