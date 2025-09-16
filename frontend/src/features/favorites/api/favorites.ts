import { httpJson } from "@/lib/http";
import { z } from "zod";
import { bookSchema, pagedSchema } from "@/features/books/types/book";

export const favoritesListParamsSchema = z.object({
  search: z.string().optional(),
  genre: z.string().optional(),
  sortBy: z.enum(["title","author","genre","publishedDate","rating","createdAt"]).optional(),
  sortOrder: z.enum(["asc","desc"]).optional(),
  page: z.coerce.number().int().min(1).default(1),
  pageSize: z.coerce.number().int().positive().default(20),
});

export type FavoritesListParams = z.infer<typeof favoritesListParamsSchema>;

function toQuery(params: FavoritesListParams): string {
  const usp = new URLSearchParams();
  Object.entries(params).forEach(([k,v]) => {
    if (v === undefined || v === null || v === "") return;
    usp.append(k, String(v));
  });
  const q = usp.toString();
  return q ? `?${q}` : "";
}

export async function listFavorites(params: FavoritesListParams) {
  const res = await httpJson<unknown>(`/api/favorites${toQuery(params)}`);
  return pagedSchema(bookSchema).parse(res);
}

export async function favoriteBook(id: string): Promise<void> {
  await httpJson<void>(`/api/books/${id}/favorite`, { method: "POST" });
}

export async function unfavoriteBook(id: string): Promise<void> {
  await httpJson<void>(`/api/books/${id}/favorite`, { method: "DELETE" });
}


