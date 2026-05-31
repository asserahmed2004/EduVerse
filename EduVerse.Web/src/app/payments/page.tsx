"use client";

import { CreditCard, Search, Wallet, XCircle } from "lucide-react";
import { FormEvent, useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { useToast } from "@/components/toast-provider";
import { Badge, Button, EmptyState, LoadingState, PageHeader, StatCard } from "@/components/ui";
import { paymentService, studentService } from "@/lib/api";
import { getStoredUser } from "@/lib/auth";
import type { Payment } from "@/lib/types";
import { formatCurrency, formatDate } from "@/lib/utils";

type AdminSummary = {
  totalPayments?: number;
  paidPayments?: number;
  pendingPayments?: number;
  failedPayments?: number;
  totalRevenue?: number;
};

export default function PaymentsPage() {
  const { showToast } = useToast();
  const [payments, setPayments] = useState<Payment[]>([]);
  const [summary, setSummary] = useState<AdminSummary | null>(null);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [status, setStatus] = useState("");
  const [search, setSearch] = useState("");
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const user = getStoredUser();
  const isAdmin = user?.role === "Admin";
  const isOrganizationAdmin = user?.role === "OrganizationAdmin";

  async function loadPayments(nextPage = page) {
    setLoading(true);
    setError("");
    try {
      if (isAdmin || isOrganizationAdmin) {
        const [summaryData, transactions] = await Promise.all([
          isAdmin ? paymentService.getAdminSummary() : paymentService.getOrganizationSummary(),
          isAdmin
            ? paymentService.getAdminTransactions({ page: nextPage, pageSize: 10, status: status || undefined, search: search || undefined, fromDate: fromDate || undefined, toDate: toDate || undefined })
            : paymentService.getOrganizationTransactions({ page: nextPage, pageSize: 10, status: status || undefined, search: search || undefined, fromDate: fromDate || undefined, toDate: toDate || undefined })
        ]);
        setSummary({
          totalPayments: summaryData.totalPayments ?? summaryData.TotalPayments ?? 0,
          paidPayments: summaryData.paidPayments ?? summaryData.PaidPayments ?? 0,
          pendingPayments: summaryData.pendingPayments ?? summaryData.PendingPayments ?? 0,
          failedPayments: summaryData.failedPayments ?? summaryData.FailedPayments ?? 0,
          totalRevenue: summaryData.totalRevenue ?? summaryData.TotalRevenue ?? 0
        });
        setPayments(transactions.items ?? []);
        setTotalPages(transactions.totalPages ?? transactions.TotalPages ?? 1);
        setPage(nextPage);
      } else if (user?.role === "Student") {
        setPayments(await studentService.getPayments());
      } else {
        setPayments([]);
      }
    } catch {
      setError("Could not load payments from the API.");
      showToast({ title: "Payments unavailable", message: "Could not load real payment data from the backend.", tone: "error" });
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadPayments(1);
  }, []);

  function applyFilters(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    loadPayments(1);
  }

  function exportCsv() {
    const headers = ["student", "course", "amount", "status", "method", "provider", "date", "reference"];
    const rows = payments.map((payment) => [
      payment.studentEmail ?? payment.studentName ?? payment.studentId,
      payment.courseName ?? payment.courseId,
      payment.totalPrice,
      payment.paymentStatus,
      payment.paymentMethod,
      payment.paymentProvider,
      payment.submittingDate,
      payment.merchantOrderId ?? payment.specialReference ?? ""
    ]);
    const csv = [headers, ...rows]
      .map((row) => row.map((value) => `"${String(value ?? "").replace(/"/g, "\"\"")}"`).join(","))
      .join("\n");
    const blob = new Blob([csv], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = "eduverse-payments.csv";
    link.click();
    URL.revokeObjectURL(url);
  }

  const paidAmount = isAdmin || isOrganizationAdmin ? (summary?.totalRevenue ?? 0) : payments.filter((item) => item.paymentStatus === "Paid").reduce((sum, item) => sum + item.totalPrice, 0);

  return (
    <AppShell>
      <AuthGuard roles={["Student", "OrganizationAdmin", "Admin"]}>
        <PageHeader eyebrow="Payments" title={isAdmin ? "Platform payments" : isOrganizationAdmin ? "Organization payments" : "Payment activity"} description="Track Paymob requests, payment methods, status, references, and transaction history." />

        <div className="mt-8 grid gap-5 md:grid-cols-4">
          <StatCard label="Total revenue" value={formatCurrency(paidAmount)} icon={Wallet} />
          <StatCard label="Successful payments" value={`${isAdmin || isOrganizationAdmin ? summary?.paidPayments ?? 0 : payments.filter((item) => item.paymentStatus === "Paid").length}`} icon={CreditCard} accent="amber" />
          <StatCard label="Pending payments" value={`${isAdmin || isOrganizationAdmin ? summary?.pendingPayments ?? 0 : payments.filter((item) => item.paymentStatus === "Pending").length}`} icon={CreditCard} />
          <StatCard label="Failed payments" value={`${isAdmin || isOrganizationAdmin ? summary?.failedPayments ?? 0 : payments.filter((item) => item.paymentStatus === "Failed").length}`} icon={XCircle} accent="coral" />
        </div>

        {(isAdmin || isOrganizationAdmin) && (
          <form onSubmit={applyFilters} className="mt-8 grid gap-3 rounded-xl2 bg-white p-4 shadow-soft ring-1 ring-slate-100 md:grid-cols-[150px_1fr_150px_150px_auto]">
            <select value={status} onChange={(event) => setStatus(event.target.value)} className="h-11 rounded-xl bg-slate-50 px-4 text-sm font-semibold outline-none ring-1 ring-slate-200">
              <option value="">All statuses</option>
              <option value="Paid">Paid</option>
              <option value="Pending">Pending</option>
              <option value="Failed">Failed</option>
            </select>
            <label className="flex h-11 items-center gap-2 rounded-xl bg-slate-50 px-4 ring-1 ring-slate-200">
              <Search size={18} className="text-muted" />
              <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search student, course, or reference" className="w-full bg-transparent text-sm outline-none" />
            </label>
            <input type="date" value={fromDate} onChange={(event) => setFromDate(event.target.value)} className="h-11 rounded-xl bg-slate-50 px-4 text-sm font-semibold outline-none ring-1 ring-slate-200" aria-label="From date" />
            <input type="date" value={toDate} onChange={(event) => setToDate(event.target.value)} className="h-11 rounded-xl bg-slate-50 px-4 text-sm font-semibold outline-none ring-1 ring-slate-200" aria-label="To date" />
            <Button>Apply filters</Button>
          </form>
        )}

        {(isAdmin || isOrganizationAdmin) && (
          <div className="mt-4 flex flex-col justify-between gap-3 sm:flex-row sm:items-center">
            <p className="text-sm font-bold text-muted">Showing {payments.length} transactions</p>
            <Button variant="ghost" onClick={exportCsv} disabled={payments.length === 0}>Export CSV</Button>
          </div>
        )}

        <section className="mt-8 rounded-xl2 bg-white shadow-soft ring-1 ring-slate-100">
          {loading ? (
            <LoadingState label="Loading payments" />
          ) : error ? (
            <EmptyState title="Payments unavailable" description={error} />
          ) : payments.length === 0 ? (
            <EmptyState title="No payments yet" description="No payments yet. Payment records will appear after checkout starts." />
          ) : (
            <>
              <div className="overflow-x-auto">
                <table className="w-full min-w-[860px] text-left">
                  <thead>
                    <tr className="border-b border-slate-100 text-sm text-muted">
                      <th className="px-5 py-4 font-semibold">Course</th>
                      <th className="px-5 py-4 font-semibold">Student</th>
                      <th className="px-5 py-4 font-semibold">Amount</th>
                      <th className="px-5 py-4 font-semibold">Method</th>
                      <th className="px-5 py-4 font-semibold">Status</th>
                      <th className="px-5 py-4 font-semibold">Reference</th>
                      <th className="px-5 py-4 font-semibold">Date</th>
                    </tr>
                  </thead>
                  <tbody>
                    {payments.map((payment) => (
                      <tr key={`${payment.courseId}-${payment.studentId}-${payment.merchantOrderId}-${payment.submittingDate}`} className="border-b border-slate-100 last:border-0">
                        <td className="px-5 py-4 text-sm font-semibold text-ink">{payment.courseName ?? payment.courseId}</td>
                        <td className="px-5 py-4 text-sm text-muted">{payment.studentEmail ?? payment.studentName ?? payment.studentId}</td>
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
              {(isAdmin || isOrganizationAdmin) && (
                <div className="flex items-center justify-between border-t border-slate-100 px-5 py-4">
                  <p className="text-sm font-semibold text-muted">Page {page} of {totalPages}</p>
                  <div className="flex gap-2">
                    <Button variant="ghost" disabled={page <= 1} onClick={() => loadPayments(page - 1)}>Previous</Button>
                    <Button variant="ghost" disabled={page >= totalPages} onClick={() => loadPayments(page + 1)}>Next</Button>
                  </div>
                </div>
              )}
            </>
          )}
        </section>
      </AuthGuard>
    </AppShell>
  );
}
