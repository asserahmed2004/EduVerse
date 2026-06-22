"use client";

import { BookOpen, FileText, Users } from "lucide-react";
import Link from "next/link";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { CourseImage } from "@/components/smart-image";
import { EmptyState, LoadingState, PageHeader, StatCard } from "@/components/ui";
import { instructorService } from "@/lib/api";
import type { InstructorCourse } from "@/lib/types";

export default function InstructorMyCoursesPage() {
  const [courses, setCourses] = useState<InstructorCourse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    instructorService.getMyCourses()
      .then(setCourses)
      .catch(() => setError("Could not load your assigned courses from the API."))
      .finally(() => setLoading(false));
  }, []);

  return (
    <AppShell>
      <AuthGuard roles={["Instructor"]}>
        <PageHeader eyebrow="Instructor" title="My Courses" description="Courses assigned to you for teaching." />
        <div className="mt-8 grid gap-5 md:grid-cols-3">
          <StatCard label="Assigned courses" value={`${courses.length}`} icon={BookOpen} />
          <StatCard label="Total students" value={`${courses.reduce((sum, course) => sum + (course.studentsCount ?? 0), 0)}`} icon={Users} accent="amber" />
          <StatCard label="Total assignments" value={`${courses.reduce((sum, course) => sum + course.assignmentsCount, 0)}`} icon={FileText} accent="coral" />
        </div>

        <section className="mt-8">
          {loading ? (
            <LoadingState label="Loading your courses" />
          ) : error ? (
            <EmptyState title="Courses unavailable" description={error} />
          ) : courses.length === 0 ? (
            <EmptyState title="No assigned courses" description="Courses will appear here once you are assigned as an instructor." />
          ) : (
            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
              {courses.map((course) => (
                <Link
                  key={course.courseId}
                  href={`/instructor/my-courses/${course.courseId}`}
                  className="group rounded-xl2 bg-white p-5 shadow-soft ring-1 ring-slate-100 transition hover:-translate-y-0.5 hover:shadow-lg"
                >
                  <CourseImage
                    course={course}
                    alt={course.title || course.name}
                    className="mb-5 h-36 w-full rounded-xl object-cover"
                  />
                  <h2 className="text-lg font-bold text-ink group-hover:text-teal-600">{course.title || course.name}</h2>
                  <p className="mt-1 text-sm font-semibold text-muted">{course.organizationName ?? "EduVerseOrganization"}</p>
                  <div className="mt-4 grid gap-2 text-sm font-semibold text-muted">
                    <p>{course.studentsCount ?? 0} students</p>
                    <p>{course.sessionsCount ?? 0} sessions</p>
                    <p>{course.assignmentsCount} assignments</p>
                  </div>
                </Link>
              ))}
            </div>
          )}
        </section>
      </AuthGuard>
    </AppShell>
  );
}
