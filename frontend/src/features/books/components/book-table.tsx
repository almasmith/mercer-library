import { useMemo } from "react";
import { useSearchParams } from "react-router-dom";
import { Link } from "react-router-dom";
import { useBooks } from "@/features/books/hooks/use-books";
import { useDeleteBook } from "@/features/books/hooks/use-books";
import { useConfirm } from "@/components/confirm-dialog";
import type { ListParams } from "@/features/books/api/books";
import { RATING_MIN, RATING_MAX, type Book } from "@/features/books/types/book";
import { Pagination } from "./pagination";
import { FavoriteToggle } from "@/features/favorites/components/favorite-toggle";
import { formatDate } from "@/lib/dates";
import { DateInput } from "./date-input";

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
      sortBy: (params.get("sortBy") as ListParams["sortBy"]) ?? DEFAULT_SORT.sortBy,
      sortOrder: (params.get("sortOrder") as ListParams["sortOrder"]) ?? DEFAULT_SORT.sortOrder,
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

export function BookTable({ useData = useBooks }: { useData?: (p: ListParams) => { data?: { items: Book[]; totalItems: number }; isLoading: boolean; isError: boolean } }) {
  const [listParams, setListParams] = useListParams();
  const { data, isLoading, isError } = useData(listParams);
  const del = useDeleteBook();
  const { Confirm, confirm } = useConfirm();

  const toggleSort = (key: NonNullable<ListParams["sortBy"]>) => {
    const { sortBy, sortOrder } = listParams;
    if (sortBy !== key) return setListParams({ sortBy: key, sortOrder: "asc" });
    if (sortOrder === "asc") return setListParams({ sortOrder: "desc" });
    // third click resets to default
    setListParams({ sortBy: DEFAULT_SORT.sortBy, sortOrder: DEFAULT_SORT.sortOrder });
  };

  return (
    <div className="space-y-3">
      <Confirm />
      <div className="flex flex-wrap items-end justify-between gap-2">
        <div className="flex flex-wrap items-end gap-2">
          <div>
          <label className="block text-sm">Search</label>
          <input
            type="text"
            className="input-sm mt-1"
            value={listParams.search ?? ""}
            onChange={(e) => setListParams({ search: e.target.value, page: 1 })}
            placeholder="Title or author"
          />
          </div>
          <div>
          <label className="block text-sm">Genre</label>
          <input
            type="text"
            className="input-sm mt-1"
            value={listParams.genre ?? ""}
            onChange={(e) => setListParams({ genre: e.target.value, page: 1 })}
            placeholder="e.g., Sci-Fi"
          />
          </div>
          <div>
          <label className="block text-sm">Rating</label>
          <div className="mt-1 flex items-center gap-1">
            <input type="number" min={RATING_MIN} max={RATING_MAX} className="input-sm w-16"
              value={listParams.minRating ?? ""} onChange={(e) => setListParams({ minRating: e.target.value ? Number(e.target.value) : undefined, page: 1 })}/>
            <span>–</span>
            <input type="number" min={RATING_MIN} max={RATING_MAX} className="input-sm w-16"
              value={listParams.maxRating ?? ""} onChange={(e) => setListParams({ maxRating: e.target.value ? Number(e.target.value) : undefined, page: 1 })}/>
          </div>
          </div>
          <div>
          <label className="block text-sm">Published</label>
          <div className="mt-1 flex items-center gap-1">
            <DateInput size="sm" value={listParams.publishedFrom ?? undefined} onChange={(v) => setListParams({ publishedFrom: v, page: 1 })} />
            <span>→</span>
            <DateInput size="sm" value={listParams.publishedTo ?? undefined} onChange={(v) => setListParams({ publishedTo: v, page: 1 })} />
          </div>
          </div>
        </div>
        <a href="/books/new" className="ml-auto inline-flex h-9 items-center gap-2 rounded bg-blue-600 px-3 text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-300">
          <span className="text-sm font-medium">Add book</span>
        </a>
      </div>

      <div className="overflow-x-auto">
        {isLoading ? (
          <div>Loading…</div>
        ) : isError ? (
          <div role="alert">Failed to load books.</div>
        ) : data?.items?.length ? (
          <table className="min-w-full rounded-md border text-left text-sm shadow-sm">
            <thead className="bg-white">
              <tr>
                <th className="border-b px-3 py-2" aria-label="Favorite" />
                {(["title","author","genre","publishedDate","rating"] as const).map((col) => (
                  <th key={col} className="select-none border-b px-3 py-2" onClick={() => toggleSort(col)}>
                    <button className="inline-flex items-center gap-1 rounded px-1 py-0.5 hover:bg-slate-100">
                      <span className="capitalize text-slate-700">{col}</span>
                      {listParams.sortBy === col ? (
                        <span aria-hidden className="text-xs text-slate-500">{listParams.sortOrder === "asc" ? "▲" : "▼"}</span>
                      ) : null}
                    </button>
                  </th>
                ))}
                <th className="border-b px-3 py-2" aria-label="Actions" />
              </tr>
            </thead>
            <tbody>
              {data.items.map((b: Book) => (
                <tr key={b.id} className="border-b hover:bg-slate-50">
                  <td className="px-3 py-2">
                    <FavoriteToggle bookId={b.id} />
                  </td>
                  <td className="px-3 py-2 font-medium text-slate-800">{b.title}</td>
                  <td className="px-3 py-2 text-slate-700">{b.author}</td>
                  <td className="px-3 py-2">
                    {b.genre ? (
                      <span className="chip">{b.genre}</span>
                    ) : (
                      <span className="text-slate-400">—</span>
                    )}
                  </td>
                  <td className="px-3 py-2">{formatDate(b.publishedDate)}</td>
                  <td className="px-3 py-2">
                    <span className="rounded bg-emerald-50 px-2 py-0.5 text-xs font-semibold text-emerald-700 ring-1 ring-emerald-200">{b.rating.toFixed(1)}</span>
                  </td>
                  
                  <td className="px-3 py-2">
                    <RowActions id={b.id} onDelete={async () => {
                      const ok = await confirm(`Delete "${b.title}"? This cannot be undone.`);
                      if (!ok) return;
                      const prev = data.items;
                      data.items = data.items.filter((x) => x.id !== b.id);
                      try { await del.mutateAsync(b.id); }
                      catch { data.items = prev; alert("Failed to delete"); }
                    }} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : (
          <div className="text-sm text-slate-600">No matching books.</div>
        )}
      </div>
      <Pagination totalItems={data?.totalItems ?? 0} />
    </div>
  );
}


function RowActions({ id, onDelete }: { id: string; onDelete: () => void }) {
  return (
    <div className="relative">
      <details className="group inline-block">
        <summary className="list-none inline-flex h-8 w-8 cursor-pointer items-center justify-center rounded hover:bg-slate-100" aria-label="Actions">
          <span className="text-xl leading-none">⋯</span>
        </summary>
        <div className="absolute right-0 z-50 mt-1 w-32 overflow-hidden rounded-md border bg-white py-1 text-sm shadow-md">
          <Link className="block px-3 py-1.5 hover:bg-slate-50" to={`/books/${id}/edit`}>Edit</Link>
          <button className="block w-full px-3 py-1.5 text-left text-red-600 hover:bg-red-50" onClick={onDelete}>Delete</button>
        </div>
      </details>
    </div>
  );
}

