"use client";

import { BookOpen, CreditCard, Users, Wallet } from "lucide-react";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { LoadingState, CourseCard, EmptyState, PageHeader, StatCard } from "@/components/ui";
import { courseService, instructorService } from "@/lib/api";
import type { Course, Payment } from "@/lib/types";
import { formatCurrency } from "@/lib/utils";

export default function InstructorDashboardPage() {
  const [courses, setCourses] = useState<Course[]>([]);
  const [payments, setPayments] = useState<Payment[]>([]);
  const [studentCount, setStudentCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    courseService.getOwnedByCurrentUser().then(async (coursesData) => {
      const paymentsData = (await Promise.all(coursesData.map((course) => instructorService.getCoursePayments(course.id).catch(() => [])))).flat();
      const enrolledUsers = (await Promise.all(coursesData.map((course) => instructorService.getEnrolledUsers(course.id).catch(() => [])))).flat();
      const uniqueStudentIds = new Set(enrolledUsers.map((user: any) => user.id ?? user.Id ?? user.email ?? user.Email).filter(Boolean));
      setCourses(coursesData);
      setPayments(paymentsData);
      setStudentCount(uniqueStudentIds.size);
    }).catch(() => {
      setError("Could not load instructor dashboard data from the API.");
    }).finally(() => setLoading(false));
  }, []);

  return (
    <AppShell>
      <AuthGuard roles={["Instructor", "Admin"]}>
        <PageHeader eyebrow="Instructor dashboard" title="Manage your courses" description="Review course performance, payment activity, and enrolled students." />
        <div className="mt-8 grid gap-5 md:grid-cols-2 xl:grid-cols-4">
          <StatCard label="Courses" value={`${courses.length}`} icon={BookOpen} />
          <StatCard label="Students" value={`${studentCount}`} icon={Users} accent="amber" />
          <StatCard label="Revenue" value={formatCurrency(payments.reduce((sum, item) => sum + item.totalPrice, 0))} icon={Wallet} accent="coral" />
          <StatCard label="Pending payments" value={`${payments.filter((item) => item.paymentStatus === "Pending").length}`} icon={CreditCard} accent="ink" />
        </div>

        <section className="mt-8">
          <h2 className="text-xl font-bold text-ink">Owned courses</h2>
          {loading ? (
            <div className="mt-5">
              <LoadingState label="Loading instructor courses" />
            </div>
          ) : error ? (
            <div className="mt-5">
              <EmptyState title="Instructor data unavailable" description={error} />
            </div>
          ) : courses.length === 0 ? (
            <div className="mt-5">
              <EmptyState title="No courses yet" description="Create a course to start managing your catalog." />
            </div>
          ) : (
            <div className="mt-5 grid gap-6 md:grid-cols-2 xl:grid-cols-3">
              {courses.map((course) => (
                <CourseCard key={course.id} course={course} compact />
              ))}
            </div>
          )}
        </section>
      </AuthGuard>
    </AppShell>
  );
}
