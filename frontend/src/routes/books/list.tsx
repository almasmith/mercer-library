import { BookTable } from "@/features/books/components/book-table";
import { useBookStats } from "@/features/books/hooks/use-books";

export default function BooksListPage() {
  useBookStats();
  return (
    <section className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-semibold">Books</h1>
        <a href="/books/new" className="rounded bg-slate-900 px-3 py-2 text-white">Add book</a>
      </div>
      {/* Stats could feed genre suggestions later */}
      <BookTable />
      {/* Pagination is rendered inside BookTable; this is left for layout flexibility if needed */}
    </section>
  );
}
