import { NavLink } from "react-router-dom";
import { useRef, useEffect } from "react";

/* ------- tiny helpers local to this file ------- */
function cx(...s: Array<string | false | undefined>) {
  return s.filter(Boolean).join(" ");
}

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

export default function ActionsDropdown() {
  const ref = useRef<HTMLDetailsElement>(null);
  useDetailsDismiss(ref);

  const itemCls = ({ isActive }: { isActive: boolean }) =>
    cx(
      "block px-4 py-2 text-sm",
      isActive
        ? "bg-blue-50 text-blue-700"
        : "text-slate-700 hover:bg-slate-50 hover:text-slate-900"
    );

  const close = () => {
    if (ref.current) ref.current.open = false;
  };

  return (
    <li className="relative">
      <details ref={ref} className="group">
        <summary
          className="flex cursor-pointer list-none items-center gap-1 text-sm text-slate-600 hover:text-slate-800"
          aria-haspopup="menu"
        >
          Actions
          <svg
            viewBox="0 0 20 20"
            className="h-4 w-4 text-slate-500 transition-transform group-open:rotate-180"
            fill="currentColor"
          >
            <path d="M5.23 7.21a.75.75 0 011.06.02L10 11.146l3.71-3.915a.75.75 0 111.08 1.04l-4.24 4.47a.75.75 0 01-1.08 0L5.21 8.27a.75.75 0 01.02-1.06z" />
          </svg>
        </summary>

        <div
          role="menu"
          className="absolute left-0 z-10 mt-2 w-52 overflow-hidden rounded-xl border bg-white shadow-xl"
        >
          <NavLink to="/vision" className={itemCls} onClick={close}>
            Vision
          </NavLink>
          <NavLink to="/cameras" className={itemCls} onClick={close}>
            Cameras
          </NavLink>
          <NavLink to="/targets" className={itemCls} onClick={close}>
            Targets
          </NavLink>
          <NavLink to="/users" className={itemCls} onClick={close}>
            Users
          </NavLink>
        </div>
      </details>
    </li>
  );
}
