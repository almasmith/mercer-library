import { useEffect } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { listFavorites, type FavoritesListParams } from "@/features/favorites/api/favorites";
import { favoritesKeys } from "./use-favorites";

export function useFavoritesIndex() {
  const qc = useQueryClient();
  const query = useQuery({
    queryKey: favoritesKeys.index(),
    queryFn: async () => {
      const pageSize = 200;
      let page = 1;
      const ids = new Set<string>();
      // Fetch all pages to build a complete membership set
      // Guard against infinite loops by relying on totalPages
      // eslint-disable-next-line no-constant-condition
      while (true) {
        const res = await listFavorites({ page, pageSize } as FavoritesListParams);
        res.items.forEach((b) => ids.add(b.id));
        if (page >= res.totalPages || res.items.length === 0) break;
        page += 1;
      }
      return ids;
    },
    staleTime: 30_000,
  });

  useEffect(() => {
    if (query.data) {
      qc.setQueryData(favoritesKeys.index(), query.data);
    }
  }, [qc, query.data]);
}


