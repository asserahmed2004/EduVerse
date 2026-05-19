"use client";

import { BookCheck, CalendarDays, TrendingUp } from "lucide-react";
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
          <StatCard label="Average progress" value={`${Math.round(enrollments.reduce((sum, item) => sum + item.progression, 0) / Math.max(enrollments.length, 1))}%`} icon={TrendingUp} accent="amber" />
          <StatCard label="Graduated" value={`${enrollments.filter((item) => item.graduationDate).length}`} icon={CalendarDays} accent="coral" />
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
                  <p className="text-lg font-bold text-teal-600">{item.progression}%</p>
                </div>
                <div className="mt-4">
                  <ProgressBar value={item.progression} />
                </div>
              </article>
            ))
          )}
        </section>
      </AuthGuard>
    </AppShell>
  );
}
