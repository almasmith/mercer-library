import { useSearchParams } from "react-router-dom";
import { AvgRatingChart } from "@/features/analytics/components/avg-rating-chart";
import { MostReadGenresChart } from "@/features/analytics/components/most-read-genres-chart";
import { DateRangeControls } from "@/features/analytics/components/date-range-controls";
import { useAvgRating, useMostReadGenres, useRecordRead } from "@/features/analytics/hooks/use-analytics";

export default function AnalyticsPage() {
  const { data: ratings, isLoading: rL, isError: rE } = useAvgRating({ bucket: "month" });
  const { data: genres, isLoading: gL, isError: gE } = useMostReadGenres({});
  const rec = useRecordRead();

  return (
    <section className="space-y-6">
      <div className="toolbar card p-3">
        <h1 className="text-xl font-semibold">Analytics</h1>
        <p className="text-sm text-slate-500">Insights about your library</p>
      </div>

      <div className="card space-y-3 p-3">
        <h2 className="text-lg font-medium">Average rating over time</h2>
        {rL ? <div className="h-80 animate-pulse rounded bg-slate-100" /> : rE ? <div role="alert">Failed to load average rating.</div> : <AvgRatingChart data={ratings ?? []} />}
      </div>

      <div className="card space-y-3 p-3">
        <h2 className="text-lg font-medium">Most read genres</h2>
        {gL ? <div className="h-80 animate-pulse rounded bg-slate-100" /> : gE ? <div role="alert">Failed to load genres.</div> : <MostReadGenresChart data={genres ?? []} />}
      </div>

      {/* Optional: quick action to record a read by ID for demo purposes */}
      <div className="card flex items-center gap-2 p-3">
        <input id="markReadId" placeholder="Book ID" className="input w-64" />
        <button
          className="btn"
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


