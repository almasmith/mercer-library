import { QueryClient } from "@tanstack/react-query";
import { favoritesKeys } from "@/features/favorites/hooks/use-favorites";
import type { Book } from "@/features/books/types/book";

export function collectFavoritedIds(queryClient: QueryClient): Set<string> {
  const ids = new Set<string>();
  const index = queryClient.getQueryData<Set<string>>(favoritesKeys.index());
  if (index) index.forEach((id) => ids.add(id));

  const entries = queryClient.getQueriesData<{ items?: Array<Pick<Book, "id">> }>({ queryKey: favoritesKeys.all });
  for (const [, data] of entries) {
    // Favorites lists
    const items = data?.items;
    if (Array.isArray(items)) {
      items.forEach((b) => ids.add(b.id));
    }
  }
  return ids;
}


