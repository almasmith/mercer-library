import { useEffect, useState } from "react";
import { getRealtimeStatus, onRealtimeStatus, type RealtimeStatus } from "@/lib/realtime";

export function useRealtimeStatus() {
  const [status, setStatus] = useState<RealtimeStatus>(getRealtimeStatus());
  useEffect(() => onRealtimeStatus(setStatus), []);
  return status;
}


