import { useId } from "react";

export function DateInput({ value, onChange, placeholder = "mm/dd/yyyy", size = "sm" }: { value?: string; onChange: (v?: string) => void; placeholder?: string; size?: "sm" | "md" }) {
  const id = useId();
  const showPlaceholder = !value;
  return (
    <div className="relative leading-none">
      {showPlaceholder && (
        <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-400">{placeholder}</span>
      )}
      <input
        id={id}
        type="date"
        className={`${size === "sm" ? "input-sm" : "input"} pr-2 align-middle ${showPlaceholder ? "text-transparent" : ""}`}
        value={value || ""}
        onChange={(e) => onChange(e.target.value || undefined)}
      />
    </div>
  );
}


