"use client";

import { Users } from "lucide-react";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { EmptyState, LoadingState, PageHeader, ProgressBar, StatCard } from "@/components/ui";
import { instructorService } from "@/lib/api";
import type { InstructorStudent } from "@/lib/types";
import { formatDate } from "@/lib/utils";

export default function InstructorStudentsPage() {
  const [students, setStudents] = useState<InstructorStudent[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    instructorService.getStudents()
      .then(setStudents)
      .catch(() => setError("Could not load assigned students from the API."))
      .finally(() => setLoading(false));
  }, []);

  return (
    <AppShell>
      <AuthGuard roles={["Instructor"]}>
        <PageHeader eyebrow="Instructor" title="Students" description="Students enrolled in your assigned courses." />
        <div className="mt-8 grid gap-5 md:grid-cols-3">
          <StatCard label="My students" value={`${new Set(students.map((student) => student.studentId)).size}`} icon={Users} accent="amber" />
        </div>

        <section className="mt-8">
          {loading ? (
            <LoadingState label="Loading students" />
          ) : error ? (
            <EmptyState title="Students unavailable" description={error} />
          ) : students.length === 0 ? (
            <EmptyState title="No students yet" description="Students will appear here after they enroll in your assigned courses." />
          ) : (
            <div className="grid gap-4">
              {students.map((student) => (
                <article key={`${student.studentId}-${student.courseId}`} className="rounded-xl2 bg-white p-5 shadow-soft ring-1 ring-slate-100">
                  <div className="flex flex-col justify-between gap-4 md:flex-row md:items-center">
                    <div>
                      <h2 className="text-lg font-bold text-ink">{student.studentName || student.studentEmail}</h2>
                      <p className="mt-1 text-sm text-muted">{student.studentEmail}</p>
                      <p className="mt-1 text-sm font-semibold text-teal-600">{student.courseName}</p>
                      <p className="mt-1 text-xs font-semibold text-muted">Enrolled {formatDate(student.enrollmentDate)}</p>
                    </div>
                    <div className="min-w-48">
                      <div className="flex items-center justify-between text-sm">
                        <span className="font-semibold text-muted">Progress</span>
                        <span className="font-bold text-teal-600">{Math.round(student.progressPercentage)}%</span>
                      </div>
                      <div className="mt-2">
                        <ProgressBar value={student.progressPercentage} />
                      </div>
                      <p className="mt-2 text-xs font-semibold text-muted">Course progress based on completed sessions</p>
                    </div>
                  </div>
                </article>
              ))}
            </div>
          )}
        </section>
      </AuthGuard>
    </AppShell>
  );
}
