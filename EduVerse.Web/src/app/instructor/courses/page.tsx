"use client";

import { BookOpen, Plus, Trash2, UploadCloud, Users } from "lucide-react";
import { FormEvent, type InputHTMLAttributes, type SelectHTMLAttributes, useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { useToast } from "@/components/toast-provider";
import { Badge, Button, EmptyState, LoadingState, PageHeader, StatCard } from "@/components/ui";
import { courseService, organizationService } from "@/lib/api";
import { getCurrentUserId, getStoredUser } from "@/lib/auth";
import type { Course, CourseSession, OrganizationDetails } from "@/lib/types";
import { formatCurrency } from "@/lib/utils";

export default function InstructorCoursesPage() {
  const { showToast } = useToast();
  const [courses, setCourses] = useState<Course[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [assignmentCourseId, setAssignmentCourseId] = useState("");
  const [sessions, setSessions] = useState<CourseSession[]>([]);
  const [sessionsLoading, setSessionsLoading] = useState(false);
  const [organization, setOrganization] = useState<OrganizationDetails | null>(null);
  const [assigningInstructor, setAssigningInstructor] = useState("");

  async function loadCourses() {
    setLoading(true);
    setError("");
    try {
      const data = await courseService.getAll();
      setCourses(data);
      setAssignmentCourseId((current) => current || data[0]?.id || "");
      return true;
    } catch {
      setError("Could not load courses.");
      return false;
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadCourses();
    const user = getStoredUser();
    if (user?.organizationId) {
      organizationService.getById(user.organizationId)
        .then(setOrganization)
        .catch(() => setOrganization(null));
    }
  }, []);

  useEffect(() => {
    if (!assignmentCourseId) {
      setSessions([]);
      return;
    }

    setSessionsLoading(true);
    courseService.getSessions(assignmentCourseId)
      .then(setSessions)
      .catch(() => {
        setSessions([]);
        showToast({ title: "Sessions unavailable", message: "Could not load sessions for the selected course.", tone: "error" });
      })
      .finally(() => setSessionsLoading(false));
  }, [assignmentCourseId, showToast]);

  async function createCourse(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formElement = event.currentTarget;
    setSaving(true);
    const form = new FormData(formElement);

    try {
      const result = await courseService.create(form);
      showToast({ title: "Course created", message: result.message ?? "The course was sent to the backend successfully.", tone: "success" });
      formElement.reset();

      const refreshed = await loadCourses();
      if (!refreshed) {
        showToast({ title: "Course created, but failed to refresh list", message: "Refresh the page to see the latest course list.", tone: "info" });
      }
    } catch (error) {
      showToast({
        title: "Course creation failed",
        message: error instanceof Error ? error.message : "Check required fields, image file, categories, and your role permissions.",
        tone: "error"
      });
    } finally {
      setSaving(false);
    }
  }

  async function deleteCourse(id: string) {
    try {
      await courseService.delete(id);
      setCourses((current) => current.filter((course) => course.id !== id));
      showToast({ title: "Course deleted", tone: "success" });
    } catch {
      showToast({ title: "Delete failed", message: "You may not own this course or the backend rejected the request.", tone: "error" });
    }
  }

  async function assignInstructor(courseId: string, instructorId: string) {
    if (!instructorId) {
      showToast({ title: "Select instructor", message: "Choose an instructor before saving.", tone: "info" });
      return;
    }

    setAssigningInstructor(courseId);
    try {
      await courseService.assignInstructor(courseId, instructorId);
      setCourses((current) => current.map((course) => {
        if (course.id !== courseId) return course;
        const instructor = organization?.instructors?.find((item) => item.userId === instructorId);
        return { ...course, instructorId, instructorName: instructor?.fullName ?? instructor?.userName ?? "Assigned instructor" };
      }));
      showToast({ title: "Instructor assigned", message: "The course instructor was updated successfully.", tone: "success" });
    } catch (error) {
      showToast({ title: "Instructor assignment failed", message: error instanceof Error ? error.message : "Could not assign instructor to this course.", tone: "error" });
    } finally {
      setAssigningInstructor("");
    }
  }

  async function addSession(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const currentUserId = getCurrentUserId();
    if (currentUserId) {
      form.set("TrainerId", currentUserId);
    }

    try {
      await courseService.addSession(form);
      showToast({ title: "Session added", message: "The session was sent to the backend successfully.", tone: "success" });
      event.currentTarget.reset();
      if (String(form.get("Course")) === assignmentCourseId) {
        courseService.getSessions(assignmentCourseId).then(setSessions).catch(() => undefined);
      }
    } catch {
      showToast({ title: "Session failed", message: "Check course id, trainer id, file, and ownership permissions.", tone: "error" });
    }
  }

  async function addAssignment(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    try {
      await courseService.addAssignment(form);
      showToast({ title: "Assignment added", message: "The assignment was sent to the backend successfully.", tone: "success" });
      event.currentTarget.reset();
    } catch {
      showToast({ title: "Assignment failed", message: "Check session id, file, and ownership permissions.", tone: "error" });
    }
  }

  return (
    <AppShell>
      <AuthGuard roles={["OrganizationAdmin"]}>
        <PageHeader eyebrow="Organization tools" title="Course management" description="Create, review, and soft-delete courses owned by your organization." />

        <div className="mt-8 grid gap-5 md:grid-cols-3">
          <StatCard label="Courses" value={`${courses.length}`} icon={BookOpen} />
          <StatCard label="Catalog value" value={formatCurrency(courses.reduce((sum, course) => sum + course.price, 0))} icon={UploadCloud} accent="amber" />
          <StatCard label="Students" value="No data yet" icon={Users} accent="coral" />
        </div>

        <section className="mt-8 grid gap-8 xl:grid-cols-[420px_1fr]">
          <div className="space-y-6">
            <form onSubmit={createCourse} className="h-fit rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
              <div className="flex items-center gap-3">
                <div className="grid size-11 place-items-center rounded-xl bg-teal-50 text-teal-600">
                  <Plus size={20} />
                </div>
                <div>
                  <h2 className="text-lg font-bold text-ink">Create course</h2>
                  <p className="text-sm text-muted">Uses `POST /Course/Create`</p>
                </div>
              </div>

              <div className="mt-6 space-y-4">
                <Field name="Name" label="Name" required />
                <Field name="Title" label="Title" required />
                <label className="block">
                  <span className="text-sm font-semibold text-ink">Description</span>
                  <textarea name="Description" className="mt-2 min-h-24 w-full rounded-xl bg-slate-50 px-4 py-3 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500" required />
                </label>
                <div className="grid gap-4 sm:grid-cols-2">
                  <Field name="Price" label="Price" type="number" required />
                  <Field name="Duration" label="Duration" type="number" defaultValue="0" required />
                </div>
                <Field name="Categories" label="Category IDs comma separated" placeholder="guid,guid" required />
                <label className="block">
                  <span className="text-sm font-semibold text-ink">Image</span>
                  <input name="Image" type="file" accept="image/*" className="mt-2 w-full rounded-xl bg-slate-50 px-4 py-3 text-sm ring-1 ring-slate-200" required />
                </label>
              </div>

              <Button className="mt-6 w-full" disabled={saving}>
                <Plus size={18} />
                {saving ? "Saving..." : "Create course"}
              </Button>
            </form>

            <form onSubmit={addSession} className="rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
              <h2 className="text-lg font-bold text-ink">Add session</h2>
              <p className="mt-1 text-sm text-muted">Uses `POST /Course/AddSession`</p>
              <div className="mt-5 space-y-4">
                <SelectField name="Course" label="Course" required disabled={courses.length === 0} defaultValue="">
                  <option value="" disabled>Select course</option>
                  {courses.map((course) => <option key={course.id} value={course.id}>{course.name}</option>)}
                </SelectField>
                <Field name="Title" label="Session title" required />
                <Field name="SessionNumber" label="Session number" type="number" required />
                <label className="block">
                  <span className="text-sm font-semibold text-ink">Description</span>
                  <textarea name="Description" className="mt-2 min-h-20 w-full rounded-xl bg-slate-50 px-4 py-3 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500" />
                </label>
                <Field name="VideoUrl" label="Video URL" />
                <Field name="ExternalLink" label="External link" />
                <label className="block">
                  <span className="text-sm font-semibold text-ink">Session file</span>
                  <input name="File" type="file" className="mt-2 w-full rounded-xl bg-slate-50 px-4 py-3 text-sm ring-1 ring-slate-200" />
                </label>
              </div>
              <Button className="mt-5 w-full" variant="ghost">Add session</Button>
            </form>

            <form onSubmit={addAssignment} className="rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
              <h2 className="text-lg font-bold text-ink">Add assignment</h2>
              <p className="mt-1 text-sm text-muted">Uses `POST /Course/AddAssignment`</p>
              <div className="mt-5 space-y-4">
                <SelectField
                  label="Course"
                  value={assignmentCourseId}
                  onChange={(event) => setAssignmentCourseId(event.target.value)}
                  disabled={courses.length === 0}
                >
                  <option value="" disabled>Select course</option>
                  {courses.map((course) => <option key={course.id} value={course.id}>{course.name}</option>)}
                </SelectField>
                <SelectField name="SessionId" label="Session" required disabled={!assignmentCourseId || sessionsLoading || sessions.length === 0} defaultValue="">
                  <option value="" disabled>{sessionsLoading ? "Loading sessions..." : "Select session"}</option>
                  {sessions.map((session) => (
                    <option key={session.id} value={session.id}>
                      {session.sessionNumber ? `Session ${session.sessionNumber} - ` : ""}{session.title}
                    </option>
                  ))}
                </SelectField>
                <Field name="Subject" label="Subject" required />
                <Field name="DueDate" label="Due date" type="date" />
                <label className="block">
                  <span className="text-sm font-semibold text-ink">Description</span>
                  <textarea name="Description" className="mt-2 min-h-20 w-full rounded-xl bg-slate-50 px-4 py-3 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500" required />
                </label>
                <label className="block">
                  <span className="text-sm font-semibold text-ink">Assignment file</span>
                  <input name="File" type="file" className="mt-2 w-full rounded-xl bg-slate-50 px-4 py-3 text-sm ring-1 ring-slate-200" required />
                </label>
              </div>
              <Button className="mt-5 w-full" variant="ghost">Add assignment</Button>
            </form>
          </div>

          <div>
            {error && <div className="mb-5 rounded-xl bg-coral-100 px-4 py-3 text-sm font-semibold text-coral-500">{error}</div>}
            {loading ? (
              <LoadingState label="Loading managed courses" />
            ) : courses.length === 0 ? (
              <EmptyState title="No courses" description="Create your first course from the form." />
            ) : (
              <div className="space-y-4">
                {courses.map((course) => (
                  <article key={course.id} className="rounded-xl2 bg-white p-5 shadow-soft ring-1 ring-slate-100">
                    <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
                      <div className="flex gap-4">
                        <div className="h-20 w-24 shrink-0 overflow-hidden rounded-xl bg-slate-100">
                          {course.imageUrl ? <img src={course.imageUrl} alt={course.name} className="size-full object-cover" /> : null}
                        </div>
                        <div>
                          <Badge>{course.name}</Badge>
                          <h3 className="mt-2 text-lg font-bold text-ink">{course.title}</h3>
                          <p className="mt-1 text-sm text-muted">{formatCurrency(course.price)} - {course.rating.toFixed(1)} rating</p>
                          <p className="mt-1 text-sm text-muted">Instructor: {course.instructorName ?? "Unassigned"}</p>
                        </div>
                      </div>
                      <div className="grid gap-3 md:min-w-72">
                        <div className="grid gap-2 md:grid-cols-[1fr_auto]">
                          <select
                            defaultValue={course.instructorId ?? ""}
                            className="h-11 rounded-xl bg-slate-50 px-3 text-sm font-semibold text-ink outline-none ring-1 ring-slate-200"
                            onChange={(event) => assignInstructor(course.id, event.target.value)}
                            disabled={(organization?.instructors ?? []).length === 0 || assigningInstructor === course.id}
                          >
                            <option value="">Instructor: Unassigned</option>
                            {(organization?.instructors ?? []).map((instructor) => (
                              <option key={instructor.userId} value={instructor.userId}>{instructor.fullName || instructor.userName}</option>
                            ))}
                          </select>
                        </div>
                        <Button variant="ghost" onClick={() => deleteCourse(course.id)}>
                          <Trash2 size={17} />
                          Delete
                        </Button>
                      </div>
                    </div>
                  </article>
                ))}
              </div>
            )}
          </div>
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

function SelectField(props: SelectHTMLAttributes<HTMLSelectElement> & { label: string }) {
  const { label, children, ...selectProps } = props;
  return (
    <label className="block">
      <span className="text-sm font-semibold text-ink">{label}</span>
      <select className="mt-2 h-12 w-full rounded-xl bg-slate-50 px-4 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500 disabled:cursor-not-allowed disabled:opacity-60" {...selectProps}>
        {children}
      </select>
    </label>
  );
}
