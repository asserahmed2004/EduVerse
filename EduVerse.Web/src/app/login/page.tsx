"use client";

import { Eye, EyeOff, Lock, Mail } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { FormEvent, useEffect, useState } from "react";
import { BrandLogo } from "@/components/brand-logo";
import { SmartImage } from "@/components/smart-image";
import { Button } from "@/components/ui";
import { authService } from "@/lib/api";
import { SEED_IMAGES } from "@/lib/image-fallbacks";
import { getDashboardPath, getStoredUser, getToken } from "@/lib/auth";

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    const token = getToken();
    const user = getStoredUser();

    if (token && user) {
      router.replace(getDashboardPath(user.role));
    }
  }, [router]);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setLoading(true);
    setError("");

    try {
      const user = await authService.login({ email, password });
      router.push(getDashboardPath(user.role));
    } catch (error) {
      setError(error instanceof Error ? error.message : "Login failed. Check backend API, email, and password.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="grid min-h-screen place-items-center px-5 py-10">
      <div className="grid w-full max-w-5xl overflow-hidden rounded-xl2 bg-white shadow-soft ring-1 ring-slate-100 lg:grid-cols-[0.95fr_1.05fr]">
        <div className="hidden bg-ink p-8 text-white lg:block">
          <SmartImage src={SEED_IMAGES.learningHero} fallbackSrc={SEED_IMAGES.learningHero} alt="Online learning" className="h-72 w-full rounded-xl object-cover" />
          <h1 className="mt-8 text-3xl font-bold">Welcome back to EduVerse</h1>
          <p className="mt-3 text-sm leading-6 text-slate-300">Continue learning, manage enrollments, and keep your education flow moving from one clean workspace.</p>
        </div>

        <form onSubmit={onSubmit} className="p-6 sm:p-10">
          <BrandLogo imageClassName="h-20 max-w-56" />
          <p className="mt-6 text-sm font-semibold text-teal-600">Login</p>
          <h2 className="mt-2 text-3xl font-bold text-ink">Access your workspace</h2>
          <p className="mt-3 text-sm text-muted">Use your EduVerse account to continue.</p>

          {error && <div className="mt-6 rounded-xl bg-amber-100 px-4 py-3 text-sm font-semibold text-amber-500">{error}</div>}

          <label className="mt-7 block">
            <span className="text-sm font-semibold text-ink">Email</span>
            <span className="mt-2 flex items-center gap-3 rounded-xl bg-slate-50 px-4 py-3 ring-1 ring-slate-200">
              <Mail size={18} className="text-muted" />
              <input value={email} onChange={(event) => setEmail(event.target.value)} className="w-full bg-transparent text-sm outline-none" type="email" required />
            </span>
          </label>

          <label className="mt-5 block">
            <span className="text-sm font-semibold text-ink">Password</span>
            <span className="mt-2 flex items-center gap-3 rounded-xl bg-slate-50 px-4 py-3 ring-1 ring-slate-200">
              <Lock size={18} className="text-muted" />
              <input value={password} onChange={(event) => setPassword(event.target.value)} className="w-full bg-transparent text-sm outline-none" type={showPassword ? "text" : "password"} required />
              <button
                type="button"
                onClick={() => setShowPassword((visible) => !visible)}
                className="grid size-8 shrink-0 place-items-center rounded-lg text-muted hover:bg-white hover:text-ink"
                aria-label={showPassword ? "Hide password" : "Show password"}
                aria-pressed={showPassword}
              >
                {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
              </button>
            </span>
          </label>

          <Button className="mt-7 w-full" disabled={loading}>{loading ? "Signing in..." : "Login"}</Button>

          <p className="mt-6 text-center text-sm text-muted">
            New here? <Link href="/register" className="font-semibold text-teal-600">Create account</Link>
          </p>
        </form>
      </div>
    </main>
  );
}
