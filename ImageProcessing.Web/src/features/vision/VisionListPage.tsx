import { useEffect, useMemo, useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { listEdgeData } from "./api";
import { listCameras } from "../cameras/api";

/* ---------- Types & helpers ---------- */

type EdgeRow = {
  id: string;
  captureTimestampUtc: string | Date;
  createdUtc: string | Date;
  cameraId: string;
  computeModel: string | number;
  computeInferenceMs: number | string;
  imageWidth: number;
  imageHeight: number;
  detections: string | null;
  frameRawUrl: string;
  frameAnnotatedUrl: string;
};

/** Accepts camelCase or PascalCase; returns unified camelCase. */
function normalizeEdge(x: any): EdgeRow {
  return {
    id: x.id ?? x.Id ?? "",
    captureTimestampUtc: x.captureTimestampUtc ?? x.CaptureTimestampUtc ?? "",
    createdUtc: x.createdUtc ?? x.CreatedUtc ?? "",
    cameraId: x.cameraId ?? x.CameraId ?? "",
    computeModel: x.computeModel ?? x.ComputeModel ?? "",
    computeInferenceMs: x.computeInferenceMs ?? x.ComputeInferenceMs ?? 0,
    imageWidth: x.imageWidth ?? x.ImageWidth ?? 0,
    imageHeight: x.imageHeight ?? x.ImageHeight ?? 0,
    detections: x.detections ?? x.Detections ?? null,
    frameRawUrl: x.frameRawUrl ?? x.FrameRawUrl ?? "",
    frameAnnotatedUrl: x.frameAnnotatedUrl ?? x.FrameAnnotatedUrl ?? "",
  };
}

/** Compact numeric pagination list with ellipsis. */
function buildPageList(
  current: number,
  totalPages: number,
  max = 9
): (number | "...")[] {
  if (totalPages <= max)
    return Array.from({ length: totalPages }, (_, i) => i + 1);
  const pages: (number | "...")[] = [];
  const first = 1;
  const last = totalPages;
  const window = 2;
  pages.push(first);
  const start = Math.max(current - window, 2);
  const end = Math.min(current + window, totalPages - 1);
  if (start > 2) pages.push("...");
  for (let p = start; p <= end; p++) pages.push(p);
  if (end < totalPages - 1) pages.push("...");
  pages.push(last);
  return pages;
}

/** Simple controlled toggle (no peer CSS). */
function Toggle({
  checked,
  onChange,
  label,
}: {
  checked: boolean;
  onChange: (v: boolean) => void;
  label?: string;
}) {
  return (
    <button
      type="button"
      aria-pressed={checked}
      onClick={() => onChange(!checked)}
      className="inline-flex items-center gap-2"
    >
      {label ? <span className="text-sm text-slate-600">{label}</span> : null}
      <span
        className={`relative inline-block h-6 w-11 rounded-full transition-colors ${checked ? "bg-blue-600" : "bg-gray-300"}`}
      >
        <span
          className={`absolute top-[2px] left-[2px] h-5 w-5 rounded-full bg-white shadow transition-transform ${
            checked ? "translate-x-5" : "translate-x-0"
          }`}
        />
      </span>
    </button>
  );
}

/* ---------- Page ---------- */

export default function EdgeDataListPage() {
  const [search, setSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const pageSize = 10;
  const [live, setLive] = useState(true);
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [cameraId, setCameraId] = useState<string>("");
  const [timelapseQuality, setTimelapseQuality] = useState<
    "low" | "medium" | "high"
  >("medium");
  function parseUtcWithoutZ(value: string | Date): Date {
    if (value instanceof Date) return value;

    // Expecting formats like "2025-11-19T03:31:05.11"
    const [datePart, timePart] = value.split("T");
    if (!datePart || !timePart) return new Date(value); // fallback

    const [year, month, day] = datePart.split("-").map(Number);

    const [hStr, mStr, sStrRaw] = timePart.split(":");
    const [sStr, msStrRaw] = (sStrRaw ?? "").split(".");

    const hours = Number(hStr ?? 0);
    const minutes = Number(mStr ?? 0);
    const seconds = Number(sStr ?? 0);
    const ms = msStrRaw ? Number((msStrRaw + "000").slice(0, 3)) : 0; // to milliseconds

    // Interpret as UTC
    return new Date(
      Date.UTC(year, month - 1, day, hours, minutes, seconds, ms)
    );
  }

  function formatUtcToToronto(value: string | Date) {
    const d = parseUtcWithoutZ(value);
    if (Number.isNaN(d.getTime())) return String(value);

    return d.toLocaleString("en-CA", {
      timeZone: "America/Toronto",
      hour12: false,
    });
  }

  const handleOpenTimelapse = () => {
    const base = import.meta.env.VITE_API_BASE;
    const url = new URL("/api/v1/Timelapse/from-edge/stream", base);

    if (search.trim()) {
      url.searchParams.set("search", search.trim());
    }

    if (cameraId.trim()) {
      url.searchParams.set("cameraId", cameraId.trim());
    }

    if (fromDate) {
      // start of day in UTC
      const fromIso = new Date(fromDate + "T00:00:00Z").toISOString();
      url.searchParams.set("fromUtc", fromIso);
    }

    if (toDate) {
      // end of day in UTC
      const toIso = new Date(toDate + "T23:59:59Z").toISOString();
      url.searchParams.set("toUtc", toIso);
    }

    if (timelapseQuality) {
      url.searchParams.set("quality", timelapseQuality);
    }

    window.open(url.toString(), "_blank", "noopener,noreferrer");
  };

  // Uses your api.ts: returns { items, pagination } where pagination comes from X-Pagination header.
  const { data, isFetching, isError, refetch } = useQuery({
    queryKey: [
      "EdgeData",
      { search, cameraId, fromDate, toDate, pageNumber, pageSize },
    ],
    queryFn: () =>
      listEdgeData({
        search,
        fromDate,
        toDate,
        cameraId,
        pageNumber,
        pageSize,
      }),
    staleTime: 30_000,
  });

  const { data: cameraData, isLoading: camerasLoading } = useQuery({
    queryKey: ["cameras"],
    queryFn: () => listCameras({ pageSize: 100 }),
  });
  const rawItems: any[] = data?.items ?? [];
  const rows: EdgeRow[] = useMemo(
    () => rawItems.map(normalizeEdge),
    [rawItems]
  );
  const totalCount: number | null =
    (data?.pagination?.TotalCount as number | undefined) ??
    (data?.pagination?.totalCount as number | undefined) ??
    null;

  const totalPages = totalCount
    ? Math.max(1, Math.ceil(totalCount / pageSize))
    : null;

  // Selection by id so selection survives refetches
  const [selectedId, setSelectedId] = useState<string | null>(null);

  useEffect(() => {
    if (!rows.length) {
      setSelectedId(null);
      return;
    }
    if (selectedId && rows.some((r) => r.id === selectedId)) return;
    setSelectedId(rows[0].id);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [pageNumber, rows]);

  const selected = useMemo(
    () => rows.find((r) => r.id === selectedId) || null,
    [rows, selectedId]
  );

  const captureStr = useMemo(() => {
    if (!selected?.captureTimestampUtc) return "";
    return formatUtcToToronto(selected.captureTimestampUtc);
  }, [selected]);

  const detCount = useMemo(() => {
    const raw = selected?.detections;
    if (!raw) return 0;
    try {
      const parsed = JSON.parse(raw);
      if (Array.isArray(parsed)) return parsed.length;
      if (
        parsed &&
        typeof parsed === "object" &&
        Array.isArray((parsed as any).detections)
      ) {
        return (parsed as any).detections.length;
      }
    } catch {}
    return 0;
  }, [selected]);

  /* ---------- Live polling + auto jump to page 1 when new arrives ---------- */

  const lastFirstIdRef = useRef<string | null>(null);
  const lastTotalRef = useRef<number | null>(null);

  useEffect(() => {
    const currentFirstId = rows[0]?.id ?? null;
    const currentTotal = totalCount ?? null;

    const hadFirst = lastFirstIdRef.current !== null;
    const hadTotal = lastTotalRef.current !== null;

    const firstChanged =
      hadFirst && currentFirstId && currentFirstId !== lastFirstIdRef.current;
    const totalIncreased =
      hadTotal &&
      currentTotal !== null &&
      currentTotal > (lastTotalRef.current ?? 0);

    lastFirstIdRef.current = currentFirstId;
    lastTotalRef.current = currentTotal;

    if (live && (firstChanged || totalIncreased)) setPageNumber(1);
  }, [rows, totalCount, live]);

  useEffect(() => {
    if (!live) return;
    const id = setInterval(() => refetch(), 30_000);
    return () => clearInterval(id);
  }, [live, refetch]);

  /* ---------- UI ---------- */

  return (
    <section className="space-y-5">
      <div className="flex flex-col gap-3">
        {/* Row 1: Search */}
        <div className="flex flex-col sm:flex-row sm:items-center gap-3">
          <input
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPageNumber(1);
            }}
            placeholder="Search by camera/model…"
            className="border p-2 rounded w-full sm:w-72"
          />
          <select
            value={cameraId}
            onChange={(e) => {
              setCameraId(e.target.value);
              setPageNumber(1);
            }}
            className="border p-2 rounded w-full sm:w-auto"
            disabled={camerasLoading}
          >
            <option value="">All Cameras</option>

            {cameraData?.items?.map((c) => (
              <option key={c.id} value={c.id}>
                {c.key}
              </option>
            ))}
          </select>
        </div>

        {/* Row 2: Filters / actions */}
        <div className="flex flex-wrap items-center gap-3">
          <input
            type="date"
            value={fromDate}
            onChange={(e) => {
              setFromDate(e.target.value);
              setPageNumber(1);
            }}
            className="border p-2 rounded w-full sm:w-auto"
          />
          <input
            type="date"
            value={toDate}
            onChange={(e) => {
              setToDate(e.target.value);
              setPageNumber(1);
            }}
            className="border p-2 rounded w-full sm:w-auto"
          />
          <select
            value={timelapseQuality}
            onChange={(e) =>
              setTimelapseQuality(e.target.value as "low" | "medium" | "high")
            }
            className="border p-2 rounded w-full sm:w-auto"
          >
            <option value="low">Low (small size)</option>
            <option value="medium">Medium</option>
            <option value="high">High (large size)</option>
          </select>

          <button
            type="button"
            onClick={handleOpenTimelapse}
            className="px-3 py-2 rounded bg-blue-600 text-white text-sm hover:bg-blue-700 disabled:opacity-50 w-full sm:w-auto"
            disabled={isFetching}
          >
            Timelapse Viewer
          </button>

          {isFetching && (
            <span className="text-sm text-slate-500 w-full sm:w-auto">
              Refreshing…
            </span>
          )}

          {/* Right side info: push to the end on larger screens, stack on small */}
          <div className="flex items-center gap-3 w-full sm:w-auto sm:ml-auto justify-between sm:justify-end">
            <div className="text-sm text-slate-600">
              Total: {totalCount ?? "—"}
            </div>
            <Toggle checked={live} onChange={setLive} label="Live (30s)" />
          </div>
        </div>
      </div>

      {isError && (
        <div className="text-red-600 text-sm">Failed to load edge data.</div>
      )}

      {selected ? (
        <>
          {/* Details */}
          <div className="mx-auto  rounded-lg border bg-white p-3 text-sm text-slate-700">
            <div className="flex flex-wrap gap-x-6 gap-y-1">
              <div>
                <span className="text-slate-500">Captured:</span> {captureStr}{" "}
                (Canada/Toronto)
              </div>
              <div>
                <span className="text-slate-500">Camera:</span>{" "}
                {selected.cameraId}
              </div>
              <div>
                <span className="text-slate-500">Model:</span>{" "}
                {selected.computeModel}
              </div>
              <div>
                <span className="text-slate-500">Inference:</span>{" "}
                {selected.computeInferenceMs} ms
              </div>
              <div>
                <span className="text-slate-500">Size:</span>{" "}
                {selected.imageWidth}×{selected.imageHeight}
              </div>
              <div>
                <span className="text-slate-500">Detections:</span> {detCount}
              </div>
            </div>
          </div>

          {/* Two large images */}
          <div className="mx-auto ">
            <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
              <ImageCard title="Raw frame" src={selected.frameRawUrl} />
              <ImageCard title="Annotated" src={selected.frameAnnotatedUrl} />
            </div>
          </div>

          {/* Thumbnails */}
          <div className="mx-auto ">
            <div className="mt-4 grid grid-cols-2 sm:grid-cols-5 gap-3">
              {rows.map((it) => {
                const active = it.id === selectedId;
                return (
                  <button
                    key={it.id}
                    onClick={() => setSelectedId(it.id)}
                    className={
                      "relative overflow-hidden rounded-lg border bg-white hover:shadow focus:outline-none " +
                      (active ? "ring-2 ring-blue-500" : "")
                    }
                    title={formatUtcToToronto(it.captureTimestampUtc)}
                  >
                    <img
                      src={it.frameAnnotatedUrl || it.frameRawUrl}
                      alt="thumb"
                      className="aspect-video h-full w-full object-cover"
                      loading="lazy"
                    />
                    <div className="absolute bottom-0 left-0 right-0 bg-black/40 px-1 py-0.5 text-[10px] text-white">
                      {formatUtcToToronto(it.captureTimestampUtc).split(" ")[1]}
                    </div>
                  </button>
                );
              })}
            </div>
          </div>
        </>
      ) : (
        <EmptyOrSkeleton isFetching={isFetching} />
      )}

      {/* Numbered Pagination */}
      <div className="flex items-center justify-center gap-1">
        <button
          className="px-3 py-1 rounded border disabled:opacity-50"
          disabled={pageNumber <= 1}
          onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
        >
          Prev
        </button>

        {totalPages
          ? buildPageList(pageNumber, totalPages).map((p, i) =>
              p === "..." ? (
                <span key={`d-${i}`} className="px-2 text-slate-500">
                  …
                </span>
              ) : (
                <button
                  key={p}
                  onClick={() => setPageNumber(p as number)}
                  className={
                    "min-w-9 px-3 py-1 rounded border text-sm " +
                    ((p as number) === pageNumber
                      ? "bg-blue-600 text-white border-blue-600"
                      : "bg-white hover:bg-slate-50")
                  }
                >
                  {p}
                </button>
              )
            )
          : null}

        <button
          className="px-3 py-1 rounded border disabled:opacity-50"
          disabled={
            totalPages ? pageNumber >= totalPages : rows.length < pageSize
          }
          onClick={() => setPageNumber((p) => p + 1)}
        >
          Next
        </button>
      </div>
    </section>
  );
}

/* ---------- Small UI pieces ---------- */
import { useState as useReactState } from "react";

function ImageCard({ title, src }: { title: string; src: string }) {
  const [err, setErr] = useReactState<string | null>(null);
  return (
    <figure className="rounded-xl border bg-white p-2">
      <figcaption className="px-1 pb-2 text-sm text-slate-600">
        {title}
      </figcaption>
      <div className="relative w-full overflow-hidden rounded-lg">
        {src && !err ? (
          <img
            src={src}
            alt={title}
            onError={() => setErr("Failed to load")}
            className="aspect-video w-full object-contain bg-slate-100"
          />
        ) : (
          <div className="aspect-video grid place-items-center bg-slate-100 text-slate-400">
            {err ?? "No image"}
          </div>
        )}
      </div>
    </figure>
  );
}

function EmptyOrSkeleton({ isFetching }: { isFetching: boolean }) {
  if (isFetching) {
    return (
      <div className="mx-auto  grid grid-cols-1 md:grid-cols-2 gap-4">
        <div className="h-64 animate-pulse rounded-lg bg-slate-200" />
        <div className="h-64 animate-pulse rounded-lg bg-slate-200" />
      </div>
    );
  }
  return (
    <div className="mx-auto  text-center text-slate-500">No images found.</div>
  );
}
