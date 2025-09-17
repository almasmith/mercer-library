import { BookTable } from "@/features/books/components/book-table";
import { useFavorites } from "@/features/favorites/hooks/use-favorites";
import type { FavoritesListParams } from "@/features/favorites/api/favorites";
import type { ListParams } from "@/features/books/api/books";

export default function FavoritesPage() {
  return (
    <section className="space-y-4">
      <div className="toolbar card p-3">
        <h1 className="text-xl font-semibold">Favorites</h1>
        <p className="text-sm text-slate-500">Your saved books</p>
      </div>
      {/* bridge hook shape to BookTable without violating hook rules */}
      <div className="card p-3">
        <FavoritesTable />
      </div>
    </section>
  );
}

function FavoritesTable() {
  function useFavoritesForBookTable(p: ListParams) {
    const params: FavoritesListParams = {
      search: p.search,
      genre: p.genre,
      sortBy: p.sortBy as FavoritesListParams["sortBy"],
      sortOrder: p.sortOrder as FavoritesListParams["sortOrder"],
      page: p.page,
      pageSize: p.pageSize,
    };
    return useFavorites(params);
  }
  return <BookTable useData={useFavoritesForBookTable} />;
}


