"use client";

import { ChevronLeft, ChevronRight, Filter } from "lucide-react";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { Badge, EmptyState, LoadingState, PageHeader } from "@/components/ui";
import { adminService } from "@/lib/api";
import type { ActivityLog, PaginatedResponse } from "@/lib/types";
import { formatDate } from "@/lib/utils";

export default function ActivityLogsPage() {
  const [logs, setLogs] = useState<PaginatedResponse<ActivityLog> | null>(null);
  const [page, setPage] = useState(1);
  const [action, setAction] = useState("");
  const [entityType, setEntityType] = useState("");
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    setLoading(true);
    adminService.getActivityLogs({ page, pageSize: 20, action, entityType, search })
      .then(setLogs)
      .catch(() => setError("Could not load activity logs from the API."))
      .finally(() => setLoading(false));
  }, [page, action, entityType, search]);

  return (
    <AppShell>
      <AuthGuard roles={["Admin"]}>
        <PageHeader eyebrow="Admin audit" title="Activity logs" description="Track important platform actions, actor, entity, and timestamp." />

        <section className="mt-8 rounded-xl2 bg-white p-5 shadow-soft ring-1 ring-slate-100">
          <div className="grid gap-3 md:grid-cols-[1fr_180px_180px]">
            <div className="relative">
              <Filter className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-muted" size={18} />
              <input value={search} onChange={(event) => { setSearch(event.target.value); setPage(1); }} placeholder="Search user, description, entity id..." className="h-12 w-full rounded-xl bg-slate-50 px-11 text-sm font-semibold text-ink ring-1 ring-slate-200 outline-none focus:ring-teal-300" />
            </div>
            <input value={action} onChange={(event) => { setAction(event.target.value); setPage(1); }} placeholder="Action" className="h-12 rounded-xl bg-slate-50 px-4 text-sm font-semibold text-ink ring-1 ring-slate-200 outline-none focus:ring-teal-300" />
            <input value={entityType} onChange={(event) => { setEntityType(event.target.value); setPage(1); }} placeholder="Entity type" className="h-12 rounded-xl bg-slate-50 px-4 text-sm font-semibold text-ink ring-1 ring-slate-200 outline-none focus:ring-teal-300" />
          </div>
        </section>

        <section className="mt-6 rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
          {loading ? (
            <LoadingState label="Loading activity logs" />
          ) : error ? (
            <EmptyState title="Activity logs unavailable" description={error} />
          ) : !logs || logs.items.length === 0 ? (
            <EmptyState title="No activity logs yet" description="Important admin actions will appear here after users start changing data." />
          ) : (
            <>
              <div className="overflow-x-auto">
                <table className="w-full min-w-[920px] text-left">
                  <thead>
                    <tr className="border-b border-slate-100 text-sm text-muted">
                      <th className="px-4 py-3 font-semibold">Action</th>
                      <th className="px-4 py-3 font-semibold">Entity</th>
                      <th className="px-4 py-3 font-semibold">User</th>
                      <th className="px-4 py-3 font-semibold">Description</th>
                      <th className="px-4 py-3 font-semibold">Date</th>
                    </tr>
                  </thead>
                  <tbody>
                    {logs.items.map((log) => (
                      <tr key={log.id} className="border-b border-slate-100 last:border-0">
                        <td className="px-4 py-4"><Badge tone={badgeTone(log.action)}>{log.action || "Action"}</Badge></td>
                        <td className="px-4 py-4">
                          <p className="text-sm font-bold text-ink">{log.entityType || "Entity"}</p>
                          <p className="mt-1 max-w-44 truncate text-xs text-muted">{log.entityId ?? "No entity id"}</p>
                        </td>
                        <td className="px-4 py-4 text-sm font-semibold text-ink">{log.userName}</td>
                        <td className="px-4 py-4 text-sm text-muted">{log.description}</td>
                        <td className="px-4 py-4 text-sm text-muted">{formatDate(log.createdAt)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className="mt-5 flex items-center justify-between gap-4">
                <p className="text-sm font-semibold text-muted">Showing {logs.items.length} of {logs.totalCount} logs</p>
                <div className="flex items-center gap-2">
                  <button disabled={page <= 1} onClick={() => setPage((value) => Math.max(1, value - 1))} className="grid size-10 place-items-center rounded-xl bg-slate-50 text-ink ring-1 ring-slate-200 disabled:opacity-40">
                    <ChevronLeft size={18} />
                  </button>
                  <span className="text-sm font-bold text-ink">Page {logs.page} / {Math.max(logs.totalPages, 1)}</span>
                  <button disabled={page >= logs.totalPages} onClick={() => setPage((value) => value + 1)} className="grid size-10 place-items-center rounded-xl bg-slate-50 text-ink ring-1 ring-slate-200 disabled:opacity-40">
                    <ChevronRight size={18} />
                  </button>
                </div>
              </div>
            </>
          )}
        </section>
      </AuthGuard>
    </AppShell>
  );
}

function badgeTone(action: string): "teal" | "amber" | "coral" | "slate" {
  const normalized = action.toLowerCase();
  if (normalized.includes("delete")) return "coral";
  if (normalized.includes("restore") || normalized.includes("create")) return "teal";
  if (normalized.includes("password") || normalized.includes("role")) return "amber";
  return "slate";
}
