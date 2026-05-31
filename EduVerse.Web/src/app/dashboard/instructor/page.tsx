"use client";

import { BookOpen, CalendarClock, FileText, Users } from "lucide-react";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { LoadingState, EmptyState, PageHeader, StatCard } from "@/components/ui";
import { dashboardService } from "@/lib/api";
import type { DashboardStats } from "@/lib/types";

export default function InstructorDashboardPage() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    dashboardService.getOrganizationOverview().then(setStats).catch(() => {
      setError("Could not load instructor dashboard data from the API.");
    }).finally(() => setLoading(false));
  }, []);

  return (
    <AppShell>
      <AuthGuard roles={["Instructor"]}>
        <PageHeader eyebrow="Instructor dashboard" title="My teaching workspace" description="Review sessions, assignments, submitted work, and students assigned to your teaching schedule." />
        <div className="mt-8 grid gap-5 md:grid-cols-2 xl:grid-cols-4">
          <StatCard label="My sessions" value={`${stats?.totalSessions ?? 0}`} icon={CalendarClock} />
          <StatCard label="My students" value={`${stats?.totalStudents ?? 0}`} icon={Users} accent="amber" />
          <StatCard label="My assignments" value={`${stats?.totalAssignments ?? 0}`} icon={FileText} accent="coral" />
          <StatCard label="Assigned courses" value={`${stats?.totalCourses ?? 0}`} icon={BookOpen} accent="ink" />
        </div>

        <section className="mt-8">
          <h2 className="text-xl font-bold text-ink">Upcoming sessions</h2>
          {loading ? (
            <div className="mt-5">
              <LoadingState label="Loading instructor workspace" />
            </div>
          ) : error ? (
            <div className="mt-5">
              <EmptyState title="Instructor data unavailable" description={error} />
            </div>
          ) : (stats?.totalSessions ?? 0) === 0 ? (
            <div className="mt-5">
              <EmptyState title="No assigned sessions yet" description="Assigned sessions will appear here once an organization admin links you to course sessions." />
            </div>
          ) : (
            <div className="mt-5">
              <EmptyState title="Session list endpoint pending" description="Summary is loaded from the backend. A detailed upcoming sessions endpoint can be added next." />
            </div>
          )}
        </section>
      </AuthGuard>
    </AppShell>
  );
}
