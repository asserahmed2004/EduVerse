"use client";

import { BookOpen, FileText, Upload } from "lucide-react";
import { FormEvent, useEffect, useMemo, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { FileActionButtons } from "@/components/file-actions";
import { useToast } from "@/components/toast-provider";
import { Badge, Button, EmptyState, LinkButton, LoadingState, PageHeader, StatCard } from "@/components/ui";
import { getApiErrorMessage, studentService } from "@/lib/api";
import type { StudentAssignment } from "@/lib/types";
import { cn, formatDate, gradeTextColor } from "@/lib/utils";

function AssignmentCard({ assignment, onSubmit }: { assignment: StudentAssignment; onSubmit: (assignment: StudentAssignment) => void }) {
  const hasSubmission = Boolean(assignment.submittedAt) || ["Submitted", "Pending", "Late", "Graded"].includes(assignment.submissionStatus);

  return (
    <article className="rounded-xl2 bg-white p-5 shadow-soft ring-1 ring-slate-100 transition duration-200 hover:-translate-y-0.5 hover:shadow-lg">
      <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-center">
        <div>
          <Badge tone={assignment.submissionStatus === "Graded" ? "teal" : assignment.submissionStatus === "Missing" || assignment.submissionStatus === "Late" ? "coral" : "amber"}>{assignment.submissionStatus}</Badge>
          <h3 className="mt-3 text-lg font-bold text-ink">{assignment.title}</h3>
          <p className="mt-1 text-sm text-muted">{assignment.sessionTitle} - Session {assignment.sessionNumber}</p>
          <p className="mt-2 text-sm text-muted">{assignment.description}</p>
          <p className="mt-2 text-xs font-semibold text-muted">Due: {assignment.dueDate ? formatDate(assignment.dueDate) : "Not set"}</p>
          {assignment.assignmentFileUrl && (
            <div className="mt-3 rounded-xl bg-slate-50 p-3 ring-1 ring-slate-100">
              <p className="text-xs font-bold uppercase tracking-wide text-muted">Instructor attachment</p>
              <FileActionButtons url={assignment.assignmentFileUrl} className="mt-3" previewLabel="Open" downloadLabel="Download" />
            </div>
          )}
          {assignment.submissionStatus === "Graded" && (
            <div className="mt-3 rounded-xl bg-teal-50 p-3 ring-1 ring-teal-100">
              <p className={cn("text-sm font-bold", gradeTextColor(assignment.grade))}>Grade: {assignment.grade ?? "Not available"} / 100</p>
              <p className="mt-1 whitespace-pre-wrap text-sm text-muted">Feedback: {assignment.feedback?.trim() || "No feedback provided."}</p>
            </div>
          )}
        </div>
        <div className="flex flex-wrap gap-2">
          {hasSubmission && <LinkButton href={`/assignments/${assignment.assignmentId}`} variant="ghost">View details</LinkButton>}
          <Button onClick={() => onSubmit(assignment)} variant="ghost">{hasSubmission ? "Resubmit" : "Submit"}</Button>
        </div>
      </div>
    </article>
  );
}

export default function StudentAssignmentsPage() {
  const { showToast } = useToast();
  const [assignments, setAssignments] = useState<StudentAssignment[]>([]);
  const [selected, setSelected] = useState<StudentAssignment | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const pending = assignments.filter((item) => item.submissionStatus === "Not Submitted" || item.submissionStatus === "Missing" || item.submissionStatus === "Pending").length;
  const groupedAssignments = useMemo(() => assignments.reduce<Record<string, StudentAssignment[]>>((groups, assignment) => {
    const courseKey = assignment.courseId || assignment.courseName;
    (groups[courseKey] ??= []).push(assignment);
    return groups;
  }, {}), [assignments]);

  async function load(notifyOnError = true) {
    setLoading(true);
    try {
      setAssignments(await studentService.getAssignments());
      return true;
    } catch (loadError) {
      if (notifyOnError) {
        setAssignments([]);
        showToast({ title: "Assignments unavailable", message: getApiErrorMessage(loadError, "Could not load your assignments from the backend."), tone: "error" });
      }
      return false;
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { load(); }, []);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selected) return;
    const form = new FormData(event.currentTarget);
    const textAnswer = String(form.get("textAnswer") ?? "").trim();
    const rawFile = form.get("file");
    const file = rawFile instanceof File && rawFile.size > 0 ? rawFile : null;
    if (!textAnswer && !file) {
      showToast({ title: "Submission required", message: "Add a text answer or upload a file before submitting.", tone: "error" });
      return;
    }

    setSaving(true);
    try {
      const result = await studentService.submitAssignment(selected.assignmentId, { textAnswer, file });
      showToast({ title: "Submitted", message: result.message ?? "Assignment submitted successfully.", tone: "success" });
      setSelected(null);
      const refreshed = await load(false);
      if (!refreshed) {
        showToast({ title: "Submission saved", message: "Your submission was saved, but the assignment list could not be refreshed.", tone: "info" });
      }
    } catch (error) {
      showToast({ title: "Submit failed", message: error instanceof Error ? error.message : "Could not submit assignment.", tone: "error" });
    } finally {
      setSaving(false);
    }
  }

  return (
    <AppShell>
      <AuthGuard roles={["Student"]}>
        <PageHeader eyebrow="Assignments" title="My assignments" description="Submit work, track due dates, and review grades from your enrolled courses." />
        <div className="mt-8 grid gap-5 md:grid-cols-3">
          <StatCard label="Assignments" value={`${assignments.length}`} icon={FileText} />
          <StatCard label="Pending" value={`${pending}`} icon={Upload} accent="amber" />
          <StatCard label="Graded" value={`${assignments.filter((item) => item.submissionStatus === "Graded").length}`} icon={FileText} accent="coral" />
        </div>

        <section className="mt-8">
          {loading ? <LoadingState label="Loading assignments" /> : assignments.length === 0 ? (
            <EmptyState title="No assignments yet" description="Assignments will appear after you enroll in courses with sessions." />
          ) : (
            <div className="space-y-10">
              {Object.entries(groupedAssignments).map(([courseKey, courseAssignments]) => (
                <section key={courseKey} className="border-b border-slate-200 pb-10 last:border-0 last:pb-0">
                  <header className="mb-5 flex flex-col justify-between gap-3 sm:flex-row sm:items-center">
                    <div className="flex items-center gap-3">
                      <div className="grid size-11 shrink-0 place-items-center rounded-xl bg-teal-50 text-teal-600 shadow-sm">
                        <BookOpen size={20} />
                      </div>
                      <div>
                        <p className="text-xs font-bold uppercase text-teal-600">Course assignments</p>
                        <h2 className="mt-1 text-xl font-black text-ink">{courseAssignments[0].courseName} Assignments</h2>
                      </div>
                    </div>
                    <Badge tone="slate">{courseAssignments.length} {courseAssignments.length === 1 ? "assignment" : "assignments"}</Badge>
                  </header>
                  <div className="grid gap-4">
                    {courseAssignments.map((assignment) => (
                      <AssignmentCard key={assignment.assignmentId} assignment={assignment} onSubmit={setSelected} />
                    ))}
                  </div>
                </section>
              ))}
            </div>
          )}
        </section>

        {selected && (
          <div className="fixed inset-0 z-50 grid place-items-center bg-ink/50 p-4" onClick={() => setSelected(null)}>
            <form onSubmit={submit} className="w-full max-w-xl rounded-xl2 bg-white p-6 shadow-xl ring-1 ring-slate-100" onClick={(event) => event.stopPropagation()}>
              <h2 className="text-2xl font-black text-ink">{selected.title}</h2>
              <p className="mt-2 text-sm text-muted">{selected.courseName}</p>
              <div className="mt-4 rounded-xl bg-slate-50 p-4 ring-1 ring-slate-100">
                <p className="text-xs font-bold uppercase tracking-wide text-muted">Instructions</p>
                <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-ink">{selected.description?.trim() || "No instructions provided."}</p>
                <p className="mt-3 text-xs font-semibold text-muted">Due: {selected.dueDate ? formatDate(selected.dueDate) : "Not set"}</p>
              </div>
              <div className="mt-4 rounded-xl bg-slate-50 p-4 ring-1 ring-slate-100">
                <p className="text-xs font-bold uppercase tracking-wide text-muted">Instructor attachment</p>
                {selected.assignmentFileUrl ? (
                  <FileActionButtons url={selected.assignmentFileUrl} className="mt-3" previewLabel="Open" downloadLabel="Download" />
                ) : (
                  <p className="mt-2 text-sm text-muted">No attachment provided.</p>
                )}
              </div>
              {selected.submissionStatus === "Graded" && (
                <div className="mt-5 rounded-xl bg-teal-50 p-4 ring-1 ring-teal-100">
                  <p className={cn("font-bold", gradeTextColor(selected.grade))}>Grade: {selected.grade ?? "Not available"} / 100</p>
                  <p className="mt-2 whitespace-pre-wrap text-sm text-muted">Feedback: {selected.feedback?.trim() || "No feedback provided."}</p>
                </div>
              )}
              <textarea name="textAnswer" rows={5} placeholder="Write your answer" className="mt-5 w-full rounded-xl bg-slate-50 p-4 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500" />
              <input name="file" type="file" className="mt-4 block w-full rounded-xl bg-slate-50 p-3 text-sm ring-1 ring-slate-200" />
              <div className="mt-5 flex gap-3">
                <Button disabled={saving}>{saving ? "Submitting..." : "Submit"}</Button>
                <Button type="button" variant="ghost" onClick={() => setSelected(null)}>Cancel</Button>
              </div>
            </form>
          </div>
        )}
      </AuthGuard>
    </AppShell>
  );
}
