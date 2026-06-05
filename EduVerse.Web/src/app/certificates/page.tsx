"use client";

import { Award, Download, ShieldCheck } from "lucide-react";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { useToast } from "@/components/toast-provider";
import { Button, EmptyState, LinkButton, LoadingState, PageHeader, StatCard } from "@/components/ui";
import { studentService } from "@/lib/api";
import type { Certificate } from "@/lib/types";
import { formatDate } from "@/lib/utils";

export default function CertificatesPage() {
  const { showToast } = useToast();
  const [certificates, setCertificates] = useState<Certificate[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    studentService.getCertificates().then((data) => {
      setCertificates(data);
    }).catch(() => {
      setError("Could not load certificates from the API.");
      showToast({ title: "Certificates unavailable", message: "Could not load real certificate data from the backend.", tone: "error" });
    })
      .finally(() => setLoading(false));
  }, [showToast]);

  return (
    <AppShell>
      <AuthGuard roles={["Student"]}>
        <PageHeader eyebrow="Certificates" title="Your achievements" description="View and download certificates issued after course completion." />
        {error && <div className="mt-6 rounded-xl bg-coral-100 px-4 py-3 text-sm font-semibold text-coral-500">{error}</div>}

        <div className="mt-8 grid gap-5 md:grid-cols-2">
          <StatCard label="Certificates" value={`${certificates.length}`} icon={Award} />
          <StatCard label="Verified records" value={`${certificates.filter((certificate) => certificate.fileUrl).length}`} icon={ShieldCheck} accent="amber" />
        </div>

        <div className="mt-8 grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          {loading ? (
            <div className="md:col-span-2 xl:col-span-3">
              <LoadingState label="Loading certificates" />
            </div>
          ) : certificates.length === 0 ? (
            <div className="md:col-span-2 xl:col-span-3">
              <EmptyState title="No certificates yet" description="Finish a course to receive a certificate." />
            </div>
          ) : (
            certificates.map((certificate) => (
              <article key={certificate.id} className="rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
                <div className="grid size-14 place-items-center rounded-xl bg-amber-100 text-amber-500">
                  <Award />
                </div>
                <h2 className="mt-5 text-xl font-bold text-ink">{certificate.courseName}</h2>
                <p className="mt-2 text-sm text-muted">Issued at {formatDate(certificate.issuedAt)}</p>
                <p className="mt-2 text-xs font-semibold text-muted">Code: {certificate.certificateCode ?? certificate.id}</p>
                <p className="mt-1 text-xs font-semibold text-muted">Status: {certificate.status ?? "Valid"}</p>
                {certificate.fileUrl ? (
                  <LinkButton href={certificate.fileUrl} variant="ghost" className="mt-6 w-full">
                    <Download size={18} />
                    Download
                  </LinkButton>
                ) : (
                  <Button variant="ghost" className="mt-6 w-full cursor-not-allowed opacity-60" disabled>
                    <Download size={18} />
                    Certificate file not available
                  </Button>
                )}
                <LinkButton href={`/verify-certificate?code=${encodeURIComponent(certificate.certificateCode ?? certificate.id)}`} variant="ghost" className="mt-3 w-full">
                  <ShieldCheck size={18} />
                  Verify
                </LinkButton>
              </article>
            ))
          )}
        </div>
      </AuthGuard>
    </AppShell>
  );
}
