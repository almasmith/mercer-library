import { type ReactNode, useEffect } from "react";

/**
 * Ensures a #toast-root portal exists for toasts. Replace with a real toast system later.
 */
export function ToastProvider({ children }: { children: ReactNode }) {
  useEffect(() => {
    let root = document.getElementById("toast-root");
    if (!root) {
      root = document.createElement("div");
      root.id = "toast-root";
      document.body.appendChild(root);
    }
  }, []);
  return <>{children}</>;
}
