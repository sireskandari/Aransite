import { useState, type ReactNode } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { listLogEvents, deleteLogEvent, type LogEvent } from "./api";

type Pagination = {
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages?: number;
};

type LogEventsQueryResult = {
  items: LogEvent[];
  pagination: Pagination | null;
};

export default function LogEventsListPage() {
  const [search, setSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const pageSize = 10;

  const qc = useQueryClient();

  const { data, isFetching, isError } = useQuery<LogEventsQueryResult>({
    queryKey: ["LogEvents", { search, pageNumber, pageSize }],
    queryFn: () => listLogEvents({ search, pageNumber, pageSize }),
    staleTime: 30_000,
  });

  const items = data?.items ?? [];
  const pagination = data?.pagination ?? null;

  const totalCount = pagination?.totalCount ?? items.length;
  const pageCount =
    pagination?.totalPages ??
    (pagination
      ? Math.max(1, Math.ceil(pagination.totalCount / pagination.pageSize))
      : pageNumber);

  // ----- Delete dialog state -----
  const [deleteOpen, setDeleteOpen] = useState(false);

  const delMutation = useMutation({
    mutationFn: () => deleteLogEvent(), // DELETE /LogEvents
    onSuccess: () => {
      setDeleteOpen(false);
      // reload all log lists
      qc.invalidateQueries({ queryKey: ["LogEvents"] });
    },
  });

  const handlePrev = () => {
    setPageNumber((p) => Math.max(1, p - 1));
  };

  const handleNext = () => {
    if (pagination?.totalPages) {
      if (pageNumber < pagination.totalPages) {
        setPageNumber((p) => p + 1);
      }
    } else {
      // fallback when we don’t know totalPages:
      if (items.length === pageSize) {
        setPageNumber((p) => p + 1);
      }
    }
  };

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
          placeholder="Search logs (message, exception, properties)…"
          className="border border-slate-300 bg-white p-2 text-sm rounded w-full sm:w-72"
        />
        {isFetching && <span className="text-xs text-slate-500">Loading…</span>}
        <div className="flex-1" />
        <button
          className="px-3 py-2 text-sm rounded bg-red-600 text-white hover:bg-red-700 disabled:opacity-50"
          onClick={() => setDeleteOpen(true)}
          disabled={delMutation.isPending || items.length === 0}
        >
          Delete All Logs
        </button>
      </div>

      {isError && (
        <div className="text-red-600 text-sm">
          Failed to load log events. Please try again.
        </div>
      )}

      {!isError && items.length === 0 && !isFetching && (
        <div className="text-sm text-slate-500">No logs found.</div>
      )}

      {/* Desktop table */}
      <div className="hidden md:block">
        <div className="overflow-x-auto rounded border border-slate-200 bg-white">
          <table className="min-w-full text-sm">
            <thead className="bg-slate-50 text-left text-xs font-semibold text-slate-600">
              <tr>
                <th className="px-3 py-2">Time</th>
                <th className="px-3 py-2">Level</th>
                <th className="px-3 py-2">Message</th>
                <th className="px-3 py-2">Template</th>
                <th className="px-3 py-2">Exception</th>
                <th className="px-3 py-2">Properties</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {items.map((log) => (
                <tr key={log.id} className="align-top">
                  <td className="px-3 py-2 whitespace-nowrap text-xs text-slate-600">
                    {formatTime(log.timeStamp)}
                  </td>
                  <td className="px-3 py-2 whitespace-nowrap">
                    <LevelBadge level={log.level} />
                  </td>
                  <td className="px-3 py-2">
                    <div className="font-medium text-slate-800">
                      {log.message || <span className="text-slate-400">—</span>}
                    </div>
                  </td>
                  <td className="px-3 py-2 text-xs text-slate-600 max-w-xs break-words">
                    {log.messageTemplate
                      ? truncate(log.messageTemplate, 160)
                      : "—"}
                  </td>
                  <td className="px-3 py-2 text-xs text-red-600 max-w-xs break-words">
                    {log.exception ? truncate(log.exception, 220) : "—"}
                  </td>
                  <td className="px-3 py-2 text-xs text-slate-600 max-w-xs break-words">
                    {log.properties ? truncate(log.properties, 220) : "—"}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Mobile cards */}
      <div className="grid gap-3 md:hidden">
        {items.map((log) => (
          <div
            key={log.id}
            className="border border-slate-200 rounded-lg bg-white p-3 space-y-2"
          >
            <div className="flex items-center justify-between gap-2">
              <span className="text-xs text-slate-500">
                {formatTime(log.timeStamp)}
              </span>
              <LevelBadge level={log.level} />
            </div>

            <div>
              <div className="text-sm font-medium text-slate-800">
                {log.message || (
                  <span className="text-slate-400">No message</span>
                )}
              </div>
              {log.messageTemplate && (
                <div className="mt-0.5 text-xs text-slate-500">
                  Template: {truncate(log.messageTemplate, 120)}
                </div>
              )}
            </div>

            <div className="space-y-1 text-xs">
              <Field label="Exception">
                {log.exception ? truncate(log.exception, 200) : "—"}
              </Field>
              <Field label="Properties">
                {log.properties ? truncate(log.properties, 200) : "—"}
              </Field>
            </div>
          </div>
        ))}
      </div>

      {/* Pager */}
      {totalCount > 0 && (
        <div className="flex flex-wrap items-center gap-2 text-sm text-slate-600">
          <button
            className="px-3 py-1 rounded border border-slate-300 bg-white disabled:opacity-40"
            disabled={pageNumber <= 1}
            onClick={handlePrev}
          >
            Prev
          </button>
          <span>
            Page {pageNumber}
            {pageCount ? ` of ${pageCount}` : null}
          </span>
          <button
            className="px-3 py-1 rounded border border-slate-300 bg-white disabled:opacity-40"
            disabled={
              pagination?.totalPages
                ? pageNumber >= pagination.totalPages
                : items.length < pageSize
            }
            onClick={handleNext}
          >
            Next
          </button>
          <span className="ml-auto text-xs">
            {totalCount} log{totalCount === 1 ? "" : "s"}
          </span>
        </div>
      )}

      {/* Delete Dialog */}
      <Modal
        open={deleteOpen}
        onClose={() => setDeleteOpen(false)}
        title="Delete all logs?"
      >
        <p className="text-sm text-slate-600">
          Are you sure you want to delete <strong>all log entries</strong>? This
          cannot be undone.
        </p>
        <div className="mt-4 flex justify-end gap-2">
          <button
            className="px-3 py-2 rounded border border-slate-300 hover:bg-slate-50 text-sm"
            onClick={() => setDeleteOpen(false)}
            disabled={delMutation.isPending}
          >
            Cancel
          </button>
          <button
            className="px-3 py-2 rounded bg-red-600 text-white text-sm disabled:opacity-50"
            onClick={() => delMutation.mutate()}
            disabled={delMutation.isPending}
          >
            {delMutation.isPending ? "Deleting…" : "Delete all"}
          </button>
        </div>
      </Modal>
    </section>
  );
}

/* ---------- Small UI helpers ---------- */

function LevelBadge({ level }: { level: string | null }) {
  const lv = (level ?? "").toUpperCase();

  let classes =
    "inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium";
  let label = lv || "UNKNOWN";

  if (lv === "ERROR" || lv === "FATAL") {
    classes += " bg-red-100 text-red-700 border border-red-200";
  } else if (lv === "WARNING" || lv === "WARN") {
    classes += " bg-amber-100 text-amber-700 border-amber-200 border";
  } else if (lv === "INFORMATION" || lv === "INFO") {
    classes += " bg-slate-100 text-slate-700 border border-slate-200";
  } else {
    classes += " bg-slate-100 text-slate-500 border border-slate-200";
  }

  return <span className={classes}>{label}</span>;
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div>
      <div className="font-medium text-slate-700">{label}</div>
      <div className="text-slate-600 break-words">{children}</div>
    </div>
  );
}

function formatTime(iso: string) {
  if (!iso) return "—";
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleString(); // you can pass locale/timeZone if needed
}

function truncate(text: string, max: number) {
  if (text.length <= max) return text;
  return text.slice(0, max) + "…";
}

function Modal({
  open,
  onClose,
  title,
  children,
}: {
  open: boolean;
  onClose: () => void;
  title: string;
  children: ReactNode;
}) {
  if (!open) return null;
  return (
    <div
      className="fixed inset-0 z-50 grid place-items-center bg-black/30 p-4"
      onClick={onClose}
      role="dialog"
      aria-modal="true"
    >
      <div
        className="w-full max-w-lg rounded-xl bg-white p-5 shadow-xl"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between gap-2">
          <h2 className="text-lg font-semibold">{title}</h2>
          <button
            className="text-slate-500 hover:text-slate-700"
            onClick={onClose}
            aria-label="Close"
          >
            ✕
          </button>
        </div>
        <div className="mt-3">{children}</div>
      </div>
    </div>
  );
}
