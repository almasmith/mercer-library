import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  listBooks,
  getBook,
  createBook,
  updateBook,
  deleteBook,
  getBookStats,
  type ListParams,
} from "@/features/books/api/books";
import type {
  Book,
  CreateBookInput,
  UpdateBookInput,
} from "@/features/books/types/book";

export const booksKeys = {
  all: ["books"] as const,
  list: (params: ListParams) => [...booksKeys.all, "list", params] as const,
  detail: (id: string) => [...booksKeys.all, "detail", id] as const,
  stats: () => [...booksKeys.all, "stats"] as const,
};

export function useBooks(params: ListParams) {
  return useQuery({
    queryKey: booksKeys.list(params),
    queryFn: () => listBooks(params),
    staleTime: 30_000,
    keepPreviousData: true,
  });
}

export function useBook(id: string) {
  return useQuery({
    queryKey: booksKeys.detail(id),
    queryFn: () => getBook(id),
    enabled: !!id,
    staleTime: 30_000,
  });
}

export function useCreateBook() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateBookInput) => createBook(input),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: booksKeys.all });
    },
  });
}

export function useUpdateBook(id: string, ifMatch?: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateBookInput) => updateBook(id, input, ifMatch),
    onSuccess: (updated: Book) => {
      qc.setQueryData(booksKeys.detail(id), updated);
      qc.invalidateQueries({ queryKey: booksKeys.list as unknown as string[] });
    },
  });
}

export function useDeleteBook() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteBook(id),
    onSuccess: (_data, id) => {
      qc.invalidateQueries({ queryKey: booksKeys.all });
      qc.removeQueries({ queryKey: booksKeys.detail(id) });
    },
  });
}

export function useBookStats() {
  return useQuery({
    queryKey: booksKeys.stats(),
    queryFn: getBookStats,
    staleTime: 60_000,
  });
}


