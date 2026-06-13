"use client";

import { ArrowLeft, CheckCircle2, FileText } from "lucide-react";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { Badge, EmptyState, LinkButton, LoadingState, PageHeader } from "@/components/ui";
import { studentService } from "@/lib/api";
import type { StudentAssignment, StudentSubmission } from "@/lib/types";
import { cn, formatDate, gradeTextColor } from "@/lib/utils";

export default function StudentSubmissionDetailsPage() {
  const params = useParams<{ id: string }>();
  const assignmentId = params.id;
  const [assignment, setAssignment] = useState<StudentAssignment | null>(null);
  const [submission, setSubmission] = useState<StudentSubmission | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    let cancelled = false;

    Promise.all([studentService.getAssignments(), studentService.getSubmission(assignmentId)])
      .then(([assignments, submissionData]) => {
        if (cancelled) return;
        const assignmentData = assignments.find((item) => item.assignmentId === assignmentId);
        if (!assignmentData) {
          setError("This assignment is unavailable or is not part of one of your enrolled courses.");
          return;
        }
        setAssignment(assignmentData);
        setSubmission(submissionData);
      })
      .catch(() => {
        if (!cancelled) setError("Submission details could not be loaded.");
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => { cancelled = true; };
  }, [assignmentId]);

  const grade = submission?.grade ?? assignment?.grade;
  const feedback = submission?.feedback ?? assignment?.feedback;
  const isGraded = grade !== undefined && grade !== null;

  return (
    <AppShell>
      <AuthGuard roles={["Student"]}>
        <PageHeader
          eyebrow="Submission details"
          title={assignment?.title ?? "Assignment submission"}
          description={assignment ? `${assignment.courseName} - ${assignment.sessionTitle}` : "Review your submitted work and grading result."}
          action={<LinkButton href="/assignments" variant="ghost"><ArrowLeft size={17} />Back to assignments</LinkButton>}
        />

        {loading ? (
          <div className="mt-8"><LoadingState label="Loading submission details" /></div>
        ) : error || !assignment || !submission ? (
          <div className="mt-8"><EmptyState title="Submission unavailable" description={error || "No submission was found for this assignment."} /></div>
        ) : (
          <div className="mt-8 grid gap-6 lg:grid-cols-[1fr_360px]">
            <section className="rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
              <div className="flex flex-wrap gap-2">
                <Badge tone={isGraded ? "teal" : submission.isLate ? "coral" : "amber"}>{isGraded ? "Graded" : submission.isLate ? "Late" : "Submitted"}</Badge>
                <Badge tone="slate">{assignment.courseName}</Badge>
              </div>
              <h2 className="mt-5 text-lg font-bold text-ink">Submitted answer</h2>
              <div className="mt-3 rounded-xl bg-slate-50 p-4 ring-1 ring-slate-100">
                <p className="whitespace-pre-wrap text-sm leading-6 text-ink">{submission.textAnswer?.trim() || "No text answer was submitted."}</p>
              </div>
              {submission.fileUrl ? (
                <a href={submission.fileUrl} target="_blank" rel="noreferrer" className="mt-4 inline-flex items-center gap-2 text-sm font-bold text-teal-600">
                  <FileText size={17} />
                  Open submitted file
                </a>
              ) : (
                <p className="mt-4 text-sm text-muted">No file was submitted.</p>
              )}
            </section>

            <aside className="space-y-5">
              <section className="rounded-xl2 bg-white p-5 shadow-soft ring-1 ring-slate-100">
                <h2 className="flex items-center gap-2 font-bold text-ink"><CheckCircle2 size={18} className="text-teal-600" />Grading result</h2>
                {isGraded ? (
                  <>
                    <p className={cn("mt-4 text-3xl font-black", gradeTextColor(grade))}>{grade} / 100</p>
                    <p className="mt-4 text-xs font-bold uppercase tracking-wide text-muted">Instructor feedback</p>
                    <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-ink">{feedback?.trim() || "No feedback provided."}</p>
                  </>
                ) : (
                  <p className="mt-4 text-sm leading-6 text-muted">This submission has not been graded yet.</p>
                )}
              </section>

              <section className="rounded-xl2 bg-white p-5 shadow-soft ring-1 ring-slate-100">
                <p className="text-sm text-muted">Submitted</p>
                <p className="mt-1 font-bold text-ink">{formatDate(submission.submittedAt)}</p>
                <p className="mt-4 text-sm text-muted">Due</p>
                <p className="mt-1 font-bold text-ink">{assignment.dueDate ? formatDate(assignment.dueDate) : "Not set"}</p>
              </section>
            </aside>
          </div>
        )}
      </AuthGuard>
    </AppShell>
  );
}
