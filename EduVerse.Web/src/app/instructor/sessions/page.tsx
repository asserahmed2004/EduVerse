"use client";

import { CalendarClock } from "lucide-react";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { EmptyState, LoadingState, PageHeader, StatCard } from "@/components/ui";
import { dashboardService } from "@/lib/api";
import type { DashboardStats } from "@/lib/types";

export default function InstructorSessionsPage() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    dashboardService.getOrganizationOverview().then(setStats).finally(() => setLoading(false));
  }, []);

  return (
    <AppShell>
      <AuthGuard roles={["Instructor"]}>
        <PageHeader eyebrow="Instructor" title="My sessions" description="Sessions assigned to your instructor account." />
        <div className="mt-8 grid gap-5 md:grid-cols-3">
          <StatCard label="Assigned sessions" value={`${stats?.totalSessions ?? 0}`} icon={CalendarClock} />
        </div>
        <div className="mt-8">
          {loading ? <LoadingState label="Loading sessions" /> : <EmptyState title="Session details endpoint pending" description="The dashboard summary is live. Detailed assigned session rows can be wired when the backend exposes them." />}
        </div>
      </AuthGuard>
    </AppShell>
  );
}
