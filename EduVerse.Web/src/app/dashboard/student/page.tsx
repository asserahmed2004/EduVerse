"use client";

import { Award, BookOpen, CreditCard, TrendingUp } from "lucide-react";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { CourseCard, EmptyState, LoadingState, PageHeader, ProgressBar, StatCard } from "@/components/ui";
import { courseService, studentService } from "@/lib/api";
import type { Certificate, Course, Enrollment, Payment } from "@/lib/types";
import { formatCurrency } from "@/lib/utils";

export default function StudentDashboardPage() {
  const [courses, setCourses] = useState<Course[]>([]);
  const [enrollments, setEnrollments] = useState<Enrollment[]>([]);
  const [payments, setPayments] = useState<Payment[]>([]);
  const [certificates, setCertificates] = useState<Certificate[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    Promise.all([courseService.getAll(), studentService.getEnrollments(), studentService.getPayments(), studentService.getCertificates()]).then(([coursesData, enrollmentsData, paymentsData, certificatesData]) => {
      setCourses(coursesData.slice(0, 2));
      setEnrollments(enrollmentsData);
      setPayments(paymentsData);
      setCertificates(certificatesData);
    }).catch(() => {
      setError("Could not load dashboard data from the API.");
    }).finally(() => setLoading(false));
  }, []);

  return (
    <AppShell>
      <AuthGuard roles={["Student"]}>
        <PageHeader eyebrow="Student dashboard" title="Your learning workspace" description="Track progress, payments, submissions, and certificates from one place." />
        <div className="mt-8 grid gap-5 md:grid-cols-2 xl:grid-cols-4">
          <StatCard label="Enrolled" value={`${enrollments.length}`} icon={BookOpen} />
          <StatCard label="Average progress" value={`${Math.round(enrollments.reduce((sum, item) => sum + item.progression, 0) / Math.max(enrollments.length, 1))}%`} icon={TrendingUp} accent="amber" />
          <StatCard label="Paid" value={formatCurrency(payments.filter((item) => item.paymentStatus === "Paid").reduce((sum, item) => sum + item.totalPrice, 0))} icon={CreditCard} accent="coral" />
          <StatCard label="Certificates" value={`${certificates.length}`} icon={Award} accent="ink" />
        </div>

        {loading ? (
          <div className="mt-8">
            <LoadingState label="Loading dashboard" />
          </div>
        ) : error ? (
          <div className="mt-8">
            <EmptyState title="Dashboard unavailable" description={error} />
          </div>
        ) : (
        <div className="mt-8 grid gap-8 xl:grid-cols-[1fr_360px]">
          <section>
            <h2 className="text-xl font-bold text-ink">Continue learning</h2>
            <div className="mt-5 grid gap-6 md:grid-cols-2">
              {courses.map((course) => (
                <CourseCard key={course.id} course={course} compact />
              ))}
            </div>
          </section>

          <aside className="rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
            <h2 className="text-xl font-bold text-ink">Progress</h2>
            <div className="mt-5 space-y-5">
              {enrollments.map((item) => (
                <div key={item.courseId}>
                  <div className="flex items-center justify-between gap-4">
                    <p className="text-sm font-semibold text-ink">{item.courseName}</p>
                    <p className="text-sm font-bold text-teal-600">{item.progression}%</p>
                  </div>
                  <div className="mt-2">
                    <ProgressBar value={item.progression} />
                  </div>
                </div>
              ))}
            </div>
          </aside>
        </div>
        )}
      </AuthGuard>
    </AppShell>
  );
}
