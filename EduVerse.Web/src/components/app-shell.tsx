"use client";

import { Activity, Award, BookOpen, Building2, CalendarClock, CreditCard, FileText, GraduationCap, Home, LayoutDashboard, LogOut, Menu, Search, Settings, ShieldCheck, User, Users } from "lucide-react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { clearAuth, getStoredUser } from "@/lib/auth";
import { adminService } from "@/lib/api";
import type { AuthUser, GlobalSearchResult } from "@/lib/types";
import { cn } from "@/lib/utils";
import { ThemeToggle } from "./theme-toggle";

const navItems = [
  { href: "/dashboard/student", label: "Student", icon: LayoutDashboard, roles: ["Student"] },
  { href: "/dashboard/organization", label: "Organization Dashboard", icon: LayoutDashboard, roles: ["OrganizationAdmin"] },
  { href: "/dashboard/instructor", label: "Instructor Dashboard", icon: LayoutDashboard, roles: ["Instructor"] },
  { href: "/instructor/courses", label: "Manage courses", icon: Settings, roles: ["OrganizationAdmin"] },
  { href: "/instructor/sessions", label: "My Sessions", icon: CalendarClock, roles: ["Instructor"] },
  { href: "/instructor/assignments", label: "Assignments", icon: FileText, roles: ["Instructor"] },
  { href: "/instructor/students", label: "Students", icon: Users, roles: ["Instructor"] },
  { href: "/admin", label: "Admin Dashboard", icon: ShieldCheck, roles: ["Admin"] },
  { href: "/admin/users", label: "Users / Roles", icon: Users, roles: ["Admin"] },
  { href: "/admin/activity-logs", label: "Activity Logs", icon: Activity, roles: ["Admin"] },
  { href: "/organizations", label: "Organizations", icon: Building2, roles: ["Admin"] },
  { href: "/admin/deleted-courses", label: "Deleted courses", icon: BookOpen, roles: ["Admin"] },
  { href: "/courses", label: "Courses", icon: BookOpen, roles: ["Student", "OrganizationAdmin", "Admin"] },
  { href: "/instructor/my-courses", label: "My Courses", icon: BookOpen, roles: ["Instructor"] },
  { href: "/enrollments", label: "Enrollments", icon: GraduationCap, roles: ["Student"] },
  { href: "/assignments", label: "Assignments", icon: FileText, roles: ["Student"] },
  { href: "/certificates", label: "Certificates", icon: Award, roles: ["Student"] },
  { href: "/payments", label: "Payments", icon: CreditCard, roles: ["Student", "OrganizationAdmin", "Admin"] },
  { href: "/profile", label: "Profile", icon: User, roles: ["Student", "Instructor", "OrganizationAdmin", "Admin"] }
];

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const [user, setUser] = useState<AuthUser | null>(null);
  const [open, setOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const [searchResults, setSearchResults] = useState<GlobalSearchResult | null>(null);

  useEffect(() => {
    setUser(getStoredUser());
  }, []);

  const visibleItems = navItems.filter((item) => user ? item.roles.includes(user.role) : item.href === "/courses" || item.href === "/profile");

  useEffect(() => {
    if (user?.role !== "Admin" || searchQuery.trim().length < 2) {
      setSearchResults(null);
      return;
    }

    const handle = window.setTimeout(() => {
      adminService.globalSearch(searchQuery)
        .then(setSearchResults)
        .catch(() => setSearchResults(null));
    }, 250);

    return () => window.clearTimeout(handle);
  }, [searchQuery, user?.role]);

  function logout() {
    clearAuth();
    router.push("/login");
  }

  return (
    <div className="min-h-screen bg-surface">
      <aside
        className={cn(
          "fixed inset-y-0 left-0 z-40 w-72 border-r border-slate-200 bg-white/95 p-6 shadow-soft backdrop-blur transition lg:translate-x-0",
          open ? "translate-x-0" : "-translate-x-full"
        )}
      >
        <Link href="/" className="flex items-center gap-3">
          <div className="grid size-12 place-items-center rounded-2xl bg-gradient-to-br from-teal-500 to-indigo-500 text-white shadow-button">
            <Home size={20} />
          </div>
          <div>
            <p className="text-lg font-bold text-ink">EduVerse</p>
            <p className="text-xs text-muted">Learning OS</p>
          </div>
        </Link>

        <nav className="mt-10 space-y-2">
          {visibleItems.map((item) => {
            const active = pathname === item.href;
            return (
              <Link
                key={item.href}
                href={item.href}
                className={cn(
                  "flex items-center gap-3 rounded-xl px-4 py-3 text-sm font-semibold transition",
                  active ? "bg-teal-50 text-teal-600 shadow-sm ring-1 ring-teal-100" : "text-muted hover:-translate-y-0.5 hover:bg-slate-50 hover:text-ink"
                )}
              >
                <item.icon size={19} />
                {item.label}
              </Link>
            );
          })}
        </nav>

        <button onClick={logout} className="absolute bottom-6 left-6 right-6 flex items-center gap-3 rounded-xl px-4 py-3 text-sm font-semibold text-muted transition hover:-translate-y-0.5 hover:bg-slate-50 hover:text-ink">
          <LogOut size={19} />
          Logout
        </button>
      </aside>

      <div className="lg:pl-72">
        <header className="sticky top-0 z-30 border-b border-slate-200 bg-surface/85 px-5 py-5 backdrop-blur lg:px-8">
          <div className="flex items-center justify-between">
            <button className="grid size-11 place-items-center rounded-xl bg-white shadow-soft ring-1 ring-slate-200 lg:hidden" onClick={() => setOpen((value) => !value)} aria-label="Toggle menu">
              <Menu size={20} />
            </button>
            <div className="hidden lg:block">
              <p className="text-sm text-muted">Welcome back</p>
              <p className="font-bold text-ink">{user?.fullName ?? user?.userName ?? "EduVerse user"}</p>
            </div>
            {user?.role === "Admin" && (
              <div className="relative hidden w-full max-w-lg lg:block">
                <Search className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-muted" size={18} />
                <input
                  value={searchQuery}
                  onChange={(event) => setSearchQuery(event.target.value)}
                  placeholder="Search users, courses, organizations..."
                  className="h-12 w-full rounded-2xl bg-white px-11 text-sm font-semibold text-ink shadow-soft ring-1 ring-slate-200 outline-none transition focus:ring-teal-300"
                />
                {searchResults && (
                  <div className="absolute left-0 right-0 top-14 z-50 max-h-96 overflow-auto rounded-2xl bg-white p-3 shadow-xl ring-1 ring-slate-100">
                    <SearchGroup title="Users">
                      {searchResults.users.map((item) => (
                        <SearchLink key={item.userId} href="/admin/users" title={item.fullName || item.email} meta={`${item.email} - ${item.role}`} onClick={() => setSearchQuery("")} />
                      ))}
                    </SearchGroup>
                    <SearchGroup title="Courses">
                      {searchResults.courses.map((item) => (
                        <SearchLink key={item.courseId} href={`/courses/${item.courseId}`} title={item.title || item.name} meta={`${item.category || "Course"} - ${item.isDeleted ? "Deleted" : "Active"}`} onClick={() => setSearchQuery("")} />
                      ))}
                    </SearchGroup>
                    <SearchGroup title="Organizations">
                      {searchResults.organizations.map((item) => (
                        <SearchLink key={item.organizationAdminId} href={`/organizations/${item.organizationAdminId}`} title={item.organizationAdminName} meta={item.email} onClick={() => setSearchQuery("")} />
                      ))}
                    </SearchGroup>
                    {searchResults.users.length + searchResults.courses.length + searchResults.organizations.length === 0 && (
                      <p className="rounded-xl bg-slate-50 p-3 text-sm font-semibold text-muted">No matching records</p>
                    )}
                  </div>
                )}
              </div>
            )}
            <div className="flex items-center gap-3">
              <ThemeToggle />
              <Link href="/profile" className="flex items-center gap-3 rounded-2xl bg-white px-3 py-2 shadow-soft ring-1 ring-slate-200 transition hover:-translate-y-0.5">
                <div className="grid size-10 place-items-center rounded-xl bg-coral-100 text-coral-500">
                  <User size={18} />
                </div>
                <div className="hidden text-left sm:block">
                  <p className="text-sm font-bold text-ink">{user?.role ?? "Student"}</p>
                  <p className="text-xs text-muted">{user?.email ?? "demo@eduverse.com"}</p>
                </div>
              </Link>
            </div>
          </div>
        </header>
        <main className="px-5 py-10 lg:px-10">{children}</main>
      </div>
    </div>
  );
}

function SearchGroup({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="mb-2 last:mb-0">
      <p className="px-3 py-2 text-xs font-bold uppercase text-muted">{title}</p>
      <div className="space-y-1">{children}</div>
    </div>
  );
}

function SearchLink({ href, title, meta, onClick }: { href: string; title: string; meta: string; onClick: () => void }) {
  return (
    <Link href={href} onClick={onClick} className="block rounded-xl px-3 py-2 transition hover:bg-slate-50">
      <p className="text-sm font-bold text-ink">{title || "Not available"}</p>
      <p className="mt-1 text-xs text-muted">{meta}</p>
    </Link>
  );
}
