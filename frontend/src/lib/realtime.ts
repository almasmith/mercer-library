import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { env } from "@/lib/env";

export type RealtimeStatus = "disconnected" | "connecting" | "connected" | "reconnecting";

let connection: HubConnection | null = null;
let status: RealtimeStatus = "disconnected";
const subscribers = new Set<(s: RealtimeStatus) => void>();
let tokenProvider: (() => string | undefined) | undefined;
// Keep track of desired event handlers to attach when a connection exists
const eventHandlers = new Map<string, Set<(...args: unknown[]) => void>>();

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

function attachAllHandlers(conn: HubConnection) {
  for (const [event, handlers] of eventHandlers) {
    for (const handler of handlers) {
      conn.on(event, handler);
    }
  }
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
    .withUrl(hubUrl(), { accessTokenFactory: () => tokenProvider?.() ?? "", withCredentials: false })
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

  // Ensure any previously registered handlers are wired up on this connection
  attachAllHandlers(conn);

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
  const typed = handler as (...args: unknown[]) => void;
  let set = eventHandlers.get(event);
  if (!set) {
    set = new Set();
    eventHandlers.set(event, set);
  }
  set.add(typed);
  if (connection) {
    connection.on(event, typed);
  }
  return () => {
    const s = eventHandlers.get(event);
    if (s) {
      s.delete(typed);
      if (s.size === 0) eventHandlers.delete(event);
    }
    try { connection?.off(event, typed); } catch { /* ignore */ }
  };
}


