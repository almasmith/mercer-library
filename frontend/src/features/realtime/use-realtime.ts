import { useEffect } from "react";
import { onEvent, connectRealtime } from "@/lib/realtime";
import { useQueryClient } from "@tanstack/react-query";
import { booksKeys } from "@/features/books/hooks/use-books";

// Optional imports; guard existence at runtime when not present
// eslint-disable-next-line @typescript-eslint/no-explicit-any
let favoritesKeys: any, analyticsKeys: any;
// Declare require for TS since app tsconfig doesn't include Node types
// eslint-disable-next-line @typescript-eslint/no-explicit-any
declare const require: any;
try {
  ({ favoritesKeys } = require("@/features/favorites/hooks/use-favorites"));
} catch {}
try {
  ({ analyticsKeys } = require("@/features/analytics/hooks/use-analytics"));
} catch {}

export function useRealtime() {
  const qc = useQueryClient();

  useEffect(() => {
    // Ensure connection (no-op if already connected)
    connectRealtime().catch(() => {
      /* connection handled by F29 lifecycle */
    });

    const unsubs: Array<() => void> = [];

    // Books CRUD
    unsubs.push(
      onEvent("bookCreated", (_payload: unknown) => {
        // Best-effort: invalidate lists; detail will be fetched when visited
        qc.invalidateQueries({ queryKey: booksKeys.all });
      }),
    );
    unsubs.push(
      onEvent("bookUpdated", (_payload: unknown) => {
        qc.invalidateQueries({ queryKey: booksKeys.all });
      }),
    );
    unsubs.push(
      onEvent("bookDeleted", (_payload: { id: string }) => {
        qc.invalidateQueries({ queryKey: booksKeys.all });
      }),
    );

    // Favorites
    unsubs.push(
      onEvent("bookFavorited", () => {
        qc.invalidateQueries({ queryKey: booksKeys.all });
        if (favoritesKeys) qc.invalidateQueries({ queryKey: favoritesKeys.all });
      }),
    );
    unsubs.push(
      onEvent("bookUnfavorited", () => {
        qc.invalidateQueries({ queryKey: booksKeys.all });
        if (favoritesKeys) qc.invalidateQueries({ queryKey: favoritesKeys.all });
      }),
    );

    // Reads / Stats / Analytics
    unsubs.push(
      onEvent("bookRead", () => {
        if (analyticsKeys) qc.invalidateQueries({ queryKey: analyticsKeys.all });
      }),
    );
    unsubs.push(
      onEvent("statsUpdated", () => {
        qc.invalidateQueries({ queryKey: booksKeys.stats() });
        if (analyticsKeys) qc.invalidateQueries({ queryKey: analyticsKeys.all });
      }),
    );

    return () => {
      unsubs.forEach((u) => u());
    };
  }, [qc]);
}


