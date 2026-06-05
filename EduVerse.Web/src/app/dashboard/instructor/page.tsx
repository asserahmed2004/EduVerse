"use client";

import { BookOpen, CalendarClock, FileText, Users } from "lucide-react";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { LoadingState, EmptyState, PageHeader, StatCard } from "@/components/ui";
import { instructorService } from "@/lib/api";
import type { InstructorOverview } from "@/lib/types";
import { formatDate } from "@/lib/utils";

export default function InstructorDashboardPage() {
  const [overview, setOverview] = useState<InstructorOverview | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    instructorService.getOverview().then(setOverview).catch(() => {
      setError("Could not load instructor dashboard data from the API.");
    }).finally(() => setLoading(false));
  }, []);

  return (
    <AppShell>
      <AuthGuard roles={["Instructor"]}>
        <PageHeader eyebrow="Instructor dashboard" title="My teaching workspace" description="Review sessions, assignments, submitted work, and students assigned to your teaching schedule." />
        <div className="mt-8 grid gap-5 md:grid-cols-2 xl:grid-cols-4">
          <StatCard label="Assigned courses" value={`${overview?.assignedCourses ?? 0}`} icon={BookOpen} />
          <StatCard label="My students" value={`${overview?.myStudents ?? 0}`} icon={Users} accent="amber" />
          <StatCard label="Pending submissions" value={`${overview?.pendingSubmissions ?? 0}`} icon={FileText} accent="coral" />
          <StatCard label="My assignments" value={`${overview?.totalAssignments ?? 0}`} icon={CalendarClock} accent="ink" />
        </div>

        <section className="mt-8 grid gap-6 xl:grid-cols-2">
          {loading ? (
            <div className="xl:col-span-2">
              <LoadingState label="Loading instructor workspace" />
            </div>
          ) : error ? (
            <div className="xl:col-span-2">
              <EmptyState title="Instructor data unavailable" description={error} />
            </div>
          ) : (
            <>
              <Panel title="Upcoming sessions">
                {(overview?.upcomingSessions ?? []).length === 0 ? (
                  <Muted>No upcoming sessions yet.</Muted>
                ) : overview?.upcomingSessions.map((session) => (
                  <Row key={session.sessionId} title={session.title} meta={`${session.courseName} - ${formatDate(session.date)}`} />
                ))}
              </Panel>
              <Panel title="Recent submissions">
                {(overview?.recentSubmissions ?? []).length === 0 ? (
                  <Muted>No submitted assignments yet.</Muted>
                ) : overview?.recentSubmissions.map((submission) => (
                  <Row key={`${submission.assignmentId}-${submission.studentId}`} title={submission.assignmentTitle} meta={`${submission.studentName} - ${submission.courseName}`} />
                ))}
              </Panel>
            </>
          )}
        </section>
      </AuthGuard>
    </AppShell>
  );
}

function Panel({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
      <h2 className="text-xl font-bold text-ink">{title}</h2>
      <div className="mt-4 space-y-3">{children}</div>
    </div>
  );
}

function Row({ title, meta }: { title: string; meta: string }) {
  return (
    <div className="rounded-xl bg-slate-50 p-4">
      <p className="font-bold text-ink">{title}</p>
      <p className="mt-1 text-sm text-muted">{meta}</p>
    </div>
  );
}

function Muted({ children }: { children: React.ReactNode }) {
  return <p className="rounded-xl bg-slate-50 p-4 text-sm font-semibold text-muted">{children}</p>;
}
