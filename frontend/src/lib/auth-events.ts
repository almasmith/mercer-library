export function dispatchUnauthorized(returnTo: string) {
  window.dispatchEvent(new CustomEvent("auth:unauthorized", { detail: { returnTo } }));
}
