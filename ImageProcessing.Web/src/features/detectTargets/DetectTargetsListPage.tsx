import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  listDetectTargets,
  updateDetectTarget,
  deleteDetectTarget,
  createDetectTarget,
} from "./api";

type DetectTargetForm = { cameraKey: string; targets: string };

export default function DetectTargetsListPage() {
  const [search, setSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const pageSize = 10;

  const qc = useQueryClient();

  const { data, isFetching, isError } = useQuery({
    queryKey: ["DetectTargets", { search, pageNumber, pageSize }],
    queryFn: () => listDetectTargets({ search, pageNumber, pageSize }),
    staleTime: 30_000,
  });

  // ----- Delete dialog state -----
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [toDelete, setToDelete] = useState<{ id: string; key: string } | null>(
    null
  );
  const askDelete = (model: { id: string; cameraKey: string }) => {
    setToDelete({ id: model.id, key: model.cameraKey });
    setDeleteOpen(true);
  };

  // ----- Add modal state -----
  const [addOpen, setAddOpen] = useState(false);
  const [addForm, setAddForm] = useState<DetectTargetForm>({
    cameraKey: "",
    targets: "",
  });

  // ----- Edit modal state -----
  const [editOpen, setEditOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState<DetectTargetForm>({
    cameraKey: "",
    targets: "",
  });

  const beginEdit = (model: {
    id: string;
    cameraKey: string;
    targets: string;
  }) => {
    setEditingId(model.id);
    setEditForm({
      cameraKey: model.cameraKey ?? "",
      targets: model.targets ?? "",
    });
    setEditOpen(true);
  };
  const cancelEdit = () => {
    setEditOpen(false);
    setEditingId(null);
    setEditForm({ cameraKey: "", targets: "" });
  };

  // ----- Mutations -----
  const saveMutation = useMutation({
    mutationFn: (p: { id: string } & DetectTargetForm) =>
      updateDetectTarget(p.id, {
        cameraKey: p.cameraKey,
        targets: p.targets,
      }),
    onSuccess: () => {
      cancelEdit();
      qc.invalidateQueries({ queryKey: ["DetectTargets"] });
    },
  });

  const delMutation = useMutation({
    mutationFn: (id: string) => deleteDetectTarget(id),
    onSuccess: () => {
      setDeleteOpen(false);
      setToDelete(null);
      qc.invalidateQueries({ queryKey: ["DetectTargets"] });
    },
  });

  const createMutation = useMutation({
    mutationFn: (p: DetectTargetForm) => createDetectTarget(p),
    onSuccess: () => {
      setAddOpen(false);
      setAddForm({ cameraKey: "", targets: "" });
      setPageNumber(1);
      qc.invalidateQueries({ queryKey: ["DetectTargets"] });
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
          placeholder="Search DetectTargets…"
          className="border p-2 rounded w-64"
        />
        {isFetching && <span className="text-sm text-slate-500">Loading…</span>}
        <div className="flex-1" />
        <button
          className="px-3 py-2 rounded bg-slate-900 text-white hover:opacity-90"
          onClick={() => setAddOpen(true)}
        >
          + Add Detect Targets
        </button>
      </div>

      {isError && (
        <div className="text-red-600 text-sm">
          Failed to load DetectTargets.
        </div>
      )}

      {/* List */}
      <div className="grid gap-3">
        {data?.items?.map((u: any) => (
          <div
            key={u.id}
            className="border rounded p-3 flex items-start gap-3 bg-white"
          >
            <div className="flex-1 space-y-1">
              <div className="font-medium">{u.cameraKey}</div>
              <div className="text-xs px-2 py-1 rounded bg-slate-100 text-slate-700 break-all">
                {u.targets}
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
        title="Delete DetectTarget?"
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

      {/* Add DetectTarget Modal */}
      <Modal
        open={addOpen}
        onClose={() => setAddOpen(false)}
        title="Add DetectTarget"
      >
        <div className="space-y-3">
          <Field label="CameraKey">
            <input
              className="border p-2 rounded w-full"
              value={addForm.cameraKey}
              onChange={(e) =>
                setAddForm((f) => ({ ...f, cameraKey: e.target.value }))
              }
            />
          </Field>
          <Field label="Targets">
            <input
              className="border p-2 rounded w-full"
              value={addForm.targets}
              onChange={(e) =>
                setAddForm((f) => ({ ...f, targets: e.target.value }))
              }
            />
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
                cameraKey: addForm.cameraKey.trim(),
                targets: addForm.targets.trim(),
              })
            }
            disabled={
              createMutation.isPending ||
              !addForm.cameraKey.trim() ||
              !addForm.targets.trim()
            }
          >
            {createMutation.isPending ? "Saving…" : "Save"}
          </button>
        </div>
      </Modal>

      {/* Edit DetectTarget Modal */}
      <Modal open={editOpen} onClose={cancelEdit} title="Edit DetectTarget">
        <div className="space-y-3">
          <Field label="CameraKey">
            <input
              className="border p-2 rounded w-full"
              value={editForm.cameraKey}
              onChange={(e) =>
                setEditForm((f) => ({ ...f, cameraKey: e.target.value }))
              }
            />
          </Field>
          <Field label="Targets">
            <input
              className="border p-2 rounded w-full"
              value={editForm.targets}
              onChange={(e) =>
                setEditForm((f) => ({ ...f, targets: e.target.value }))
              }
            />
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
                cameraKey: editForm.cameraKey.trim(),
                targets: editForm.targets.trim(),
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
