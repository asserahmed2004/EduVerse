"use client";

import { Building2, GraduationCap, Star, Wallet } from "lucide-react";
import Link from "next/link";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { Badge, EmptyState, LoadingState, PageHeader, StatCard } from "@/components/ui";
import { dashboardService } from "@/lib/api";
import type { OrganizationOverview } from "@/lib/types";
import { formatCurrency } from "@/lib/utils";

export default function OrganizationsPage() {
  const [organizations, setOrganizations] = useState<OrganizationOverview[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    dashboardService.getOrganizationsOverview()
      .then(setOrganizations)
      .catch(() => setError("Could not load organizations overview from the API."))
      .finally(() => setLoading(false));
  }, []);

  return (
    <AppShell>
      <AuthGuard roles={["Admin"]}>
        <PageHeader eyebrow="Admin" title="Organizations" description="Organization admins are shown as organizations until a dedicated Organization entity is added." />

        <div className="mt-8 grid gap-5 md:grid-cols-3">
          <StatCard label="Organizations" value={`${organizations.length}`} icon={Building2} />
          <StatCard label="Total revenue" value={formatCurrency(organizations.reduce((sum, item) => sum + item.revenue, 0))} icon={Wallet} accent="amber" />
          <StatCard label="Students" value={`${organizations.reduce((sum, item) => sum + item.studentsCount, 0)}`} icon={GraduationCap} accent="coral" />
        </div>

        <section className="mt-8 rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
          {loading ? (
            <LoadingState label="Loading organizations" />
          ) : error ? (
            <EmptyState title="Organizations unavailable" description={error} />
          ) : organizations.length === 0 ? (
            <EmptyState title="No organizations yet" description="Organization admin accounts will appear here." />
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[860px] text-left">
                <thead>
                  <tr className="border-b border-slate-100 text-sm text-muted">
                    <th className="px-4 py-3 font-semibold">Organization admin</th>
                    <th className="px-4 py-3 font-semibold">Email</th>
                    <th className="px-4 py-3 font-semibold">Courses</th>
                    <th className="px-4 py-3 font-semibold">Students</th>
                    <th className="px-4 py-3 font-semibold">Enrollments</th>
                    <th className="px-4 py-3 font-semibold">Revenue</th>
                    <th className="px-4 py-3 font-semibold">Rating</th>
                    <th className="px-4 py-3 font-semibold">Status</th>
                    <th className="px-4 py-3 font-semibold">Details</th>
                  </tr>
                </thead>
                <tbody>
                  {organizations.map((organization) => (
                    <tr key={organization.organizationAdminId} className="border-b border-slate-100 last:border-0">
                      <td className="px-4 py-4 text-sm font-bold text-ink">{organization.organizationAdminName}</td>
                      <td className="px-4 py-4 text-sm text-muted">{organization.email}</td>
                      <td className="px-4 py-4 text-sm text-ink">{organization.coursesCount}</td>
                      <td className="px-4 py-4 text-sm text-ink">{organization.studentsCount}</td>
                      <td className="px-4 py-4 text-sm text-ink">{organization.enrollmentsCount}</td>
                      <td className="px-4 py-4 text-sm font-semibold text-ink">{formatCurrency(organization.revenue)}</td>
                      <td className="px-4 py-4 text-sm text-muted"><Star className="mr-1 inline text-amber-500" size={15} />{organization.averageRating.toFixed(1)}</td>
                      <td className="px-4 py-4">
                        <Badge tone={organization.coursesCount > 0 ? "teal" : "slate"}>{organization.coursesCount > 0 ? "Active" : "No Courses"}</Badge>
                      </td>
                      <td className="px-4 py-4">
                        <Link href={`/organizations/${organization.organizationAdminId}`} className="text-sm font-bold text-teal-600 hover:text-teal-700">Details</Link>
                      </td>
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
