import { QueryClient } from "@tanstack/react-query";
import { favoritesKeys } from "@/features/favorites/hooks/use-favorites";
import type { Book } from "@/features/books/types/book";

export function collectFavoritedIds(queryClient: QueryClient): Set<string> {
  const ids = new Set<string>();
  const entries = queryClient.getQueriesData<{ items: Book[] }>({ queryKey: favoritesKeys.all });
  for (const [, data] of entries) {
    if (!data?.items) continue;
    data.items.forEach(b => ids.add(b.id));
  }
  return ids;
}


