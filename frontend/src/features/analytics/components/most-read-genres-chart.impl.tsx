import { ResponsiveContainer, BarChart, Bar, XAxis, YAxis, Tooltip, CartesianGrid } from "recharts";
import type { MostReadGenre } from "@/features/analytics/types/analytics";

export default function MostReadGenresChartImpl({ data }: { data: MostReadGenre[] }) {
  const normalized = data.map(d => ({ genre: d.genre || "Unknown", readCount: d.readCount }));
  return (
    <ResponsiveContainer width="100%" height={320}>
      <BarChart data={normalized} margin={{ top: 8, right: 16, left: 0, bottom: 24 }}>
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis dataKey="genre" angle={-30} textAnchor="end" height={60} />
        <YAxis allowDecimals={false} />
        <Tooltip />
        <Bar dataKey="readCount" fill="#22c55e" />
      </BarChart>
    </ResponsiveContainer>
  );
}


