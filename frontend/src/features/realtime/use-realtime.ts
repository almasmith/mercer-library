import { useEffect } from "react";
import { onEvent } from "@/lib/realtime";
import type { RealtimeEvents } from "./events";
import { useQueryClient } from "@tanstack/react-query";
import { booksKeys } from "@/features/books/hooks/use-books";

// Optional imports; guard existence at runtime when not present
type FavoritesKeys = typeof import("@/features/favorites/hooks/use-favorites").favoritesKeys;
type AnalyticsKeys = typeof import("@/features/analytics/hooks/use-analytics").analyticsKeys;
let favoritesKeys: FavoritesKeys | undefined;
let analyticsKeys: AnalyticsKeys | undefined;
// Declare require for TS since app tsconfig doesn't include Node types
declare const require: (id: string) => unknown;
function isFavoritesModule(mod: unknown): mod is { favoritesKeys: FavoritesKeys } {
  return !!mod && typeof mod === "object" && "favoritesKeys" in (mod as Record<string, unknown>);
}
function isAnalyticsModule(mod: unknown): mod is { analyticsKeys: AnalyticsKeys } {
  return !!mod && typeof mod === "object" && "analyticsKeys" in (mod as Record<string, unknown>);
}
try {
  const mod = require("@/features/favorites/hooks/use-favorites");
  if (isFavoritesModule(mod)) favoritesKeys = mod.favoritesKeys;
} catch {
  void 0; // optional module not present
}
try {
  const mod = require("@/features/analytics/hooks/use-analytics");
  if (isAnalyticsModule(mod)) analyticsKeys = mod.analyticsKeys;
} catch {
  void 0; // optional module not present
}

function onTyped<T extends keyof RealtimeEvents>(event: T, handler: (payload: RealtimeEvents[T]) => void) {
  return onEvent<[RealtimeEvents[T]]>(event as string, (payload) => handler(payload));
}

export function useRealtime() {
  const qc = useQueryClient();

  useEffect(() => {
    const unsubs: Array<() => void> = [];

    // Books CRUD
    unsubs.push(
      onTyped("bookCreated", () => {
        // Best-effort: invalidate lists; detail will be fetched when visited
        qc.invalidateQueries({ queryKey: booksKeys.all });
      }),
    );
    unsubs.push(
      onTyped("bookUpdated", () => {
        qc.invalidateQueries({ queryKey: booksKeys.all });
      }),
    );
    unsubs.push(
      onTyped("bookDeleted", () => {
        qc.invalidateQueries({ queryKey: booksKeys.all });
      }),
    );

    // Favorites
    unsubs.push(
      onTyped("bookFavorited", () => {
        qc.invalidateQueries({ queryKey: booksKeys.all });
        if (favoritesKeys) qc.invalidateQueries({ queryKey: favoritesKeys.all });
      }),
    );
    unsubs.push(
      onTyped("bookUnfavorited", () => {
        qc.invalidateQueries({ queryKey: booksKeys.all });
        if (favoritesKeys) qc.invalidateQueries({ queryKey: favoritesKeys.all });
      }),
    );

    // Reads / Stats / Analytics
    unsubs.push(
      onTyped("bookRead", () => {
        if (analyticsKeys) qc.invalidateQueries({ queryKey: analyticsKeys.all });
      }),
    );
    unsubs.push(
      onTyped("statsUpdated", () => {
        qc.invalidateQueries({ queryKey: booksKeys.stats() });
        if (analyticsKeys) qc.invalidateQueries({ queryKey: analyticsKeys.all });
      }),
    );

    return () => {
      unsubs.forEach((u) => u());
    };
  }, [qc]);
}


