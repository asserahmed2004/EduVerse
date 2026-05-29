"use client";

import { RotateCcw, Trash2 } from "lucide-react";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { useToast } from "@/components/toast-provider";
import { Button, CourseCard, EmptyState, LoadingState, PageHeader, StatCard } from "@/components/ui";
import { courseService } from "@/lib/api";
import type { Course } from "@/lib/types";

export default function DeletedCoursesPage() {
  const { showToast } = useToast();
  const [courses, setCourses] = useState<Course[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  async function loadDeletedCourses() {
    setLoading(true);
    setError("");
    try {
      setCourses(await courseService.getDeleted());
    } catch {
      setCourses([]);
      setError("Could not load deleted courses from the API.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadDeletedCourses();
  }, []);

  async function restoreCourse(id: string) {
    try {
      await courseService.restore(id);
      showToast({ title: "Course restored", message: "The course is visible again.", tone: "success" });
      await loadDeletedCourses();
    } catch (error) {
      showToast({ title: "Restore failed", message: error instanceof Error ? error.message : "Could not restore course.", tone: "error" });
    }
  }

  return (
    <AppShell>
      <AuthGuard roles={["Admin"]}>
        <PageHeader eyebrow="Admin" title="Deleted courses" description="Restore soft-deleted courses without losing sessions, enrollments, payments, or ratings." />

        <div className="mt-8 grid gap-5 md:grid-cols-3">
          <StatCard label="Soft deleted" value={`${courses.length}`} icon={Trash2} accent="coral" />
        </div>

        {loading ? (
          <div className="mt-8"><LoadingState label="Loading deleted courses" /></div>
        ) : error ? (
          <div className="mt-8"><EmptyState title="Deleted courses unavailable" description={error} /></div>
        ) : courses.length === 0 ? (
          <div className="mt-8"><EmptyState title="No deleted courses" description="Soft-deleted courses will appear here." /></div>
        ) : (
          <div className="mt-8 grid gap-6 md:grid-cols-2 xl:grid-cols-3">
            {courses.map((course) => (
              <div key={course.id} className="space-y-3">
                <CourseCard course={course} compact />
                <Button className="w-full" onClick={() => restoreCourse(course.id)}>
                  <RotateCcw size={16} />
                  Restore course
                </Button>
              </div>
            ))}
          </div>
        )}
      </AuthGuard>
    </AppShell>
  );
}
