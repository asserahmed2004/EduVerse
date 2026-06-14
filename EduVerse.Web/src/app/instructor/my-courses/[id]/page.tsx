"use client";

import { ArrowLeft, BookOpen, FileText, Users } from "lucide-react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { Badge, EmptyState, LoadingState, PageHeader, StatCard } from "@/components/ui";
import { courseService } from "@/lib/api";
import type { CourseAdminDetails } from "@/lib/types";

export default function InstructorMyCourseDetailsPage() {
  const params = useParams<{ id: string }>();
  const courseId = params.id;
  const [course, setCourse] = useState<CourseAdminDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!courseId) return;

    courseService.getAdminDetails(courseId)
      .then(setCourse)
      .catch(() => setError("Could not load course details or you may not have access to this course."))
      .finally(() => setLoading(false));
  }, [courseId]);

  return (
    <AppShell>
      <AuthGuard roles={["Instructor"]}>
        <Link href="/instructor/my-courses" className="inline-flex items-center gap-2 text-sm font-bold text-teal-600 hover:text-teal-700">
          <ArrowLeft size={16} />
          Back to My Courses
        </Link>

        {loading ? (
          <div className="mt-8">
            <LoadingState label="Loading course details" />
          </div>
        ) : error || !course ? (
          <div className="mt-8">
            <EmptyState title="Course unavailable" description={error || "Course details were not found."} />
          </div>
        ) : (
          <>
            <PageHeader
              eyebrow="Instructor"
              title={course.title || course.name}
              description={course.description || "Assigned course overview."}
            />

            <div className="mt-8 grid gap-5 md:grid-cols-3">
              <StatCard label="Students" value={`${course.studentsCount}`} icon={Users} accent="amber" />
              <StatCard label="Sessions" value={`${course.sessionsCount}`} icon={BookOpen} />
              <StatCard label="Assignments" value={`${course.assignments.length}`} icon={FileText} accent="coral" />
            </div>

            <article className="mt-8 rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
              <div className="flex flex-col gap-6 lg:flex-row">
                {course.imageUrl ? (
                  <div className="h-48 w-full shrink-0 overflow-hidden rounded-xl bg-slate-100 lg:h-56 lg:w-72">
                    <img src={course.imageUrl} alt={course.title || course.name} className="size-full object-cover" />
                  </div>
                ) : null}
                <div className="flex-1">
                  <div className="flex flex-wrap gap-2">
                    {course.category ? <Badge>{course.category}</Badge> : null}
                    {course.instructorName ? <Badge tone="teal">Instructor: {course.instructorName}</Badge> : null}
                  </div>
                  <h2 className="mt-4 text-2xl font-bold text-ink">{course.title || course.name}</h2>
                  <p className="mt-2 text-sm font-semibold text-muted">Organization: {course.organizationName ?? "EduVerseOrganization"}</p>
                  <p className="mt-4 text-sm leading-7 text-muted">{course.description}</p>
                  <div className="mt-6 grid gap-3 sm:grid-cols-3">
                    <InfoBlock label="Students" value={`${course.studentsCount}`} />
                    <InfoBlock label="Sessions" value={`${course.sessionsCount}`} />
                    <InfoBlock label="Assignments" value={`${course.assignments.length}`} />
                  </div>
                </div>
              </div>
            </article>
          </>
        )}
      </AuthGuard>
    </AppShell>
  );
}

function InfoBlock({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl bg-slate-50 p-4">
      <p className="text-xs font-bold uppercase text-muted">{label}</p>
      <p className="mt-1 text-lg font-bold text-ink">{value}</p>
    </div>
  );
}
