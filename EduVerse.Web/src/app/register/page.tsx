"use client";

import { Eye, EyeOff } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { FormEvent, useState } from "react";
import { Button } from "@/components/ui";
import { authService, getApiErrorMessage } from "@/lib/api";
import type { RegistrationDetails, UserRole } from "@/lib/types";

type RegistrationRole = Extract<UserRole, "Student" | "Instructor">;
type RegistrationStep = "details" | "confirmation";

const initialDetails: RegistrationDetails = {
  fullName: "",
  userName: "",
  email: "",
  phoneNumber: "",
  password: "",
  confirmPassword: "",
  birth: "",
  role: "Student"
};

export default function RegisterPage() {
  const router = useRouter();
  const [step, setStep] = useState<RegistrationStep>("details");
  const [details, setDetails] = useState<RegistrationDetails>(initialDetails);
  const [confirmationCode, setConfirmationCode] = useState("");
  const [loading, setLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [message, setMessage] = useState("");
  const [messageTone, setMessageTone] = useState<"success" | "error">("error");

  function updateDetails<K extends keyof RegistrationDetails>(field: K, value: RegistrationDetails[K]) {
    setDetails((current) => ({ ...current, [field]: value }));
    if (field === "email") {
      setConfirmationCode("");
      setMessage("");
    }
  }

  function validatePasswords() {
    const password = details.password;
    if (password.length < 8 || !/[a-z]/.test(password) || !/[A-Z]/.test(password) || !/[0-9]/.test(password) || !/[^a-zA-Z0-9]/.test(password)) {
      return "Password must contain at least 8 characters, including uppercase, lowercase, a number, and a symbol.";
    }
    if (password !== details.confirmPassword) return "Passwords do not match.";
    return "";
  }

  async function startRegistration(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const passwordError = validatePasswords();
    if (passwordError) {
      setMessageTone("error");
      setMessage(passwordError);
      return;
    }

    setLoading(true);
    setMessage("");
    try {
      await authService.startRegistration(details);
      setConfirmationCode("");
      setStep("confirmation");
      setMessageTone("success");
      setMessage("We sent a confirmation code to your email. Enter it to finish registration.");
    } catch (error) {
      setMessageTone("error");
      setMessage(getApiErrorMessage(error, "Could not start registration."));
    } finally {
      setLoading(false);
    }
  }

  async function finishRegistration(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!confirmationCode.trim()) {
      setMessageTone("error");
      setMessage("Enter the confirmation code sent to your email.");
      return;
    }

    setLoading(true);
    setMessage("");
    try {
      const result = await authService.register({ ...details, confirmationCode });
      setMessageTone("success");
      setMessage(result.message ?? "Registration successful. Redirecting to login...");
      window.setTimeout(() => router.push("/login"), 700);
    } catch (error) {
      setMessageTone("error");
      setMessage(getApiErrorMessage(error, "Registration confirmation failed."));
    } finally {
      setLoading(false);
    }
  }

  async function resendCode() {
    setLoading(true);
    setConfirmationCode("");
    setMessage("");
    try {
      await authService.startRegistration(details);
      setMessageTone("success");
      setMessage("A new confirmation code was sent to your email.");
    } catch (error) {
      setMessageTone("error");
      setMessage(getApiErrorMessage(error, "Could not resend the confirmation code."));
    } finally {
      setLoading(false);
    }
  }

  function editDetails() {
    setStep("details");
    setConfirmationCode("");
    setMessage("");
  }

  return (
    <main className="grid min-h-screen place-items-center px-5 py-10">
      <section className="w-full max-w-3xl rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100 sm:p-10">
        <p className="text-sm font-semibold text-teal-600">{step === "details" ? "Create account" : "Confirm your email"}</p>
        <h1 className="mt-2 text-3xl font-bold text-ink">{step === "details" ? "Join EduVerse" : "Finish registration"}</h1>
        <p className="mt-3 text-sm text-muted">
          {step === "details"
            ? "Register as a student or instructor. Platform and organization accounts are managed by administrators."
            : `Enter the confirmation code sent to ${details.email}.`}
        </p>

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

        {step === "details" ? (
          <form onSubmit={startRegistration}>
            <div className="mt-7 grid gap-4 sm:grid-cols-2">
              <TextField label="Full name" value={details.fullName} onChange={(value) => updateDetails("fullName", value)} />
              <TextField label="Username" value={details.userName} onChange={(value) => updateDetails("userName", value)} />
              <TextField label="Email" type="email" value={details.email} onChange={(value) => updateDetails("email", value)} />
              <TextField label="Phone number" value={details.phoneNumber} onChange={(value) => updateDetails("phoneNumber", value)} />
              <PasswordField
                label="Password"
                value={details.password}
                visible={showPassword}
                onChange={(value) => updateDetails("password", value)}
                onToggle={() => setShowPassword((visible) => !visible)}
              />
              <PasswordField
                label="Confirm password"
                value={details.confirmPassword}
                visible={showConfirmPassword}
                onChange={(value) => updateDetails("confirmPassword", value)}
                onToggle={() => setShowConfirmPassword((visible) => !visible)}
              />
              <TextField label="Birth date" type="date" value={details.birth} onChange={(value) => updateDetails("birth", value)} />
            </div>

            <p className="mt-3 text-xs font-semibold text-muted">
              Passwords need at least 8 characters, including uppercase, lowercase, a number, and a symbol.
            </p>

            <div className="mt-6">
              <p className="text-sm font-semibold text-ink">Role</p>
              <div className="mt-2 grid grid-cols-2 gap-3 rounded-xl bg-slate-100 p-1">
                {(["Student", "Instructor"] as RegistrationRole[]).map((role) => (
                  <button
                    key={role}
                    type="button"
                    onClick={() => updateDetails("role", role)}
                    className={`rounded-lg px-4 py-3 text-sm font-semibold transition ${
                      details.role === role ? "bg-white text-teal-600 shadow-sm" : "text-muted"
                    }`}
                  >
                    {role}
                  </button>
                ))}
              </div>
            </div>

            <Button className="mt-7 w-full" disabled={loading}>
              {loading ? "Sending confirmation code..." : "Create account"}
            </Button>
          </form>
        ) : (
          <form onSubmit={finishRegistration} className="mt-7">
            <label className="block">
              <span className="text-sm font-semibold text-ink">Confirmation code</span>
              <input
                value={confirmationCode}
                onChange={(event) => setConfirmationCode(event.target.value)}
                inputMode="numeric"
                autoComplete="one-time-code"
                autoFocus
                className="mt-2 h-12 w-full rounded-xl bg-slate-50 px-4 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500"
                required
              />
            </label>

            <Button className="mt-6 w-full" disabled={loading || !confirmationCode.trim()}>
              {loading ? "Confirming..." : "Confirm and finish registration"}
            </Button>
            <div className="mt-3 grid gap-3 sm:grid-cols-2">
              <Button type="button" variant="ghost" onClick={editDetails} disabled={loading}>
                Back / Edit details
              </Button>
              <Button type="button" variant="ghost" onClick={resendCode} disabled={loading}>
                Resend code
              </Button>
            </div>
          </form>
        )}

        <p className="mt-6 text-center text-sm text-muted">
          Already have an account? <Link href="/login" className="font-semibold text-teal-600">Login</Link>
        </p>
      </section>
    </main>
  );
}

function TextField({
  label,
  type = "text",
  value,
  onChange
}: {
  label: string;
  type?: string;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <label className="block">
      <span className="text-sm font-semibold text-ink">{label}</span>
      <input
        type={type}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="mt-2 h-12 w-full rounded-xl bg-slate-50 px-4 text-sm outline-none ring-1 ring-slate-200 focus:ring-teal-500"
        required
      />
    </label>
  );
}

function PasswordField({
  label,
  value,
  visible,
  onChange,
  onToggle
}: {
  label: string;
  value: string;
  visible: boolean;
  onChange: (value: string) => void;
  onToggle: () => void;
}) {
  return (
    <label className="block">
      <span className="text-sm font-semibold text-ink">{label}</span>
      <span className="mt-2 flex h-12 items-center rounded-xl bg-slate-50 px-4 ring-1 ring-slate-200 focus-within:ring-teal-500">
        <input
          value={value}
          onChange={(event) => onChange(event.target.value)}
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
