"use client";

import { Search } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { CourseCard, EmptyState, LoadingState, PageHeader } from "@/components/ui";
import { courseService } from "@/lib/api";
import type { Course } from "@/lib/types";

export default function CoursesPage() {
  const [courses, setCourses] = useState<Course[]>([]);
  const [query, setQuery] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    courseService.getAll()
      .then(setCourses)
      .catch(() => setError("Could not load courses from the API."))
      .finally(() => setLoading(false));
  }, []);

  const filteredCourses = useMemo(() => {
    return courses.filter((course) => `${course.name} ${course.title} ${course.description}`.toLowerCase().includes(query.toLowerCase()));
  }, [courses, query]);

  return (
    <AppShell>
      <PageHeader eyebrow="Catalog" title="Courses" description="Explore learning paths, compare prices and ratings, then enroll from the course details page." />

      <div className="mt-8 flex items-center gap-3 rounded-xl2 bg-white px-4 py-3 shadow-soft ring-1 ring-slate-100">
        <Search size={20} className="text-muted" />
        <input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search courses" className="w-full bg-transparent text-sm outline-none" />
      </div>

      {loading ? (
        <div className="mt-8">
          <LoadingState label="Loading courses" />
        </div>
      ) : error ? (
        <div className="mt-8">
          <EmptyState title="Courses unavailable" description={error} />
        </div>
      ) : filteredCourses.length === 0 ? (
        <div className="mt-8">
          <EmptyState title="No courses found" description="Try another search term or check again later." />
        </div>
      ) : (
        <div className="mt-8 grid gap-6 md:grid-cols-2 xl:grid-cols-3">
          {filteredCourses.map((course) => (
            <CourseCard key={course.id} course={course} />
          ))}
        </div>
      )}
    </AppShell>
  );
}
