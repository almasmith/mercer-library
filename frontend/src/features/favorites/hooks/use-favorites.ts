import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { favoriteBook, listFavorites, unfavoriteBook, type FavoritesListParams } from "@/features/favorites/api/favorites";
import { booksKeys } from "@/features/books/hooks/use-books";

export const favoritesKeys = {
  all: ["favorites"] as const,
  list: (params: FavoritesListParams) => [...favoritesKeys.all, "list", params] as const,
  index: () => [...favoritesKeys.all, "index"] as const,
};

export function useFavorites(params: FavoritesListParams) {
  return useQuery({
    queryKey: favoritesKeys.list(params),
    queryFn: () => listFavorites(params),
    staleTime: 30_000,
  });
}

export function useFavorite(id: string, listParams?: FavoritesListParams) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => favoriteBook(id),
    onMutate: async () => {
      const prev = qc.getQueryData<Set<string>>(favoritesKeys.index());
      const next = new Set<string>(prev ? Array.from(prev) : []);
      next.add(id);
      qc.setQueryData(favoritesKeys.index(), next);
      return { prevIndex: prev } as const;
    },
    onError: (_err, _vars, ctx) => {
      if (ctx?.prevIndex) qc.setQueryData(favoritesKeys.index(), ctx.prevIndex);
      else qc.removeQueries({ queryKey: favoritesKeys.index() });
    },
    onSuccess: () => {
      // Update membership index for immediate cache-derived reads
      qc.setQueryData<Set<string>>(favoritesKeys.index(), (prev) => {
        const next = new Set<string>(prev ? Array.from(prev) : []);
        next.add(id);
        return next;
      });
      qc.invalidateQueries({ queryKey: favoritesKeys.all });
      qc.invalidateQueries({ queryKey: booksKeys.all });
      if (listParams) qc.invalidateQueries({ queryKey: favoritesKeys.list(listParams) });
    },
  });
}

export function useUnfavorite(id: string, listParams?: FavoritesListParams) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => unfavoriteBook(id),
    onMutate: async () => {
      const prev = qc.getQueryData<Set<string>>(favoritesKeys.index());
      const next = new Set<string>(prev ? Array.from(prev) : []);
      next.delete(id);
      qc.setQueryData(favoritesKeys.index(), next);
      return { prevIndex: prev } as const;
    },
    onError: (_err, _vars, ctx) => {
      if (ctx?.prevIndex) qc.setQueryData(favoritesKeys.index(), ctx.prevIndex);
      else qc.removeQueries({ queryKey: favoritesKeys.index() });
    },
    onSuccess: () => {
      // Update membership index for immediate cache-derived reads
      qc.setQueryData<Set<string>>(favoritesKeys.index(), (prev) => {
        const next = new Set<string>(prev ? Array.from(prev) : []);
        next.delete(id);
        return next;
      });
      qc.invalidateQueries({ queryKey: favoritesKeys.all });
      qc.invalidateQueries({ queryKey: booksKeys.all });
      if (listParams) qc.invalidateQueries({ queryKey: favoritesKeys.list(listParams) });
    },
  });
}


