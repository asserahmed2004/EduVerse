"use client";

import { CalendarClock, Plus, QrCode } from "lucide-react";
import { FormEvent, type InputHTMLAttributes, useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { useToast } from "@/components/toast-provider";
import { Button, EmptyState, LoadingState, PageHeader, StatCard } from "@/components/ui";
import { courseService, instructorService } from "@/lib/api";
import type { Course, InstructorSession } from "@/lib/types";
import { formatDate } from "@/lib/utils";

export default function InstructorSessionsPage() {
  const { showToast } = useToast();
  const [sessions, setSessions] = useState<InstructorSession[]>([]);
  const [courses, setCourses] = useState<Course[]>([]);
  const [qrCodes, setQrCodes] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    Promise.all([instructorService.getSessions(), courseService.getAll()])
      .then(([sessionsData, coursesData]) => {
        setSessions(sessionsData);
        setCourses(coursesData);
      })
      .catch(() => setError("Could not load assigned sessions from the API."))
      .finally(() => setLoading(false));
  }, []);

  async function addSession(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formElement = event.currentTarget;
    const form = new FormData(formElement);
    try {
      await courseService.addSession(form);
    } catch (error) {
      showToast({ title: "Session failed", message: error instanceof Error ? error.message : "Could not add session to this course.", tone: "error" });
      return;
    }

    showToast({ title: "Session added", message: "The session was added to your assigned course.", tone: "success" });
    formElement?.reset();
    instructorService.getSessions().then(setSessions).catch(() => undefined);
  }

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

        <form onSubmit={addSession} className="mt-8 rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
          <div className="flex items-center gap-3">
            <div className="grid size-11 place-items-center rounded-xl bg-teal-50 text-teal-600">
              <Plus size={20} />
            </div>
            <div>
              <h2 className="text-lg font-bold text-ink">Add session</h2>
              <p className="text-sm text-muted">Create sessions only for courses assigned to you.</p>
            </div>
          </div>
          <div className="mt-5 grid gap-4 lg:grid-cols-2">
            <label className="block">
              <span className="text-sm font-semibold text-ink">Course</span>
              <select name="CourseId" className="mt-2 h-12 w-full rounded-xl bg-slate-50 px-4 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500" required disabled={courses.length === 0}>
                <option value="">Select course</option>
                {courses.map((course) => <option key={course.id} value={course.id}>{course.name}</option>)}
              </select>
            </label>
            <Field name="SessionNumber" label="Session number" type="number" required />
            <Field name="Title" label="Session title" required />
            <Field name="VideoUrl" label="Video URL" />
            <Field name="ExternalLink" label="External link" />
            <label className="block lg:col-span-2">
              <span className="text-sm font-semibold text-ink">Description</span>
              <textarea name="Description" className="mt-2 min-h-24 w-full rounded-xl bg-slate-50 px-4 py-3 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500" />
            </label>
            <label className="block lg:col-span-2">
              <span className="text-sm font-semibold text-ink">Optional session file</span>
              <input name="File" type="file" className="mt-2 w-full rounded-xl bg-slate-50 px-4 py-3 text-sm ring-1 ring-slate-200" />
            </label>
          </div>
          <Button className="mt-5" variant="ghost">
            <Plus size={18} />
            Add session
          </Button>
        </form>

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

function Field(props: InputHTMLAttributes<HTMLInputElement> & { label: string }) {
  const { label, ...inputProps } = props;
  return (
    <label className="block">
      <span className="text-sm font-semibold text-ink">{label}</span>
      <input className="mt-2 h-12 w-full rounded-xl bg-slate-50 px-4 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500" {...inputProps} />
    </label>
  );
}
