import { type ReactNode } from "react";

/**
 * Lightweight theme provider placeholder. Extend later with system/OS theme syncing.
 */
export function ThemeProvider({ children }: { children: ReactNode }) {
  return <>{children}</>;
}
