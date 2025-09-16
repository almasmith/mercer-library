import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { getAvgRating, getMostReadGenres, recordRead } from "@/features/analytics/api/analytics";
import type { AvgRatingParams, RangeParams } from "@/features/analytics/types/analytics";

export const analyticsKeys = {
  all: ["analytics"] as const,
  avgRating: (p: AvgRatingParams) => [...analyticsKeys.all, "avgRating", p] as const,
  mostReadGenres: (p: RangeParams) => [...analyticsKeys.all, "mostReadGenres", p] as const,
};

export function useAvgRating(params: AvgRatingParams) {
  return useQuery({
    queryKey: analyticsKeys.avgRating(params),
    queryFn: () => getAvgRating(params),
    staleTime: 60_000,
  });
}

export function useMostReadGenres(params: RangeParams) {
  return useQuery({
    queryKey: analyticsKeys.mostReadGenres(params),
    queryFn: () => getMostReadGenres(params),
    staleTime: 60_000,
  });
}

export function useRecordRead() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (bookId: string) => recordRead(bookId),
    onSuccess: () => {
      // Refresh analytics on reads
      qc.invalidateQueries({ queryKey: analyticsKeys.all });
    },
  });
}
