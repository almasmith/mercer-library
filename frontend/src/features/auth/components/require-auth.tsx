import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "@/features/auth/hooks/use-auth";
import type { ReactElement } from "react";

export default function RequireAuth({ children }: { children: ReactElement }) {
  const { isAuthenticated } = useAuth();
  const location = useLocation();

  if (!isAuthenticated) {
    const returnTo = encodeURIComponent(location.pathname + location.search);
    return <Navigate to={`/login?returnTo=${returnTo}`} replace />;
  }
  return children;
}


