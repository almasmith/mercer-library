import { StatsChart } from "@/features/stats/components/stats-chart";
import { useBookStats } from "@/features/stats/hooks/use-stats";

export default function StatsPage() {
  const { data, isLoading, isError } = useBookStats();

  if (isLoading) return <div className="card h-80 animate-pulse" />;
  if (isError) return <div role="alert">Failed to load stats.</div>;

  return (
    <section className="space-y-4">
      <div className="toolbar card p-3">
        <h1 className="text-xl font-semibold">Stats</h1>
        <p className="text-sm text-slate-500">Genres by count</p>
      </div>
      <div className="card p-3">
        <StatsChart data={data ?? []} />
      </div>
    </section>
  );
}
