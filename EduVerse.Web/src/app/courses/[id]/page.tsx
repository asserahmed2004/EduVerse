"use client";

import { Award, BookOpen, CheckCircle2, Clock, CreditCard, ExternalLink, FileText, Link as LinkIcon, Star, Users } from "lucide-react";
import { useParams, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { FileActionButtons } from "@/components/file-actions";
import { useToast } from "@/components/toast-provider";
import { RecommendationSection } from "@/components/recommendation-section";
import { Badge, Button, EmptyState, LoadingState, PageHeader, ProgressBar } from "@/components/ui";
import { courseService, studentService } from "@/lib/api";
import { getStoredUser } from "@/lib/auth";
import type { Course, CourseAdminDetails, CourseProgress, Enrollment } from "@/lib/types";
import { cn, formatCurrency, formatDate, gradeTextColor } from "@/lib/utils";

export default function CourseDetailsPage() {
  const { showToast } = useToast();
  const params = useParams<{ id: string }>();
  const courseId = params.id;
  const router = useRouter();
  const [course, setCourse] = useState<Course | null>(null);
  const [adminDetails, setAdminDetails] = useState<CourseAdminDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [paymentLoading, setPaymentLoading] = useState(false);
  const [enrollLoading, setEnrollLoading] = useState(false);
  const [progressLoading, setProgressLoading] = useState(false);
  const [certificateLoading, setCertificateLoading] = useState(false);
  const [progress, setProgress] = useState<CourseProgress | null>(null);
  const [progressError, setProgressError] = useState("");
  const [enrollment, setEnrollment] = useState<Enrollment | null>(null);
  const [enrollmentLoading, setEnrollmentLoading] = useState(false);
  const [enrollmentError, setEnrollmentError] = useState("");
  const user = getStoredUser();
  const canViewAdminDetails = user?.role === "Admin" || user?.role === "OrganizationAdmin" || user?.role === "Instructor";
  const isStudent = user?.role === "Student";
  const isEnrolled = Boolean(enrollment) || Boolean(progress);
  const studentEnrollmentCheckLoading = Boolean(isStudent && (enrollmentLoading || progressLoading));

  useEffect(() => {
    let cancelled = false;

    async function loadCourse() {
      setLoading(true);
      setError("");
      setAdminDetails(null);
      try {
        const publicCourse = await courseService.getById(courseId);
        if (!cancelled) setCourse(publicCourse);

        if (canViewAdminDetails) {
          courseService.getAdminDetails(courseId)
            .then((details) => {
              if (cancelled) return;
              setAdminDetails(details);
              setCourse({
                id: details.courseId,
                name: details.name,
                title: details.title,
                description: details.description,
                price: details.price,
                duration: 0,
                rating: details.averageRating,
                category: details.category,
                instructorId: details.instructorId,
                instructorName: details.instructorName,
                organizationId: details.organizationId,
                organizationName: details.organizationName,
                organizationOwnerName: details.organizationOwner,
                organizationOwnerEmail: details.organizationOwnerEmail,
                studentsCount: details.studentsCount,
                sessionsCount: details.sessionsCount,
                imageUrl: details.imageUrl,
                isDeleted: details.isDeleted,
                deletedAt: details.deletedAt,
                deletedById: details.deletedById,
                deletedByName: details.deletedByName
              });
            })
            .catch(() => undefined);
        }
      } catch {
        if (!canViewAdminDetails) {
          if (!cancelled) setError("Course not found or unavailable.");
          return;
        }

        try {
          const details = await courseService.getAdminDetails(courseId);
          if (cancelled) return;
          setAdminDetails(details);
          setCourse({
            id: details.courseId,
            name: details.name,
            title: details.title,
            description: details.description,
            price: details.price,
            duration: 0,
            rating: details.averageRating,
            category: details.category,
            instructorId: details.instructorId,
            instructorName: details.instructorName,
            organizationId: details.organizationId,
            organizationName: details.organizationName,
            organizationOwnerName: details.organizationOwner,
            organizationOwnerEmail: details.organizationOwnerEmail,
            studentsCount: details.studentsCount,
            sessionsCount: details.sessionsCount,
            imageUrl: details.imageUrl,
            isDeleted: details.isDeleted,
            deletedAt: details.deletedAt,
            deletedById: details.deletedById,
            deletedByName: details.deletedByName
          });
        } catch {
          if (!cancelled) setError("Course not found or unavailable.");
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    }

    loadCourse();
    return () => { cancelled = true; };
  }, [courseId, canViewAdminDetails]);

  useEffect(() => {
    if (!isStudent) return;

    let cancelled = false;

    setEnrollmentLoading(true);
    setEnrollment(null);
    setEnrollmentError("");
    setProgressLoading(true);
    setProgress(null);
    setProgressError("");

    studentService.getEnrollment(courseId)
      .then((data) => {
        if (!cancelled) setEnrollment(data);
      })
      .catch((error) => {
        if (cancelled) return;
        const status = (error as { response?: { status?: number } })?.response?.status;
        setEnrollment(null);
        if (status !== 404) {
          setEnrollmentError("We could not verify your enrollment status. Please refresh before purchasing.");
        }
      })
      .finally(() => {
        if (!cancelled) setEnrollmentLoading(false);
      });

    studentService.getCourseProgress(courseId)
      .then((data) => {
        if (!cancelled) setProgress(data);
      })
      .catch(() => {
        if (cancelled) return;
        setProgress(null);
        setProgressError("Progress is unavailable. Enroll in this course or try again later.");
      })
      .finally(() => {
        if (!cancelled) setProgressLoading(false);
      });

    return () => { cancelled = true; };
  }, [courseId, isStudent]);

  async function enrollFree() {
    if (isEnrolled) {
      showToast({ title: "Already enrolled", message: "You are already enrolled in this course.", tone: "success" });
      return;
    }

    setEnrollLoading(true);
    try {
      const result = await studentService.enrollFree(courseId);
      showToast({ title: "Enrollment complete", message: result.message ?? "You are enrolled in this free course.", tone: "success" });
      const refreshedEnrollment = await studentService.getEnrollment(courseId).catch(() => null);
      const refreshed = await studentService.getCourseProgress(courseId).catch(() => null);
      if (refreshedEnrollment) setEnrollment(refreshedEnrollment);
      if (refreshed) setProgress(refreshed);
    } catch (error) {
      showToast({ title: "Enrollment failed", message: error instanceof Error ? error.message : "Could not enroll in this course.", tone: "error" });
    } finally {
      setEnrollLoading(false);
    }
  }

  async function markCompleted(sessionId: string) {
    try {
      const updated = await studentService.markSessionCompleted(sessionId);
      setProgress(updated);
      showToast({ title: "Session completed", message: "Your progress has been updated.", tone: "success" });
    } catch (error) {
      showToast({ title: "Progress update failed", message: error instanceof Error ? error.message : "Could not update this session.", tone: "error" });
    }
  }

  async function generateCertificate() {
    setCertificateLoading(true);
    try {
      const certificate = await studentService.generateCertificate(courseId);
      showToast({ title: "Certificate ready", message: certificate.certificateCode ?? "Certificate generated successfully.", tone: "success" });
      router.push("/certificates");
    } catch (error) {
      showToast({ title: "Certificate unavailable", message: error instanceof Error ? error.message : "Complete the course first to generate a certificate.", tone: "error" });
    } finally {
      setCertificateLoading(false);
    }
  }

  async function pay(method: "card" | "wallet") {
    if (isEnrolled) {
      showToast({ title: "Already enrolled", message: "You already own this course. Continue learning from the course content section.", tone: "success" });
      return;
    }

    setPaymentLoading(true);
    try {
      const redirectUrl = await studentService.createPayment(courseId, method);
      if (redirectUrl) {
        window.location.href = redirectUrl;
      }
    } catch {
      showToast({ title: "Payment service unavailable", message: "Payment service is currently unavailable. Please try again later.", tone: "error" });
    } finally {
      setPaymentLoading(false);
    }
  }

  function scrollToProgress() {
    document.getElementById("course-progress")?.scrollIntoView({ behavior: "smooth", block: "start" });
  }

  if (loading) {
    return (
      <AppShell>
        <LoadingState label="Loading course details" />
      </AppShell>
    );
  }

  if (error || !course) {
    return (
      <AppShell>
        <EmptyState title="Course unavailable" description={error || "Course details are not available."} />
      </AppShell>
    );
  }

  return (
    <AppShell>
      <PageHeader eyebrow="Course details" title={course.name} description={course.title} />

      <div className="mt-8 grid gap-8 xl:grid-cols-[1fr_380px]">
        <section className="overflow-hidden rounded-xl2 bg-white shadow-soft ring-1 ring-slate-100">
          {course.imageUrl ? <img src={course.imageUrl} alt={course.name} className="h-80 w-full object-cover" /> : <div className="grid h-80 place-items-center bg-teal-50 text-teal-600"><BookOpen size={42} /></div>}
          <div className="p-6">
            <div className="flex flex-wrap gap-2">
              <Badge>{course.category ?? course.categories?.[0]?.name ?? "Course"}</Badge>
              <Badge tone={course.isDeleted ? "coral" : "teal"}>{course.isDeleted ? "Deleted" : "Active"}</Badge>
            </div>
            <h2 className="mt-6 text-2xl font-bold text-ink">About this course</h2>
            <p className="mt-3 leading-7 text-muted">{course.description}</p>
            <div className="mt-6 grid gap-4 sm:grid-cols-3">
              <Metric icon={Clock} label="Sessions" value={`${course.sessionsCount ?? 0}`} />
              <Metric icon={Star} label="Average rating" value={(course.rating ?? 0).toFixed(1)} />
              <Metric icon={Users} label="Students" value={`${course.studentsCount ?? course.students ?? 0}`} />
            </div>
          </div>
        </section>

        <aside className="h-fit rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
          <p className="text-sm font-semibold text-muted">Course price</p>
          <p className="mt-2 text-4xl font-bold text-ink">{course.price <= 0 ? "Free" : formatCurrency(course.price)}</p>
          <div className="mt-5 space-y-3 text-sm text-muted">
            <p><span className="font-bold text-ink">Organization:</span> {course.organizationName ?? course.organizationOwnerName ?? "EduVerseOrganization"}</p>
            <p><span className="font-bold text-ink">Instructor:</span> {course.instructorName ?? "Unassigned"}</p>
            {course.isDeleted && <p><span className="font-bold text-ink">Deleted:</span> {course.deletedAt ? formatDate(course.deletedAt) : "Not recorded"} by {course.deletedByName ?? "Unknown"}</p>}
            {adminDetails?.restoredAt && <p><span className="font-bold text-ink">Restored:</span> {formatDate(adminDetails.restoredAt)} by {adminDetails.restoredByName ?? "Unknown"}</p>}
          </div>
          {isStudent && !course.isDeleted && (
            <div className="mt-6 grid gap-3">
              {studentEnrollmentCheckLoading ? (
                <Button disabled>
                  <CheckCircle2 size={18} />
                  Checking enrollment...
                </Button>
              ) : isEnrolled ? (
                <div className="rounded-xl bg-teal-50 p-4 ring-1 ring-teal-100">
                  <div className="flex items-start gap-3">
                    <CheckCircle2 size={20} className="mt-0.5 shrink-0 text-teal-600" />
                    <div>
                      <p className="font-bold text-ink">You are already enrolled in this course</p>
                      <p className="mt-1 text-sm text-muted">Continue learning, view sessions, and track your course progress below.</p>
                    </div>
                  </div>
                  <div className="mt-4 grid gap-2">
                    <Button onClick={scrollToProgress}>
                      <BookOpen size={18} />
                      Continue Learning
                    </Button>
                    <Button variant="ghost" onClick={scrollToProgress}>
                      <CheckCircle2 size={18} />
                      View Progress
                    </Button>
                  </div>
                </div>
              ) : enrollmentError ? (
                <div className="rounded-xl bg-amber-50 p-4 text-sm font-semibold text-amber-700 ring-1 ring-amber-100">
                  {enrollmentError}
                </div>
              ) : course.price <= 0 ? (
                <Button onClick={enrollFree} disabled={enrollLoading || Boolean(progress)}>
                  <CheckCircle2 size={18} />
                  {enrollLoading ? "Enrolling..." : "Enroll for free"}
                </Button>
              ) : (
                <>
                  <Button onClick={() => pay("card")} disabled={paymentLoading}>
                    <CreditCard size={18} />
                    Pay with card
                  </Button>
                  <Button variant="ghost" onClick={() => pay("wallet")} disabled={paymentLoading}>
                    <Award size={18} />
                    Pay with wallet
                  </Button>
                </>
              )}
            </div>
          )}
          {!user && !course.isDeleted && (
            <div className="mt-6 grid gap-3">
              <Button onClick={() => router.push("/login")}>
                <CreditCard size={18} />
                Login to enroll
              </Button>
              <Button variant="ghost" onClick={() => router.push("/register")}>
                <Award size={18} />
                Create account
              </Button>
            </div>
          )}
          {user && !isStudent && !course.isDeleted && (
            <div className="mt-6 rounded-xl bg-slate-50 p-4 text-sm font-semibold text-muted ring-1 ring-slate-100">
              Student purchase actions are hidden for your role.
            </div>
          )}
        </aside>
      </div>

      {isStudent && (
        <section id="course-progress" className="mt-8 scroll-mt-24 rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
          <div className="flex flex-col justify-between gap-4 md:flex-row md:items-center">
            <div>
              <h2 className="text-xl font-bold text-ink">Learning progress</h2>
              <p className="mt-1 text-sm text-muted">Sessions, materials, assignments, and completion status for this course.</p>
            </div>
            {progress?.isCompleted && (
              <Button onClick={generateCertificate} disabled={certificateLoading}>
                <Award size={18} />
                {certificateLoading ? "Generating..." : "Generate certificate"}
              </Button>
            )}
          </div>

          {progressLoading ? (
            <div className="mt-5">
              <LoadingState label="Loading learning progress" />
            </div>
          ) : !progress ? (
            <div className="mt-5">
              <EmptyState title={progressError ? "Progress unavailable" : "Not enrolled yet"} description={progressError || (course.price <= 0 ? "Enroll for free to unlock sessions and progress tracking." : "Complete payment to unlock the learning workspace.")} />
            </div>
          ) : (
            <div className="mt-6">
              <div className="flex items-center justify-between gap-4">
                <p className="text-sm font-semibold text-muted">Overall progress</p>
                <p className="text-sm font-bold text-teal-600">{Math.round(progress.progressPercentage)}%</p>
              </div>
              <div className="mt-2">
                <ProgressBar value={progress.progressPercentage} />
              </div>

              <div className="mt-6 space-y-4">
                {progress.sessions.length === 0 ? (
                  <Muted>No sessions have been added to this course yet.</Muted>
                ) : progress.sessions.map((session) => (
                  <article key={session.id} className="rounded-xl bg-slate-50 p-4 ring-1 ring-slate-100">
                    <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
                      <div>
                        <div className="flex flex-wrap items-center gap-2">
                          <Badge tone={session.isCompleted ? "teal" : "slate"}>{session.isCompleted ? "Completed" : `Session ${session.sessionNumber}`}</Badge>
                          {session.date && <span className="text-xs font-semibold text-muted">{formatDate(session.date)}</span>}
                        </div>
                        <h3 className="mt-3 text-lg font-bold text-ink">{session.title}</h3>
                        {session.description && <p className="mt-2 text-sm leading-6 text-muted">{session.description}</p>}
                      </div>
                      <Button variant="ghost" onClick={() => markCompleted(session.id)} disabled={session.isCompleted}>
                        <CheckCircle2 size={17} />
                        {session.isCompleted ? "Done" : "Mark completed"}
                      </Button>
                    </div>

                    <div className="mt-4 grid gap-3 md:grid-cols-2">
                      {(session.materials ?? []).map((material) => (
                        <MaterialLink
                          key={material.id}
                          title={material.title}
                          fileHref={material.fileUrl ?? material.filePath}
                          href={material.url ?? material.materialUrl ?? material.link}
                        />
                      ))}
                      {session.videoUrl && <LearningLink href={session.videoUrl} label="Video material" />}
                      {session.externalLink && <LearningLink href={session.externalLink} label="External link" />}
                      {session.fileUrl ? <FileResourceCard href={session.fileUrl} title="Session file" /> : (session.materials ?? []).length === 0 && <NoMaterial />}
                    </div>

                    {(session.assignments ?? []).length > 0 && (
                      <div className="mt-4 space-y-2">
                        {(session.assignments ?? []).map((assignment) => (
                          <div key={assignment.assignmentId} className="rounded-xl bg-white p-3 ring-1 ring-slate-100">
                            <div className="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
                              <div>
                                <p className="font-bold text-ink">{assignment.title}</p>
                                <p className="mt-1 text-xs font-semibold text-muted">Due: {assignment.dueDate ? formatDate(assignment.dueDate) : "Not set"}</p>
                                {assignment.submissionStatus === "Graded" && (
                                  <div className="mt-2 rounded-lg bg-teal-50 p-2 ring-1 ring-teal-100">
                                    <p className={cn("text-sm font-bold", gradeTextColor(assignment.grade))}>Grade: {assignment.grade ?? "Not available"} / 100</p>
                                    <p className="mt-1 whitespace-pre-wrap text-xs text-muted">Feedback: {assignment.feedback?.trim() || "No feedback provided."}</p>
                                  </div>
                                )}
                              </div>
                              <Badge tone={assignment.submissionStatus === "Graded" ? "teal" : assignment.submissionStatus === "Late" || assignment.submissionStatus === "Missing" ? "coral" : "amber"}>{assignment.submissionStatus}</Badge>
                            </div>
                          </div>
                        ))}
                      </div>
                    )}
                  </article>
                ))}
              </div>
            </div>
          )}
        </section>
      )}

      <RecommendationSection
        title="Similar Courses"
        description="Courses with matching tags, categories, and level."
        type="similar"
        courseId={courseId}
      />

      {adminDetails && (
        <section className="mt-8 grid gap-6 xl:grid-cols-2">
          <DetailsPanel title="Sessions list">
            {adminDetails.sessions.length === 0 ? <Muted>No sessions yet</Muted> : adminDetails.sessions.map((session) => (
              <Row key={session.id} title={session.title} meta={`Session ${session.sessionNumber} - ${session.duration ?? 0} minutes`} />
            ))}
          </DetailsPanel>
          <DetailsPanel title="Enrolled students">
            {adminDetails.students.length === 0 ? <Muted>No students yet</Muted> : adminDetails.students.map((student) => (
              <Row key={student.studentId} title={student.studentName || student.studentEmail || student.studentId} meta={`${Math.round(student.progression)}% progress - ${formatDate(student.enrollmentDate)}`} />
            ))}
          </DetailsPanel>
          <DetailsPanel title="Assignments">
            {adminDetails.assignments.length === 0 ? <Muted>No assignments yet</Muted> : adminDetails.assignments.map((assignment, index) => (
              <Row key={assignment.id ?? `${assignment.sessionId}-${index}`} title={assignment.subject ?? "Assignment"} meta={assignment.description ?? "No description"} />
            ))}
          </DetailsPanel>
          <DetailsPanel title="Recent payments">
            {adminDetails.recentPayments.length === 0 ? <Muted>No payments yet</Muted> : adminDetails.recentPayments.map((payment) => (
              <Row key={`${payment.studentId}-${payment.submittingDate}`} title={payment.studentEmail ?? payment.studentName ?? payment.studentId} meta={`${payment.paymentStatus} - ${formatCurrency(payment.totalPrice)} - ${formatDate(payment.submittingDate)}`} />
            ))}
          </DetailsPanel>
          <DetailsPanel title="Audit">
            <Row title="Deleted by" meta={adminDetails.deletedByName ?? adminDetails.deletedById ?? "Not recorded"} />
            <Row title="Deleted at" meta={adminDetails.deletedAt ? formatDate(adminDetails.deletedAt) : "Not recorded"} />
            <Row title="Restored by" meta={adminDetails.restoredByName ?? adminDetails.restoredById ?? "Not recorded"} />
            <Row title="Restored at" meta={adminDetails.restoredAt ? formatDate(adminDetails.restoredAt) : "Not recorded"} />
          </DetailsPanel>
        </section>
      )}
    </AppShell>
  );
}

function Metric({ icon: Icon, label, value }: { icon: any; label: string; value: string }) {
  return (
    <div className="rounded-xl bg-slate-50 p-4">
      <Icon size={20} className="text-teal-600" />
      <p className="mt-3 text-xs font-semibold uppercase text-muted">{label}</p>
      <p className="mt-1 text-lg font-bold text-ink">{value}</p>
    </div>
  );
}

function DetailsPanel({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
      <h2 className="text-lg font-bold text-ink">{title}</h2>
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

function LearningLink({ href, label }: { href: string; label: string }) {
  return (
    <a href={href} target="_blank" rel="noreferrer" className="rounded-xl bg-white p-3 text-sm font-semibold text-ink ring-1 ring-slate-100 transition hover:-translate-y-0.5 hover:shadow-soft">
      <span className="inline-flex items-center gap-2">
        <ExternalLink size={16} className="text-teal-600" />
        {label}
      </span>
    </a>
  );
}

function FileResourceCard({ href, title }: { href: string; title: string }) {
  return (
    <div className="rounded-xl bg-white p-3 ring-1 ring-slate-100">
      <span className="inline-flex items-center gap-2 text-sm font-semibold text-ink">
        <FileText size={16} className="text-teal-600" />
        {title}
      </span>
      <FileActionButtons url={href} className="mt-3" fullWidth previewLabel="Open" downloadLabel="Download" />
    </div>
  );
}

function MaterialLink({ href, fileHref, title }: { href?: string; fileHref?: string; title: string }) {
  if (fileHref) return <FileResourceCard href={fileHref} title={title} />;
  if (!href) return <NoMaterial />;

  return (
    <a href={href} target="_blank" rel="noreferrer" className="rounded-xl bg-white p-3 text-sm font-semibold text-ink ring-1 ring-slate-100 transition hover:-translate-y-0.5 hover:shadow-soft">
      <span className="inline-flex items-center gap-2">
        <LinkIcon size={16} className="text-teal-600" />
        {title}
      </span>
    </a>
  );
}

function NoMaterial() {
  return <div className="rounded-xl bg-white p-3 text-sm font-semibold text-muted ring-1 ring-slate-100">No material available</div>;
}
