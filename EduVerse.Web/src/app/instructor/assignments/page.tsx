"use client";

import { CheckCircle2, FileText } from "lucide-react";
import { FormEvent, useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { useToast } from "@/components/toast-provider";
import { Badge, Button, EmptyState, LoadingState, PageHeader, StatCard } from "@/components/ui";
import { instructorService } from "@/lib/api";
import type { InstructorSubmission } from "@/lib/types";
import { formatDate } from "@/lib/utils";

export default function InstructorAssignmentsPage() {
  const { showToast } = useToast();
  const [submissions, setSubmissions] = useState<InstructorSubmission[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    instructorService.getSubmissions()
      .then(setSubmissions)
      .catch(() => setError("Could not load assignment submissions from the API."))
      .finally(() => setLoading(false));
  }, []);

  async function gradeSubmission(event: FormEvent<HTMLFormElement>, submission: InstructorSubmission) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const grade = Number(form.get("grade") ?? 0);
    const feedback = String(form.get("feedback") ?? "");

    try {
      await instructorService.gradeSubmission(submission.assignmentId, submission.studentId, grade, feedback);
      setSubmissions((current) => current.map((item) => item.assignmentId === submission.assignmentId && item.studentId === submission.studentId ? { ...item, grade, feedback } : item));
      showToast({ title: "Submission graded", message: "Grade and feedback were saved.", tone: "success" });
    } catch (error) {
      showToast({ title: "Grading failed", message: error instanceof Error ? error.message : "Could not grade this submission.", tone: "error" });
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
                      <p className="mt-1 text-sm text-muted">Submitted: {formatDate(submission.submittedAt)}</p>
                      {submission.fileUrl && <a href={submission.fileUrl} target="_blank" rel="noreferrer" className="mt-3 inline-flex text-sm font-bold text-teal-600">Open submission file</a>}
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
