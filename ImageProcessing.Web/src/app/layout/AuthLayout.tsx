import { Outlet, Link } from "react-router-dom";

export default function AuthLayout() {
  return (
    <div className="min-h-screen grid place-items-center bg-slate-50">
      <div className="w-full max-w-md rounded-lg border bg-white p-6 shadow-sm">
        <Link
          to="/"
          className="mb-4 inline-block text-sm text-slate-600 hover:text-slate-800"
        >
          ← Back to home
        </Link>
        <Outlet />
      </div>
    </div>
  );
}
