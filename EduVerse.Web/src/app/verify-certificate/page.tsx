"use client";

import { ShieldCheck, Search } from "lucide-react";
import { useSearchParams } from "next/navigation";
import { FormEvent, Suspense, useEffect, useState } from "react";
import { Button, EmptyState, LoadingState } from "@/components/ui";
import { studentService } from "@/lib/api";
import { formatDate } from "@/lib/utils";

type VerificationResult = {
  certificateCode?: string;
  studentName?: string;
  courseName?: string;
  issueDate?: string;
  issuedAt?: string;
  status?: string;
};

export default function VerifyCertificatePage() {
  return (
    <Suspense fallback={<main className="min-h-screen bg-canvas px-4 py-10 text-ink"><LoadingState label="Loading verification" /></main>}>
      <VerifyCertificateContent />
    </Suspense>
  );
}

function VerifyCertificateContent() {
  const searchParams = useSearchParams();
  const [code, setCode] = useState(searchParams.get("code") ?? "");
  const [result, setResult] = useState<VerificationResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  async function verify(value = code) {
    if (!value.trim()) {
      setError("Enter a certificate code first.");
      setResult(null);
      return;
    }

    setLoading(true);
    setError("");
    try {
      const data = await studentService.verifyCertificate(value.trim());
      setResult(data);
    } catch {
      setResult(null);
      setError("Certificate not found or invalid.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    const initialCode = searchParams.get("code");
    if (initialCode) verify(initialCode);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    verify();
  }

  return (
    <main className="min-h-screen bg-canvas px-4 py-10 text-ink">
      <section className="mx-auto max-w-3xl rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100 md:p-8">
        <div className="flex items-center gap-3">
          <div className="grid size-12 place-items-center rounded-xl bg-teal-50 text-teal-600">
            <ShieldCheck />
          </div>
          <div>
            <p className="text-sm font-bold uppercase tracking-[0.08em] text-teal-600">Certificate verification</p>
            <h1 className="mt-1 text-3xl font-black">Verify EduVerse certificate</h1>
          </div>
        </div>

        <form onSubmit={submit} className="mt-8 grid gap-3 md:grid-cols-[1fr_auto]">
          <label className="flex h-12 items-center gap-3 rounded-xl bg-slate-50 px-4 ring-1 ring-slate-200">
            <Search size={18} className="text-muted" />
            <input value={code} onChange={(event) => setCode(event.target.value)} placeholder="Certificate code" className="w-full bg-transparent text-sm outline-none" />
          </label>
          <Button disabled={loading}>{loading ? "Checking..." : "Verify"}</Button>
        </form>

        <div className="mt-8">
          {loading ? (
            <LoadingState label="Verifying certificate" />
          ) : result ? (
            <div className="rounded-xl bg-teal-50 p-5 ring-1 ring-teal-100">
              <p className="text-sm font-bold text-teal-600">{result.status ?? "Valid"}</p>
              <h2 className="mt-2 text-2xl font-bold text-ink">{result.courseName ?? "Verified course"}</h2>
              <div className="mt-4 grid gap-3 text-sm text-muted md:grid-cols-2">
                <p><span className="font-bold text-ink">Student:</span> {result.studentName ?? "Not available"}</p>
                <p><span className="font-bold text-ink">Code:</span> {result.certificateCode ?? code}</p>
                <p><span className="font-bold text-ink">Issued:</span> {formatDate(result.issueDate ?? result.issuedAt)}</p>
              </div>
            </div>
          ) : error ? (
            <EmptyState title="Certificate not verified" description={error} />
          ) : (
            <EmptyState title="Enter a certificate code" description="Verification results will appear here." />
          )}
        </div>
      </section>
    </main>
  );
}
