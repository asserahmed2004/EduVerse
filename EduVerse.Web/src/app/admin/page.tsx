"use client";

import { Activity, Award, BookOpen, CreditCard, FileText, GraduationCap, ShieldCheck, Star, Trash2, Users, Wallet } from "lucide-react";
import Link from "next/link";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { EmptyState, LoadingState, PageHeader, StatCard } from "@/components/ui";
import { dashboardService } from "@/lib/api";
import type { DashboardStats, Payment, RecentActivity, RecentCourse, RecentEnrollment, RoleCount, TopCourse, TopCourseChart, TopInstructor, TopOrganization, TrendPoint } from "@/lib/types";
import { formatCurrency, formatDate } from "@/lib/utils";

export default function AdminPage() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [recentEnrollments, setRecentEnrollments] = useState<RecentEnrollment[]>([]);
  const [recentPayments, setRecentPayments] = useState<Payment[]>([]);
  const [recentCourses, setRecentCourses] = useState<RecentCourse[]>([]);
  const [topCourses, setTopCourses] = useState<TopCourse[]>([]);
  const [topOrganizations, setTopOrganizations] = useState<TopOrganization[]>([]);
  const [topInstructors, setTopInstructors] = useState<TopInstructor[]>([]);
  const [activities, setActivities] = useState<RecentActivity[]>([]);
  const [revenueTrend, setRevenueTrend] = useState<TrendPoint[]>([]);
  const [enrollmentsTrend, setEnrollmentsTrend] = useState<TrendPoint[]>([]);
  const [usersByRole, setUsersByRole] = useState<RoleCount[]>([]);
  const [topCoursesChart, setTopCoursesChart] = useState<TopCourseChart[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [detailTitle, setDetailTitle] = useState("");
  const [detailRows, setDetailRows] = useState<{ title: string; meta: string; value?: string }[]>([]);
  const [detailLoading, setDetailLoading] = useState(false);

  useEffect(() => {
    Promise.all([
      dashboardService.getOrganizationOverview(),
      dashboardService.getRecentEnrollments(),
      dashboardService.getRecentPayments(),
      dashboardService.getRecentCourses(),
      dashboardService.getTopCourses(),
      dashboardService.getTopOrganizations(),
      dashboardService.getTopInstructors(),
      dashboardService.getRecentActivities(),
      dashboardService.getRevenueTrend(30),
      dashboardService.getEnrollmentsTrend(30),
      dashboardService.getUsersByRole(),
      dashboardService.getTopCoursesChart()
    ])
      .then(([statsData, enrollmentsData, paymentsData, coursesData, topCoursesData, topOrganizationsData, topInstructorsData, activitiesData, revenueTrendData, enrollmentsTrendData, usersByRoleData, topCoursesChartData]) => {
        setStats(statsData);
        setRecentEnrollments(enrollmentsData);
        setRecentPayments(paymentsData);
        setRecentCourses(coursesData);
        setTopCourses(topCoursesData);
        setTopOrganizations(topOrganizationsData);
        setTopInstructors(topInstructorsData);
        setActivities(activitiesData);
        setRevenueTrend(revenueTrendData);
        setEnrollmentsTrend(enrollmentsTrendData);
        setUsersByRole(usersByRoleData);
        setTopCoursesChart(topCoursesChartData);
      })
      .catch(() => setError("Could not load admin dashboard data from the API."))
      .finally(() => setLoading(false));
  }, []);

  async function openDetail(kind: "students" | "instructors" | "enrollments" | "sessions" | "assignments" | "rating") {
    setDetailTitle(kind[0].toUpperCase() + kind.slice(1));
    setDetailRows([]);
    setDetailLoading(true);
    try {
      if (kind === "students") {
        const rows = await dashboardService.getAdminStudents();
        setDetailRows(rows.map((item) => ({ title: item.fullName || item.email, meta: item.email, value: item.enrollmentsCount ? `${item.enrollmentsCount} enrollments` : item.role })));
      } else if (kind === "instructors") {
        const rows = await dashboardService.getAdminInstructors();
        setDetailRows(rows.map((item) => ({ title: item.fullName || item.email, meta: item.email, value: item.sessionsCount ? `${item.sessionsCount} sessions` : item.role })));
      } else if (kind === "enrollments") {
        setDetailRows(recentEnrollments.map((item) => ({ title: item.studentName || item.studentEmail || item.studentId, meta: item.courseName, value: formatDate(item.enrollmentDate) })));
      } else if (kind === "sessions") {
        const rows = await dashboardService.getRecentSessions();
        setDetailRows(rows.map((item) => ({ title: item.title, meta: `${item.courseName} - ${item.instructorName || "Unassigned"}`, value: `#${item.sessionNumber}` })));
      } else if (kind === "assignments") {
        const rows = await dashboardService.getRecentAssignments();
        setDetailRows(rows.map((item) => ({ title: item.subject, meta: item.courseName || item.description, value: "Assignment" })));
      } else {
        const rows = await dashboardService.getTopRatedCourses();
        setDetailRows(rows.map((item) => ({ title: item.title || item.courseName, meta: `${item.studentsCount} students`, value: item.averageRating.toFixed(1) })));
      }
    } finally {
      setDetailLoading(false);
    }
  }

  return (
    <AppShell>
      <AuthGuard roles={["Admin"]}>
        <PageHeader eyebrow="Admin control center" title="Platform overview" description="A global view of EduVerse users, organizations, courses, payments, and learning activity." />

        <div className="mt-8 grid gap-5 md:grid-cols-2 xl:grid-cols-4">
          <LinkedStat href="/admin/users"><StatCard label="Total users" value={`${stats?.totalUsers ?? 0}`} icon={Users} /></LinkedStat>
          <LinkedStat href="/organizations"><StatCard label="Total organizations" value={`${stats?.totalOrganizations ?? 0}`} icon={ShieldCheck} accent="amber" /></LinkedStat>
          <LinkedStat href="/courses"><StatCard label="Total courses" value={`${stats?.totalCourses ?? 0}`} icon={BookOpen} /></LinkedStat>
          <LinkedStat href="/admin/deleted-courses"><StatCard label="Deleted courses" value={`${stats?.deletedCourses ?? 0}`} icon={Trash2} accent="coral" /></LinkedStat>
          <LinkedStat href="/payments"><StatCard label="Total revenue" value={formatCurrency(stats?.totalRevenue ?? 0)} icon={Wallet} /></LinkedStat>
          <LinkedStat href="/payments"><StatCard label="Total payments" value={`${stats?.totalPayments ?? 0}`} icon={CreditCard} accent="amber" /></LinkedStat>
          <DetailStat onClick={() => openDetail("students")}><StatCard label="Total students" value={`${stats?.totalStudents ?? 0}`} icon={GraduationCap} /></DetailStat>
          <DetailStat onClick={() => openDetail("instructors")}><StatCard label="Total instructors" value={`${stats?.totalInstructors ?? 0}`} icon={Users} accent="coral" /></DetailStat>
          <DetailStat onClick={() => openDetail("enrollments")}><StatCard label="Total enrollments" value={`${stats?.totalEnrollments ?? 0}`} icon={Award} /></DetailStat>
          <DetailStat onClick={() => openDetail("sessions")}><StatCard label="Total sessions" value={`${stats?.totalSessions ?? 0}`} icon={BookOpen} accent="amber" /></DetailStat>
          <DetailStat onClick={() => openDetail("assignments")}><StatCard label="Total assignments" value={`${stats?.totalAssignments ?? 0}`} icon={FileText} /></DetailStat>
          <DetailStat onClick={() => openDetail("rating")}><StatCard label="Average rating" value={`${(stats?.averageRating ?? 0).toFixed(1)}`} icon={Star} accent="coral" /></DetailStat>
        </div>

        {loading ? (
          <div className="mt-8">
            <LoadingState label="Loading admin activity" />
          </div>
        ) : error ? (
          <div className="mt-8">
            <EmptyState title="Admin data unavailable" description={error} />
          </div>
        ) : (
          <>
            <section className="mt-8 grid gap-6 xl:grid-cols-2">
              <ChartPanel title="Revenue trend" subtitle="Last 30 days">
                <SparkBars data={revenueTrend} formatter={formatCurrency} />
              </ChartPanel>
              <ChartPanel title="Enrollments trend" subtitle="Last 30 days">
                <SparkBars data={enrollmentsTrend} />
              </ChartPanel>
              <ChartPanel title="Users by role" subtitle="Current identity roles">
                <RoleBars data={usersByRole} />
              </ChartPanel>
              <ChartPanel title="Top courses chart" subtitle="Ranked by enrollments and revenue">
                <TopCourseBars data={topCoursesChart} />
              </ChartPanel>
            </section>

            <section className="mt-8 grid gap-6 xl:grid-cols-3">
              <Panel title="Recent enrollments">
                {recentEnrollments.length === 0 ? <Muted>No data yet</Muted> : recentEnrollments.map((item) => (
                  <Row key={`${item.courseId}-${item.studentId}-${item.enrollmentDate}`} title={item.studentName || item.studentEmail || item.studentId} meta={`${item.courseName} - ${formatDate(item.enrollmentDate)}`} value={`${Math.round(item.progression)}%`} />
                ))}
              </Panel>
              <Panel title="Recent payments">
                {recentPayments.length === 0 ? <Muted>No data yet</Muted> : recentPayments.map((item) => (
                  <Row key={`${item.courseId}-${item.studentId}-${item.submittingDate}`} title={item.courseName ?? "Unknown course"} meta={`${item.studentEmail ?? item.studentId} - ${item.paymentStatus}`} value={formatCurrency(item.totalPrice)} />
                ))}
              </Panel>
              <Panel title="Recent created courses">
                {recentCourses.length === 0 ? <Muted>No data yet</Muted> : recentCourses.map((item) => (
                  <Row key={item.courseId} title={item.title || item.courseName} meta={item.organizationAdminName || "No organization owner"} value={formatCurrency(item.price)} />
                ))}
              </Panel>
            </section>

            <section className="mt-8 grid gap-6 xl:grid-cols-3">
              <Panel title="Top courses">
                {topCourses.length === 0 ? <Muted>No data yet</Muted> : topCourses.map((item) => (
                  <Row key={item.courseId} title={item.title || item.courseName} meta={`${item.studentsCount} students - ${item.sessionsCount} sessions`} value={item.averageRating.toFixed(1)} />
                ))}
              </Panel>
              <Panel title="Top organizations">
                {topOrganizations.length === 0 ? <Muted>No data yet</Muted> : topOrganizations.map((item) => (
                  <Row key={item.organizationAdminId} title={item.organizationAdminName} meta={`${item.coursesCount} courses - ${item.enrollmentsCount} enrollments`} value={formatCurrency(item.revenue)} />
                ))}
              </Panel>
              <Panel title="Top instructors">
                {topInstructors.length === 0 ? <Muted>No data yet</Muted> : topInstructors.map((item) => (
                  <Row key={item.instructorId} title={item.instructorName || item.email} meta={`${item.coursesCount} courses - ${item.studentsCount} students`} value={`${item.sessionsCount} sessions`} />
                ))}
              </Panel>
            </section>

            <section className="mt-8 rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
              <h2 className="text-lg font-bold text-ink">Activity feed</h2>
              <div className="mt-5 space-y-4">
                {activities.length === 0 ? <Muted>No activity yet</Muted> : activities.map((item, index) => (
                  <div key={`${item.type}-${item.createdAt}-${index}`} className="flex gap-4 rounded-xl bg-slate-50 p-4">
                    <div className="grid size-10 shrink-0 place-items-center rounded-xl bg-teal-50 text-teal-600">
                      <Activity size={18} />
                    </div>
                    <div className="min-w-0">
                      <p className="font-bold text-ink">{item.title}</p>
                      <p className="mt-1 text-sm text-muted">{item.description}</p>
                      <p className="mt-2 text-xs font-semibold text-muted">{formatDate(item.createdAt)}</p>
                    </div>
                  </div>
                ))}
              </div>
            </section>
          </>
        )}

        {detailTitle && (
          <div className="fixed inset-0 z-50 grid place-items-center bg-ink/50 p-4" onClick={() => setDetailTitle("")}>
            <div className="w-full max-w-2xl rounded-xl2 bg-white p-6 shadow-xl ring-1 ring-slate-100" onClick={(event) => event.stopPropagation()}>
              <div className="flex items-start justify-between gap-4">
                <div>
                  <p className="text-sm font-bold uppercase text-teal-600">Quick details</p>
                  <h2 className="mt-2 text-2xl font-black text-ink">{detailTitle}</h2>
                </div>
                <button onClick={() => setDetailTitle("")} className="rounded-xl bg-slate-50 px-3 py-2 text-sm font-bold text-muted">Close</button>
              </div>
              <div className="mt-5 space-y-3">
                {detailLoading ? <LoadingState label="Loading details" /> : detailRows.length === 0 ? <EmptyState title="No data yet" description="No records are available for this quick view." /> : detailRows.map((item, index) => (
                  <Row key={`${item.title}-${index}`} title={item.title} meta={item.meta} value={item.value ?? "View"} />
                ))}
              </div>
            </div>
          </div>
        )}
      </AuthGuard>
    </AppShell>
  );
}

