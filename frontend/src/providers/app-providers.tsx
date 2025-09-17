import { type ReactNode } from "react";
import { BrowserRouter } from "react-router-dom";
import { QueryClientProvider } from "@tanstack/react-query";
import { AuthProvider } from "@/features/auth/hooks/use-auth";
import { queryClient } from "../lib/query-client";
import { ThemeProvider } from "./theme-provider";
import { ToastProvider } from "./toast-provider";
import { RealtimeSubscriptions } from "@/features/realtime/realtime-subscriptions";
import { useFavoritesIndex } from "@/features/favorites/hooks/use-favorites-index";

export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider>
        <ToastProvider>
          <AuthProvider>
            <BrowserRouter>
              <RealtimeSubscriptions />
              {/* Seed favorites index so stars render from cache immediately */}
              <FavoritesIndexLoader />
              {children}
            </BrowserRouter>
          </AuthProvider>
        </ToastProvider>
      </ThemeProvider>
    </QueryClientProvider>
  );
}

function FavoritesIndexLoader() {
  useFavoritesIndex();
  return null;
}
