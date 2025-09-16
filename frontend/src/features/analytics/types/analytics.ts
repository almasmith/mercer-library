export type AvgRatingPoint = { bucket: string; average: number };
export type MostReadGenre = { genre: string; readCount: number };
export type RangeParams = { from?: string; to?: string };
export type AvgRatingParams = RangeParams & { bucket?: "month" };
