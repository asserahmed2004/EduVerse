"use client";

import { FormEvent, useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { SmartImage } from "@/components/smart-image";
import { useToast } from "@/components/toast-provider";
import { Badge, Button, EmptyState, LoadingState, PageHeader } from "@/components/ui";
import { adminService, getApiErrorMessage, organizationService } from "@/lib/api";
import { SEED_IMAGES } from "@/lib/image-fallbacks";
import type { AdminUserDetails, ManagedUser, OrganizationOverview, UserRole } from "@/lib/types";
import { formatDate } from "@/lib/utils";

const roles: UserRole[] = ["Student", "Instructor", "OrganizationAdmin", "Admin"];
const backendRoleNames: Record<UserRole, string> = {
  Admin: "admin",
  OrganizationAdmin: "organizationAdmin",
  Instructor: "instructor",
  Student: "student"
};

export default function AdminUsersPage() {
  const { showToast } = useToast();
  const [users, setUsers] = useState<ManagedUser[]>([]);
  const [roleFilter, setRoleFilter] = useState("");
  const [selectedUser, setSelectedUser] = useState<ManagedUser | null>(null);
  const [detailsUser, setDetailsUser] = useState<AdminUserDetails | null>(null);
  const [assignUser, setAssignUser] = useState<ManagedUser | null>(null);
  const [organizations, setOrganizations] = useState<OrganizationOverview[]>([]);
  const [assignRoleValue, setAssignRoleValue] = useState<UserRole>("Student");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  async function loadUsers(role = roleFilter, showPageError = true) {
    setLoading(true);
    if (showPageError) setError("");
    try {
      setUsers(await adminService.getUsers(role || undefined));
      setError("");
    } catch (loadError) {
      if (showPageError) {
        setUsers([]);
        setError(getApiErrorMessage(loadError, "Could not load users from the API."));
      }
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadUsers("");
    organizationService.getAll().then(setOrganizations).catch(() => setOrganizations([]));
  }, []);

  async function assignRole(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const userId = String(form.get("userId"));
    const role = String(form.get("role")) as UserRole;
    const backendRole = backendRoleNames[role] ?? role;
    const organizationId = String(form.get("organizationId") ?? "");
    const userAlreadyHasRole = assignUser?.role === role;
    const shouldAssignOrganization = Boolean(organizationId) && (role === "OrganizationAdmin" || role === "Instructor");

    try {
      if (userAlreadyHasRole && shouldAssignOrganization) {
        if (role === "OrganizationAdmin") {
          await organizationService.assignAdmin(organizationId, userId);
        }

        if (role === "Instructor") {
          await organizationService.assignInstructor(organizationId, userId);
        }

        showToast({
          title: "Organization assignment updated",
          message: "Organization assignment updated successfully",
          tone: "success"
        });
        event.currentTarget.reset();
        setAssignUser(null);
        loadUsers(roleFilter, false).catch(() => undefined);
        return;
      }

      if (!userAlreadyHasRole) {
        await adminService.addUserToRole(userId, backendRole);
      }

      if (role === "OrganizationAdmin" && organizationId) {
        await organizationService.assignAdmin(organizationId, userId);
      }

      if (role === "Instructor" && organizationId) {
        await organizationService.assignInstructor(organizationId, userId);
      }

      showToast({
        title: shouldAssignOrganization ? "Organization assignment updated" : "User role updated",
        message: shouldAssignOrganization ? "Organization assignment updated successfully" : `${userId} assigned to ${role}.`,
        tone: "success"
      });
      event.currentTarget.reset();
      setAssignUser(null);
      loadUsers(roleFilter, false).catch(() => undefined);
    } catch (error) {
      showToast({ title: "Assign role failed", message: getErrorMessage(error), tone: "error" });
    }
  }

  return (
    <AppShell>
      <AuthGuard roles={["Admin"]}>
        <PageHeader eyebrow="Admin" title="Users and roles" description="Manage role names, assign roles, and review users from real backend data." />

        <section className="mt-8">
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
                  <table className="w-full min-w-[860px] text-left">
                    <thead>
                      <tr className="border-b border-slate-100 text-sm text-muted">
                        <th className="px-4 py-3 font-semibold">Name</th>
                        <th className="px-4 py-3 font-semibold">Email</th>
                        <th className="px-4 py-3 font-semibold">Role</th>
                        <th className="px-4 py-3 font-semibold">Organization</th>
                        <th className="px-4 py-3 font-semibold">User id</th>
                        <th className="px-4 py-3 font-semibold">Actions</th>
                      </tr>
                    </thead>
                    <tbody>
                      {users.map((user) => (
                        <tr key={`${user.email}-${user.id}`} className="border-b border-slate-100 last:border-0">
                          <td className="px-4 py-4">
                            <div className="flex items-center gap-3">
                              <SmartImage src={user.profilePicture} fallbackSrc={SEED_IMAGES.profile} alt="" className="size-10 rounded-xl object-cover" />
                              <span className="text-sm font-semibold text-ink">{user.fullName}</span>
                            </div>
                          </td>
                          <td className="px-4 py-4 text-sm text-muted">{user.email}</td>
                          <td className="px-4 py-4"><Badge>{user.role}</Badge></td>
                          <td className="px-4 py-4 text-sm font-semibold text-muted">{user.organizationName ?? "EduVerseOrganization"}</td>
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
                              <button onClick={() => {
                                setAssignUser(user);
                                setAssignRoleValue(user.role);
                              }} className="text-sm font-bold text-amber-500">
                                {user.role === "OrganizationAdmin" ? "Change Organization" : "Assign Role"}
                              </button>
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
                  <Info label="Organization" value={detailsUser.organizationName ?? "EduVerseOrganization"} />
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
              <select
                name="role"
                value={assignRoleValue}
                onChange={(event) => setAssignRoleValue(event.target.value as UserRole)}
                className="mt-5 h-12 w-full rounded-xl bg-slate-50 px-4 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500"
              >
                {roles.map((role) => <option key={role}>{role}</option>)}
              </select>
              {(assignRoleValue === "OrganizationAdmin" || assignRoleValue === "Instructor") && (
                <select name="organizationId" className="mt-4 h-12 w-full rounded-xl bg-slate-50 px-4 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500" required>
                  <option value="">Select organization</option>
                  {organizations.map((organization) => {
                    const id = organization.organizationId ?? organization.organizationAdminId;
                    return <option key={id} value={id}>{organization.organizationName ?? organization.organizationAdminName}</option>;
                  })}
                </select>
              )}
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

function getErrorMessage(error: unknown) {
  return getApiErrorMessage(error, "Check user id, role name, selected organization, and Admin permissions.");
}
