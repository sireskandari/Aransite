import { useState, useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { listTimelapses } from "./api";

type TimelapseRow = {
  id: string;
  filePath: string | null;
  fileFormat: string | null;
  fileSize: string | null;
  status: number | string; // enum or string
  errorMessage?: string | null;
  createdUtc?: string | Date | null;
  // add extra properties here if your API sends them
};

function formatDateToronto(value?: string | Date | null) {
  if (!value) return "—";
  const d = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(d.getTime())) return String(value);
  return d.toLocaleString("en-CA", {
    timeZone: "America/Toronto",
    hour12: false,
  });
}

function formatFileSize(sizeStr?: string | null) {
  if (!sizeStr) return "—";
  const n = Number(sizeStr);
  if (!Number.isFinite(n) || n <= 0) return sizeStr;

  if (n < 1024) return `${n} B`;
  const kb = n / 1024;
  if (kb < 1024) return `${kb.toFixed(1)} KB`;
  const mb = kb / 1024;
  if (mb < 1024) return `${mb.toFixed(1)} MB`;
  const gb = mb / 1024;
  return `${gb.toFixed(1)} GB`;
}

function getStatusMeta(status: number | string) {
  // If backend sends numeric enum (0 = Pending, 1 = Processing, 2 = Completed, 3 = Failed)
  const num =
    typeof status === "number"
      ? status
      : Number.isNaN(Number(status))
        ? null
        : Number(status);

  let label = String(status);
  let className = "bg-slate-100 text-slate-700";

  if (num === 0 || /pending/i.test(label)) {
    label = "Pending";
    className = "bg-amber-100 text-amber-700";
  } else if (num === 1 || /processing|running/i.test(label)) {
    label = "Processing";
    className = "bg-blue-100 text-blue-700";
  } else if (num === 2 || /completed|done/i.test(label)) {
    label = "Completed";
    className = "bg-emerald-100 text-emerald-700";
  } else if (num === 3 || /failed|error/i.test(label)) {
    label = "Failed";
    className = "bg-red-100 text-red-700";
  }

  return { label, className };
}

function buildDownloadUrl(row: TimelapseRow): string | null {
  if (!row.filePath) return null;

  const path = row.filePath.trim();
  if (!path) return null;

  // If API already returns an absolute URL, just use it
  if (/^https?:\/\//i.test(path)) return path;

  // Otherwise, join it to the API base
  const base = import.meta.env.VITE_API_BASE;
  if (!base) return path;

  const normalized =
    path.startsWith("/") || path.startsWith("\\") ? path : "/" + path;

  return new URL(normalized, base).toString();
}

export default function TimelapsesListPage() {
  const [search, setSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const pageSize = 10;

  const { data, isFetching, isError } = useQuery({
    queryKey: ["Timelapses", { search, pageNumber, pageSize }],
    queryFn: () => listTimelapses({ search, pageNumber, pageSize }),
    staleTime: 30_000,
  });

  const items: TimelapseRow[] = useMemo(
    () => (data?.items ?? []) as TimelapseRow[],
    [data]
  );

  const totalCount: number | null =
    (data?.pagination?.TotalCount as number | undefined) ??
    (data?.pagination?.totalCount as number | undefined) ??
    null;

  const totalPages = totalCount
    ? Math.max(1, Math.ceil(totalCount / pageSize))
    : null;

  return (
    <section className="space-y-4">
      {/* Toolbar */}
      <div className="flex flex-wrap items-center gap-2">
        <input
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPageNumber(1);
          }}
          placeholder="Search timelapses…"
          className="border p-2 rounded w-full sm:w-64"
        />
        {isFetching && <span className="text-sm text-slate-500">Loading…</span>}
        <div className="flex-1" />
        {totalCount !== null && (
          <span className="text-sm text-slate-600">
            Total: <strong>{totalCount}</strong>
          </span>
        )}
      </div>

      {isError && (
        <div className="text-red-600 text-sm">
          Failed to load timelapses. Please try again.
        </div>
      )}

      {/* List */}
      <div className="grid gap-3">
        {items.map((u) => {
          const statusMeta = getStatusMeta(u.status);
          const downloadUrl = buildDownloadUrl(u);
          const canDownload =
            !!downloadUrl && /completed/i.test(statusMeta.label);

          return (
            <div
              key={u.id}
              className="border rounded-lg p-3 flex flex-col sm:flex-row sm:items-center gap-3 bg-white shadow-sm"
            >
              <div className="flex-1 space-y-1">
                {/* Status + basic info */}
                <div className="flex flex-wrap items-center gap-2">
                  <span
                    className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${statusMeta.className}`}
                  >
                    {statusMeta.label}
                  </span>
                  {u.fileFormat && (
                    <span className="text-xs rounded bg-slate-100 px-2 py-0.5 text-slate-700">
                      {u.fileFormat.toUpperCase()}
                    </span>
                  )}
                  {u.fileSize && (
                    <span className="text-xs text-slate-500">
                      {formatFileSize(u.fileSize)}
                    </span>
                  )}
                </div>

                {/* File path */}
                <div className="text-xs text-slate-500 break-all">
                  {u.filePath || "— no file yet —"}
                </div>

                {/* Created date */}
                <div className="text-xs text-slate-500">
                  Created: {formatDateToronto(u.createdUtc)}
                </div>

                {/* Error message (if any) */}
                {u.errorMessage && (
                  <div className="text-xs text-red-600">
                    Error: {u.errorMessage}
                  </div>
                )}
              </div>

              {/* Actions */}
              <div className="flex items-center gap-2">
                <a
                  href={canDownload ? (downloadUrl ?? undefined) : undefined}
                  target="_blank"
                  rel="noopener noreferrer"
                  className={
                    "inline-flex items-center justify-center rounded px-3 py-1.5 text-sm font-medium " +
                    (canDownload
                      ? "bg-blue-600 text-white hover:bg-blue-700"
                      : "bg-slate-200 text-slate-500 cursor-not-allowed")
                  }
                  onClick={(e) => {
                    if (!canDownload) e.preventDefault();
                  }}
                >
                  {canDownload ? "Download" : "Not ready"}
                </a>
              </div>
            </div>
          );
        })}

        {!isFetching && items.length === 0 && !isError && (
          <div className="text-center text-sm text-slate-500">
            No timelapses found.
          </div>
        )}
      </div>

      {/* Pager */}
      <div className="flex items-center justify-center gap-2">
        <button
          className="px-3 py-1 rounded border disabled:opacity-50"
          disabled={pageNumber <= 1}
          onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
        >
          Prev
        </button>
        <span className="text-sm text-slate-600">
          Page {pageNumber}
          {totalPages ? ` / ${totalPages}` : ""}
        </span>
        <button
          className="px-3 py-1 rounded border disabled:opacity-50"
          disabled={
            totalPages
              ? pageNumber >= totalPages
              : !!data && items.length < pageSize
          }
          onClick={() => setPageNumber((p) => p + 1)}
        >
          Next
        </button>
      </div>
    </section>
  );
}
