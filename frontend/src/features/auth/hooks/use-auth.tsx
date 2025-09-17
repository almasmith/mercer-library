import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { setAccessTokenProvider } from "@/lib/http";
import { connectRealtime, disconnectRealtime, setRealtimeAccessTokenProvider } from "@/lib/realtime";
import type { AuthResponse } from "../types/auth";

type AuthState = {
  accessToken?: string;
  expiresAt?: number; // epoch seconds
};

const STORAGE_KEY = "auth";

function readStorage(): AuthState {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as AuthState) : {};
  } catch {
    return {};
  }
}

function writeStorage(state: AuthState) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
  } catch {
    // ignore storage errors
  }
}

const AuthContext = createContext<{
  accessToken?: string;
  expiresAt?: number;
  isAuthenticated: boolean;
  setAuth: (auth: AuthResponse) => void;
  logout: () => void;
} | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>(() => readStorage());

  const isAuthenticated = !!state.accessToken && (!state.expiresAt || state.expiresAt * 1000 > Date.now());

  const setAuth = useCallback((auth: AuthResponse) => {
    const next: AuthState = {
      accessToken: auth.accessToken,
      expiresAt: Math.floor(Date.now() / 1000) + Number(auth.expiresIn || 0),
    };
    setState(next);
    writeStorage(next);
  }, []);

  const logout = useCallback(() => {
    setState({});
    writeStorage({});
  }, []);

  // Cross-tab logout/login sync
  useEffect(() => {
    const onStorage = (e: StorageEvent) => {
      if (e.key === STORAGE_KEY) setState(readStorage());
    };
    window.addEventListener("storage", onStorage);
    return () => window.removeEventListener("storage", onStorage);
  }, []);

  // Global unauthorized handling
  useEffect(() => {
    type UnauthorizedDetail = { returnTo?: string };
    const onUnauthorized = (e: CustomEvent<UnauthorizedDetail>) => {
      const detail = e.detail;
      logout();
      const returnTo = detail?.returnTo || window.location.pathname + window.location.search;
      window.location.assign(`/login?returnTo=${encodeURIComponent(returnTo)}`);
    };
    window.addEventListener("auth:unauthorized", onUnauthorized as EventListener);
    return () => window.removeEventListener("auth:unauthorized", onUnauthorized as EventListener);
  }, [logout]);

  // Realtime lifecycle: bridge token and connect/disconnect on changes
  useEffect(() => {
    setRealtimeAccessTokenProvider(() => state.accessToken);
    if (state.accessToken) {
      connectRealtime().catch(() => { /* ignore; will retry via reconnect */ });
    } else {
      void disconnectRealtime();
    }
  }, [state.accessToken]);

  const value = useMemo(
    () => ({ ...state, isAuthenticated, setAuth, logout }),
    [state, isAuthenticated, setAuth, logout],
  );

  // Bridge the access token to the HTTP layer (idempotent assignment).
  setAccessTokenProvider(() => state.accessToken);
  setRealtimeAccessTokenProvider(() => state.accessToken);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within <AuthProvider>");
  return ctx;
}


