import { Link } from "react-router-dom";

export default function NotFoundPage() {
  return (
    <section aria-labelledby="not-found-heading" className="card p-6 text-center">
      <h1 id="not-found-heading" className="text-xl font-semibold">Page not found</h1>
      <p className="mt-2 text-sm text-slate-600">The page you are looking for doesn’t exist.</p>
      <div className="mt-4">
        <Link to="/" className="btn-primary">Go to Home</Link>
      </div>
    </section>
  );
}
