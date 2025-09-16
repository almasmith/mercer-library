import { BookTable } from "@/features/books/components/book-table";
import { useFavorites } from "@/features/favorites/hooks/use-favorites";

export default function FavoritesPage() {
  return (
    <section className="space-y-4">
      <h1 className="text-xl font-semibold">Favorites</h1>
      <BookTable useData={(params: any) => useFavorites(params)} />
    </section>
  );
}


