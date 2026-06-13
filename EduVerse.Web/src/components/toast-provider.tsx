"use client";

import { CheckCircle2, Info, X, XCircle } from "lucide-react";
import { createContext, useCallback, useContext, useMemo, useRef, useState } from "react";
import { cn } from "@/lib/utils";

type ToastTone = "success" | "error" | "info";

type Toast = {
  id: number;
  title: string;
  message?: string;
  tone: ToastTone;
};

type ToastContextValue = {
  showToast: (toast: Omit<Toast, "id">) => void;
};

const ToastContext = createContext<ToastContextValue | null>(null);

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);
  const nextToastId = useRef(0);

  const removeToast = useCallback((id: number) => {
    setToasts((current) => current.filter((toast) => toast.id !== id));
  }, []);

  const showToast = useCallback((toast: Omit<Toast, "id">) => {
    const id = ++nextToastId.current;
    setToasts((current) => [...current, { ...toast, id }]);
    window.setTimeout(() => removeToast(id), 4500);
  }, [removeToast]);

  const value = useMemo(() => ({ showToast }), [showToast]);

  return (
    <ToastContext.Provider value={value}>
      {children}
      <div className="fixed right-4 top-4 z-50 flex w-[calc(100vw-2rem)] max-w-sm flex-col gap-3">
        {toasts.map((toast) => {
          const Icon = toast.tone === "success" ? CheckCircle2 : toast.tone === "error" ? XCircle : Info;
          return (
            <div
              key={toast.id}
              className={cn(
                "rounded-xl2 bg-white p-4 shadow-soft ring-1",
                toast.tone === "success" && "ring-teal-100",
                toast.tone === "error" && "ring-coral-100",
                toast.tone === "info" && "ring-slate-200"
              )}
            >
              <div className="flex gap-3">
                <Icon
                  size={20}
                  className={cn(
                    "mt-0.5 shrink-0",
                    toast.tone === "success" && "text-teal-600",
                    toast.tone === "error" && "text-coral-500",
                    toast.tone === "info" && "text-muted"
                  )}
                />
                <div className="min-w-0 flex-1">
                  <p className="text-sm font-bold text-ink">{toast.title}</p>
                  {toast.message && <p className="mt-1 text-sm leading-5 text-muted">{toast.message}</p>}
                </div>
                <button onClick={() => removeToast(toast.id)} className="grid size-7 shrink-0 place-items-center rounded-lg hover:bg-slate-50" aria-label="Dismiss notification">
                  <X size={16} />
                </button>
              </div>
            </div>
          );
        })}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast() {
  const context = useContext(ToastContext);
  if (!context) {
    throw new Error("useToast must be used inside ToastProvider");
  }
  return context;
}
