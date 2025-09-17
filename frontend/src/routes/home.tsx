export default function HomePage() {
  return (
    <section className="card grid place-items-center p-8 text-center">
      <div className="space-y-3">
        <h1 className="text-2xl font-semibold">Discover your next read</h1>
        <p className="text-slate-600">Search, track, and analyze your personal library.</p>
        <div className="flex justify-center gap-2">
          <a href="/books/new" className="btn-primary">Add book</a>
          <a href="/favorites" className="btn">View favorites</a>
        </div>
      </div>
    </section>
  );
}
