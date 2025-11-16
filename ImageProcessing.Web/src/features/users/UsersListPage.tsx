import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { listUsers } from "./api";

export default function UsersListPage() {
  const [search, setSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const pageSize = 10;

  const { data, isFetching, isError } = useQuery({
    queryKey: ["users", { search, pageNumber, pageSize }],
    queryFn: () => listUsers({ search, pageNumber, pageSize }),
    staleTime: 30_000,
  });

  return (
    <section className="space-y-4">
      <div className="flex items-center gap-2">
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search users…"
          className="border p-2 rounded w-64"
        />
        {isFetching && <span className="text-sm text-slate-500">Loading…</span>}
      </div>

      {isError && (
        <div className="text-red-600 text-sm">Failed to load users.</div>
      )}

      <div className="grid gap-3">
        {data?.items?.map((u) => (
          <div
            key={u.id}
            className="border rounded p-3 flex items-center gap-3 bg-white"
          >
            {u.profileImagePath ? (
              <img
                src={`/uploads/${u.profileImagePath}`}
                className="w-10 h-10 rounded-full object-cover"
                onError={(e) => {
                  (e.target as HTMLImageElement).style.display = "none";
                }}
              />
            ) : (
              <div className="w-10 h-10 rounded-full bg-slate-200" />
            )}

            <div className="flex-1">
              <div className="font-medium">{u.name}</div>
              <div className="text-sm text-slate-500">{u.email}</div>
            </div>
            <div className="text-xs px-2 py-1 rounded bg-slate-100 text-slate-700">
              {u.role}
            </div>
          </div>
        ))}
      </div>

      <div className="flex items-center gap-2">
        <button
          className="px-3 py-1 rounded border"
          disabled={pageNumber <= 1}
          onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
        >
          Prev
        </button>
        <span className="text-sm text-slate-600">Page {pageNumber}</span>
        <button
          className="px-3 py-1 rounded border"
          disabled={!!data && data.items.length < pageSize}
          onClick={() => setPageNumber((p) => p + 1)}
        >
          Next
        </button>
      </div>
    </section>
  );
}
