import { httpJson } from "@/lib/http";
import type { AvgRatingPoint, MostReadGenre, AvgRatingParams, RangeParams } from "../types/analytics";

function toQuery(params: Record<string, unknown>) {
  const usp = new URLSearchParams();
  Object.entries(params).forEach(([k, v]) => {
    if (v === undefined || v === null || v === "") return;
    usp.append(k, String(v));
  });
  const q = usp.toString();
  return q ? `?${q}` : "";
}

export async function recordRead(bookId: string): Promise<void> {
  await httpJson<void>(`/api/books/${bookId}/read`, { method: "POST" });
}

export async function getAvgRating(params: AvgRatingParams = { bucket: "month" }): Promise<AvgRatingPoint[]> {
  const query = toQuery({ bucket: params.bucket ?? "month", from: params.from, to: params.to });
  return await httpJson<AvgRatingPoint[]>(`/api/analytics/avg-rating${query}`);
}

export async function getMostReadGenres(params: RangeParams = {}): Promise<MostReadGenre[]> {
  const query = toQuery({ from: params.from, to: params.to });
  return await httpJson<MostReadGenre[]>(`/api/analytics/most-read-genres${query}`);
}
