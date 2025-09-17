import { useMemo } from "react";
import { useSearchParams } from "react-router-dom";

const PAGE_SIZES = [10, 20, 50, 100];

export function Pagination({ totalItems }: { totalItems: number }) {
  const [params, setParams] = useSearchParams();
  const page = Number(params.get("page") || 1);
  const pageSize = Number(params.get("pageSize") || 20);
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize));
  const from = totalItems === 0 ? 0 : (page - 1) * pageSize + 1;
  const to = Math.min(totalItems, page * pageSize);

  const set = (k: string, v?: string) => {
    const next = new URLSearchParams(params);
    if (!v) next.delete(k);
    else next.set(k, v);
    setParams(next, { replace: true });
  };

  const disablePrev = page <= 1;
  const disableNext = page >= totalPages;

  const sizeOptions = useMemo(() => PAGE_SIZES, []);

  return (
    <div className="flex flex-wrap items-center justify-between gap-3">
      <div className="text-sm text-slate-600">Rows {from}–{to} of {totalItems}</div>
      <div className="flex items-center gap-2">
        <label className="text-sm">Rows per page</label>
        <select className="rounded border px-2 py-1 focus:outline-none focus:ring-2 focus:ring-slate-300" value={pageSize}
          onChange={(e) => { set("pageSize", e.target.value); set("page", "1"); }}>
          {sizeOptions.map(s => <option key={s} value={s}>{s}</option>)}
        </select>
        <button className="rounded border px-2 py-1 hover:bg-slate-50 disabled:opacity-50" disabled={disablePrev}
          onClick={() => set("page", String(page - 1))}>Prev</button>
        <input aria-label="Page number" type="number" min={1} max={totalPages}
          className="w-20 rounded border px-2 py-1 focus:outline-none focus:ring-2 focus:ring-slate-300"
          value={page} onChange={(e) => {
            const v = Math.min(Math.max(1, Number(e.target.value || 1)), totalPages);
            set("page", String(v));
          }} />
        <button className="rounded border px-2 py-1 hover:bg-slate-50 disabled:opacity-50" disabled={disableNext}
          onClick={() => set("page", String(page + 1))}>Next</button>
      </div>
    </div>
  );
}


