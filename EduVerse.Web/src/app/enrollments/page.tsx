"use client";

import { BookCheck, CalendarDays, CheckCircle2, TrendingUp } from "lucide-react";
import Link from "next/link";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { useToast } from "@/components/toast-provider";
import { EmptyState, LoadingState, PageHeader, ProgressBar, StatCard } from "@/components/ui";
import { studentService } from "@/lib/api";
import type { Enrollment } from "@/lib/types";
import { formatDate } from "@/lib/utils";

export default function EnrollmentsPage() {
  const { showToast } = useToast();
  const [enrollments, setEnrollments] = useState<Enrollment[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    studentService.getEnrollments()
      .then(setEnrollments)
      .catch(() => {
        setError("Could not load enrollments from the API.");
        showToast({ title: "Enrollments unavailable", message: "Could not load real enrollment data from the backend.", tone: "error" });
      })
      .finally(() => setLoading(false));
  }, [showToast]);

  return (
    <AppShell>
      <AuthGuard roles={["Student"]}>
        <PageHeader eyebrow="Enrollments" title="Course enrollments" description="Monitor course dates, progression, and graduation status." />
        <div className="mt-8 grid gap-5 md:grid-cols-3">
          <StatCard label="Active courses" value={`${enrollments.length}`} icon={BookCheck} />
          <StatCard label="Average progress" value={`${Math.round(enrollments.reduce((sum, item) => sum + (item.progressPercentage ?? item.progression), 0) / Math.max(enrollments.length, 1))}%`} icon={TrendingUp} accent="amber" />
          <StatCard label="Completed" value={`${enrollments.filter((item) => item.isCompleted || item.graduationDate).length}`} icon={CalendarDays} accent="coral" />
        </div>

        <section className="mt-8 space-y-4">
          {loading ? (
            <LoadingState label="Loading enrollments" />
          ) : error ? (
            <EmptyState title="Enrollments unavailable" description={error} />
          ) : enrollments.length === 0 ? (
            <EmptyState title="No enrollments" description="Enroll in a course to start tracking progress." />
          ) : (
            enrollments.map((item) => (
              <article key={item.courseId} className="rounded-xl2 bg-white p-5 shadow-soft ring-1 ring-slate-100">
                <div className="flex flex-col justify-between gap-4 md:flex-row md:items-center">
                  <div>
                    <h2 className="text-lg font-bold text-ink">{item.courseName}</h2>
                    <p className="mt-1 text-sm text-muted">Started {formatDate(item.enrollmentDate)}</p>
                  </div>
                  <div className="flex items-center gap-3">
                    {(item.isCompleted || item.graduationDate) && <span className="inline-flex items-center gap-1 rounded-full bg-teal-50 px-3 py-1 text-xs font-bold text-teal-600"><CheckCircle2 size={14} /> Completed</span>}
                    <p className="text-lg font-bold text-teal-600">{Math.round(item.progressPercentage ?? item.progression)}%</p>
                  </div>
                </div>
                <div className="mt-4">
                  <ProgressBar value={item.progressPercentage ?? item.progression} />
                </div>
                <Link href={`/courses/${item.courseId}`} className="mt-4 inline-flex text-sm font-bold text-teal-600 hover:text-teal-700">Open learning page</Link>
              </article>
            ))
          )}
        </section>
      </AuthGuard>
    </AppShell>
  );
}
