import { useQuery } from "@tanstack/react-query";
import { getBookStats } from "@/features/stats/api/stats";

export const statsKeys = {
  all: ["stats"] as const,
  books: () => [...statsKeys.all, "books"] as const,
};

export function useBookStats() {
  return useQuery({
    queryKey: statsKeys.books(),
    queryFn: getBookStats,
    staleTime: 60_000,
  });
}


