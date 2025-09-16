import { useRealtimeStatus } from "./use-realtime-status";

const COLOR: Record<string, string> = {
  connected: "bg-emerald-500",
  connecting: "bg-amber-500",
  reconnecting: "bg-amber-500",
  disconnected: "bg-slate-400",
};

const LABEL: Record<string, string> = {
  connected: "Live",
  connecting: "Connecting",
  reconnecting: "Reconnecting",
  disconnected: "Offline",
};

export function RealtimeBadge() {
  const status = useRealtimeStatus();
  return (
    <span className="inline-flex items-center gap-2 text-xs text-slate-600">
      <span className={`h-2 w-2 rounded-full ${COLOR[status]}`} aria-hidden="true" />
      <span aria-live="polite">{LABEL[status]}</span>
    </span>
  );
}


