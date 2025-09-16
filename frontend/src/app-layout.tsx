import { Link, Outlet } from "react-router-dom";
import { AppErrorBoundary } from "./components/app-error-boundary";

export default function AppLayout() {
  return (
    <div className="min-h-screen bg-white text-slate-900">
      <header className="border-b">
        <nav className="mx-auto flex max-w-5xl items-center justify-between px-4 py-3">
          <Link to="/" className="font-semibold">Library</Link>
          <div className="flex items-center gap-4">
            <Link to="/" className="text-sm text-slate-600 hover:text-slate-900">Home</Link>
            <Link to="/stats" className="text-sm text-slate-600 hover:text-slate-900">Stats</Link>
            <a href="/favorites" className="text-sm text-slate-600 hover:text-slate-900">Favorites</a>
          </div>
        </nav>
      </header>
      <main className="mx-auto max-w-5xl px-4 py-6">
        <AppErrorBoundary>
          <Outlet />
        </AppErrorBoundary>
      </main>
    </div>
  );
}
