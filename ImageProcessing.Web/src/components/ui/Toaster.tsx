import React, { createContext, useContext, useState, useCallback } from "react";

type Toast = {
  id: number;
  type: "info" | "success" | "error";
  message: string;
};
type ToastCtx = { toast: (t: Omit<Toast, "id">) => void };

const ToastContext = createContext<ToastCtx | null>(null);

export function useToast() {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error("useToast must be used within <ToasterProvider>");
  return ctx.toast;
}

export function ToasterProvider({ children }: { children: React.ReactNode }) {
  const [items, setItems] = useState<Toast[]>([]);

  const toast = useCallback((t: Omit<Toast, "id">) => {
    const id = Date.now() + Math.random();
    setItems((xs) => [...xs, { id, ...t }]);
    setTimeout(() => {
      setItems((xs) => xs.filter((i) => i.id !== id));
    }, 3500);
  }, []);

  return (
    <ToastContext.Provider value={{ toast }}>
      {children}
      <div className="fixed right-4 top-4 z-[9999] space-y-2">
        {items.map((t) => (
          <div
            key={t.id}
            className={
              "rounded-lg px-3 py-2 shadow text-sm text-white " +
              (t.type === "error"
                ? "bg-red-600"
                : t.type === "success"
                  ? "bg-emerald-600"
                  : "bg-slate-900")
            }
            role="status"
          >
            {t.message}
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}
