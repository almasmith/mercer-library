import { useState } from "react";
import { useFavorite, useUnfavorite } from "@/features/favorites/hooks/use-favorites";
import { useIsFavorited } from "@/features/favorites/hooks/use-is-favorited";

export function FavoriteToggle({
  bookId,
  listParams,
  onChange,
}: {
  bookId: string;
  listParams?: Parameters<typeof useFavorite>[1];
  onChange?: (isOn: boolean) => void;
}) {
  const derivedIsOn = useIsFavorited(bookId);
  const [pending, setPending] = useState<boolean | null>(null);
  const fav = useFavorite(bookId, listParams);
  const unfav = useUnfavorite(bookId, listParams);

  const toggle = async () => {
    const next = !derivedIsOn;
    setPending(next);
    onChange?.(next);
    try {
      if (next) await fav.mutateAsync();
      else await unfav.mutateAsync();
    } catch {
      onChange?.(!next);
    } finally {
      setPending(null);
    }
  };

  const isOn = pending ?? derivedIsOn;

  return (
    <button
      type="button"
      aria-pressed={isOn}
      onClick={toggle}
      className={`text-lg ${isOn ? "text-yellow-500" : "text-slate-300"} hover:opacity-80`}
      title={isOn ? "Unfavorite" : "Favorite"}
    >
      {isOn ? "★" : "☆"}
    </button>
  );
}
