"use client";

import { FormEvent, useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { useToast } from "@/components/toast-provider";
import { Badge, Button, EmptyState, LoadingState, PageHeader } from "@/components/ui";
import { adminService } from "@/lib/api";
import type { AdminUserDetails, ManagedUser, UserRole } from "@/lib/types";
import { formatDate } from "@/lib/utils";

const roles: UserRole[] = ["Student", "Instructor", "OrganizationAdmin", "Admin"];

export default function AdminUsersPage() {
  const { showToast } = useToast();
  const [users, setUsers] = useState<ManagedUser[]>([]);
  const [roleFilter, setRoleFilter] = useState("");
  const [selectedUser, setSelectedUser] = useState<ManagedUser | null>(null);
  const [detailsUser, setDetailsUser] = useState<AdminUserDetails | null>(null);
  const [assignUser, setAssignUser] = useState<ManagedUser | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  async function loadUsers(role = roleFilter) {
    setLoading(true);
    setError("");
    try {
      setUsers(await adminService.getUsers(role || undefined));
    } catch {
      setUsers([]);
      setError("Could not load users from the API.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadUsers("");
  }, []);

  async function addRole(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const role = String(new FormData(event.currentTarget).get("role"));
    try {
      await adminService.addRole(role);
      showToast({ title: "Role added", message: `${role} is now available.`, tone: "success" });
      event.currentTarget.reset();
    } catch {
      showToast({ title: "Role action failed", message: "The role may already exist or your token is not Admin.", tone: "error" });
    }
  }

  async function assignRole(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const userId = String(form.get("userId"));
    const role = String(form.get("role"));
    try {
      await adminService.addUserToRole(userId, role);
      showToast({ title: "User role updated", message: `${userId} assigned to ${role}.`, tone: "success" });
      event.currentTarget.reset();
      setAssignUser(null);
      await loadUsers();
    } catch {
      showToast({ title: "Assign role failed", message: "Check user id, role name, and Admin permissions.", tone: "error" });
    }
  }

  async function removeRole(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const role = String(new FormData(event.currentTarget).get("role"));
    if (role.toLowerCase() === "admin") {
      showToast({ title: "Admin role protected", message: "Do not remove the critical Admin role from the platform.", tone: "error" });
      return;
    }

    if (!window.confirm(`Remove role ${role}?`)) return;

    try {
      await adminService.removeRole(role);
      showToast({ title: "Role removed", message: `${role} was removed.`, tone: "success" });
      event.currentTarget.reset();
      await loadUsers();
    } catch {
      showToast({ title: "Remove role failed", message: "The role may still be assigned or backend rejected the request.", tone: "error" });
    }
  }

  return (
    <AppShell>
      <AuthGuard roles={["Admin"]}>
        <PageHeader eyebrow="Admin" title="Users and roles" description="Manage role names, assign roles, and review users from real backend data." />

        <section className="mt-8 grid gap-8 xl:grid-cols-[360px_1fr]">
          <div className="space-y-6">
            <form onSubmit={addRole} className="rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
              <h2 className="text-lg font-bold text-ink">Add role</h2>
              <p className="mt-1 text-sm text-muted">Uses POST /Auth/AddRole/role</p>
              <input name="role" placeholder="Role name" className="mt-5 h-12 w-full rounded-xl bg-slate-50 px-4 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500" required />
              <Button className="mt-4 w-full">Add role</Button>
            </form>

            <form onSubmit={removeRole} className="rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
              <h2 className="text-lg font-bold text-ink">Remove role</h2>
              <p className="mt-1 text-sm text-muted">Global role removal is disabled for safety. User-level role removal needs a dedicated backend endpoint.</p>
              <select name="role" className="mt-5 h-12 w-full rounded-xl bg-slate-50 px-4 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500">
                {roles.filter((role) => role !== "Admin").map((role) => <option key={role}>{role}</option>)}
              </select>
              <Button className="mt-4 w-full" variant="ghost" disabled>Remove role disabled</Button>
            </form>
          </div>

          <div className="rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
            <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
              <div>
                <h2 className="text-xl font-bold text-ink">Users</h2>
                <p className="mt-1 text-sm text-muted">Filter by platform role.</p>
              </div>
              <select
                value={roleFilter}
                onChange={(event) => {
                  setRoleFilter(event.target.value);
                  loadUsers(event.target.value);
                }}
                className="h-11 rounded-xl bg-slate-50 px-4 text-sm font-semibold outline-none ring-1 ring-slate-200"
              >
                <option value="">All roles</option>
                {roles.map((role) => <option key={role}>{role}</option>)}
              </select>
            </div>

            <div className="mt-6">
              {loading ? (
                <LoadingState label="Loading users" />
              ) : error ? (
                <EmptyState title="Users unavailable" description={error} />
              ) : users.length === 0 ? (
                <EmptyState title="No users found" description="Try another role filter." />
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full min-w-[680px] text-left">
                    <thead>
                      <tr className="border-b border-slate-100 text-sm text-muted">
                        <th className="px-4 py-3 font-semibold">Name</th>
                        <th className="px-4 py-3 font-semibold">Email</th>
                        <th className="px-4 py-3 font-semibold">Role</th>
                        <th className="px-4 py-3 font-semibold">User id</th>
                        <th className="px-4 py-3 font-semibold">Actions</th>
                      </tr>
                    </thead>
                    <tbody>
                      {users.map((user) => (
                        <tr key={`${user.email}-${user.id}`} className="border-b border-slate-100 last:border-0">
                          <td className="px-4 py-4 text-sm font-semibold text-ink">{user.fullName}</td>
                          <td className="px-4 py-4 text-sm text-muted">{user.email}</td>
                          <td className="px-4 py-4"><Badge>{user.role}</Badge></td>
                          <td className="px-4 py-4 text-xs text-muted">{user.id ?? "-"}</td>
                          <td className="px-4 py-4">
                            <div className="flex flex-wrap gap-3">
                              <button onClick={async () => {
                                setSelectedUser(user);
                                setDetailsUser(null);
                                if (user.id) {
                                  setDetailsUser(await adminService.getUserDetails(user.id));
                                }
                              }} className="text-sm font-bold text-teal-600">View Details</button>
                              <button onClick={() => setAssignUser(user)} className="text-sm font-bold text-amber-500">Assign Role</button>
                              <button disabled className="text-sm font-bold text-muted opacity-50">Remove Role</button>
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>
        </section>

        {selectedUser && (
          <div className="fixed inset-0 z-50 grid place-items-center bg-ink/50 p-4" onClick={() => setSelectedUser(null)}>
            <div className="w-full max-w-xl rounded-xl2 bg-white p-6 shadow-xl ring-1 ring-slate-100" onClick={(event) => event.stopPropagation()}>
              <div className="flex items-start justify-between gap-4">
                <div>
                  <p className="text-sm font-bold uppercase text-teal-600">User details</p>
                  <h2 className="mt-2 text-2xl font-black text-ink">{selectedUser.fullName}</h2>
                </div>
                <button onClick={() => setSelectedUser(null)} className="rounded-xl bg-slate-50 px-3 py-2 text-sm font-bold text-muted">Close</button>
              </div>
              {!detailsUser ? (
                <div className="mt-6"><LoadingState label="Loading user details" /></div>
              ) : (
                <div className="mt-6 grid gap-4 md:grid-cols-2">
                  <Info label="Email" value={detailsUser.email} />
                  <Info label="Username" value={detailsUser.userName || "Not available"} />
                  <Info label="Role" value={detailsUser.role} />
                  <Info label="UserId" value={detailsUser.userId || "Not available"} />
                  <Info label="Phone" value={detailsUser.phone ?? "Not available"} />
                  <Info label="Created At" value={detailsUser.createdAt && detailsUser.createdAt !== "Not available" ? formatDate(detailsUser.createdAt) : "Not available"} />
                  <Info label="Last Login" value={detailsUser.lastLogin && detailsUser.lastLogin !== "Not available" ? formatDate(detailsUser.lastLogin) : "Not available"} />
                  <Info label="Courses Count" value={`${detailsUser.coursesCount}`} />
                  <Info label="Sessions Count" value={`${detailsUser.sessionsCount}`} />
                  <Info label="Enrollments Count" value={`${detailsUser.enrollmentsCount}`} />
                  <div className="md:col-span-2">
                    <p className="mb-3 text-sm font-bold text-ink">Recent activity</p>
                    {detailsUser.recentActivityLogs?.length ? (
                      <div className="space-y-2">
                        {detailsUser.recentActivityLogs.map((log) => (
                          <div key={log.id} className="rounded-xl bg-slate-50 p-3">
                            <p className="text-sm font-bold text-ink">{log.action}</p>
                            <p className="mt-1 text-xs text-muted">{log.description}</p>
                            <p className="mt-2 text-xs font-semibold text-teal-600">{formatDate(log.createdAt)}</p>
                          </div>
                        ))}
                      </div>
                    ) : (
                      <p className="rounded-xl bg-slate-50 p-4 text-sm font-semibold text-muted">No recent activity logs for this user.</p>
                    )}
                  </div>
                </div>
              )}
            </div>
          </div>
        )}

        {assignUser && (
          <div className="fixed inset-0 z-50 grid place-items-center bg-ink/50 p-4" onClick={() => setAssignUser(null)}>
            <form onSubmit={assignRole} className="w-full max-w-md rounded-xl2 bg-white p-6 shadow-xl ring-1 ring-slate-100" onClick={(event) => event.stopPropagation()}>
              <h2 className="text-2xl font-black text-ink">Assign role</h2>
              <p className="mt-2 text-sm text-muted">{assignUser.fullName} - {assignUser.email}</p>
              <input type="hidden" name="userId" value={assignUser.id ?? ""} />
              <select name="role" defaultValue={assignUser.role} className="mt-5 h-12 w-full rounded-xl bg-slate-50 px-4 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500">
                {roles.map((role) => <option key={role}>{role}</option>)}
              </select>
              <div className="mt-5 flex gap-3">
                <Button className="flex-1" disabled={!assignUser.id}>Save</Button>
                <Button type="button" variant="ghost" onClick={() => setAssignUser(null)}>Cancel</Button>
              </div>
            </form>
          </div>
        )}
      </AuthGuard>
    </AppShell>
  );
}

function Info({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl bg-slate-50 p-4">
      <p className="text-xs font-semibold uppercase text-muted">{label}</p>
      <p className="mt-2 break-words text-sm font-bold text-ink">{value}</p>
    </div>
  );
}
