import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useAuthStore } from "./authStore";

export default function ProtectedRoute({
  children,
  roles,
}: {
  children: ReactNode;
  roles?: string[];
}) {
  const { accessToken, user } = useAuthStore();
  const hydrated = useAuthStore.persist.hasHydrated();
  const location = useLocation();
  if (!hydrated) {
    return <div className="p-6 text-sm text-slate-500">Loading session…</div>;
  }

  if (!accessToken) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  if (roles && user && !roles.includes(user.role ?? "")) {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}
