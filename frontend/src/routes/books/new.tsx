import { useNavigate } from "react-router-dom";
import { BookForm } from "@/features/books/components/book-form";
import { useCreateBook } from "@/features/books/hooks/use-books";
import type { CreateBookInput } from "@/features/books/types/book";

export default function NewBookPage() {
  const nav = useNavigate();
  const create = useCreateBook();

  return (
    <section className="mx-auto max-w-lg space-y-4">
      <h1 className="text-xl font-semibold">Add book</h1>
      <BookForm
        onSubmit={async (values: CreateBookInput) => {
          await create.mutateAsync(values);
          alert("Book created");
          nav(`/`);
        }}
        submittingLabel="Create"
      />
    </section>
  );
}
