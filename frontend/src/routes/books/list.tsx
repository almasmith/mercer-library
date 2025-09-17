import { BookTable } from "@/features/books/components/book-table";
import { useBookStats } from "@/features/books/hooks/use-books";

export default function BooksListPage() {
  useBookStats();
  return (
    <section className="space-y-4">
      <div className="toolbar card items-center p-3">
        <h1 className="text-xl font-semibold">Books</h1>
        <p className="text-sm text-slate-500">Manage your library and track ratings</p>
      </div>
      {/* Stats could feed genre suggestions later */}
      <div className="card p-3">
        <BookTable />
      </div>
      {/* Pagination is rendered inside BookTable; this is left for layout flexibility if needed */}
    </section>
  );
}
