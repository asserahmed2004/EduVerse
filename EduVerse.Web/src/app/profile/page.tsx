"use client";

import { Mail, Phone, Shield, User } from "lucide-react";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/app-shell";
import { AuthGuard } from "@/components/auth-guard";
import { Badge, PageHeader } from "@/components/ui";
import { authService } from "@/lib/api";
import { getStoredUser } from "@/lib/auth";
import type { AuthUser } from "@/lib/types";

export default function ProfilePage() {
  const [user, setUser] = useState<AuthUser | null>(null);

  useEffect(() => {
    setUser(getStoredUser());
    authService.getProfile().then((profile) => {
      const stored = getStoredUser();
      setUser({
        ...stored,
        id: profile.id ?? stored?.id,
        fullName: profile.fullName ?? profile.FullName ?? stored?.fullName,
        userName: profile.userName ?? profile.UserName ?? stored?.userName,
        email: profile.email ?? profile.Email ?? stored?.email,
        role: stored?.role ?? "Student",
        profilePicture: profile.profilePicture ?? profile.ProfilePicture
      });
    }).catch(() => undefined);
  }, []);

  return (
    <AppShell>
      <AuthGuard roles={["Student", "Instructor", "Admin"]}>
        <PageHeader eyebrow="Profile" title="Account settings" description="Your identity and role information from EduVerse authentication." />
        <section className="mt-8 grid gap-8 lg:grid-cols-[340px_1fr]">
          <aside className="rounded-xl2 bg-white p-6 text-center shadow-soft ring-1 ring-slate-100">
            <div className="mx-auto grid size-24 place-items-center rounded-3xl bg-teal-50 text-teal-600">
              <User size={42} />
            </div>
            <h2 className="mt-5 text-2xl font-bold text-ink">{user?.fullName ?? user?.userName ?? "EduVerse user"}</h2>
            <p className="mt-2 text-sm text-muted">{user?.email ?? "demo@eduverse.com"}</p>
            <div className="mt-4">
              <Badge>{user?.role ?? "Student"}</Badge>
            </div>
          </aside>

          <div className="rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
            <h2 className="text-xl font-bold text-ink">Profile details</h2>
            <div className="mt-6 grid gap-4 md:grid-cols-2">
              <Info icon={User} label="Username" value={user?.userName ?? "Not available"} />
              <Info icon={Mail} label="Email" value={user?.email ?? "Not available"} />
              <Info icon={Shield} label="Role" value={user?.role ?? "Student"} />
              <Info icon={Phone} label="Phone" value="Managed by backend profile" />
            </div>
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
      <p className="mt-1 font-bold text-ink">{value}</p>
    </div>
  );
}
