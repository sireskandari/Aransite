import { useRef, useEffect } from "react";

/* ------- tiny helpers local to this file ------- */
function useDetailsDismiss(ref: React.RefObject<HTMLDetailsElement | null>) {
  useEffect(() => {
    const onDocClick = (e: MouseEvent) => {
      const el = ref.current;
      if (!el || !el.open) return;
      if (!el.contains(e.target as Node)) el.open = false;
    };
    const onEsc = (e: KeyboardEvent) => {
      const el = ref.current;
      if (e.key === "Escape" && el?.open) el.open = false;
    };
    document.addEventListener("mousedown", onDocClick);
    document.addEventListener("keydown", onEsc);
    return () => {
      document.removeEventListener("mousedown", onDocClick);
      document.removeEventListener("keydown", onEsc);
    };
  }, [ref]);
}
/* ---------------------------------------------- */

export default function UserMenu({
  name,
  email,
  onLogout,
}: {
  name?: string;
  email?: string;
  onLogout: () => void;
}) {
  const ref = useRef<HTMLDetailsElement>(null);
  useDetailsDismiss(ref);

  const initials =
    (name || email || "?")
      .split(" ")
      .map((p) => p[0])
      .slice(0, 2)
      .join("")
      .toUpperCase() || "?";

  const close = () => {
    if (ref.current) ref.current.open = false;
  };

  return (
    <details ref={ref} className="relative">
      <summary className="flex cursor-pointer list-none items-center gap-2 rounded-lg border px-2 py-1 hover:bg-slate-50">
        <span className="grid h-7 w-7 place-items-center rounded-full bg-slate-900 text-xs font-semibold text-white">
          {initials}
        </span>
        <span className="text-slate-700 max-w-[10rem] truncate">
          {name || email || "User"}
        </span>
        <svg
          className="h-4 w-4 text-slate-500"
          viewBox="0 0 20 20"
          fill="currentColor"
        >
          <path d="M5.23 7.21a.75.75 0 011.06.02L10 11.146l3.71-3.915a.75.75 0 111.08 1.04l-4.24 4.47a.75.75 0 01-1.08 0L5.21 8.27a.75.75 0 01.02-1.06z" />
        </svg>
      </summary>

      <div
        role="menu"
        className="absolute right-0 z-10 mt-2 w-56 overflow-hidden rounded-xl border bg-white shadow-xl"
      >
        <div className="border-b px-3 py-2">
          <div className="text-sm font-medium text-slate-800">
            {name || "Signed in"}
          </div>
          {email && (
            <div className="truncate text-xs text-slate-500">{email}</div>
          )}
        </div>

        <button
          role="menuitem"
          onClick={() => {
            close();
            onLogout();
          }}
          className="w-full px-4 py-2 text-left text-sm text-red-600 hover:bg-red-50"
        >
          Logout
        </button>
      </div>
    </details>
  );
}
