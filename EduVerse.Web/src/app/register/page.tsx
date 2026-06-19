"use client";

import { Eye, EyeOff } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { FormEvent, useRef, useState } from "react";
import { Button } from "@/components/ui";
import { authService, getApiErrorMessage } from "@/lib/api";
import type { UserRole } from "@/lib/types";

type RegistrationRole = Extract<UserRole, "Student" | "Instructor">;

export default function RegisterPage() {
  const router = useRouter();
  const formRef = useRef<HTMLFormElement>(null);
  const [role, setRole] = useState<RegistrationRole>("Student");
  const [loading, setLoading] = useState(false);
  const [sendingCode, setSendingCode] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [email, setEmail] = useState("");
  const [confirmationCode, setConfirmationCode] = useState("");
  const [confirmationRequested, setConfirmationRequested] = useState(false);
  const [message, setMessage] = useState("");
  const [messageTone, setMessageTone] = useState<"success" | "error">("error");

  function validatePasswords(form: HTMLFormElement) {
    const formData = new FormData(form);
    const password = String(formData.get("password") ?? "");
    const confirmPassword = String(formData.get("confirmPassword") ?? "");

    if (password.length < 8 || !/[a-z]/.test(password) || !/[A-Z]/.test(password) || !/[0-9]/.test(password) || !/[^a-zA-Z0-9]/.test(password)) {
      return "Password must contain at least 8 characters, including uppercase, lowercase, a number, and a symbol.";
    }
    if (password !== confirmPassword) {
      return "Passwords do not match.";
    }

    return "";
  }

  async function sendConfirmationCode() {
    const form = formRef.current;
    if (!form || !form.reportValidity()) return;

    const passwordError = validatePasswords(form);
    if (passwordError) {
      setMessageTone("error");
      setMessage(passwordError);
      return;
    }

    setSendingCode(true);
    setConfirmationRequested(false);
    setConfirmationCode("");
    setMessage("");
    try {
      await authService.sendConfirmationEmail(email);
      setConfirmationRequested(true);
      setMessageTone("success");
      setMessage("Confirmation code sent to your email.");
    } catch (error) {
      setConfirmationRequested(false);
      setMessageTone("error");
      setMessage(getApiErrorMessage(error, "Could not send the confirmation code."));
    } finally {
      setSendingCode(false);
    }
  }

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!confirmationRequested || !confirmationCode.trim()) {
      setMessageTone("error");
      setMessage("Request a confirmation code and enter it before creating your account.");
      return;
    }

    setLoading(true);
    setMessage("");
    const form = new FormData(event.currentTarget);

    try {
      const password = String(form.get("password") ?? "");
      const confirmPassword = String(form.get("confirmPassword") ?? "");
      const passwordError = validatePasswords(event.currentTarget);
      if (passwordError) throw new Error(passwordError);

      await authService.register({
        fullName: String(form.get("fullName")),
        userName: String(form.get("userName")),
        email: String(form.get("email")),
        password,
        confirmPassword,
        phoneNumber: String(form.get("phoneNumber")),
        birth: String(form.get("birth")),
        role,
        confirmationCode
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
      <form ref={formRef} onSubmit={onSubmit} className="w-full max-w-3xl rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100 sm:p-10">
        <p className="text-sm font-semibold text-teal-600">Create account</p>
        <h1 className="mt-2 text-3xl font-bold text-ink">Join EduVerse</h1>
        <p className="mt-3 text-sm text-muted">Register as a student or instructor. Platform and organization accounts are managed by administrators.</p>

        {message && (
          <div
            role="alert"
            className={`mt-6 rounded-xl px-4 py-3 text-sm font-semibold ${
              messageTone === "success" ? "bg-teal-50 text-teal-700" : "bg-amber-100 text-amber-600"
            }`}
          >
            {message}
          </div>
        )}

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
                value={name === "email" ? email : undefined}
                onChange={name === "email" ? (event) => {
                  setEmail(event.target.value);
                  setConfirmationRequested(false);
                  setConfirmationCode("");
                  setMessage("");
                } : undefined}
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

          {confirmationRequested && (
            <label className="block">
              <span className="text-sm font-semibold text-ink">Confirmation code</span>
              <input
                name="confirmationCode"
                value={confirmationCode}
                onChange={(event) => setConfirmationCode(event.target.value)}
                inputMode="numeric"
                autoComplete="one-time-code"
                className="mt-2 h-12 w-full rounded-xl bg-slate-50 px-4 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500"
              />
            </label>
          )}
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

        <Button
          type="button"
          variant="ghost"
          className="mt-7 w-full"
          disabled={sendingCode || loading}
          onClick={sendConfirmationCode}
        >
          {sendingCode ? "Sending confirmation code..." : confirmationRequested ? "Resend confirmation code" : "Send confirmation code"}
        </Button>
        <Button className="mt-3 w-full" disabled={loading || sendingCode || !confirmationRequested || !confirmationCode.trim()}>
          {loading ? "Creating..." : "Create account"}
        </Button>
        {!confirmationRequested && (
          <p className="mt-3 text-center text-xs font-semibold text-muted">
            Complete the form, then request and enter your email confirmation code.
          </p>
        )}
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
