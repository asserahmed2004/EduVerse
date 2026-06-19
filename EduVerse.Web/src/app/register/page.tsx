"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { FormEvent, useState } from "react";
import { Button } from "@/components/ui";
import { authService, getApiErrorMessage } from "@/lib/api";
import type { UserRole } from "@/lib/types";

export default function RegisterPage() {
  const router = useRouter();
  const [role, setRole] = useState<UserRole>("Student");
  const [loading, setLoading] = useState(false);
  const [sendingCode, setSendingCode] = useState(false);
  const [email, setEmail] = useState("");
  const [message, setMessage] = useState("");
  const [messageTone, setMessageTone] = useState<"success" | "error">("error");

  async function sendConfirmationCode() {
    setSendingCode(true);
    setMessage("");
    try {
      const result = await authService.sendConfirmationEmail(email);
      setMessageTone("success");
      setMessage(result.message ?? "Confirmation code sent. Check your email.");
    } catch (error) {
      setMessageTone("error");
      setMessage(getApiErrorMessage(error, "Could not send the confirmation code."));
    } finally {
      setSendingCode(false);
    }
  }

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
        role,
        confirmationCode: String(form.get("confirmationCode") ?? "")
      });
      router.push("/login");
    } catch (error) {
      setMessageTone("error");
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
        <p className="mt-3 text-sm text-muted">Register as a student, instructor, or organization admin. Platform admin accounts are managed from backend roles.</p>

        {message && (
          <div className={`mt-6 rounded-xl px-4 py-3 text-sm font-semibold ${messageTone === "success" ? "bg-teal-50 text-teal-700" : "bg-amber-100 text-amber-600"}`}>
            {message}
          </div>
        )}

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
                value={name === "email" ? email : undefined}
                onChange={name === "email" ? (event) => setEmail(event.target.value) : undefined}
                className="mt-2 h-12 w-full rounded-xl bg-slate-50 px-4 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500"
                required
              />
              {name === "email" && (
                <button
                  type="button"
                  onClick={sendConfirmationCode}
                  disabled={sendingCode || !email.trim()}
                  className="mt-2 text-sm font-semibold text-teal-600 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  {sendingCode ? "Sending code..." : "Send confirmation code"}
                </button>
              )}
            </label>
          ))}
        </div>
        <p className="mt-3 text-xs font-semibold text-muted">
          Passwords need at least 8 characters, including uppercase, lowercase, a number, and a symbol.
        </p>

        <div className="mt-6">
          <p className="text-sm font-semibold text-ink">Role</p>
          <div className="mt-2 grid grid-cols-3 gap-3 rounded-xl bg-slate-100 p-1">
            {(["Student", "Instructor", "OrganizationAdmin"] as UserRole[]).map((item) => (
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
