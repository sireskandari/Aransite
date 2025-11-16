import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { listCameras, updateCamera, deleteCamera, createCamera } from "./api";

type CameraForm = {
  key: string;
  location: string;
  rtsp: string;
  isActive: boolean;
};

export default function CamerasListPage() {
  const [search, setSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const pageSize = 10;

  const qc = useQueryClient();

  const { data, isFetching, isError } = useQuery({
    queryKey: ["Cameras", { search, pageNumber, pageSize }],
    queryFn: () => listCameras({ search, pageNumber, pageSize }),
    staleTime: 30_000,
  });

  // ----- Delete dialog state -----
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [toDelete, setToDelete] = useState<{ id: string; key: string } | null>(
    null
  );
  const askDelete = (cam: { id: string; key: string }) => {
    setToDelete({ id: cam.id, key: cam.key });
    setDeleteOpen(true);
  };

  // ----- Add modal state -----
  const [addOpen, setAddOpen] = useState(false);
  const [addForm, setAddForm] = useState<CameraForm>({
    key: "",
    location: "",
    rtsp: "",
    isActive: false,
  });

  // ----- Edit modal state -----
  const [editOpen, setEditOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState<CameraForm>({
    key: "",
    location: "",
    rtsp: "",
    isActive: false,
  });

  const beginEdit = (cam: {
    id: string;
    key: string;
    location: string;
    rtsp: string;
    isActive: boolean;
  }) => {
    setEditingId(cam.id);
    setEditForm({
      key: cam.key ?? "",
      location: cam.location ?? "",
      rtsp: cam.rtsp ?? "",
      isActive: cam.isActive ?? false,
    });
    setEditOpen(true);
  };
  const cancelEdit = () => {
    setEditOpen(false);
    setEditingId(null);
    setEditForm({ key: "", location: "", rtsp: "", isActive: false });
  };

  // ----- Mutations -----
  const saveMutation = useMutation({
    mutationFn: (p: { id: string } & CameraForm) =>
      updateCamera(p.id, {
        key: p.key,
        location: p.location,
        rtsp: p.rtsp,
        isActive: p.isActive,
      }),
    onSuccess: () => {
      cancelEdit();
      qc.invalidateQueries({ queryKey: ["Cameras"] });
    },
  });

  const delMutation = useMutation({
    mutationFn: (id: string) => deleteCamera(id),
    onSuccess: () => {
      setDeleteOpen(false);
      setToDelete(null);
      qc.invalidateQueries({ queryKey: ["Cameras"] });
    },
  });

  const createMutation = useMutation({
    mutationFn: (p: CameraForm) => createCamera(p),
    onSuccess: () => {
      setAddOpen(false);
      setAddForm({ key: "", location: "", rtsp: "", isActive: false });
      setPageNumber(1);
      qc.invalidateQueries({ queryKey: ["Cameras"] });
    },
  });

  return (
    <section className="space-y-4">
      {/* Toolbar */}
      <div className="flex items-center gap-2">
        <input
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPageNumber(1);
          }}
          placeholder="Search Cameras…"
          className="border p-2 rounded w-64"
        />
        {isFetching && <span className="text-sm text-slate-500">Loading…</span>}
        <div className="flex-1" />
        <button
          className="px-3 py-2 rounded bg-slate-900 text-white hover:opacity-90"
          onClick={() => setAddOpen(true)}
        >
          + Add Camera
        </button>
      </div>

      {isError && (
        <div className="text-red-600 text-sm">Failed to load Cameras.</div>
      )}

      {/* List */}
      <div className="grid gap-3">
        {data?.items?.map((u: any) => (
          <div
            key={u.id}
            className="border rounded p-3 flex items-start gap-3 bg-white"
          >
            <div className="flex-1 space-y-1">
              <div className="font-medium">{u.key}</div>
              <div>
                <span
                  className={
                    u.isActive
                      ? "inline-flex items-center rounded-md bg-green-400/10 px-2 py-1 text-xs font-medium text-green-400 inset-ring inset-ring-green-500/20"
                      : "inline-flex items-center rounded-md bg-red-400/10 px-2 py-1 text-xs font-medium text-red-400 inset-ring inset-ring-red-500/20"
                  }
                >
                  {u.isActive ? "Active" : "Inactive"}
                </span>
              </div>
              <div className="text-sm text-slate-500">{u.location}</div>
              <div className="text-xs px-2 py-1 rounded bg-slate-100 text-slate-700 break-all">
                {u.rtsp}
              </div>
            </div>

            {/* Actions */}
            <div className="flex items-center gap-2">
              <button
                className="px-3 py-1 rounded border text-slate-700 hover:bg-slate-50"
                onClick={() => beginEdit(u)}
              >
                Edit
              </button>
              <button
                className="px-3 py-1 rounded border border-red-300 text-red-700 hover:bg-red-50 disabled:opacity-50"
                onClick={() => askDelete(u)}
              >
                Delete
              </button>
            </div>
          </div>
        ))}
      </div>

      {/* Pager */}
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

      {/* Delete Dialog */}
      <Modal
        open={deleteOpen}
        onClose={() => setDeleteOpen(false)}
        title="Delete camera?"
      >
        <p className="text-sm text-slate-600">
          {toDelete ? (
            <>
              Are you sure you want to delete <b>{toDelete.key}</b>? This cannot
              be undone.
            </>
          ) : null}
        </p>
        <div className="mt-4 flex justify-end gap-2">
          <button
            className="px-3 py-2 rounded border hover:bg-slate-50"
            onClick={() => setDeleteOpen(false)}
            disabled={delMutation.isPending}
          >
            Cancel
          </button>
          <button
            className="px-3 py-2 rounded bg-red-600 text-white disabled:opacity-50"
            onClick={() => toDelete && delMutation.mutate(toDelete.id)}
            disabled={delMutation.isPending}
          >
            {delMutation.isPending ? "Deleting…" : "Delete"}
          </button>
        </div>
      </Modal>

      {/* Add Camera Modal */}
      <Modal
        open={addOpen}
        onClose={() => setAddOpen(false)}
        title="Add camera"
      >
        <div className="space-y-3">
          <Field label="Key">
            <input
              className="border p-2 rounded w-full"
              value={addForm.key}
              onChange={(e) =>
                setAddForm((f) => ({ ...f, key: e.target.value }))
              }
            />
          </Field>
          <Field label="Location">
            <input
              className="border p-2 rounded w-full"
              value={addForm.location}
              onChange={(e) =>
                setAddForm((f) => ({ ...f, location: e.target.value }))
              }
            />
          </Field>
          <Field label="RTSP">
            <input
              className="border p-2 rounded w-full"
              value={addForm.rtsp}
              onChange={(e) =>
                setAddForm((f) => ({ ...f, rtsp: e.target.value }))
              }
            />
          </Field>
          <Field label="">
            <label className="inline-flex items-center cursor-pointer">
              <input
                type="checkbox"
                checked={addForm.isActive}
                onChange={(e) =>
                  setAddForm((f) => ({ ...f, isActive: e.target.checked }))
                }
                className="sr-only peer"
                aria-label="Active"
              />
              <div
                className="relative w-11 h-6 bg-gray-200 rounded-full
                    peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300
                    peer-checked:bg-blue-600
                    after:content-[''] after:absolute after:top-[2px] after:left-[2px]
                    after:bg-white after:border-gray-300 after:border after:rounded-full
                    after:h-5 after:w-5 after:transition-all
                    peer-checked:after:translate-x-5"
              ></div>
              <span className="ml-3 text-sm font-medium text-gray-900">
                Active
              </span>
            </label>
          </Field>
        </div>
        <div className="mt-4 flex justify-end gap-2">
          <button
            className="px-3 py-2 rounded border hover:bg-slate-50"
            onClick={() => setAddOpen(false)}
            disabled={createMutation.isPending}
          >
            Cancel
          </button>
          <button
            className="px-3 py-2 rounded bg-slate-900 text-white disabled:opacity-50"
            onClick={() =>
              createMutation.mutate({
                key: addForm.key.trim(),
                location: addForm.location.trim(),
                rtsp: addForm.rtsp.trim(),
                isActive: addForm.isActive,
              })
            }
            disabled={
              createMutation.isPending ||
              !addForm.key.trim() ||
              !addForm.rtsp.trim()
            }
          >
            {createMutation.isPending ? "Saving…" : "Save"}
          </button>
        </div>
      </Modal>

      {/* Edit Camera Modal */}
      <Modal open={editOpen} onClose={cancelEdit} title="Edit camera">
        <div className="space-y-3">
          <Field label="Key">
            <input
              className="border p-2 rounded w-full"
              value={editForm.key}
              onChange={(e) =>
                setEditForm((f) => ({ ...f, key: e.target.value }))
              }
            />
          </Field>
          <Field label="Location">
            <input
              className="border p-2 rounded w-full"
              value={editForm.location}
              onChange={(e) =>
                setEditForm((f) => ({ ...f, location: e.target.value }))
              }
            />
          </Field>
          <Field label="RTSP">
            <input
              className="border p-2 rounded w-full"
              value={editForm.rtsp}
              onChange={(e) =>
                setEditForm((f) => ({ ...f, rtsp: e.target.value }))
              }
            />
          </Field>
          <Field label="">
            <label className="inline-flex items-center cursor-pointer">
              <input
                type="checkbox"
                checked={editForm.isActive}
                onChange={(e) =>
                  setEditForm((f) => ({ ...f, isActive: e.target.checked }))
                }
                className="sr-only peer"
                aria-label="Active"
              />
              <div
                className="relative w-11 h-6 bg-gray-200 rounded-full
                    peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300
                    peer-checked:bg-blue-600
                    after:content-[''] after:absolute after:top-[2px] after:left-[2px]
                    after:bg-white after:border-gray-300 after:border after:rounded-full
                    after:h-5 after:w-5 after:transition-all
                    peer-checked:after:translate-x-5"
              ></div>
              <span className="ml-3 text-sm font-medium text-gray-900">
                Active
              </span>
            </label>
          </Field>
        </div>
        <div className="mt-4 flex justify-end gap-2">
          <button
            className="px-3 py-2 rounded border hover:bg-slate-50"
            onClick={cancelEdit}
          >
            Cancel
          </button>
          <button
            className="px-3 py-2 rounded bg-slate-900 text-white disabled:opacity-50"
            disabled={saveMutation.isPending || !editingId}
            onClick={() =>
              editingId &&
              saveMutation.mutate({
                id: editingId,
                key: editForm.key.trim(),
                location: editForm.location.trim(),
                rtsp: editForm.rtsp.trim(),
                isActive: editForm.isActive,
              })
            }
          >
            {saveMutation.isPending ? "Saving…" : "Save"}
          </button>
        </div>
      </Modal>
    </section>
  );
}

/** Tiny helpers */
function Modal({
  open,
  onClose,
  title,
  children,
}: {
  open: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
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
        <div className="flex items-center justify-between">
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

function Field({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
  return (
    <label className="block">
      <div className="text-sm text-slate-600 mb-1">{label}</div>
      {children}
    </label>
  );
}
