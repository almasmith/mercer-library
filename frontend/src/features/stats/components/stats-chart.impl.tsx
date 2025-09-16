import { ResponsiveContainer, BarChart, XAxis, YAxis, Tooltip, Bar, CartesianGrid } from "recharts";
import type { BookStats } from "@/features/stats/types/stats";

export default function StatsChartImpl({ data }: { data: BookStats }) {
  const normalized = data.map(d => ({ genre: d.genre || "Unknown", count: d.count }));
  return (
    <ResponsiveContainer width="100%" height={320}>
      <BarChart data={normalized} margin={{ top: 8, right: 16, left: 0, bottom: 24 }}>
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis dataKey="genre" angle={-30} textAnchor="end" height={60} />
        <YAxis allowDecimals={false} />
        <Tooltip />
        <Bar dataKey="count" fill="#0ea5e9" />
      </BarChart>
    </ResponsiveContainer>
  );
}


