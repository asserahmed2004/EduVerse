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
  const [category, setCategory] = useState("All");
  const [priceFilter, setPriceFilter] = useState<"All" | "Free" | "Paid">("All");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    courseService.getAll()
      .then(setCourses)
      .catch(() => setError("Could not load courses from the API."))
      .finally(() => setLoading(false));
  }, []);

  const categories = useMemo(() => {
    const values = courses
      .map((course) => course.category ?? course.categories?.[0]?.name)
      .filter((value): value is string => Boolean(value));

    return ["All", ...Array.from(new Set(values))];
  }, [courses]);

  const filteredCourses = useMemo(() => {
    return courses.filter((course) => {
      const haystack = `${course.name} ${course.title} ${course.description} ${course.category ?? ""}`.toLowerCase();
      const matchesQuery = haystack.includes(query.toLowerCase());
      const courseCategory = course.category ?? course.categories?.[0]?.name ?? "";
      const matchesCategory = category === "All" || courseCategory === category;
      const matchesPrice = priceFilter === "All" || (priceFilter === "Free" ? course.price <= 0 : course.price > 0);

      return matchesQuery && matchesCategory && matchesPrice;
    });
  }, [courses, query, category, priceFilter]);

  return (
    <AppShell>
      <PageHeader eyebrow="Catalog" title="Courses" description="Explore learning paths, compare prices and ratings, then enroll from the course details page." />

      <div className="mt-8 grid gap-3 rounded-xl2 bg-white p-4 shadow-soft ring-1 ring-slate-100 lg:grid-cols-[1fr_220px_180px]">
        <label className="flex items-center gap-3 rounded-xl bg-slate-50 px-4 py-3 ring-1 ring-slate-200">
          <Search size={20} className="text-muted" />
          <input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search courses" className="w-full bg-transparent text-sm outline-none" />
        </label>
        <select value={category} onChange={(event) => setCategory(event.target.value)} className="h-12 rounded-xl bg-slate-50 px-4 text-sm font-semibold text-ink outline-none ring-1 ring-slate-200">
          {categories.map((item) => <option key={item} value={item}>{item}</option>)}
        </select>
        <select value={priceFilter} onChange={(event) => setPriceFilter(event.target.value as "All" | "Free" | "Paid")} className="h-12 rounded-xl bg-slate-50 px-4 text-sm font-semibold text-ink outline-none ring-1 ring-slate-200">
          <option value="All">All prices</option>
          <option value="Free">Free</option>
          <option value="Paid">Paid</option>
        </select>
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
