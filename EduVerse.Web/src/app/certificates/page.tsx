"use client";

import { Award, CheckCircle2, Download, LockKeyhole } from "lucide-react";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { useToast } from "@/components/toast-provider";
import { Button, EmptyState, LoadingState, PageHeader, StatCard } from "@/components/ui";
import { studentService } from "@/lib/api";
import type { Certificate, CertificateEligibility, Enrollment } from "@/lib/types";
import { formatDate } from "@/lib/utils";

type CertificateCourseItem = {
  courseId: string;
  courseName: string;
  enrollment?: Enrollment;
  certificate?: Certificate;
  eligibility?: CertificateEligibility;
};

export default function CertificatesPage() {
  const { showToast } = useToast();
  const [items, setItems] = useState<CertificateCourseItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [downloadingCourseId, setDownloadingCourseId] = useState("");

  useEffect(() => {
    let active = true;

    async function loadCertificates() {
      try {
        const [certificates, enrollments] = await Promise.all([
          studentService.getCertificates(),
          studentService.getEnrollments()
        ]);
        const certificateByCourseId = new Map(
          certificates
            .filter((certificate) => certificate.courseId)
            .map((certificate) => [certificate.courseId as string, certificate])
        );

        const enrollmentItems = await Promise.all(enrollments.map(async (enrollment) => {
          const certificate = certificateByCourseId.get(enrollment.courseId);
          let eligibility: CertificateEligibility | undefined;
          if (!certificate) {
            try {
              eligibility = await studentService.getCertificateEligibility(enrollment.courseId);
            } catch {
              eligibility = undefined;
            }
          }

          return {
            courseId: enrollment.courseId,
            courseName: enrollment.courseName,
            enrollment,
            certificate,
            eligibility
          };
        }));

        const enrolledCourseIds = new Set(enrollments.map((enrollment) => enrollment.courseId));
        const issuedOnlyItems = certificates
          .filter((certificate) => certificate.courseId && !enrolledCourseIds.has(certificate.courseId))
          .map((certificate) => ({
            courseId: certificate.courseId as string,
            courseName: certificate.courseName,
            certificate
          }));

        if (active) {
          setItems([...enrollmentItems, ...issuedOnlyItems]);
          setError("");
        }
      } catch (loadError) {
        if (!active) return;
        const message = loadError instanceof Error ? loadError.message : "Could not load certificates from the API.";
        setError(message);
        showToast({ title: "Certificates unavailable", message, tone: "error" });
      } finally {
        if (active) setLoading(false);
      }
    }

    void loadCertificates();
    return () => {
      active = false;
    };
  }, [showToast]);

  async function downloadCertificate(item: CertificateCourseItem) {
    setDownloadingCourseId(item.courseId);
    try {
      const certificate = item.certificate ?? await studentService.generateCertificate(item.courseId);
      if (!certificate.id) {
        throw new Error("The certificate was issued, but its download id was not returned.");
      }

      await studentService.downloadCertificate(
        certificate.id,
        `EduVerse-Certificate-${certificate.courseName.replace(/[^a-z0-9]+/gi, "-")}.pdf`
      );
      setItems((currentItems) => currentItems.map((currentItem) => (
        currentItem.courseId === item.courseId
          ? { ...currentItem, certificate }
          : currentItem
      )));
      showToast({
        title: "Certificate ready",
        message: "Your certificate download has started.",
        tone: "success"
      });
    } catch (downloadError) {
      showToast({
        title: "Certificate unavailable",
        message: downloadError instanceof Error ? downloadError.message : "The certificate could not be downloaded.",
        tone: "error"
      });
    } finally {
      setDownloadingCourseId("");
    }
  }

  const issuedCount = items.filter((item) => item.certificate).length;
  const readyCount = items.filter((item) => item.certificate || item.eligibility?.canReceiveCertificate).length;

  return (
    <AppShell>
      <AuthGuard roles={["Student"]}>
        <PageHeader eyebrow="Certificates" title="Your achievements" description="View and download certificates issued after course completion." />
        {error && <div className="mt-6 rounded-xl bg-coral-100 px-4 py-3 text-sm font-semibold text-coral-500">{error}</div>}

        <div className="mt-8 grid gap-5 md:grid-cols-2">
          <StatCard label="Certificates issued" value={`${issuedCount}`} icon={Award} />
          <StatCard label="Ready to download" value={`${readyCount}`} icon={CheckCircle2} accent="amber" />
        </div>

        <div className="mt-8 grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          {loading ? (
            <div className="md:col-span-2 xl:col-span-3">
              <LoadingState label="Loading certificates" />
            </div>
          ) : items.length === 0 ? (
            <div className="md:col-span-2 xl:col-span-3">
              <EmptyState title="No certificate courses yet" description="Enroll in a course and complete it to unlock a certificate." />
            </div>
          ) : (
            items.map((item) => {
              const certificate = item.certificate;
              const canDownload = Boolean(certificate || item.eligibility?.canReceiveCertificate);
              const lockedMessage = item.eligibility?.message
                ?? "Complete the course and submit at least 80% of assignments to unlock your certificate.";
              const isDownloading = downloadingCourseId === item.courseId;

              return (
                <article key={item.courseId} className="rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
                  <div className={`grid size-14 place-items-center rounded-xl ${canDownload ? "bg-amber-100 text-amber-500" : "bg-slate-100 text-muted"}`}>
                    {canDownload ? <Award /> : <LockKeyhole />}
                  </div>
                  <h2 className="mt-5 text-xl font-bold text-ink">{item.courseName}</h2>

                  {certificate ? (
                    <>
                      <p className="mt-2 text-sm text-muted">Issued on {formatDate(certificate.issuedAt)}</p>
                      {certificate.organizationName && <p className="mt-1 text-sm text-muted">{certificate.organizationName}</p>}
                      <p className="mt-3 text-xs font-semibold text-muted">Code: {certificate.certificateCode ?? certificate.id}</p>
                      <p className="mt-1 text-xs font-semibold text-muted">Status: {certificate.status ?? "Valid"}</p>
                    </>
                  ) : (
                    <div className={`mt-4 rounded-xl px-4 py-3 text-sm font-semibold ring-1 ${canDownload ? "bg-teal-50 text-teal-700 ring-teal-100" : "bg-amber-50 text-amber-700 ring-amber-100"}`}>
                      {canDownload ? "You meet the certificate requirements. Download it when you are ready." : lockedMessage}
                    </div>
                  )}

                  <Button
                    className="mt-6 w-full"
                    variant={canDownload ? "primary" : "ghost"}
                    disabled={!canDownload || isDownloading}
                    onClick={() => void downloadCertificate(item)}
                  >
                    {canDownload ? <Download size={18} /> : <LockKeyhole size={18} />}
                    {isDownloading ? "Preparing certificate..." : canDownload ? "Download Certificate" : "Certificate Locked"}
                  </Button>
                </article>
              );
            })
          )}
        </div>
      </AuthGuard>
    </AppShell>
  );
}
