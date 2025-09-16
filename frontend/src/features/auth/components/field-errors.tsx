export function mapProblemDetailsErrors(errors?: Record<string, string[]>) {
  const entries = Object.entries(errors ?? {});
  return Object.fromEntries(entries.map(([k, v]) => [k, v?.[0] ?? "Invalid value"]));
}


