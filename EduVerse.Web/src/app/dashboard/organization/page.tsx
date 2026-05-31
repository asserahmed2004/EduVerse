"use client";

import { BookOpen, CreditCard, FileText, GraduationCap, Star, Trash2, Users, Wallet } from "lucide-react";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { EmptyState, LoadingState, PageHeader, StatCard } from "@/components/ui";
import { dashboardService } from "@/lib/api";
import type { DashboardStats } from "@/lib/types";
import { formatCurrency } from "@/lib/utils";

export default function OrganizationDashboardPage() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    dashboardService.getOrganizationOverview()
      .then(setStats)
      .catch(() => setError("Could not load organization dashboard data from the API."))
      .finally(() => setLoading(false));
  }, []);

  return (
    <AppShell>
      <AuthGuard roles={["OrganizationAdmin"]}>
        <PageHeader eyebrow="Organization dashboard" title="Organization control center" description="Review your organization courses, students, payments, sessions, and assignments." />

        {loading ? (
          <div className="mt-8"><LoadingState label="Loading organization dashboard" /></div>
        ) : error ? (
          <div className="mt-8"><EmptyState title="Organization data unavailable" description={error} /></div>
        ) : (
          <div className="mt-8 grid gap-5 md:grid-cols-2 xl:grid-cols-4">
            <StatCard label="Total courses" value={`${stats?.totalCourses ?? 0}`} icon={BookOpen} />
            <StatCard label="Deleted courses" value={`${stats?.deletedCourses ?? 0}`} icon={Trash2} accent="coral" />
            <StatCard label="Total students" value={`${stats?.totalStudents ?? 0}`} icon={GraduationCap} />
            <StatCard label="Total instructors" value={`${stats?.totalInstructors ?? 0}`} icon={Users} accent="amber" />
            <StatCard label="Total enrollments" value={`${stats?.totalEnrollments ?? 0}`} icon={Users} />
            <StatCard label="Total revenue" value={formatCurrency(stats?.totalRevenue ?? 0)} icon={Wallet} />
            <StatCard label="Total sessions" value={`${stats?.totalSessions ?? 0}`} icon={BookOpen} accent="amber" />
            <StatCard label="Total assignments" value={`${stats?.totalAssignments ?? 0}`} icon={FileText} />
            <StatCard label="Pending payments" value={`${stats?.pendingPayments ?? 0}`} icon={CreditCard} accent="coral" />
            <StatCard label="Average rating" value={`${(stats?.averageRating ?? 0).toFixed(1)}`} icon={Star} />
          </div>
        )}
      </AuthGuard>
    </AppShell>
  );
}
