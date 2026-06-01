"use client";

import { BookOpen, CreditCard, GraduationCap, Star, UserPlus, Wallet } from "lucide-react";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { Badge, EmptyState, LoadingState, PageHeader, StatCard } from "@/components/ui";
import { dashboardService } from "@/lib/api";
import type { OrganizationDetails } from "@/lib/types";
import { formatCurrency, formatDate } from "@/lib/utils";

export default function OrganizationDetailsPage() {
  const params = useParams<{ id: string }>();
  const [details, setDetails] = useState<OrganizationDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    dashboardService.getOrganizationDetails(params.id)
      .then(setDetails)
      .catch(() => setError("Could not load organization details from the API."))
      .finally(() => setLoading(false));
  }, [params.id]);

  return (
    <AppShell>
      <AuthGuard roles={["Admin"]}>
        {loading ? (
          <LoadingState label="Loading organization details" />
        ) : error || !details ? (
          <EmptyState title="Organization unavailable" description={error || "Organization details are not available."} />
        ) : (
          <>
            <PageHeader eyebrow="Organization details" title={details.organizationAdminName} description={details.email} />

            <div className="mt-8 grid gap-5 md:grid-cols-4">
              <StatCard label="Courses" value={`${details.coursesCount}`} icon={BookOpen} />
              <StatCard label="Students" value={`${details.studentsCount}`} icon={GraduationCap} accent="amber" />
              <StatCard label="Revenue" value={formatCurrency(details.revenue)} icon={Wallet} />
              <StatCard label="Average rating" value={details.averageRating.toFixed(1)} icon={Star} accent="coral" />
            </div>

            <section className="mt-8 rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
              <div className="flex items-center justify-between">
                <h2 className="text-xl font-bold text-ink">Courses</h2>
                <Badge tone={details.coursesCount > 0 ? "teal" : "slate"}>{details.coursesCount > 0 ? "Active" : "No Courses"}</Badge>
              </div>

              {details.courses.length === 0 ? (
                <div className="mt-5">
                  <EmptyState title="No courses" description="This organization admin does not own active courses yet." />
                </div>
              ) : (
                <div className="mt-5 overflow-x-auto">
                  <table className="w-full min-w-[760px] text-left">
                    <thead>
                      <tr className="border-b border-slate-100 text-sm text-muted">
                        <th className="px-4 py-3 font-semibold">Course</th>
                        <th className="px-4 py-3 font-semibold">Price</th>
                        <th className="px-4 py-3 font-semibold">Students</th>
                        <th className="px-4 py-3 font-semibold">Sessions</th>
                        <th className="px-4 py-3 font-semibold">Rating</th>
                      </tr>
                    </thead>
                    <tbody>
                      {details.courses.map((course) => (
                        <tr key={course.courseId} className="border-b border-slate-100 last:border-0">
                          <td className="px-4 py-4">
                            <p className="text-sm font-bold text-ink">{course.name}</p>
                            <p className="mt-1 text-xs text-muted">{course.title}</p>
                          </td>
                          <td className="px-4 py-4 text-sm text-ink">{formatCurrency(course.price)}</td>
                          <td className="px-4 py-4 text-sm text-ink">{course.studentsCount}</td>
                          <td className="px-4 py-4 text-sm text-ink">{course.sessionsCount}</td>
                          <td className="px-4 py-4 text-sm text-muted">{course.averageRating.toFixed(1)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </section>

            <section className="mt-8 grid gap-6 xl:grid-cols-2">
              <Panel title="Recent enrollments" icon={UserPlus}>
                {details.recentEnrollments?.length ? details.recentEnrollments.map((item) => (
                  <Row key={`${item.courseId}-${item.studentId}-${item.enrollmentDate}`} title={item.studentName || item.studentEmail || item.studentId} meta={`${item.courseName} - ${formatDate(item.enrollmentDate)}`} value={`${Math.round(item.progression)}%`} />
                )) : <Muted>No recent enrollments yet</Muted>}
              </Panel>

              <Panel title="Recent payments" icon={CreditCard}>
                {details.recentPayments?.length ? details.recentPayments.map((item) => (
                  <Row key={`${item.courseId}-${item.studentId}-${item.submittingDate}`} title={item.courseName ?? item.courseId} meta={`${item.studentEmail ?? item.studentId} - ${item.paymentStatus}`} value={formatCurrency(item.totalPrice)} />
                )) : <Muted>No recent payments yet</Muted>}
              </Panel>
            </section>
          </>
        )}
      </AuthGuard>
    </AppShell>
  );
}

function Panel({ title, icon: Icon, children }: { title: string; icon: any; children: React.ReactNode }) {
  return (
    <div className="rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-bold text-ink">{title}</h2>
        <div className="grid size-10 place-items-center rounded-xl bg-teal-50 text-teal-600">
          <Icon size={18} />
        </div>
      </div>
      <div className="mt-5 space-y-3">{children}</div>
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
