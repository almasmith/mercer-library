import { Link, NavLink, Outlet } from "react-router-dom";
import { AppErrorBoundary } from "./components/app-error-boundary";
import { RealtimeBadge } from "@/features/realtime/realtime-badge";
import { HiOutlineHome, HiOutlineHeart, HiOutlineChartBar, HiOutlineChartPie, HiOutlineBookOpen } from "react-icons/hi";

export default function AppLayout() {
  return (
    <div className="min-h-screen">
      <div className="mx-auto flex min-h-screen max-w-7xl gap-6 px-4 py-6">
        {/* Sidebar */}
        <aside className="hidden w-60 shrink-0 md:block">
          <div className="card sticky top-0 p-4">
            <Link to="/" className="mb-4 flex items-center gap-2 text-lg font-semibold">
              <HiOutlineBookOpen className="text-slate-700" />
              Library
            </Link>
            <nav className="space-y-1 text-sm">
              <SidebarLink to="/" icon={<HiOutlineHome />} label="Discover" />
              <SidebarLink to="/favorites" icon={<HiOutlineHeart />} label="Favorites" />
              <SidebarLink to="/stats" icon={<HiOutlineChartBar />} label="Stats" />
              <SidebarLink to="/analytics" icon={<HiOutlineChartPie />} label="Analytics" />
            </nav>
          </div>
        </aside>

        {/* Main column */}
        <div className="flex-1">
          <main>
            <AppErrorBoundary>
              <Outlet />
            </AppErrorBoundary>
          </main>
        </div>
      </div>
    </div>
  );
}

function SidebarLink({ to, icon, label }: { to: string; icon: React.ReactNode; label: string }) {
  return (
    <NavLink to={to} className={({ isActive }) =>
      `flex items-center gap-2 rounded px-2 py-2 transition-colors ${isActive ? "bg-slate-900/5 text-slate-900" : "text-slate-700 hover:bg-slate-100"}`
    }>
      <span className="text-lg">{icon}</span>
      <span>{label}</span>
    </NavLink>
  );
}
