import { ResponsiveContainer, LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip } from "recharts";
import type { AvgRatingPoint } from "@/features/analytics/types/analytics";

export default function AvgRatingChartImpl({ data }: { data: AvgRatingPoint[] }) {
  return (
    <ResponsiveContainer width="100%" height={320}>
      <LineChart data={data} margin={{ top: 8, right: 16, left: 0, bottom: 24 }}>
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis dataKey="bucket" />
        <YAxis domain={[0, 5]} allowDecimals />
        <Tooltip />
        <Line type="monotone" dataKey="average" stroke="#0ea5e9" strokeWidth={2} dot={false} />
      </LineChart>
    </ResponsiveContainer>
  );
}


