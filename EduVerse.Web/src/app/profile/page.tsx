"use client";

import { Camera, Lock, Mail, Phone, Shield, User } from "lucide-react";
import { FormEvent, useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { SmartImage } from "@/components/smart-image";
import { Badge, Button, PageHeader } from "@/components/ui";
import { authService } from "@/lib/api";
import { getStoredUser } from "@/lib/auth";
import { SEED_IMAGES } from "@/lib/image-fallbacks";
import type { AuthUser } from "@/lib/types";

export default function ProfilePage() {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [fullName, setFullName] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [profilePicture, setProfilePicture] = useState<File | null>(null);
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [profileLoading, setProfileLoading] = useState(false);
  const [passwordLoading, setPasswordLoading] = useState(false);
  const [message, setMessage] = useState<{ tone: "success" | "error"; text: string } | null>(null);

  useEffect(() => {
    const stored = getStoredUser();
    setUser(stored);
    setFullName(stored?.fullName ?? "");
    setPhoneNumber(stored?.phoneNumber ?? "");

    authService.getProfile().then((profile) => {
      const nextUser = { ...stored, ...profile, role: profile.role ?? stored?.role ?? "Student" } as AuthUser;
      setUser(nextUser);
      setFullName(nextUser.fullName ?? "");
      setPhoneNumber(nextUser.phoneNumber ?? "");
    }).catch(() => undefined);
  }, []);

  async function handleProfileSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setMessage(null);
    setProfileLoading(true);

    try {
      const updated = await authService.updateProfile({ fullName, phoneNumber, profilePicture });
      setUser(updated);
      setFullName(updated.fullName ?? "");
      setPhoneNumber(updated.phoneNumber ?? "");
      setProfilePicture(null);
      setMessage({ tone: "success", text: "Profile updated successfully." });
    } catch (error) {
      setMessage({ tone: "error", text: error instanceof Error ? error.message : "Profile update failed." });
    } finally {
      setProfileLoading(false);
    }
  }

  async function handlePasswordSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setMessage(null);
    setPasswordLoading(true);

    try {
      await authService.changePassword({ currentPassword, newPassword, confirmPassword });
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
      setMessage({ tone: "success", text: "Password changed successfully." });
    } catch (error) {
      setMessage({ tone: "error", text: error instanceof Error ? error.message : "Password change failed." });
    } finally {
      setPasswordLoading(false);
    }
  }

  return (
    <AppShell>
      <AuthGuard roles={["Student", "Instructor", "OrganizationAdmin", "Admin"]}>
        <PageHeader eyebrow="Profile" title="Account settings" description="Manage your EduVerse identity and account security." />

        {message && (
          <div className={`mt-6 rounded-xl px-4 py-3 text-sm font-semibold ${message.tone === "success" ? "bg-teal-50 text-teal-600" : "bg-coral-100 text-coral-500"}`}>
            {message.text}
          </div>
        )}

        <section className="mt-8 grid gap-8 lg:grid-cols-[340px_1fr]">
          <aside className="rounded-xl2 bg-white p-6 text-center shadow-soft ring-1 ring-slate-100">
            <div className="mx-auto grid size-24 place-items-center overflow-hidden rounded-3xl bg-teal-50 text-teal-600">
              <SmartImage src={user?.profilePicture} fallbackSrc={SEED_IMAGES.profile} alt={user?.fullName ?? "Profile"} className="size-full object-cover" />
            </div>
            <h2 className="mt-5 text-2xl font-bold text-ink">{user?.fullName ?? user?.userName ?? "EduVerse user"}</h2>
            <p className="mt-2 text-sm text-muted">{user?.email ?? "Not available"}</p>
            <div className="mt-4">
              <Badge>{user?.role ?? "Student"}</Badge>
            </div>
          </aside>

          <div className="space-y-6">
            <form onSubmit={handleProfileSubmit} className="rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
              <div className="flex items-center justify-between gap-4">
                <div>
                  <h2 className="text-xl font-bold text-ink">Profile info</h2>
                  <p className="mt-1 text-sm text-muted">Update your name, phone, and profile picture.</p>
                </div>
                <Camera className="text-teal-600" size={22} />
              </div>

              <div className="mt-6 grid gap-4 md:grid-cols-2">
                <label className="space-y-2">
                  <span className="text-sm font-bold text-ink">Full name</span>
                  <input value={fullName} onChange={(event) => setFullName(event.target.value)} className="h-12 w-full rounded-xl bg-slate-50 px-4 text-sm text-ink ring-1 ring-slate-200 outline-none focus:ring-teal-400" />
                </label>
                <label className="space-y-2">
                  <span className="text-sm font-bold text-ink">Phone number</span>
                  <input value={phoneNumber} onChange={(event) => setPhoneNumber(event.target.value)} className="h-12 w-full rounded-xl bg-slate-50 px-4 text-sm text-ink ring-1 ring-slate-200 outline-none focus:ring-teal-400" />
                </label>
                <label className="space-y-2 md:col-span-2">
                  <span className="text-sm font-bold text-ink">Profile picture</span>
                  <input type="file" accept="image/*" onChange={(event) => setProfilePicture(event.target.files?.[0] ?? null)} className="w-full rounded-xl bg-slate-50 px-4 py-3 text-sm text-muted ring-1 ring-slate-200 file:mr-4 file:rounded-lg file:border-0 file:bg-teal-50 file:px-3 file:py-2 file:text-sm file:font-bold file:text-teal-600" />
                </label>
              </div>

              <div className="mt-6 grid gap-4 md:grid-cols-2">
                <Info icon={User} label="Username" value={user?.userName ?? "Not available"} />
                <Info icon={Mail} label="Email" value={user?.email ?? "Not available"} />
                <Info icon={Shield} label="Role" value={user?.role ?? "Student"} />
                <Info icon={Phone} label="Phone" value={(user?.phoneNumber ?? phoneNumber) || "Not available"} />
              </div>

              <Button className="mt-6" disabled={profileLoading}>{profileLoading ? "Saving..." : "Save profile"}</Button>
            </form>

            <form onSubmit={handlePasswordSubmit} className="rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
              <div className="flex items-center justify-between gap-4">
                <div>
                  <h2 className="text-xl font-bold text-ink">Security</h2>
                  <p className="mt-1 text-sm text-muted">Change your password using your current password.</p>
                </div>
                <Lock className="text-teal-600" size={22} />
              </div>

              <div className="mt-6 grid gap-4 md:grid-cols-3">
                <input value={currentPassword} onChange={(event) => setCurrentPassword(event.target.value)} required placeholder="Current password" type="password" className="h-12 rounded-xl bg-slate-50 px-4 text-sm text-ink ring-1 ring-slate-200 outline-none focus:ring-teal-400" />
                <input value={newPassword} onChange={(event) => setNewPassword(event.target.value)} required placeholder="New password" type="password" className="h-12 rounded-xl bg-slate-50 px-4 text-sm text-ink ring-1 ring-slate-200 outline-none focus:ring-teal-400" />
                <input value={confirmPassword} onChange={(event) => setConfirmPassword(event.target.value)} required placeholder="Confirm password" type="password" className="h-12 rounded-xl bg-slate-50 px-4 text-sm text-ink ring-1 ring-slate-200 outline-none focus:ring-teal-400" />
              </div>
              <Button className="mt-5" disabled={passwordLoading}>{passwordLoading ? "Changing..." : "Change password"}</Button>
            </form>
          </div>
        </section>
      </AuthGuard>
    </AppShell>
  );
}

function Info({ icon: Icon, label, value }: { icon: any; label: string; value: string }) {
  return (
    <div className="rounded-xl bg-slate-50 p-4">
      <Icon size={20} className="text-teal-600" />
      <p className="mt-3 text-xs font-semibold uppercase text-muted">{label}</p>
      <p className="mt-1 break-words font-bold text-ink">{value}</p>
    </div>
  );
}
