"use client";

import { CheckCircle2, FileText, Plus } from "lucide-react";
import { FormEvent, type InputHTMLAttributes, useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { useToast } from "@/components/toast-provider";
import { Badge, Button, EmptyState, LoadingState, PageHeader, StatCard } from "@/components/ui";
import { courseService, instructorService } from "@/lib/api";
import type { InstructorSession, InstructorSubmission } from "@/lib/types";
import { formatDate } from "@/lib/utils";

export default function InstructorAssignmentsPage() {
  const { showToast } = useToast();
  const [submissions, setSubmissions] = useState<InstructorSubmission[]>([]);
  const [sessions, setSessions] = useState<InstructorSession[]>([]);
  const [selectedCourseId, setSelectedCourseId] = useState("");
  const [loading, setLoading] = useState(true);
  const [assignmentSaving, setAssignmentSaving] = useState(false);
  const [error, setError] = useState("");
  const courseOptions = Array.from(new Map(sessions.map((session) => [session.courseId, { id: session.courseId, name: session.courseName }])).values());
  const selectedSessions = sessions.filter((session) => session.courseId === selectedCourseId);

  useEffect(() => {
    Promise.all([instructorService.getSubmissions(), instructorService.getSessions()])
      .then(([submissionsData, sessionsData]) => {
        setSubmissions(submissionsData);
        setSessions(sessionsData);
        setSelectedCourseId(sessionsData[0]?.courseId ?? "");
      })
      .catch(() => setError("Could not load assignment submissions from the API."))
      .finally(() => setLoading(false));
  }, []);

  async function gradeSubmission(event: FormEvent<HTMLFormElement>, submission: InstructorSubmission) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const grade = Number(form.get("grade") ?? 0);
    const feedback = String(form.get("feedback") ?? "");
    if (!Number.isFinite(grade) || grade < 0 || grade > 100) {
      showToast({ title: "Invalid grade", message: "Grade must be a number from 0 to 100.", tone: "error" });
      return;
    }

    try {
      const result = await instructorService.gradeSubmission(submission.assignmentId, submission.studentId, grade, feedback);
      setSubmissions((current) => current.map((item) => item.assignmentId === submission.assignmentId && item.studentId === submission.studentId ? { ...item, grade, feedback } : item));
      showToast({ title: "Submission graded", message: result.message ?? "Grade and feedback were saved.", tone: "success" });
    } catch (error) {
      showToast({ title: "Grading failed", message: error instanceof Error ? error.message : "Could not grade this submission.", tone: "error" });
    }
  }

  async function addAssignment(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formElement = event.currentTarget;
    const form = new FormData(formElement);
    setAssignmentSaving(true);
    try {
      const result = await courseService.addAssignment(form);
      showToast({ title: "Assignment added", message: result.message ?? "The assignment is available under the selected session.", tone: "success" });
      formElement.reset();
    } catch (error) {
      showToast({ title: "Assignment failed", message: error instanceof Error ? error.message : "Could not create assignment for this session.", tone: "error" });
    } finally {
      setAssignmentSaving(false);
    }
  }

  return (
    <AppShell>
      <AuthGuard roles={["Instructor"]}>
        <PageHeader eyebrow="Instructor" title="Assignment submissions" description="Review submitted assignments for your assigned courses." />
        <div className="mt-8 grid gap-5 md:grid-cols-3">
          <StatCard label="Submissions" value={`${submissions.length}`} icon={FileText} accent="coral" />
          <StatCard label="Pending grading" value={`${submissions.filter((submission) => submission.grade === undefined || submission.grade === null).length}`} icon={CheckCircle2} />
        </div>

        <form onSubmit={addAssignment} className="mt-8 rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
          <div className="flex items-center gap-3">
            <div className="grid size-11 place-items-center rounded-xl bg-teal-50 text-teal-600">
              <Plus size={20} />
            </div>
            <div>
              <h2 className="text-lg font-bold text-ink">Add assignment</h2>
              <p className="text-sm text-muted">Create assignments only for your assigned course sessions.</p>
            </div>
          </div>
          <div className="mt-5 grid gap-4 lg:grid-cols-2">
            <label className="block">
              <span className="text-sm font-semibold text-ink">Course</span>
              <select value={selectedCourseId} onChange={(event) => setSelectedCourseId(event.target.value)} className="mt-2 h-12 w-full rounded-xl bg-slate-50 px-4 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500" disabled={courseOptions.length === 0}>
                <option value="">Select course</option>
                {courseOptions.map((course) => <option key={course.id} value={course.id}>{course.name}</option>)}
              </select>
            </label>
            <label className="block">
              <span className="text-sm font-semibold text-ink">Session</span>
              <select name="SessionId" className="mt-2 h-12 w-full rounded-xl bg-slate-50 px-4 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500" required disabled={!selectedCourseId || selectedSessions.length === 0}>
                <option value="">Select session</option>
                {selectedSessions.map((session) => <option key={session.sessionId} value={session.sessionId}>{session.sessionNumber ? `Session ${session.sessionNumber} - ` : ""}{session.title}</option>)}
              </select>
            </label>
            <Field name="Subject" label="Assignment title" required />
            <Field name="DueDate" label="Due date" type="date" />
            <label className="block lg:col-span-2">
              <span className="text-sm font-semibold text-ink">Description / instructions</span>
              <textarea name="Description" className="mt-2 min-h-24 w-full rounded-xl bg-slate-50 px-4 py-3 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500" required />
            </label>
            <label className="block lg:col-span-2">
              <span className="text-sm font-semibold text-ink">Optional file</span>
              <input name="File" type="file" className="mt-2 w-full rounded-xl bg-slate-50 px-4 py-3 text-sm ring-1 ring-slate-200" />
            </label>
          </div>
          <Button className="mt-5" variant="ghost" disabled={assignmentSaving || selectedSessions.length === 0}>
            <Plus size={18} />
            {assignmentSaving ? "Adding..." : "Add assignment"}
          </Button>
        </form>

        <section className="mt-8">
          {loading ? (
            <LoadingState label="Loading submissions" />
          ) : error ? (
            <EmptyState title="Submissions unavailable" description={error} />
          ) : submissions.length === 0 ? (
            <EmptyState title="No submissions yet" description="Student submissions will appear here after assignments are submitted." />
          ) : (
            <div className="grid gap-4">
              {submissions.map((submission) => (
                <article key={`${submission.assignmentId}-${submission.studentId}`} className="rounded-xl2 bg-white p-5 shadow-soft ring-1 ring-slate-100">
                  <div className="grid gap-5 xl:grid-cols-[1fr_360px]">
                    <div>
                      <div className="flex flex-wrap gap-2">
                        <Badge>{submission.courseName}</Badge>
                        <Badge tone={submission.grade === undefined || submission.grade === null ? "amber" : "teal"}>{submission.grade === undefined || submission.grade === null ? "Pending" : "Graded"}</Badge>
                      </div>
                      <h2 className="mt-3 text-lg font-bold text-ink">{submission.assignmentTitle}</h2>
                      <p className="mt-1 text-sm text-muted">Student: {submission.studentName || submission.studentId}</p>
                      <p className="mt-1 text-sm text-muted">Session: {submission.sessionTitle || submission.sessionId || "Not available"}</p>
                      <p className="mt-1 text-sm text-muted">Submitted: {formatDate(submission.submittedAt)}</p>
                      {submission.isLate && <Badge tone="coral">Late</Badge>}
                      <div className="mt-4 rounded-xl bg-slate-50 p-4">
                        <p className="text-xs font-bold uppercase text-muted">Text answer</p>
                        <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-ink">{submission.textAnswer || "No text answer submitted."}</p>
                      </div>
                      {(submission.fileUrl || submission.filePath) ? (
                        <a href={submission.fileUrl ?? submission.filePath} target="_blank" rel="noreferrer" className="mt-3 inline-flex text-sm font-bold text-teal-600">Open submission file</a>
                      ) : (
                        <p className="mt-3 text-sm font-semibold text-muted">No file uploaded.</p>
                      )}
                    </div>

                    <form onSubmit={(event) => gradeSubmission(event, submission)} className="rounded-xl bg-slate-50 p-4">
                      <label className="block">
                        <span className="text-sm font-semibold text-ink">Grade</span>
                        <input name="grade" type="number" min="0" max="100" defaultValue={submission.grade ?? ""} className="mt-2 h-11 w-full rounded-xl bg-white px-4 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500" required />
                      </label>
                      <label className="mt-3 block">
                        <span className="text-sm font-semibold text-ink">Feedback</span>
                        <textarea name="feedback" defaultValue={submission.feedback ?? ""} className="mt-2 min-h-20 w-full rounded-xl bg-white px-4 py-3 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500" />
                      </label>
                      <Button className="mt-4 w-full" variant="ghost">Save grade</Button>
                    </form>
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
