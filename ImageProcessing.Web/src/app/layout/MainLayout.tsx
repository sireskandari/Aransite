import { Outlet, Link, useNavigate } from "react-router-dom";
import { useState } from "react";
import { useAuthStore } from "@/features/auth/authStore";
import ActionsDropdown from "@/app/layout/nav/ActionsDropdown";
import UserMenu from "@/app/layout/nav/UserMenu";

/* local tiny helper for this file */
function cx(...s: Array<string | false | undefined>) {
  return s.filter(Boolean).join(" ");
}

export default function MainLayout() {
  const { user, logout } = useAuthStore();
  const navigate = useNavigate();
  const [mobileOpen, setMobileOpen] = useState(false);

  const onLogout = () => {
    logout();
    navigate("/login");
  };

  return (
    <div className="min-h-screen bg-slate-50 text-slate-900">
      <header className="border-b bg-white">
        <nav className="mx-auto flex max-w-6xl items-center justify-between p-4">
          {/* LEFT: brand + primary nav */}
          <div className="flex items-center gap-6">
            <Link to="/" className="font-semibold text-slate-900">
              Aran Solution
            </Link>

            {/* Desktop primary nav (left aligned) */}
            <ul className="hidden items-center gap-4 md:flex">
              <li>
                <NavLink
                  to="/"
                  className={({ isActive }) =>
                    cx(
                      "text-sm transition-colors",
                      isActive
                        ? "text-blue-600"
                        : "text-slate-600 hover:text-slate-800"
                    )
                  }
                >
                  Home
                </NavLink>
              </li>
              <li>
                <NavLink
                  to="/about-us"
                  className={({ isActive }) =>
                    cx(
                      "text-sm transition-colors",
                      isActive
                        ? "text-blue-600"
                        : "text-slate-600 hover:text-slate-800"
                    )
                  }
                >
                  About us
                </NavLink>
              </li>
              <li>
                <NavLink
                  to="/contact-us"
                  className={({ isActive }) =>
                    cx(
                      "text-sm transition-colors",
                      isActive
                        ? "text-blue-600"
                        : "text-slate-600 hover:text-slate-800"
                    )
                  }
                >
                  Contact us
                </NavLink>
              </li>

              {user && <ActionsDropdown />}
            </ul>
          </div>

          {/* RIGHT: user/login */}
          <div className="hidden items-center gap-4 md:flex">
            {user ? (
              <UserMenu
                name={user.name}
                email={user.email}
                onLogout={onLogout}
              />
            ) : (
              <NavLink
                to="/login"
                className={({ isActive }) =>
                  cx(
                    "text-sm transition-colors",
                    isActive
                      ? "text-blue-600"
                      : "text-slate-600 hover:text-slate-800"
                  )
                }
              >
                Login
              </NavLink>
            )}
          </div>

          {/* Mobile hamburger */}
          <button
            className="md:hidden inline-flex h-9 w-9 items-center justify-center rounded-lg border hover:bg-slate-50"
            aria-label="Toggle menu"
            aria-expanded={mobileOpen}
            onClick={() => setMobileOpen((v) => !v)}
          >
            <svg
              viewBox="0 0 24 24"
              className="h-5 w-5"
              fill="none"
              stroke="currentColor"
            >
              {mobileOpen ? (
                <path
                  strokeWidth="2"
                  strokeLinecap="round"
                  d="M6 18L18 6M6 6l12 12"
                />
              ) : (
                <path
                  strokeWidth="2"
                  strokeLinecap="round"
                  d="M3 6h18M3 12h18M3 18h18"
                />
              )}
            </svg>
          </button>
        </nav>

        {/* Mobile drawer (kept inside this file to keep only 3 files total) */}
        {mobileOpen && (
          <div className="md:hidden border-t">
            <div className="mx-auto max-w-6xl p-4 space-y-2">
              <MobileItem to="/" onClick={() => setMobileOpen(false)}>
                Home
              </MobileItem>

              <details className="group rounded-lg border">
                <summary className="flex cursor-pointer list-none items-center justify-between px-3 py-2">
                  <span className="font-medium text-slate-800">Actions</span>
                  <svg
                    viewBox="0 0 20 20"
                    className="h-4 w-4 text-slate-500 transition-transform group-open:rotate-180"
                    fill="currentColor"
                  >
                    <path d="M5.23 7.21a.75.75 0 011.06.02L10 11.146l3.71-3.915a.75.75 0 111.08 1.04l-4.24 4.47a.75.75 0 01-1.08 0L5.21 8.27a.75.75 0 01.02-1.06z" />
                  </svg>
                </summary>
                <div className="px-2 pb-2">
                  <MobileSubItem
                    to="/cameras"
                    onClick={() => setMobileOpen(false)}
                  >
                    Cameras
                  </MobileSubItem>
                  <MobileSubItem
                    to="/targets"
                    onClick={() => setMobileOpen(false)}
                  >
                    Targets
                  </MobileSubItem>
                  <MobileSubItem
                    to="/users"
                    onClick={() => setMobileOpen(false)}
                  >
                    Users
                  </MobileSubItem>
                </div>
              </details>

              {user ? (
                <button
                  onClick={() => {
                    setMobileOpen(false);
                    onLogout();
                  }}
                  className="w-full rounded-lg border px-3 py-2 text-left text-red-600 hover:bg-red-50"
                >
                  Logout
                </button>
              ) : (
                <MobileItem to="/login" onClick={() => setMobileOpen(false)}>
                  Login
                </MobileItem>
              )}
            </div>
          </div>
        )}
      </header>

      <main className="mx-auto max-w-6xl p-6">
        <Outlet />
      </main>

      <SiteFooter />
    </div>
  );
}

/* --------- mobile-only items (local to this file to keep 3 files total) --------- */
import { NavLink } from "react-router-dom";
import { SiteFooter } from "./SiteFooter";

function MobileItem({
  to,
  onClick,
  children,
}: {
  to: string;
  onClick?: () => void;
  children: React.ReactNode;
}) {
  return (
    <NavLink
      to={to}
      onClick={onClick}
      className={({ isActive }) =>
        cx(
          "block rounded-lg px-3 py-2 text-sm",
          isActive
            ? "bg-blue-50 text-blue-700"
            : "text-slate-700 hover:bg-slate-50"
        )
      }
    >
      {children}
    </NavLink>
  );
}

function MobileSubItem({
  to,
  onClick,
  children,
}: {
  to: string;
  onClick?: () => void;
  children: React.ReactNode;
}) {
  return (
    <NavLink
      to={to}
      onClick={onClick}
      className={({ isActive }) =>
        cx(
          "block rounded-md px-3 py-1.5 text-sm",
          isActive
            ? "bg-blue-50 text-blue-700"
            : "text-slate-700 hover:bg-slate-50"
        )
      }
    >
      {children}
    </NavLink>
  );
}
