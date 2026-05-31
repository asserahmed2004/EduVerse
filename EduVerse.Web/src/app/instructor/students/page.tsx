"use client";

import { Users } from "lucide-react";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { EmptyState, LoadingState, PageHeader, StatCard } from "@/components/ui";
import { dashboardService } from "@/lib/api";
import type { DashboardStats } from "@/lib/types";

export default function InstructorStudentsPage() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    dashboardService.getOrganizationOverview().then(setStats).finally(() => setLoading(false));
  }, []);

  return (
    <AppShell>
      <AuthGuard roles={["Instructor"]}>
        <PageHeader eyebrow="Instructor" title="Students" description="Students connected to your assigned sessions and courses." />
        <div className="mt-8 grid gap-5 md:grid-cols-3">
          <StatCard label="My students" value={`${stats?.totalStudents ?? 0}`} icon={Users} accent="amber" />
        </div>
        <div className="mt-8">
          {loading ? <LoadingState label="Loading students" /> : <EmptyState title="Student details endpoint pending" description="The student summary is live. Detailed student rows can be wired when the backend exposes them." />}
        </div>
      </AuthGuard>
    </AppShell>
  );
}
