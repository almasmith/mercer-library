import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { favoriteBook, listFavorites, unfavoriteBook, type FavoritesListParams } from "@/features/favorites/api/favorites";
import { booksKeys } from "@/features/books/hooks/use-books";

export const favoritesKeys = {
  all: ["favorites"] as const,
  list: (params: FavoritesListParams) => [...favoritesKeys.all, "list", params] as const,
};

export function useFavorites(params: FavoritesListParams) {
  return useQuery({
    queryKey: favoritesKeys.list(params),
    queryFn: () => listFavorites(params),
    staleTime: 30_000,
    keepPreviousData: true,
  });
}

export function useFavorite(id: string, listParams?: FavoritesListParams) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => favoriteBook(id),
    onSuccess: () => {
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
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: favoritesKeys.all });
      qc.invalidateQueries({ queryKey: booksKeys.all });
      if (listParams) qc.invalidateQueries({ queryKey: favoritesKeys.list(listParams) });
    },
  });
}


