"use client";

import { FormEvent, useEffect, useState } from "react";
import { Building2, GraduationCap, Plus, Star, Wallet } from "lucide-react";
import Link from "next/link";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { SmartImage } from "@/components/smart-image";
import { useToast } from "@/components/toast-provider";
import { Badge, Button, EmptyState, LoadingState, PageHeader, StatCard } from "@/components/ui";
import { getApiErrorMessage, organizationService } from "@/lib/api";
import { SEED_IMAGES } from "@/lib/image-fallbacks";
import type { OrganizationOverview } from "@/lib/types";
import { formatCurrency } from "@/lib/utils";

export default function OrganizationsPage() {
  const { showToast } = useToast();
  const [organizations, setOrganizations] = useState<OrganizationOverview[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [showCreate, setShowCreate] = useState(false);
  const [saving, setSaving] = useState(false);

  async function loadOrganizations() {
    setLoading(true);
    setError("");
    try {
      setOrganizations(await organizationService.getAll());
    } catch (loadError) {
      setError(getApiErrorMessage(loadError, "Could not load organizations from the API."));
      setOrganizations([]);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadOrganizations();
  }, []);

  async function createOrganization(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setSaving(true);
    try {
      const organization = await organizationService.create({
        name: String(form.get("name") ?? ""),
        description: String(form.get("description") ?? ""),
        email: String(form.get("email") ?? ""),
        phoneNumber: String(form.get("phoneNumber") ?? ""),
        websiteUrl: String(form.get("websiteUrl") ?? "")
      });
      setOrganizations((current) => [...current, organization]);
      setError("");
      showToast({ title: "Organization created", message: "The organization is now available.", tone: "success" });
      setShowCreate(false);
    } catch (error) {
      showToast({ title: "Create failed", message: getApiErrorMessage(error, "Check required fields and Admin permissions."), tone: "error" });
    } finally {
      setSaving(false);
    }
  }

  async function toggleStatus(organization: OrganizationOverview) {
    const id = organization.organizationId ?? organization.organizationAdminId;
    const isSuspended = organization.status?.toLowerCase() === "suspended";
    try {
      const updated = isSuspended
        ? await organizationService.activate(id)
        : await organizationService.suspend(id);
      setOrganizations((current) => current.map((item) => {
        const itemId = item.organizationId ?? item.organizationAdminId;
        return itemId === id ? updated : item;
      }));
      setError("");
      showToast({ title: isSuspended ? "Organization activated" : "Organization suspended", message: organization.organizationName ?? organization.organizationAdminName, tone: "success" });
    } catch (error) {
      showToast({ title: "Status update failed", message: getApiErrorMessage(error, "The backend rejected the organization status update."), tone: "error" });
    }
  }

  return (
    <AppShell>
      <AuthGuard roles={["Admin"]}>
        <PageHeader
          eyebrow="Admin"
          title="Organizations"
          description="Manage real platform organizations, their admins, instructors, courses, revenue, and activity."
          action={<Button onClick={() => setShowCreate(true)}><Plus size={16} /> Create organization</Button>}
        />

        <div className="mt-8 grid gap-5 md:grid-cols-3">
          <StatCard label="Organizations" value={`${organizations.length}`} icon={Building2} />
          <StatCard label="Total revenue" value={formatCurrency(organizations.reduce((sum, item) => sum + item.revenue, 0))} icon={Wallet} accent="amber" />
          <StatCard label="Students" value={`${organizations.reduce((sum, item) => sum + item.studentsCount, 0)}`} icon={GraduationCap} accent="coral" />
        </div>

        <section className="mt-8 rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100 dark:bg-slate-900 dark:ring-white/10">
          {loading ? (
            <LoadingState label="Loading organizations" />
          ) : error ? (
            <EmptyState title="Organizations unavailable" description={error} />
          ) : organizations.length === 0 ? (
            <EmptyState title="No organizations yet" description="Create an organization to start assigning organization admins and instructors." />
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[980px] text-left">
                <thead>
                  <tr className="border-b border-slate-100 text-sm text-muted dark:border-white/10">
                    <th className="px-4 py-3 font-semibold">Organization</th>
                    <th className="px-4 py-3 font-semibold">Email</th>
                    <th className="px-4 py-3 font-semibold">Phone</th>
                    <th className="px-4 py-3 font-semibold">Courses</th>
                    <th className="px-4 py-3 font-semibold">Students</th>
                    <th className="px-4 py-3 font-semibold">Enrollments</th>
                    <th className="px-4 py-3 font-semibold">Revenue</th>
                    <th className="px-4 py-3 font-semibold">Rating</th>
                    <th className="px-4 py-3 font-semibold">Status</th>
                    <th className="px-4 py-3 font-semibold">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {organizations.map((organization) => {
                    const id = organization.organizationId ?? organization.organizationAdminId;
                    const status = organization.status ?? (organization.coursesCount > 0 ? "Active" : "No Courses");
                    const suspended = status.toLowerCase() === "suspended";
                    return (
                      <tr key={id} className="border-b border-slate-100 last:border-0 dark:border-white/10">
                        <td className="px-4 py-4">
                          <div className="flex items-center gap-3">
                            <SmartImage
                              src={organization.logoUrl}
                              fallbackSrc={SEED_IMAGES.organization}
                              alt=""
                              className="size-11 rounded-xl object-cover"
                            />
                            <div>
                              <p className="text-sm font-bold text-ink dark:text-white">{organization.organizationName ?? organization.organizationAdminName}</p>
                              <p className="mt-1 text-xs text-muted">{organization.websiteUrl || "No website"}</p>
                            </div>
                          </div>
                        </td>
                        <td className="px-4 py-4 text-sm text-muted">{organization.email || "Not available"}</td>
                        <td className="px-4 py-4 text-sm text-muted">{organization.phoneNumber || "Not available"}</td>
                        <td className="px-4 py-4 text-sm text-ink dark:text-white">{organization.coursesCount}</td>
                        <td className="px-4 py-4 text-sm text-ink dark:text-white">{organization.studentsCount}</td>
                        <td className="px-4 py-4 text-sm text-ink dark:text-white">{organization.enrollmentsCount}</td>
                        <td className="px-4 py-4 text-sm font-semibold text-ink dark:text-white">{formatCurrency(organization.revenue)}</td>
                        <td className="px-4 py-4 text-sm text-muted"><Star className="mr-1 inline text-amber-500" size={15} />{organization.averageRating.toFixed(1)}</td>
                        <td className="px-4 py-4">
                          <Badge tone={suspended ? "coral" : organization.coursesCount > 0 ? "teal" : "slate"}>{status}</Badge>
                        </td>
                        <td className="px-4 py-4">
                          <div className="flex flex-wrap gap-3">
                            <Link href={`/organizations/${id}`} className="text-sm font-bold text-teal-600 hover:text-teal-700">Details</Link>
                            <button onClick={() => toggleStatus(organization)} className="text-sm font-bold text-amber-500">
                              {suspended ? "Activate" : "Suspend"}
                            </button>
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </section>

        {showCreate && (
          <div className="fixed inset-0 z-50 grid place-items-center bg-ink/50 p-4" onClick={() => setShowCreate(false)}>
            <form onSubmit={createOrganization} className="w-full max-w-xl rounded-xl2 bg-white p-6 shadow-xl ring-1 ring-slate-100 dark:bg-slate-900 dark:ring-white/10" onClick={(event) => event.stopPropagation()}>
              <div className="flex items-start justify-between gap-4">
                <div>
                  <p className="text-sm font-bold uppercase text-teal-600">New organization</p>
                  <h2 className="mt-2 text-2xl font-black text-ink dark:text-white">Create organization</h2>
                </div>
                <button type="button" onClick={() => setShowCreate(false)} className="rounded-xl bg-slate-50 px-3 py-2 text-sm font-bold text-muted dark:bg-white/10">Close</button>
              </div>
              <div className="mt-6 grid gap-4">
                <Input name="name" label="Name" required />
                <Input name="email" label="Email" type="email" />
                <Input name="phoneNumber" label="Phone number" />
                <Input name="websiteUrl" label="Website URL" />
                <label className="text-sm font-bold text-ink dark:text-white">
                  Description
                  <textarea name="description" rows={3} className="mt-2 w-full rounded-xl bg-slate-50 px-4 py-3 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500 dark:bg-slate-800 dark:ring-white/10" />
                </label>
              </div>
              <Button className="mt-5 w-full" disabled={saving}>{saving ? "Creating..." : "Create organization"}</Button>
            </form>
          </div>
        )}
      </AuthGuard>
    </AppShell>
  );
}

function Input({ name, label, type = "text", required = false }: { name: string; label: string; type?: string; required?: boolean }) {
  return (
    <label className="text-sm font-bold text-ink dark:text-white">
      {label}
      <input name={name} type={type} required={required} className="mt-2 h-12 w-full rounded-xl bg-slate-50 px-4 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500 dark:bg-slate-800 dark:ring-white/10" />
    </label>
  );
}
