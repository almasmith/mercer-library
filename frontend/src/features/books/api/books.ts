import { httpJson } from "@/lib/http";
import { type Book, type CreateBookInput, type UpdateBookInput, pagedSchema, bookSchema, type BookStats } from "@/features/books/types/book";
import { z } from "zod";

export const listParamsSchema = z.object({
  genre: z.string().optional(),
  minRating: z.coerce.number().int().optional(),
  maxRating: z.coerce.number().int().optional(),
  publishedFrom: z.string().optional(),
  publishedTo: z.string().optional(),
  search: z.string().optional(),
  sortBy: z.enum(["title","author","genre","publishedDate","rating","createdAt"]).optional(),
  sortOrder: z.enum(["asc","desc"]).optional(),
  page: z.coerce.number().int().min(1).default(1),
  pageSize: z.coerce.number().int().positive().default(20),
});

export type ListParams = z.infer<typeof listParamsSchema>;

function toQuery(params: ListParams): string {
  const p = { ...params };
  const usp = new URLSearchParams();
  Object.entries(p).forEach(([k, v]) => {
    if (v === undefined || v === null || v === "") return;
    usp.append(k, String(v));
  });
  const qs = usp.toString();
  return qs ? `?${qs}` : "";
}

export async function listBooks(params: ListParams) {
  const res = await httpJson<unknown>(`/api/books${toQuery(params)}`);
  return pagedSchema(bookSchema).parse(res);
}

export async function getBook(id: string) {
  const res = await httpJson<unknown>(`/api/books/${id}`);
  return bookSchema.parse(res) as Book;
}

export async function createBook(input: CreateBookInput) {
  const res = await httpJson<unknown>("/api/books", {
    method: "POST",
    body: JSON.stringify(input),
  });
  return bookSchema.parse(res) as Book;
}

export async function updateBook(id: string, input: UpdateBookInput, ifMatch?: string) {
  const res = await httpJson<unknown>(`/api/books/${id}`, {
    method: "PUT",
    body: JSON.stringify(input),
    ifMatch,
  });
  return bookSchema.parse(res) as Book;
}

export async function deleteBook(id: string) {
  await httpJson<void>(`/api/books/${id}`, { method: "DELETE" });
}

export async function getBookStats() {
  return (await httpJson<BookStats>("/api/books/stats")) satisfies BookStats;
}


