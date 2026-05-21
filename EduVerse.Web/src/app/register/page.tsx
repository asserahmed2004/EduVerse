"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { FormEvent, useState } from "react";
import { Button } from "@/components/ui";
import { authService } from "@/lib/api";
import type { UserRole } from "@/lib/types";

export default function RegisterPage() {
  const router = useRouter();
  const [role, setRole] = useState<UserRole>("Student");
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setLoading(true);
    setMessage("");
    const form = new FormData(event.currentTarget);

    try {
      await authService.register({
        fullName: String(form.get("fullName")),
        userName: String(form.get("userName")),
        email: String(form.get("email")),
        password: String(form.get("password")),
        confirmPassword: String(form.get("confirmPassword")),
        phoneNumber: String(form.get("phoneNumber")),
        birth: String(form.get("birth")),
        role,
        confirmationCode: String(form.get("confirmationCode") ?? "")
      });
      router.push("/login");
    } catch {
      setMessage("Registration API is not available now. The form is ready for backend integration.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="grid min-h-screen place-items-center px-5 py-10">
      <form onSubmit={onSubmit} className="w-full max-w-3xl rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100 sm:p-10">
        <p className="text-sm font-semibold text-teal-600">Create account</p>
        <h1 className="mt-2 text-3xl font-bold text-ink">Join EduVerse</h1>
        <p className="mt-3 text-sm text-muted">Register as a student or instructor. Admin accounts can be managed from backend roles.</p>

        {message && <div className="mt-6 rounded-xl bg-amber-100 px-4 py-3 text-sm font-semibold text-amber-500">{message}</div>}

        <div className="mt-7 grid gap-4 sm:grid-cols-2">
          {[
            ["fullName", "Full name"],
            ["userName", "Username"],
            ["email", "Email"],
            ["phoneNumber", "Phone number"],
            ["password", "Password"],
            ["confirmPassword", "Confirm password"],
            ["birth", "Birth date"],
            ["confirmationCode", "Confirmation code"]
          ].map(([name, label]) => (
            <label key={name} className="block">
              <span className="text-sm font-semibold text-ink">{label}</span>
              <input
                name={name}
                type={name.includes("password") ? "password" : name === "birth" ? "date" : name === "email" ? "email" : "text"}
                className="mt-2 h-12 w-full rounded-xl bg-slate-50 px-4 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500"
                required={name !== "confirmationCode"}
              />
            </label>
          ))}
        </div>

        <div className="mt-6">
          <p className="text-sm font-semibold text-ink">Role</p>
          <div className="mt-2 grid grid-cols-2 gap-3 rounded-xl bg-slate-100 p-1">
            {(["Student", "Instructor"] as UserRole[]).map((item) => (
              <button
                key={item}
                type="button"
                onClick={() => setRole(item)}
                className={`rounded-lg px-4 py-3 text-sm font-semibold transition ${role === item ? "bg-white text-teal-600 shadow-sm" : "text-muted"}`}
              >
                {item}
              </button>
            ))}
          </div>
        </div>

        <Button className="mt-7 w-full" disabled={loading}>{loading ? "Creating..." : "Create account"}</Button>
        <p className="mt-6 text-center text-sm text-muted">
          Already have an account? <Link href="/login" className="font-semibold text-teal-600">Login</Link>
        </p>
      </form>
    </main>
  );
}
