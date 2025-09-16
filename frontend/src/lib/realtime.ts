import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { env } from "@/lib/env";

export type RealtimeStatus = "disconnected" | "connecting" | "connected" | "reconnecting";

let connection: HubConnection | null = null;
let status: RealtimeStatus = "disconnected";
const subscribers = new Set<(s: RealtimeStatus) => void>();
let tokenProvider: (() => string | undefined) | undefined;

function notify(next: RealtimeStatus) {
  status = next;
  subscribers.forEach((fn) => fn(status));
}

function hubUrl(): string {
  const base = new URL(env.apiBaseUrl);
  base.pathname = (base.pathname.replace(/\/+$/, "") + "/hubs/library").replace(/\/+/, "/");
  return base.toString();
}

export function setRealtimeAccessTokenProvider(provider: () => string | undefined) {
  tokenProvider = provider;
}

export function onRealtimeStatus(fn: (s: RealtimeStatus) => void) {
  subscribers.add(fn);
  fn(status);
  return () => subscribers.delete(fn);
}

export function getRealtimeStatus(): RealtimeStatus {
  return status;
}

export async function connectRealtime(): Promise<HubConnection> {
  const token = tokenProvider?.();
  if (!token) {
    await disconnectRealtime();
    return Promise.reject(new Error("Missing access token for realtime connection"));
  }
  if (connection) return connection;

  notify("connecting");
  const conn = new HubConnectionBuilder()
    .withUrl(hubUrl(), { accessTokenFactory: () => tokenProvider?.() ?? "" })
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: (ctx) => Math.min(30_000, 1_000 * 2 ** ctx.previousRetryCount),
    })
    .configureLogging(LogLevel.Warning)
    .build();

  conn.onreconnecting(() => notify("reconnecting"));
  conn.onreconnected(() => notify("connected"));
  conn.onclose(() => {
    connection = null;
    notify("disconnected");
  });

  await conn.start();
  connection = conn;
  notify("connected");
  return conn;
}

export async function disconnectRealtime(): Promise<void> {
  if (!connection) {
    notify("disconnected");
    return;
  }
  try {
    await connection.stop();
  } finally {
    connection = null;
    notify("disconnected");
  }
}

export function onEvent<TArgs extends unknown[]>(event: string, handler: (...args: TArgs) => void) {
  if (!connection) return () => {};
  connection.on(event, handler as (...args: unknown[]) => void);
  return () => {
    try { connection?.off(event, handler as (...args: unknown[]) => void); } catch { /* ignore */ }
  };
}


