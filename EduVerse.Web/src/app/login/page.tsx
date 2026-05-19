"use client";

import { Eye, Lock, Mail } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { FormEvent, useState } from "react";
import { Button } from "@/components/ui";
import { authService } from "@/lib/api";

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState("mohamed@test.com");
  const [password, setPassword] = useState("Admin123!");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setLoading(true);
    setError("");

    try {
      const user = await authService.login({ email, password });
      router.push(user.role === "Admin" ? "/admin" : user.role === "Instructor" ? "/dashboard/instructor" : "/dashboard/student");
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
          <img src="https://images.unsplash.com/photo-1516321318423-f06f85e504b3?auto=format&fit=crop&w=1000&q=80" alt="Online learning" className="h-72 w-full rounded-xl object-cover" />
          <h1 className="mt-8 text-3xl font-bold">Welcome back to EduVerse</h1>
          <p className="mt-3 text-sm leading-6 text-slate-300">Continue learning, manage enrollments, and keep your education flow moving from one clean workspace.</p>
        </div>

        <form onSubmit={onSubmit} className="p-6 sm:p-10">
          <p className="text-sm font-semibold text-teal-600">Login</p>
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
              <input value={password} onChange={(event) => setPassword(event.target.value)} className="w-full bg-transparent text-sm outline-none" type="password" required />
              <Eye size={18} className="text-muted" />
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
