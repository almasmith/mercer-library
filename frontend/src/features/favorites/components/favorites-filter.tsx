import { useSearchParams } from "react-router-dom";

export function FavoritesFilter() {
  const [params, setParams] = useSearchParams();
  const isOn = params.get("favorites") === "true";
  return (
    <label className="inline-flex cursor-pointer items-center gap-2 text-sm">
      <input
        type="checkbox"
        checked={isOn}
        onChange={(e) => {
          const next = new URLSearchParams(params);
          if (e.target.checked) next.set("favorites", "true");
          else next.delete("favorites");
          next.delete("page");
          setParams(next, { replace: true });
        }}
      />
      Favorites only
    </label>
  );
}
