"use client";

import { BookOpen, CreditCard, ShieldCheck, Trash2, UserPlus, Users, Wallet } from "lucide-react";
import { FormEvent, useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { useToast } from "@/components/toast-provider";
import { Badge, Button, EmptyState, LoadingState, PageHeader, StatCard } from "@/components/ui";
import { adminService, courseService, dashboardService, paymentService } from "@/lib/api";
import type { Course, DashboardStats, ManagedUser, Payment, UserRole } from "@/lib/types";
import { formatCurrency, formatDate } from "@/lib/utils";

const roles: UserRole[] = ["Student", "Instructor", "OrganizationAdmin", "Admin"];

export default function AdminPage() {
  const { showToast } = useToast();
  const [users, setUsers] = useState<ManagedUser[]>([]);
  const [roleFilter, setRoleFilter] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [payments, setPayments] = useState<Payment[]>([]);
  const [topCourses, setTopCourses] = useState<Course[]>([]);

  async function loadUsers(role = roleFilter) {
    setLoading(true);
    setError("");
    try {
      const data = await adminService.getUsers(role || undefined);
      setUsers(data);
      const [statsData, paymentData, courseData] = await Promise.all([
        dashboardService.getOrganizationOverview(),
        paymentService.getAdminTransactions(1, 5),
        courseService.getAll()
      ]);
      setStats({
        ...statsData,
        totalUsers: data.length,
        totalOrganizations: data.filter((user) => user.role === "OrganizationAdmin").length
      });
      setPayments(paymentData.items ?? []);
      setTopCourses([...courseData].sort((a, b) => (b.rating ?? 0) - (a.rating ?? 0)).slice(0, 5));
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
      await loadUsers();
    } catch {
      showToast({ title: "Assign role failed", message: "Check user id, role name, and Admin permissions.", tone: "error" });
    }
  }

  return (
    <AppShell>
      <AuthGuard roles={["Admin"]}>
        <PageHeader eyebrow="Admin" title="Platform management" description="Manage roles and users while keeping the backend authorization model intact." />

        <div className="mt-8 grid gap-5 md:grid-cols-2 xl:grid-cols-4">
          <StatCard label="Total users" value={`${stats?.totalUsers ?? users.length}`} icon={Users} />
          <StatCard label="Organizations" value={`${stats?.totalOrganizations ?? 0}`} icon={ShieldCheck} accent="amber" />
          <StatCard label="Total courses" value={`${stats?.totalCourses ?? 0}`} icon={BookOpen} />
          <StatCard label="Deleted courses" value={`${stats?.deletedCourses ?? 0}`} icon={Trash2} accent="coral" />
          <StatCard label="Total revenue" value={formatCurrency(stats?.totalRevenue ?? 0)} icon={Wallet} />
          <StatCard label="Total payments" value={`${stats?.totalPayments ?? 0}`} icon={CreditCard} accent="amber" />
          <StatCard label="Students" value={`${stats?.totalStudents ?? 0}`} icon={Users} />
          <StatCard label="Instructors" value={`${stats?.totalInstructors ?? 0}`} icon={UserPlus} accent="coral" />
        </div>

        <section className="mt-8 grid gap-6 xl:grid-cols-2">
          <div className="rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
            <h2 className="text-xl font-bold text-ink">Recent payments</h2>
            <div className="mt-4 space-y-3">
              {payments.length === 0 ? <p className="text-sm text-muted">No data yet</p> : payments.map((payment) => (
                <div key={`${payment.courseId}-${payment.studentId}-${payment.submittingDate}`} className="flex items-center justify-between rounded-xl bg-slate-50 p-4">
                  <div>
                    <p className="font-semibold text-ink">{payment.courseName ?? payment.courseId}</p>
                    <p className="text-xs text-muted">{payment.studentEmail ?? payment.studentId} - {formatDate(payment.submittingDate)}</p>
                  </div>
                  <p className="font-bold text-ink">{formatCurrency(payment.totalPrice)}</p>
                </div>
              ))}
            </div>
          </div>
          <div className="rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
            <h2 className="text-xl font-bold text-ink">Top courses</h2>
            <div className="mt-4 space-y-3">
              {topCourses.length === 0 ? <p className="text-sm text-muted">No data yet</p> : topCourses.map((course) => (
                <div key={course.id} className="flex items-center justify-between rounded-xl bg-slate-50 p-4">
                  <div>
                    <p className="font-semibold text-ink">{course.title}</p>
                    <p className="text-xs text-muted">{course.studentsCount ?? 0} students - {course.sessionsCount ?? 0} sessions</p>
                  </div>
                  <Badge>{course.rating.toFixed(1)}</Badge>
                </div>
              ))}
            </div>
          </div>
        </section>

        <section className="mt-8 grid gap-8 xl:grid-cols-[360px_1fr]">
          <div className="space-y-6">
            <form onSubmit={addRole} className="rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
              <h2 className="text-lg font-bold text-ink">Add role</h2>
              <p className="mt-1 text-sm text-muted">Uses POST /Auth/AddRole/role</p>
              <input name="role" placeholder="Role name" className="mt-5 h-12 w-full rounded-xl bg-slate-50 px-4 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500" required />
              <Button className="mt-4 w-full">Add role</Button>
            </form>

            <form onSubmit={assignRole} className="rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
              <h2 className="text-lg font-bold text-ink">Assign role</h2>
              <p className="mt-1 text-sm text-muted">Uses POST /Auth/AddUserToRole/userId/role</p>
              <input name="userId" placeholder="User id" className="mt-5 h-12 w-full rounded-xl bg-slate-50 px-4 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500" required />
              <select name="role" className="mt-3 h-12 w-full rounded-xl bg-slate-50 px-4 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500">
                {roles.map((role) => <option key={role}>{role}</option>)}
              </select>
              <Button className="mt-4 w-full">Assign role</Button>
            </form>
          </div>

          <div className="rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
            <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
              <div>
                <h2 className="text-xl font-bold text-ink">Users</h2>
                <p className="mt-1 text-sm text-muted">Real API data only.</p>
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
                      </tr>
                    </thead>
                    <tbody>
                      {users.map((user) => (
                        <tr key={`${user.email}-${user.id}`} className="border-b border-slate-100 last:border-0">
                          <td className="px-4 py-4 text-sm font-semibold text-ink">{user.fullName}</td>
                          <td className="px-4 py-4 text-sm text-muted">{user.email}</td>
                          <td className="px-4 py-4"><Badge>{user.role}</Badge></td>
                          <td className="px-4 py-4 text-xs text-muted">{user.id ?? "-"}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>
        </section>
      </AuthGuard>
    </AppShell>
  );
}
