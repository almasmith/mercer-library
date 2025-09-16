import { StatsChart } from "@/features/stats/components/stats-chart";
import { useBookStats } from "@/features/stats/hooks/use-stats";

export default function StatsPage() {
  const { data, isLoading, isError } = useBookStats();

  if (isLoading) return <div className="h-80 animate-pulse rounded bg-slate-100" />;
  if (isError) return <div role="alert">Failed to load stats.</div>;

  return (
    <section className="space-y-4">
      <h1 className="text-xl font-semibold">Stats</h1>
      <StatsChart data={data ?? []} />
    </section>
  );
}
