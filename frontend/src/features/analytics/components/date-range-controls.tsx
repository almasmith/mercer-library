import { DateInput } from "@/features/books/components/date-input";

export function DateRangeControls({
  from,
  to,
  onChange,
}: {
  from?: string;
  to?: string;
  onChange: (next: { from?: string; to?: string }) => void;
}) {
  return (
    <div className="flex flex-wrap items-center gap-2">
      <div>
        <DateInput size="sm" value={from} onChange={(v) => onChange({ from: v, to })} />
      </div>
      <span className="self-center px-1 text-slate-600">–</span>
      <div>
        <DateInput size="sm" value={to} onChange={(v) => onChange({ from, to: v })} />
      </div>
    </div>
  );
}


