export type DateInput = string | number | Date | undefined | null;

function toDate(value: DateInput): Date | null {
  if (value === undefined || value === null) return null;
  const d = typeof value === "string" || typeof value === "number" ? new Date(value) : value;
  return isNaN(d.getTime()) ? null : d;
}

/**
 * Format a value as a localized date like "20 Nov 2022".
 */
export function formatDate(value: DateInput, options?: Intl.DateTimeFormatOptions): string {
  const d = toDate(value);
  if (!d) return "-";
  const fmt: Intl.DateTimeFormatOptions = {
    day: "2-digit",
    month: "short",
    year: "numeric",
    ...options,
  };
  return new Intl.DateTimeFormat(undefined, fmt).format(d);
}

/**
 * Format a value as a localized date and time like "20 Nov 2022, 14:30".
 */
export function formatDateTime(value: DateInput, options?: Intl.DateTimeFormatOptions): string {
  const d = toDate(value);
  if (!d) return "-";
  const fmt: Intl.DateTimeFormatOptions = {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    ...options,
  };
  return new Intl.DateTimeFormat(undefined, fmt).format(d);
}


