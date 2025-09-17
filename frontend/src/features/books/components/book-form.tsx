import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { createBookSchema, type CreateBookInput, RATING_MIN, RATING_MAX } from "@/features/books/types/book";
import { HttpError } from "@/lib/http";

export type BookFormValues = CreateBookInput;

export function BookForm({
  defaultValues,
  onSubmit,
  submittingLabel = "Save",
}: {
  defaultValues?: Partial<BookFormValues>;
  onSubmit: (values: BookFormValues) => Promise<void> | void;
  submittingLabel?: string;
}) {
  const { register, handleSubmit, formState: { errors, isSubmitting }, setError } = useForm<BookFormValues>({
    resolver: zodResolver(createBookSchema),
    defaultValues,
    mode: "onBlur",
  });

  const submit = async (values: BookFormValues) => {
    try {
      const toIso = (d?: string) => (d && d.length === 10 ? `${d}T00:00:00Z` : d);
      await onSubmit({ ...values, publishedDate: toIso(values.publishedDate) });
    } catch (err: unknown) {
      const pd = err instanceof HttpError ? err.problem : undefined;
      if (pd?.errors) {
        Object.entries(pd.errors).forEach(([k, v]) => setError(k as keyof BookFormValues, { message: v?.[0] ?? "Invalid value" }));
      } else if (pd?.title) {
        setError("title" as keyof BookFormValues, { message: pd.title });
      }
    }
  };

  return (
    <form onSubmit={handleSubmit(submit)} className="space-y-3">
      <div>
        <label className="block text-sm">Title</label>
        <input className="mt-1 w-full rounded border px-3 py-2" {...register("title")} />
        {errors.title?.message && (
          <p className="mt-1 text-sm text-red-600">{String(errors.title?.message)}</p>
        )}
      </div>
      <div>
        <label className="block text-sm">Author</label>
        <input className="mt-1 w-full rounded border px-3 py-2" {...register("author")} />
        {errors.author?.message && (
          <p className="mt-1 text-sm text-red-600">{String(errors.author?.message)}</p>
        )}
      </div>
      <div>
        <label className="block text-sm">Genre</label>
        <input className="mt-1 w-full rounded border px-3 py-2" {...register("genre")} />
        {errors.genre?.message && (
          <p className="mt-1 text-sm text-red-600">{String(errors.genre?.message)}</p>
        )}
      </div>
      <div>
        <label className="block text-sm">Published date</label>
        <input type="date" className="mt-1 w-full rounded border px-3 py-2" {...register("publishedDate")} />
        {errors.publishedDate?.message ? (
          <p className="mt-1 text-sm text-red-600">{errors.publishedDate.message}</p>
        ) : null}
      </div>
      <div>
        <label className="block text-sm">Rating</label>
        <input type="number" min={RATING_MIN} max={RATING_MAX} className="mt-1 w-28 rounded border px-3 py-2" {...register("rating", { valueAsNumber: true })} />
        {errors.rating?.message && (
          <p className="mt-1 text-sm text-red-600">{String(errors.rating?.message)}</p>
        )}
      </div>
      <button disabled={isSubmitting} className="rounded bg-slate-900 px-3 py-2 text-white disabled:opacity-60">
        {isSubmitting ? "Saving..." : submittingLabel}
      </button>
    </form>
  );
}


