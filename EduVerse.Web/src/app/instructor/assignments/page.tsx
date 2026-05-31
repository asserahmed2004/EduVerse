"use client";

import { FileText } from "lucide-react";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { EmptyState, LoadingState, PageHeader, StatCard } from "@/components/ui";
import { dashboardService } from "@/lib/api";
import type { DashboardStats } from "@/lib/types";

export default function InstructorAssignmentsPage() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    dashboardService.getOrganizationOverview().then(setStats).finally(() => setLoading(false));
  }, []);

  return (
    <AppShell>
      <AuthGuard roles={["Instructor"]}>
        <PageHeader eyebrow="Instructor" title="Assignments" description="Assignments connected to your assigned sessions." />
        <div className="mt-8 grid gap-5 md:grid-cols-3">
          <StatCard label="My assignments" value={`${stats?.totalAssignments ?? 0}`} icon={FileText} accent="coral" />
        </div>
        <div className="mt-8">
          {loading ? <LoadingState label="Loading assignments" /> : <EmptyState title="Assignment details endpoint pending" description="The assignment summary is live. Detailed assignment rows can be wired when the backend exposes them." />}
        </div>
      </AuthGuard>
    </AppShell>
  );
}