function LinkedStat({ href, children }: { href: string; children: React.ReactNode }) {
  return (
    <Link href={href} className="group block cursor-pointer">
      {children}
      <p className="mt-2 px-1 text-xs font-bold text-teal-600 opacity-80 transition group-hover:opacity-100">View Details</p>
    </Link>
  );
}

function DetailStat({ onClick, children }: { onClick: () => void; children: React.ReactNode }) {
  return (
    <button onClick={onClick} className="group block cursor-pointer text-left">
      {children}
      <p className="mt-2 px-1 text-xs font-bold text-teal-600 opacity-80 transition group-hover:opacity-100">View Details</p>
    </button>
  );
}

function Panel({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
      <h2 className="text-lg font-bold text-ink">{title}</h2>
      <div className="mt-4 space-y-3">{children}</div>
    </div>
  );
}

function ChartPanel({ title, subtitle, children }: { title: string; subtitle: string; children: React.ReactNode }) {
  return (
    <div className="rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h2 className="text-lg font-bold text-ink">{title}</h2>
          <p className="mt-1 text-sm text-muted">{subtitle}</p>
        </div>
      </div>
      <div className="mt-5">{children}</div>
    </div>
  );
}

function SparkBars({ data, formatter = (value: number) => `${value}` }: { data: TrendPoint[]; formatter?: (value: number) => string }) {
  const max = Math.max(...data.map((item) => item.value), 1);
  const total = data.reduce((sum, item) => sum + item.value, 0);

  if (data.length === 0) return <Muted>No chart data yet</Muted>;

  return (
    <div>
      <p className="text-2xl font-black text-ink">{formatter(total)}</p>
      <div className="mt-5 flex h-32 items-end gap-1">
        {data.map((item) => (
          <div key={`${item.date}-${item.label}`} className="group relative flex-1 rounded-t-lg bg-gradient-to-t from-teal-500 to-indigo-500 transition hover:opacity-80" style={{ height: `${Math.max(5, (item.value / max) * 100)}%` }}>
            <span className="pointer-events-none absolute bottom-full left-1/2 mb-2 hidden -translate-x-1/2 whitespace-nowrap rounded-lg bg-ink px-2 py-1 text-xs font-bold text-white group-hover:block">
              {item.label}: {formatter(item.value)}
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}

function RoleBars({ data }: { data: RoleCount[] }) {
  const max = Math.max(...data.map((item) => item.count), 1);
  if (data.length === 0) return <Muted>No user role data yet</Muted>;

  return (
    <div className="space-y-3">
      {data.map((item) => (
        <div key={item.role}>
          <div className="flex items-center justify-between text-sm font-bold">
            <span className="capitalize text-ink">{item.role}</span>
            <span className="text-teal-600">{item.count}</span>
          </div>
          <div className="mt-2 h-3 overflow-hidden rounded-full bg-slate-100">
            <div className="h-full rounded-full bg-gradient-to-r from-teal-500 to-indigo-500" style={{ width: `${(item.count / max) * 100}%` }} />
          </div>
        </div>
      ))}
    </div>
  );
}

function TopCourseBars({ data }: { data: TopCourseChart[] }) {
  const max = Math.max(...data.map((item) => item.enrollments), 1);
  if (data.length === 0) return <Muted>No top course data yet</Muted>;

  return (
    <div className="space-y-3">
      {data.map((item) => (
        <div key={item.courseId} className="rounded-xl bg-slate-50 p-3">
          <div className="flex items-center justify-between gap-3">
            <p className="truncate text-sm font-bold text-ink">{item.courseName}</p>
            <p className="shrink-0 text-xs font-bold text-teal-600">{item.enrollments} enrollments</p>
          </div>
          <div className="mt-3 h-2 overflow-hidden rounded-full bg-white">
            <div className="h-full rounded-full bg-coral-500" style={{ width: `${(item.enrollments / max) * 100}%` }} />
          </div>
          <p className="mt-2 text-xs font-semibold text-muted">{formatCurrency(item.revenue)} revenue - {item.averageRating.toFixed(1)} rating</p>
        </div>
      ))}
    </div>
  );
}

function Row({ title, meta, value }: { title: string; meta: string; value: string }) {
  return (
    <div className="flex items-center justify-between gap-4 rounded-xl bg-slate-50 p-4">
      <div className="min-w-0">
        <p className="truncate text-sm font-bold text-ink">{title || "Not available"}</p>
        <p className="mt-1 truncate text-xs text-muted">{meta}</p>
      </div>
      <p className="shrink-0 text-sm font-bold text-teal-600">{value}</p>
    </div>
  );
}

function Muted({ children }: { children: React.ReactNode }) {
  return <p className="rounded-xl bg-slate-50 p-4 text-sm font-semibold text-muted">{children}</p>;
}
