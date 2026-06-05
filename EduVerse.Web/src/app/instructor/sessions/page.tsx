"use client";

import { CalendarClock, QrCode } from "lucide-react";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { useToast } from "@/components/toast-provider";
import { Button, EmptyState, LoadingState, PageHeader, StatCard } from "@/components/ui";
import { instructorService } from "@/lib/api";
import type { InstructorSession } from "@/lib/types";
import { formatDate } from "@/lib/utils";

export default function InstructorSessionsPage() {
  const { showToast } = useToast();
  const [sessions, setSessions] = useState<InstructorSession[]>([]);
  const [qrCodes, setQrCodes] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    instructorService.getSessions()
      .then(setSessions)
      .catch(() => setError("Could not load assigned sessions from the API."))
      .finally(() => setLoading(false));
  }, []);

  async function generateQr(sessionId: string) {
    try {
      const result = await instructorService.createSessionQr(sessionId);
      setQrCodes((current) => ({ ...current, [sessionId]: result.attendanceCode }));
      showToast({ title: "Attendance QR ready", message: `Code: ${result.attendanceCode}`, tone: "success" });
    } catch (error) {
      showToast({ title: "QR generation failed", message: error instanceof Error ? error.message : "Could not create attendance code.", tone: "error" });
    }
  }

  return (
    <AppShell>
      <AuthGuard roles={["Instructor"]}>
        <PageHeader eyebrow="Instructor" title="My sessions" description="Sessions assigned through your courses, with attendance QR support." />
        <div className="mt-8 grid gap-5 md:grid-cols-3">
          <StatCard label="Assigned sessions" value={`${sessions.length}`} icon={CalendarClock} />
        </div>

        <section className="mt-8">
          {loading ? (
            <LoadingState label="Loading sessions" />
          ) : error ? (
            <EmptyState title="Sessions unavailable" description={error} />
          ) : sessions.length === 0 ? (
            <EmptyState title="No assigned sessions" description="Sessions will appear here when you are assigned to a course." />
          ) : (
            <div className="grid gap-4">
              {sessions.map((session) => (
                <article key={session.sessionId} className="rounded-xl2 bg-white p-5 shadow-soft ring-1 ring-slate-100">
                  <div className="flex flex-col justify-between gap-4 md:flex-row md:items-center">
                    <div>
                      <p className="text-xs font-bold uppercase text-teal-600">{session.courseName}</p>
                      <h2 className="mt-2 text-lg font-bold text-ink">{session.title}</h2>
                      <p className="mt-1 text-sm text-muted">Session {session.sessionNumber} - {formatDate(session.date)}</p>
                      {qrCodes[session.sessionId] && <p className="mt-2 text-sm font-bold text-teal-600">Attendance code: {qrCodes[session.sessionId]}</p>}
                    </div>
                    <Button variant="ghost" onClick={() => generateQr(session.sessionId)}>
                      <QrCode size={18} />
                      Generate QR
                    </Button>
                  </div>
                </article>
              ))}
            </div>
          )}
        </section>
      </AuthGuard>
    </AppShell>
  );
}
