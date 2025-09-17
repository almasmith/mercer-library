import { useEffect, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { collectFavoritedIds } from "@/features/favorites/lib/favorites-index";
import { favoritesKeys } from "./use-favorites";

export function useIsFavorited(bookId: string): boolean {
  const qc = useQueryClient();
  const [isFav, setIsFav] = useState<boolean>(() => collectFavoritedIds(qc).has(bookId));
  useEffect(() => {
    setIsFav(collectFavoritedIds(qc).has(bookId));
    const unsub = qc.getQueryCache().subscribe((event) => {
      // React to any query event touching favorites (added, updated, removed)
      const key = event.query?.queryKey;
      if (!Array.isArray(key)) return;
      if ((key[0] as unknown) === favoritesKeys.all[0]) {
        setIsFav(collectFavoritedIds(qc).has(bookId));
      }
    });
    return () => { unsub?.(); };
  }, [qc, bookId]);
  return isFav;
}


