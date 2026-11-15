import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

export function RoleLayout({ title, links }: { title: string; links: { to: string; label: string }[] }) {
  const { fullName, logout } = useAuth();
  const navigate = useNavigate();
  return (
    <div className="min-h-dvh pb-16 md:pb-0 md:pl-56">
      <header className="sticky top-0 z-10 flex items-center justify-between border-b bg-white px-4 py-3 md:hidden">
        <span className="font-semibold text-brand-700">{title}</span>
        <button className="rounded-lg bg-slate-100 px-3 py-1.5 text-sm" onClick={async () => { await logout(); navigate("/login"); }}>
          Logout
        </button>
      </header>
      <aside className="fixed inset-y-0 left-0 hidden w-56 flex-col border-r bg-white p-4 md:flex">
        <h1 className="text-lg font-bold text-brand-700">SevaSetu</h1>
        <p className="mb-6 text-xs text-slate-500">{fullName}</p>
        <nav className="flex flex-1 flex-col gap-1">
          {links.map((l) => (
            <NavLink key={l.to} to={l.to} end={l.to.split("/").length <= 2} className={({ isActive }) => `rounded-lg px-3 py-2 text-sm ${isActive ? "bg-brand-50 font-medium text-brand-700" : "text-slate-600"}`}>
              {l.label}
            </NavLink>
          ))}
          <a href="/manual/index.html" className="rounded-lg px-3 py-2 text-sm text-slate-600">
            Help
          </a>
        </nav>
        <button className="rounded-lg border px-3 py-2 text-left text-sm" onClick={async () => { await logout(); navigate("/login"); }}>
          Logout
        </button>
      </aside>
      <main className="mx-auto max-w-5xl p-4">
        <Outlet />
      </main>
      <nav className="fixed inset-x-0 bottom-0 flex border-t bg-white md:hidden">
        <div className="flex w-full snap-x snap-mandatory overflow-x-auto">
          {links.map((l) => (
            <NavLink key={l.to} to={l.to} className={({ isActive }) => `min-w-[4.75rem] flex-1 snap-start py-3 text-center text-xs ${isActive ? "font-semibold text-brand-700" : "text-slate-500"}`}>
              {l.label}
            </NavLink>
          ))}
          <a href="/manual/index.html" className="min-w-[4.75rem] flex-1 snap-start py-3 text-center text-xs text-slate-500">
            Help
          </a>
        </div>
      </nav>
    </div>
  );
}
