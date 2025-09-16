import { useState } from "react";
import { useFavorite, useUnfavorite } from "@/features/favorites/hooks/use-favorites";

export function FavoriteToggle({
  bookId,
  initialOn = false,
  listParams,
  onChange,
}: {
  bookId: string;
  initialOn?: boolean;
  listParams?: Parameters<typeof useFavorite>[1];
  onChange?: (isOn: boolean) => void;
}) {
  const [isOn, setIsOn] = useState<boolean>(initialOn);
  const fav = useFavorite(bookId, listParams);
  const unfav = useUnfavorite(bookId, listParams);

  const toggle = async () => {
    const next = !isOn;
    setIsOn(next);
    onChange?.(next);
    try {
      if (next) await fav.mutateAsync();
      else await unfav.mutateAsync();
    } catch {
      setIsOn(!next);
      onChange?.(!next);
      alert("Failed to update favorite");
    }
  };

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
