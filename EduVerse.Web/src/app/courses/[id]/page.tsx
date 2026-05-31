"use client";

import { Award, BookOpen, Clock, CreditCard, Star, Users } from "lucide-react";
import { useParams, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { useToast } from "@/components/toast-provider";
import { Badge, Button, EmptyState, LoadingState, PageHeader } from "@/components/ui";
import { courseService, studentService } from "@/lib/api";
import { getStoredUser } from "@/lib/auth";
import type { Course, CourseAdminDetails } from "@/lib/types";
import { formatCurrency, formatDate } from "@/lib/utils";

export default function CourseDetailsPage() {
  const { showToast } = useToast();
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const [course, setCourse] = useState<Course | null>(null);
  const [adminDetails, setAdminDetails] = useState<CourseAdminDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [paymentLoading, setPaymentLoading] = useState(false);
  const user = getStoredUser();
  const canViewAdminDetails = user?.role === "Admin" || user?.role === "OrganizationAdmin" || user?.role === "Instructor";

  useEffect(() => {
    const loader = canViewAdminDetails
      ? courseService.getAdminDetails(params.id).then((details) => {
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
          instructorName: details.instructorName,
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
      : courseService.getById(params.id).then(setCourse);

    loader
      .catch(() => setError("Could not load this course from the API."))
      .finally(() => setLoading(false));
  }, [params.id, canViewAdminDetails]);

  async function pay(method: "card" | "wallet") {
    setPaymentLoading(true);
    try {
      const redirectUrl = await studentService.createPayment(params.id, method);
      if (redirectUrl) {
        window.location.href = redirectUrl;
      }
    } catch {
      showToast({ title: "Payment API unavailable", message: "Opening the local payment page so the flow remains testable.", tone: "info" });
      router.push(`/payments?courseId=${params.id}&method=${method}`);
    } finally {
      setPaymentLoading(false);
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
          <p className="mt-2 text-4xl font-bold text-ink">{formatCurrency(course.price)}</p>
          <div className="mt-5 space-y-3 text-sm text-muted">
            <p><span className="font-bold text-ink">Course Id:</span> {course.id}</p>
            <p><span className="font-bold text-ink">Organization:</span> {course.organizationOwnerName ?? "Not available"}</p>
            <p><span className="font-bold text-ink">Instructor:</span> {course.instructorName ?? "Unassigned"}</p>
            {course.isDeleted && <p><span className="font-bold text-ink">Deleted:</span> {course.deletedAt ? formatDate(course.deletedAt) : "Not recorded"} by {course.deletedByName ?? "Unknown"}</p>}
            {adminDetails?.restoredAt && <p><span className="font-bold text-ink">Restored:</span> {formatDate(adminDetails.restoredAt)} by {adminDetails.restoredByName ?? "Unknown"}</p>}
          </div>
          {user?.role === "Student" && !course.isDeleted && (
            <div className="mt-6 grid gap-3">
              <Button onClick={() => pay("card")} disabled={paymentLoading}>
                <CreditCard size={18} />
                Pay with card
              </Button>
              <Button variant="ghost" onClick={() => pay("wallet")} disabled={paymentLoading}>
                <Award size={18} />
                Pay with wallet
              </Button>
            </div>
          )}
        </aside>
      </div>

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
