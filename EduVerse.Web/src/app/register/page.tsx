"use client";

import { Eye, EyeOff } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { FormEvent, useState } from "react";
import { Button } from "@/components/ui";
import { authService, getApiErrorMessage } from "@/lib/api";
import type { UserRole } from "@/lib/types";

type RegistrationRole = Extract<UserRole, "Student" | "Instructor">;

export default function RegisterPage() {
  const router = useRouter();
  const [role, setRole] = useState<RegistrationRole>("Student");
  const [loading, setLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [message, setMessage] = useState("");

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setLoading(true);
    setMessage("");
    const form = new FormData(event.currentTarget);

    try {
      const password = String(form.get("password") ?? "");
      const confirmPassword = String(form.get("confirmPassword") ?? "");
      if (password !== confirmPassword) {
        throw new Error("Passwords do not match.");
      }

      await authService.register({
        fullName: String(form.get("fullName")),
        userName: String(form.get("userName")),
        email: String(form.get("email")),
        password,
        confirmPassword,
        phoneNumber: String(form.get("phoneNumber")),
        birth: String(form.get("birth")),
        role
      });
      router.push("/login");
    } catch (error) {
      setMessage(getApiErrorMessage(error, "Registration failed."));
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="grid min-h-screen place-items-center px-5 py-10">
      <form onSubmit={onSubmit} className="w-full max-w-3xl rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100 sm:p-10">
        <p className="text-sm font-semibold text-teal-600">Create account</p>
        <h1 className="mt-2 text-3xl font-bold text-ink">Join EduVerse</h1>
        <p className="mt-3 text-sm text-muted">Register as a student or instructor. Platform and organization accounts are managed by administrators.</p>

        {message && <div className="mt-6 rounded-xl bg-amber-100 px-4 py-3 text-sm font-semibold text-amber-600">{message}</div>}

        <div className="mt-7 grid gap-4 sm:grid-cols-2">
          {([
            ["fullName", "Full name"],
            ["userName", "Username"],
            ["email", "Email"],
            ["phoneNumber", "Phone number"]
          ] as const).map(([name, label]) => (
            <label key={name} className="block">
              <span className="text-sm font-semibold text-ink">{label}</span>
              <input
                name={name}
                type={name === "email" ? "email" : "text"}
                className="mt-2 h-12 w-full rounded-xl bg-slate-50 px-4 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500"
                required
              />
            </label>
          ))}

          <PasswordField
            name="password"
            label="Password"
            visible={showPassword}
            onToggle={() => setShowPassword((visible) => !visible)}
          />
          <PasswordField
            name="confirmPassword"
            label="Confirm password"
            visible={showConfirmPassword}
            onToggle={() => setShowConfirmPassword((visible) => !visible)}
          />

          <label className="block">
            <span className="text-sm font-semibold text-ink">Birth date</span>
            <input
              name="birth"
              type="date"
              className="mt-2 h-12 w-full rounded-xl bg-slate-50 px-4 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500"
              required
            />
          </label>
        </div>
        <p className="mt-3 text-xs font-semibold text-muted">
          Passwords need at least 8 characters, including uppercase, lowercase, a number, and a symbol.
        </p>

        <div className="mt-6">
          <p className="text-sm font-semibold text-ink">Role</p>
          <div className="mt-2 grid grid-cols-2 gap-3 rounded-xl bg-slate-100 p-1">
            {(["Student", "Instructor"] as RegistrationRole[]).map((item) => (
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

function PasswordField({
  name,
  label,
  visible,
  onToggle
}: {
  name: "password" | "confirmPassword";
  label: string;
  visible: boolean;
  onToggle: () => void;
}) {
  return (
    <label className="block">
      <span className="text-sm font-semibold text-ink">{label}</span>
      <span className="mt-2 flex h-12 items-center rounded-xl bg-slate-50 px-4 ring-1 ring-slate-200 focus-within:ring-teal-500">
        <input
          name={name}
          type={visible ? "text" : "password"}
          className="min-w-0 flex-1 bg-transparent text-sm outline-none"
          required
        />
        <button
          type="button"
          onClick={onToggle}
          className="grid size-8 shrink-0 place-items-center rounded-lg text-muted hover:bg-white hover:text-ink"
          aria-label={visible ? `Hide ${label.toLowerCase()}` : `Show ${label.toLowerCase()}`}
          aria-pressed={visible}
        >
          {visible ? <EyeOff size={18} /> : <Eye size={18} />}
        </button>
      </span>
    </label>
  );
}
