import { useParams, useNavigate } from "react-router-dom";
import { BookForm } from "@/features/books/components/book-form";
import { useBook, useUpdateBook } from "@/features/books/hooks/use-books";
import { HttpConflictError } from "@/lib/http";

export default function EditBookPage() {
  const { id = "" } = useParams();
  const nav = useNavigate();
  const { data: book } = useBook(id);
  const update = useUpdateBook(id);

  if (!book) return <div>Loading…</div>;

  return (
    <section className="mx-auto max-w-lg space-y-4">
      <h1 className="text-xl font-semibold">Edit book</h1>
      <BookForm
        defaultValues={{
          title: book.title,
          author: book.author,
          genre: book.genre ?? "",
          publishedDate: book.publishedDate.slice(0, 10),
          rating: book.rating,
        }}
        onSubmit={async (values) => {
          try {
            await update.mutateAsync(values as any);
            alert("Book updated");
            nav(`/`);
          } catch (err) {
            if (err instanceof HttpConflictError) {
              alert("Update conflict: the record changed on the server. Refresh and try again.");
              return;
            }
            throw err;
          }
        }}
        submittingLabel="Save changes"
      />
    </section>
  );
}
