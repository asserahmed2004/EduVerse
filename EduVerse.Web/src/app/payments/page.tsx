"use client";

import { CreditCard, ExternalLink, Wallet } from "lucide-react";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { useToast } from "@/components/toast-provider";
import { Badge, EmptyState, LoadingState, PageHeader, StatCard } from "@/components/ui";
import { paymentService, studentService } from "@/lib/api";
import { getStoredUser } from "@/lib/auth";
import type { Payment } from "@/lib/types";
import { formatCurrency, formatDate } from "@/lib/utils";

export default function PaymentsPage() {
  const { showToast } = useToast();
  const [payments, setPayments] = useState<Payment[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const user = getStoredUser();
    const loader = user?.role === "Admin"
      ? paymentService.getAdminTransactions(1, 50).then((data) => data.items ?? [])
      : studentService.getPayments();

    loader
      .then(setPayments)
      .catch(() => {
        setError("Could not load payments from the API.");
        showToast({ title: "Payments unavailable", message: "Could not load real payment data from the backend.", tone: "error" });
      })
      .finally(() => setLoading(false));
  }, [showToast]);

  const paidAmount = payments.filter((item) => item.paymentStatus === "Paid").reduce((sum, item) => sum + item.totalPrice, 0);

  return (
    <AppShell>
      <AuthGuard roles={["Student", "OrganizationAdmin", "Admin"]}>
        <PageHeader eyebrow="Payments" title="Payment activity" description="Track Paymob requests, payment methods, status, and references." />

        <div className="mt-8 grid gap-5 md:grid-cols-3">
          <StatCard label="Total paid" value={formatCurrency(paidAmount)} icon={Wallet} />
          <StatCard label="Transactions" value={`${payments.length}`} icon={CreditCard} accent="amber" />
          <StatCard label="Pending" value={`${payments.filter((item) => item.paymentStatus === "Pending").length}`} icon={ExternalLink} accent="coral" />
        </div>

        <section className="mt-8 rounded-xl2 bg-white shadow-soft ring-1 ring-slate-100">
          {loading ? (
            <LoadingState label="Loading payments" />
          ) : error ? (
            <EmptyState title="Payments unavailable" description={error} />
          ) : payments.length === 0 ? (
            <EmptyState title="No payments yet" description="Payment records will appear here after checkout starts." />
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[760px] text-left">
                <thead>
                  <tr className="border-b border-slate-100 text-sm text-muted">
                    <th className="px-5 py-4 font-semibold">Course</th>
                    <th className="px-5 py-4 font-semibold">Amount</th>
                    <th className="px-5 py-4 font-semibold">Method</th>
                    <th className="px-5 py-4 font-semibold">Status</th>
                    <th className="px-5 py-4 font-semibold">Reference</th>
                    <th className="px-5 py-4 font-semibold">Date</th>
                  </tr>
                </thead>
                <tbody>
                  {payments.map((payment) => (
                    <tr key={`${payment.courseId}-${payment.merchantOrderId}`} className="border-b border-slate-100 last:border-0">
                      <td className="px-5 py-4 text-sm font-semibold text-ink">{payment.courseName ?? payment.courseId}</td>
                      <td className="px-5 py-4 text-sm text-ink">{formatCurrency(payment.totalPrice)}</td>
                      <td className="px-5 py-4 text-sm capitalize text-muted">{payment.paymentMethod}</td>
                      <td className="px-5 py-4">
                        <Badge tone={payment.paymentStatus === "Paid" ? "teal" : payment.paymentStatus === "Pending" ? "amber" : "coral"}>{payment.paymentStatus}</Badge>
                      </td>
                      <td className="px-5 py-4 text-sm text-muted">{payment.merchantOrderId ?? payment.specialReference ?? "-"}</td>
                      <td className="px-5 py-4 text-sm text-muted">{formatDate(payment.submittingDate)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      </AuthGuard>
    </AppShell>
  );
}
