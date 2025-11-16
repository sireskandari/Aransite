import { Link } from "react-router-dom";

export default function NotFoundPage() {
  return (
    <div className="text-center space-y-4">
      <h1 className="text-2xl font-semibold">404 — Not Found</h1>
      <p className="text-slate-600">
        The page you’re looking for doesn’t exist.
      </p>
      <Link to="/" className="text-blue-600 hover:underline">
        Go back home
      </Link>
    </div>
  );
}
