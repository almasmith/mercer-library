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
    <div className="flex flex-wrap items-end gap-2">
      <div>
        <label className="block text-sm">From</label>
        <input type="date" className="mt-1 rounded border px-2 py-1" value={from ?? ""} onChange={(e) => onChange({ from: e.target.value || undefined, to })} />
      </div>
      <div>
        <label className="block text-sm">To</label>
        <input type="date" className="mt-1 rounded border px-2 py-1" value={to ?? ""} onChange={(e) => onChange({ from, to: e.target.value || undefined })} />
      </div>
    </div>
  );
}


