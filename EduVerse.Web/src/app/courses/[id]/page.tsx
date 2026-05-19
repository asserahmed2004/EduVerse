"use client";

import { Award, Clock, CreditCard, Star, Users } from "lucide-react";
import { useParams, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { useToast } from "@/components/toast-provider";
import { Badge, Button, EmptyState, LoadingState, PageHeader } from "@/components/ui";
import { courseService, studentService } from "@/lib/api";
import type { Course } from "@/lib/types";
import { formatCurrency } from "@/lib/utils";

export default function CourseDetailsPage() {
  const { showToast } = useToast();
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const [course, setCourse] = useState<Course | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [paymentLoading, setPaymentLoading] = useState(false);
  const [message, setMessage] = useState("");

  useEffect(() => {
    courseService.getById(params.id)
      .then(setCourse)
      .catch(() => setError("Could not load this course from the API."))
      .finally(() => setLoading(false));
  }, [params.id]);

  async function pay(method: "card" | "wallet") {
    setPaymentLoading(true);
    setMessage("");
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
          <img src={course.imageUrl} alt={course.name} className="h-80 w-full object-cover" />
          <div className="p-6">
            <div className="flex flex-wrap gap-2">
              {(course.categories ?? []).map((category) => (
                <Badge key={category.name}>{category.name}</Badge>
              ))}
            </div>
            <h2 className="mt-6 text-2xl font-bold text-ink">About this course</h2>
            <p className="mt-3 leading-7 text-muted">{course.description}</p>
            <div className="mt-6 grid gap-4 sm:grid-cols-3">
              <Metric icon={Clock} label="Duration" value={`${course.duration || 12}h`} />
              <Metric icon={Star} label="Rating" value={course.rating.toFixed(1)} />
              <Metric icon={Users} label="Students" value={`${course.students ?? 800}+`} />
            </div>
          </div>
        </section>

        <aside className="h-fit rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
          <p className="text-sm font-semibold text-muted">Course price</p>
          <p className="mt-2 text-4xl font-bold text-ink">{formatCurrency(course.price)}</p>
          <p className="mt-3 text-sm leading-6 text-muted">Pay through Paymob and track payment status from your dashboard.</p>
          {message && <div className="mt-5 rounded-xl bg-amber-100 px-4 py-3 text-sm font-semibold text-amber-500">{message}</div>}
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
        </aside>
      </div>
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
