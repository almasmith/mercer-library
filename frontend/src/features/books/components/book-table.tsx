import { useMemo } from "react";
import { useSearchParams } from "react-router-dom";
import { useBooks } from "@/features/books/hooks/use-books";
import type { ListParams } from "@/features/books/api/books";
import { RATING_MIN, RATING_MAX } from "@/features/books/types/book";

const DEFAULT_SORT = { sortBy: "publishedDate", sortOrder: "desc" } as const;

function useListParams(): [ListParams, (next: Partial<ListParams>) => void] {
  const [params, setParams] = useSearchParams();
  const current = useMemo<ListParams>(() => {
    return {
      genre: params.get("genre") ?? undefined,
      minRating: params.get("minRating") ? Number(params.get("minRating")) : undefined,
      maxRating: params.get("maxRating") ? Number(params.get("maxRating")) : undefined,
      publishedFrom: params.get("publishedFrom") ?? undefined,
      publishedTo: params.get("publishedTo") ?? undefined,
      search: params.get("search") ?? undefined,
      sortBy: (params.get("sortBy") as ListParams["sortBy"]) ?? (DEFAULT_SORT.sortBy as any),
      sortOrder: (params.get("sortOrder") as ListParams["sortOrder"]) ?? (DEFAULT_SORT.sortOrder as any),
      page: params.get("page") ? Number(params.get("page")) : 1,
      pageSize: params.get("pageSize") ? Number(params.get("pageSize")) : 20,
    };
  }, [params]);

  const update = (next: Partial<ListParams>) => {
    const usp = new URLSearchParams(params);
    Object.entries(next).forEach(([k, v]) => {
      if (v === undefined || v === null || v === "") usp.delete(k);
      else usp.set(k, String(v));
    });
    setParams(usp, { replace: true });
  };
  return [current, update];
}

export function BookTable() {
  const [listParams, setListParams] = useListParams();
  const { data, isLoading, isError } = useBooks(listParams);

  const toggleSort = (key: NonNullable<ListParams["sortBy"]>) => {
    const { sortBy, sortOrder } = listParams;
    if (sortBy !== key) return setListParams({ sortBy: key, sortOrder: "asc" });
    if (sortOrder === "asc") return setListParams({ sortOrder: "desc" });
    // third click resets to default
    setListParams({ sortBy: DEFAULT_SORT.sortBy as any, sortOrder: DEFAULT_SORT.sortOrder as any });
  };

  if (isLoading) return <div>Loading…</div>;
  if (isError) return <div role="alert">Failed to load books.</div>;
  if (!data || data.items.length === 0) return <div>No books found.</div>;

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-end gap-2">
        <div>
          <label className="block text-sm">Search</label>
          <input
            type="text"
            className="mt-1 rounded border px-2 py-1"
            value={listParams.search ?? ""}
            onChange={(e) => setListParams({ search: e.target.value, page: 1 })}
            placeholder="Title or author"
          />
        </div>
        <div>
          <label className="block text-sm">Genre</label>
          <input
            type="text"
            className="mt-1 rounded border px-2 py-1"
            value={listParams.genre ?? ""}
            onChange={(e) => setListParams({ genre: e.target.value, page: 1 })}
            placeholder="e.g., Sci-Fi"
          />
        </div>
        <div>
          <label className="block text-sm">Rating</label>
          <div className="mt-1 flex items-center gap-1">
            <input type="number" min={RATING_MIN} max={RATING_MAX} className="w-16 rounded border px-2 py-1"
              value={listParams.minRating ?? ""} onChange={(e) => setListParams({ minRating: e.target.value ? Number(e.target.value) : undefined, page: 1 })}/>
            <span>–</span>
            <input type="number" min={RATING_MIN} max={RATING_MAX} className="w-16 rounded border px-2 py-1"
              value={listParams.maxRating ?? ""} onChange={(e) => setListParams({ maxRating: e.target.value ? Number(e.target.value) : undefined, page: 1 })}/>
          </div>
        </div>
        <div>
          <label className="block text-sm">Published</label>
          <div className="mt-1 flex items-center gap-1">
            <input type="date" className="rounded border px-2 py-1"
              value={listParams.publishedFrom ?? ""} onChange={(e) => setListParams({ publishedFrom: e.target.value || undefined, page: 1 })}/>
            <span>→</span>
            <input type="date" className="rounded border px-2 py-1"
              value={listParams.publishedTo ?? ""} onChange={(e) => setListParams({ publishedTo: e.target.value || undefined, page: 1 })}/>
          </div>
        </div>
      </div>

      <div className="overflow-x-auto">
        <table className="min-w-full border text-left text-sm">
          <thead className="bg-slate-50">
            <tr>
              {["title","author","genre","publishedDate","rating","createdAt"].map((col) => (
                <th key={col} className="cursor-pointer border-b px-3 py-2" onClick={() => toggleSort(col as any)}>
                  {col}
                  {listParams.sortBy === col ? (listParams.sortOrder === "asc" ? " ▲" : " ▼") : null}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {data.items.map((b) => (
              <tr key={b.id} className="border-b">
                <td className="px-3 py-2">{b.title}</td>
                <td className="px-3 py-2">{b.author}</td>
                <td className="px-3 py-2">{b.genre}</td>
                <td className="px-3 py-2">{new Date(b.publishedDate).toISOString().slice(0,10)}</td>
                <td className="px-3 py-2">{b.rating}</td>
                <td className="px-3 py-2">{new Date(b.createdAt).toLocaleString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}


