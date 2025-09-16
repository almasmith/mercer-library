import { z } from "zod";

export const RATING_MIN = 1;
export const RATING_MAX = 5;
export const TITLE_MAX = 200;
export const AUTHOR_MAX = 200;
export const GENRE_MAX = 100;

const trimNonEmpty = (max: number) =>
  z
    .string()
    .transform(s => s.trim())
    .pipe(z.string().min(1, "Required").max(max));

export const isoDateSchema = z
  .string()
  .regex(/^\d{4}-\d{2}-\d{2}(T\d{2}:\d{2}:\d{2}Z)?$/, "Invalid date")
  .transform(s => (s.length === 10 ? `${s}T00:00:00Z` : s));

export const bookSchema = z.object({
  id: z.string().uuid(),
  title: z.string().max(TITLE_MAX),
  author: z.string().max(AUTHOR_MAX),
  genre: z.string().max(GENRE_MAX).optional().nullable(),
  publishedDate: isoDateSchema,
  rating: z.number().int().min(RATING_MIN).max(RATING_MAX),
  createdAt: z.string(),
  updatedAt: z.string(),
});

export const createBookSchema = z.object({
  title: trimNonEmpty(TITLE_MAX),
  author: trimNonEmpty(AUTHOR_MAX),
  genre: trimNonEmpty(GENRE_MAX),
  publishedDate: isoDateSchema,
  rating: z.coerce.number().int().min(RATING_MIN).max(RATING_MAX),
});

export const updateBookSchema = createBookSchema.partial();

export const pagedSchema = <T extends z.ZodTypeAny>(item: T) =>
  z.object({
    items: z.array(item),
    page: z.number().int().min(1),
    pageSize: z.number().int().positive(),
    totalItems: z.number().int().nonnegative(),
    totalPages: z.number().int().nonnegative(),
  });

export const pagedBooksSchema = pagedSchema(bookSchema);

export type Book = z.infer<typeof bookSchema>;
export type CreateBookInput = z.infer<typeof createBookSchema>;
export type UpdateBookInput = z.infer<typeof updateBookSchema>;
export type PagedBooks = z.infer<typeof pagedBooksSchema>;
export type BookStats = Array<{ genre: string; count: number }>;


