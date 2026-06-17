"use client";

import { Award, BookOpen, CheckCircle2, Clock, CreditCard, ExternalLink, FileText, Link as LinkIcon, PlayCircle, Star, Users } from "lucide-react";
import { useParams, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { FileActionButtons } from "@/components/file-actions";
import { useToast } from "@/components/toast-provider";
import { RecommendationSection } from "@/components/recommendation-section";
import { Badge, Button, EmptyState, LoadingState, PageHeader, ProgressBar } from "@/components/ui";
import { courseService, getApiErrorMessage, getFileResourceType, getPreviewFileUrl, isEmbeddableVideoFileUrl, isKnownExternalVideoUrl, openFile, studentService } from "@/lib/api";
import { getStoredUser } from "@/lib/auth";
import type { AssignmentProgress, CertificateEligibility, Course, CourseAdminDetails, CourseProgress, Enrollment } from "@/lib/types";
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
  const [errorTitle, setErrorTitle] = useState("Course unavailable");
  const [paymentLoading, setPaymentLoading] = useState(false);
  const [enrollLoading, setEnrollLoading] = useState(false);
  const [progressLoading, setProgressLoading] = useState(false);
  const [assignmentProgressLoading, setAssignmentProgressLoading] = useState(false);
  const [certificateEligibilityLoading, setCertificateEligibilityLoading] = useState(false);
  const [certificateLoading, setCertificateLoading] = useState(false);
  const [progress, setProgress] = useState<CourseProgress | null>(null);
  const [assignmentProgress, setAssignmentProgress] = useState<AssignmentProgress | null>(null);
  const [certificateEligibility, setCertificateEligibility] = useState<CertificateEligibility | null>(null);
  const [progressError, setProgressError] = useState("");
  const [assignmentProgressError, setAssignmentProgressError] = useState("");
  const [certificateEligibilityError, setCertificateEligibilityError] = useState("");
  const [togglingSessionId, setTogglingSessionId] = useState<string | null>(null);
  const [enrollment, setEnrollment] = useState<Enrollment | null>(null);
  const [enrollmentLoading, setEnrollmentLoading] = useState(false);
  const [enrollmentError, setEnrollmentError] = useState("");
  const [ratingSaving, setRatingSaving] = useState(false);
  const user = getStoredUser();
  const canViewAdminDetails = user?.role === "Admin" || user?.role === "OrganizationAdmin" || user?.role === "Instructor";
  const isStudent = user?.role === "Student";
  const isEnrolled = Boolean(enrollment) || Boolean(progress);
  const studentEnrollmentCheckLoading = Boolean(isStudent && (enrollmentLoading || progressLoading));
  const enrollmentProgressValue = Math.max(enrollment?.progression ?? 0, enrollment?.progressPercentage ?? 0);
  const hasCompletedCourseForRating = Boolean(enrollment && (
    enrollment.isCompleted ||
    enrollment.completedAt ||
    enrollment.graduationDate ||
    enrollmentProgressValue >= 100
  ));

  useEffect(() => {
    let cancelled = false;

    async function loadCourse() {
      setLoading(true);
      setError("");
      setErrorTitle("Course unavailable");
      setAdminDetails(null);
      try {
        if (process.env.NODE_ENV === "development") {
          console.log("[EduVerse] opening course route id", courseId);
        }
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
      } catch (courseError) {
        const status = (courseError as { response?: { status?: number } })?.response?.status;
        if (process.env.NODE_ENV === "development") {
          console.error("[EduVerse] course details load failed", { courseId, status, courseError });
        }

        if (!canViewAdminDetails) {
          if (!cancelled) {
            setErrorTitle(status === 404 ? "Course unavailable" : "Course details unavailable");
            setError(status === 404 ? "Course not found or unavailable." : getApiErrorMessage(courseError, "Could not load this course from the backend."));
          }
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
        } catch (adminError) {
          const adminStatus = (adminError as { response?: { status?: number } })?.response?.status;
          if (!cancelled) {
            setErrorTitle(adminStatus === 404 ? "Course unavailable" : "Course details unavailable");
            setError(adminStatus === 404 ? "Course not found or unavailable." : getApiErrorMessage(adminError, "Could not load this course from the backend."));
          }
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
    setAssignmentProgressLoading(true);
    setAssignmentProgress(null);
    setAssignmentProgressError("");
    setCertificateEligibilityLoading(true);
    setCertificateEligibility(null);
    setCertificateEligibilityError("");

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

    studentService.getAssignmentProgress(courseId)
      .then((data) => {
        if (!cancelled) setAssignmentProgress(data);
      })
      .catch(() => {
        if (cancelled) return;
        setAssignmentProgress(null);
        setAssignmentProgressError("Assignment progress is unavailable. Enroll in this course or try again later.");
      })
      .finally(() => {
        if (!cancelled) setAssignmentProgressLoading(false);
      });

    studentService.getCertificateEligibility(courseId)
      .then((data) => {
        if (!cancelled) setCertificateEligibility(data);
      })
      .catch(() => {
        if (cancelled) return;
        setCertificateEligibility(null);
        setCertificateEligibilityError("Certificate eligibility is unavailable right now.");
      })
      .finally(() => {
        if (!cancelled) setCertificateEligibilityLoading(false);
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
      const refreshedAssignmentProgress = await studentService.getAssignmentProgress(courseId).catch(() => null);
      const refreshedEligibility = await studentService.getCertificateEligibility(courseId).catch(() => null);
      if (refreshedEnrollment) setEnrollment(refreshedEnrollment);
      if (refreshed) setProgress(refreshed);
      if (refreshedAssignmentProgress) setAssignmentProgress(refreshedAssignmentProgress);
      if (refreshedEligibility) setCertificateEligibility(refreshedEligibility);
    } catch (error) {
      showToast({ title: "Enrollment failed", message: error instanceof Error ? error.message : "Could not enroll in this course.", tone: "error" });
    } finally {
      setEnrollLoading(false);
    }
  }

  async function toggleSessionDone(sessionId: string) {
    setTogglingSessionId(sessionId);
    try {
      const updated = await studentService.toggleSessionDone(sessionId);
      setProgress((current) => {
        if (!current) return current;
        return {
          ...current,
          doneSessions: updated.doneSessions,
          totalSessions: updated.totalSessions,
          progressPercentage: updated.progressPercentage,
          isCompleted: updated.totalSessions > 0 && updated.doneSessions === updated.totalSessions,
          completedAt: updated.totalSessions > 0 && updated.doneSessions === updated.totalSessions ? new Date().toISOString() : undefined,
          sessions: current.sessions.map((session) => session.id === updated.sessionId
            ? {
              ...session,
              isDone: updated.isDone,
              doneAt: updated.doneAt,
              isCompleted: updated.isDone,
              completedAt: updated.doneAt
            }
            : session)
        };
      });
      showToast({
        title: updated.isDone ? "Session marked done" : "Session marked not done",
        message: "Your personal progress has been updated.",
        tone: "success"
      });
    } catch (error) {
      showToast({ title: "Progress update failed", message: error instanceof Error ? error.message : "Could not update this session.", tone: "error" });
    } finally {
      setTogglingSessionId(null);
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

  async function rateCourse(ratingValue: number) {
    if (!course) return;
    if (!isEnrolled) {
      showToast({ title: "Enrollment required", message: "Enroll in this course before rating it.", tone: "info" });
      return;
    }
    if (!hasCompletedCourseForRating) {
      showToast({ title: "Course not completed", message: "You can rate this course after completing it.", tone: "info" });
      return;
    }

    setRatingSaving(true);
    try {
      const result = await courseService.addRating(course.id, ratingValue);
      setCourse((current) => current ? {
        ...current,
        rating: result.averageRating,
        ratingCount: result.ratingCount,
        userRating: result.userRating
      } : current);
      showToast({ title: "Rating saved", message: result.message ?? "Thanks for rating this course.", tone: "success" });
    } catch (error) {
      showToast({ title: "Rating failed", message: error instanceof Error ? error.message : "Could not save your rating.", tone: "error" });
    } finally {
      setRatingSaving(false);
    }
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
        <EmptyState title={errorTitle} description={error || "Course details are not available."} />
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
              <Metric icon={Star} label="Average rating" value={`${(course.rating ?? 0).toFixed(1)} (${course.ratingCount ?? 0})`} />
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
        <CourseRatingPanel
          currentRating={course.userRating ?? 0}
          averageRating={course.rating ?? 0}
          ratingCount={course.ratingCount ?? 0}
          disabled={studentEnrollmentCheckLoading || !isEnrolled || !hasCompletedCourseForRating || ratingSaving}
          loading={studentEnrollmentCheckLoading}
          saving={ratingSaving}
          message={!isEnrolled
            ? "Enroll in this course before rating it."
            : hasCompletedCourseForRating
              ? "Your rating helps other learners choose confidently."
              : "You can rate this course after completing it."}
          onRate={rateCourse}
        />
      )}

      {isStudent && (
        <section id="course-progress" className="mt-8 scroll-mt-24 rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
          <div className="flex flex-col justify-between gap-4 md:flex-row md:items-center">
            <div>
              <h2 className="text-xl font-bold text-ink">Personal progress</h2>
              <p className="mt-1 text-sm text-muted">Track sessions and materials for yourself while learning this course.</p>
              <p className="mt-2 rounded-xl bg-amber-50 px-3 py-2 text-xs font-semibold text-amber-700 ring-1 ring-amber-100">
                This progress is for personal tracking only and does not affect certificate eligibility.
              </p>
            </div>
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
                <p className="text-sm font-semibold text-muted">Personal Progress</p>
                <p className="text-sm font-bold text-teal-600">
                  {Math.round(progress.progressPercentage)}% ({progress.doneSessions} / {progress.totalSessions} sessions)
                </p>
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
                          <Badge tone={session.isDone ? "teal" : "slate"}>{session.isDone ? "Done" : `Session ${session.sessionNumber}`}</Badge>
                          {session.date && <span className="text-xs font-semibold text-muted">{formatDate(session.date)}</span>}
                        </div>
                        <h3 className="mt-3 text-lg font-bold text-ink">{session.title}</h3>
                        {session.description && <p className="mt-2 text-sm leading-6 text-muted">{session.description}</p>}
                      </div>
                      <Button variant="ghost" onClick={() => toggleSessionDone(session.id)} disabled={togglingSessionId === session.id}>
                        <CheckCircle2 size={17} />
                        {togglingSessionId === session.id ? "Updating..." : session.isDone ? "Done" : "Mark as Done"}
                      </Button>
                    </div>

                    <div className="mt-4 grid gap-3 md:grid-cols-2">
                      <SessionResourceGrid session={session} />
                    </div>

                    {(session.assignments ?? []).length > 0 && (
                      <div className="mt-4 space-y-2">
                        {(session.assignments ?? []).map((assignment) => (
                          <div key={assignment.assignmentId} className="rounded-xl bg-white p-3 ring-1 ring-slate-100">
                            <div className="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
                              <div>
                                <p className="font-bold text-ink">{assignment.title}</p>
                                <p className="mt-1 text-xs font-semibold text-muted">Due: {assignment.dueDate ? formatDate(assignment.dueDate) : "Not set"}</p>
                                {assignment.assignmentFileUrl && (
                                  <div className="mt-2 rounded-lg bg-slate-50 p-2 ring-1 ring-slate-100">
                                    <p className="text-xs font-bold uppercase tracking-wide text-muted">Instructor attachment</p>
                                    <FileActionButtons url={assignment.assignmentFileUrl} className="mt-2" previewLabel="Open" downloadLabel="Download" />
                                  </div>
                                )}
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

      {isStudent && isEnrolled && (
        <section className="mt-8 rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
          <div className="flex flex-col justify-between gap-4 md:flex-row md:items-start">
            <div>
              <h2 className="text-xl font-bold text-ink">Assignment progress</h2>
              <p className="mt-1 text-sm text-muted">Official progress for certificate eligibility. This is calculated from real assignment submissions.</p>
            </div>
            <Badge tone={assignmentProgress?.hasRequiredAssignmentProgress ? "teal" : "amber"}>
              Required: {assignmentProgress?.requiredPercentage ?? 80}%
            </Badge>
          </div>

          {assignmentProgressLoading ? (
            <div className="mt-5">
              <LoadingState label="Loading assignment progress" />
            </div>
          ) : assignmentProgress ? (
            <div className="mt-6">
              <div className="flex items-center justify-between gap-4">
                <p className="text-sm font-semibold text-muted">Assignment Progress</p>
                <p className="text-sm font-bold text-teal-600">{Math.round(assignmentProgress.assignmentProgressPercentage)}%</p>
              </div>
              <div className="mt-2">
                <ProgressBar value={assignmentProgress.assignmentProgressPercentage} />
              </div>
              <div className="mt-4 grid gap-3 md:grid-cols-3">
                <Metric icon={FileText} label="Submitted Assignments" value={`${assignmentProgress.submittedAssignments} / ${assignmentProgress.totalAssignments}`} />
                <Metric icon={Award} label="Required For Certificate" value={`${assignmentProgress.requiredPercentage}%`} />
                <Metric icon={CheckCircle2} label="Status" value={assignmentProgress.hasRequiredAssignmentProgress ? "Ready" : "In progress"} />
              </div>
              <p className={cn("mt-4 rounded-xl p-4 text-sm font-semibold ring-1", assignmentProgress.hasRequiredAssignmentProgress ? "bg-teal-50 text-teal-700 ring-teal-100" : "bg-amber-50 text-amber-700 ring-amber-100")}>
                {assignmentProgress.hasRequiredAssignmentProgress
                  ? "You have completed the required assignment progress."
                  : "You need to submit at least 80% of assignments to become eligible for the certificate."}
              </p>
            </div>
          ) : (
            <div className="mt-5">
              <EmptyState title="Assignment progress unavailable" description={assignmentProgressError || "Assignment progress will appear after assignments are added to this course."} />
            </div>
          )}
        </section>
      )}

      {isStudent && isEnrolled && (
        <section className="mt-8 rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
          <div className="flex flex-col justify-between gap-4 md:flex-row md:items-start">
            <div>
              <h2 className="text-xl font-bold text-ink">Certificate eligibility</h2>
              <p className="mt-1 text-sm text-muted">Certificates depend on official assignment progress and course duration, not personal session tracking.</p>
            </div>
            <Button onClick={generateCertificate} disabled={certificateLoading || !certificateEligibility?.canReceiveCertificate}>
              <Award size={18} />
              {certificateLoading ? "Generating..." : "Generate certificate"}
            </Button>
          </div>

          {certificateEligibilityLoading ? (
            <div className="mt-5">
              <LoadingState label="Checking certificate eligibility" />
            </div>
          ) : certificateEligibility ? (
            <div className="mt-6">
              <div className="grid gap-3 md:grid-cols-3">
                <Metric icon={FileText} label="Assignment Progress" value={`${Math.round(certificateEligibility.assignmentProgressPercentage)}%`} />
                <Metric icon={Award} label="Required" value={`${certificateEligibility.requiredPercentage}%`} />
                <Metric icon={Clock} label="Duration Finished" value={certificateEligibility.isCourseDurationFinished ? "Yes" : "No"} />
              </div>
              <p className={cn("mt-4 rounded-xl p-4 text-sm font-semibold ring-1", certificateEligibility.canReceiveCertificate ? "bg-teal-50 text-teal-700 ring-teal-100" : "bg-amber-50 text-amber-700 ring-amber-100")}>
                {certificateEligibility.message}
              </p>
            </div>
          ) : (
            <div className="mt-5">
              <EmptyState title="Certificate eligibility unavailable" description={certificateEligibilityError || "Eligibility will appear after enrollment is verified."} />
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

function CourseRatingPanel({
  currentRating,
  averageRating,
  ratingCount,
  disabled,
  loading,
  saving,
  message,
  onRate
}: {
  currentRating: number;
  averageRating: number;
  ratingCount: number;
  disabled: boolean;
  loading: boolean;
  saving: boolean;
  message: string;
  onRate: (rating: number) => void;
}) {
  const roundedCurrentRating = Math.round(currentRating);

  return (
    <section className="mt-8 rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
      <div className="flex flex-col justify-between gap-4 md:flex-row md:items-start">
        <div>
          <h2 className="text-xl font-bold text-ink">Rate this course</h2>
          <p className="mt-1 text-sm text-muted">{message}</p>
          <p className="mt-2 text-xs font-semibold text-muted">
            Average: {averageRating.toFixed(1)} from {ratingCount} {ratingCount === 1 ? "rating" : "ratings"}
          </p>
        </div>
        {currentRating > 0 && <Badge tone="teal">Your rating: {currentRating} / 5</Badge>}
      </div>
      <div className="mt-5 flex flex-wrap gap-2">
        {[1, 2, 3, 4, 5].map((rating) => (
          <button
            key={rating}
            type="button"
            onClick={() => onRate(rating)}
            disabled={disabled}
            aria-label={`Rate ${rating} out of 5`}
            className={cn(
              "inline-flex h-11 w-11 items-center justify-center rounded-xl ring-1 ring-slate-200 transition",
              roundedCurrentRating >= rating ? "bg-amber-100 text-amber-500" : "bg-white text-slate-400",
              disabled ? "cursor-not-allowed opacity-60" : "hover:-translate-y-0.5 hover:bg-amber-50 hover:text-amber-500 hover:shadow-soft"
            )}
          >
            <Star size={19} fill={roundedCurrentRating >= rating ? "currentColor" : "none"} />
          </button>
        ))}
      </div>
      {(loading || saving) && (
        <p className="mt-3 text-sm font-semibold text-muted">
          {saving ? "Saving rating..." : "Checking rating eligibility..."}
        </p>
      )}
    </section>
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

function SessionResourceGrid({ session }: { session: CourseProgress["sessions"][number] }) {
  const usedResourceUrls = new Set<string>();
  const resources: React.ReactNode[] = [];

  (session.materials ?? []).forEach((material) => {
    const fileHref = material.fileUrl ?? material.filePath;
    const href = material.url ?? material.materialUrl ?? material.link;
    const resourceKey = fileHref ? normalizeResourceKey(fileHref) : undefined;
    if (resourceKey) usedResourceUrls.add(resourceKey);

    resources.push(<MaterialLink key={material.id} title={material.title} fileHref={fileHref} href={href} />);
  });

  const addFileResource = (href: string | undefined, title: string, key: string) => {
    if (!href) return;
    const resourceKey = normalizeResourceKey(href);
    if (usedResourceUrls.has(resourceKey)) return;
    usedResourceUrls.add(resourceKey);
    resources.push(<SessionFileResourceCard key={key} href={href} title={title} />);
  };

  if (session.videoUrl) {
    if (getFileResourceType(session.videoUrl) !== "unknown") {
      addFileResource(session.videoUrl, "Session video", "session-video");
    } else {
      resources.push(<ExternalVideoCard key="session-video-link" href={session.videoUrl} title="Session video" />);
    }
  }

  if (session.externalLink) {
    if (getFileResourceType(session.externalLink) !== "unknown") {
      addFileResource(session.externalLink, "External resource", "external-resource");
    } else if (isKnownExternalVideoUrl(session.externalLink)) {
      resources.push(<ExternalVideoCard key="external-video-link" href={session.externalLink} title="External video" />);
    } else {
      resources.push(<LearningLink key="external-link" href={session.externalLink} label="External link" />);
    }
  }

  if (session.fileUrl) {
    addFileResource(session.fileUrl, "Session file", "session-file");
  }

  return resources.length > 0 ? <>{resources}</> : <NoMaterial />;
}

function normalizeResourceKey(value: string) {
  const previewUrl = getPreviewFileUrl(value) ?? value;
  try {
    const url = new URL(previewUrl);
    url.searchParams.delete("download");
    return url.toString();
  } catch {
    return previewUrl.trim();
  }
}

function SessionFileResourceCard({ href, title }: { href: string; title: string }) {
  const resourceType = getFileResourceType(href);

  if (resourceType === "video") return <VideoResourceCard href={href} title={title} />;
  if (resourceType === "archive") return <ArchiveResourceCard href={href} title={title} />;
  if (resourceType === "office") return <OfficeResourceCard href={href} title={title} />;
  if (resourceType === "pdf" || resourceType === "image") return <FileResourceCard href={href} title={title} />;

  return <UnknownFileResourceCard href={href} title={title} />;
}

function VideoResourceCard({ href, title }: { href: string; title: string }) {
  const [previewFailed, setPreviewFailed] = useState(false);
  const previewUrl = getPreviewFileUrl(href) ?? href;
  const canAttemptPreview = isEmbeddableVideoFileUrl(href);

  return (
    <div className="rounded-xl bg-white p-3 ring-1 ring-slate-100 md:col-span-2">
      <span className="inline-flex items-center gap-2 text-sm font-semibold text-ink">
        <PlayCircle size={16} className="text-teal-600" />
        {title}
      </span>
      {canAttemptPreview ? (
        <>
          <p className="mt-3 text-xs font-bold uppercase tracking-wide text-muted">Play video</p>
          <div className="mt-2 overflow-hidden rounded-xl bg-ink">
            <video
              controls
              preload="metadata"
              src={previewUrl}
              className="aspect-video w-full bg-ink"
              onError={() => setPreviewFailed(true)}
            />
          </div>
        </>
      ) : (
        <p className="mt-3 rounded-xl bg-amber-50 p-3 text-sm font-semibold text-amber-700 ring-1 ring-amber-100">
          Preview is not supported for this video format. You can open or download it.
        </p>
      )}
      {previewFailed && (
        <p className="mt-3 rounded-xl bg-amber-50 p-3 text-sm font-semibold text-amber-700 ring-1 ring-amber-100">
          Preview is not supported for this video format. You can open or download it.
        </p>
      )}
      <FileActionButtons url={href} className="mt-3" fullWidth previewLabel="Open video" downloadLabel="Download" />
    </div>
  );
}

function OfficeResourceCard({ href, title }: { href: string; title: string }) {
  return (
    <div className="rounded-xl bg-white p-3 ring-1 ring-slate-100">
      <span className="inline-flex items-center gap-2 text-sm font-semibold text-ink">
        <FileText size={16} className="text-teal-600" />
        {title}
      </span>
      <p className="mt-2 rounded-xl bg-amber-50 p-3 text-xs font-semibold text-amber-700 ring-1 ring-amber-100">
        Office files may open in a new tab if your browser supports preview. Download is always available.
      </p>
      <FileActionButtons url={href} className="mt-3" fullWidth previewLabel="Open" downloadLabel="Download" />
    </div>
  );
}

function ArchiveResourceCard({ href, title }: { href: string; title: string }) {
  return (
    <div className="rounded-xl bg-white p-3 ring-1 ring-slate-100">
      <span className="inline-flex items-center gap-2 text-sm font-semibold text-ink">
        <FileText size={16} className="text-teal-600" />
        {title}
      </span>
      <p className="mt-2 rounded-xl bg-slate-50 p-3 text-xs font-semibold text-muted ring-1 ring-slate-100">
        Archive files cannot be previewed.
      </p>
      <FileActionButtons url={href} className="mt-3" fullWidth showPreview={false} downloadLabel="Download" />
    </div>
  );
}

function UnknownFileResourceCard({ href, title }: { href: string; title: string }) {
  return (
    <div className="rounded-xl bg-white p-3 ring-1 ring-slate-100">
      <span className="inline-flex items-center gap-2 text-sm font-semibold text-ink">
        <FileText size={16} className="text-teal-600" />
        {title}
      </span>
      <p className="mt-2 text-xs font-semibold text-muted">Your browser may preview this file. Download is always available.</p>
      <FileActionButtons url={href} className="mt-3" fullWidth previewLabel="Open" downloadLabel="Download" />
    </div>
  );
}

function ExternalVideoCard({ href, title }: { href: string; title: string }) {
  return (
    <div className="rounded-xl bg-white p-3 ring-1 ring-slate-100">
      <span className="inline-flex items-center gap-2 text-sm font-semibold text-ink">
        <ExternalLink size={16} className="text-teal-600" />
        {title}
      </span>
      <p className="mt-2 text-xs font-semibold text-muted">Open this video in a new tab.</p>
      <Button type="button" className="mt-3 w-full" onClick={() => openFile(href)}>
        <ExternalLink size={16} />
        Open video
      </Button>
    </div>
  );
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
  if (fileHref) {
    return <SessionFileResourceCard href={fileHref} title={title} />;
  }
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
