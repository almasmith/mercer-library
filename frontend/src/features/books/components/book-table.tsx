import { useMemo } from "react";
import { useSearchParams } from "react-router-dom";
import { Link } from "react-router-dom";
import { useBooks } from "@/features/books/hooks/use-books";
import { useDeleteBook } from "@/features/books/hooks/use-books";
import { useConfirm } from "@/components/confirm-dialog";
import type { ListParams } from "@/features/books/api/books";
import { RATING_MIN, RATING_MAX } from "@/features/books/types/book";
import { Pagination } from "./pagination";
import { FavoriteToggle } from "@/features/favorites/components/favorite-toggle";

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

export function BookTable({ useData = useBooks }: { useData?: (p: any) => { data?: any; isLoading: boolean; isError: boolean } }) {
  const [listParams, setListParams] = useListParams();
  const { data, isLoading, isError } = useData(listParams);
  const del = useDeleteBook();
  const { Confirm, confirm } = useConfirm();

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
      <Confirm />
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
              <th className="border-b px-3 py-2" aria-label="Favorite" />
              {["title","author","genre","publishedDate","rating","createdAt"].map((col) => (
                <th key={col} className="cursor-pointer border-b px-3 py-2" onClick={() => toggleSort(col as any)}>
                  {col}
                  {listParams.sortBy === col ? (listParams.sortOrder === "asc" ? " ▲" : " ▼") : null}
                </th>
              ))}
              <th className="border-b px-3 py-2">Actions</th>
            </tr>
          </thead>
          <tbody>
            {data.items.map((b: any) => (
              <tr key={b.id} className="border-b">
                <td className="px-3 py-2">
                  <FavoriteToggle bookId={b.id} />
                </td>
                <td className="px-3 py-2">{b.title}</td>
                <td className="px-3 py-2">{b.author}</td>
                <td className="px-3 py-2">{b.genre}</td>
                <td className="px-3 py-2">{new Date(b.publishedDate).toISOString().slice(0,10)}</td>
                <td className="px-3 py-2">{b.rating}</td>
                <td className="px-3 py-2">{new Date(b.createdAt).toLocaleString()}</td>
                <td className="px-3 py-2">
                  <Link to={`/books/${b.id}/edit`} className="mr-2 underline">Edit</Link>
                  <button className="text-red-700 underline" onClick={async () => {
                    const ok = await confirm(`Delete "${b.title}"? This cannot be undone.`);
                    if (!ok) return;
                    const prev = data.items;
                    (data.items as any) = data.items.filter((x: any) => x.id !== b.id);
                    try { await del.mutateAsync(b.id); }
                    catch { (data.items as any) = prev; alert("Failed to delete"); }
                  }}>Delete</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <Pagination totalItems={data.totalItems} />
    </div>
  );
}


