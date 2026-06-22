"use client";

import { ArrowRight, BookOpen, CheckCircle2, GraduationCap, ShieldCheck, Sparkles } from "lucide-react";
import { useEffect, useState } from "react";
import { BrandLogo } from "@/components/brand-logo";
import { SmartImage } from "@/components/smart-image";
import { SEED_IMAGES } from "@/lib/image-fallbacks";
import { ThemeToggle } from "@/components/theme-toggle";
import { TrendingCoursesSection } from "@/components/trending-courses-section";
import { LinkButton, StatCard } from "@/components/ui";
import { getDashboardPath, getStoredUser, getToken } from "@/lib/auth";
import type { AuthUser } from "@/lib/types";

export default function LandingPage() {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [authChecked, setAuthChecked] = useState(false);
  const dashboardHref = getDashboardPath(user?.role);

  useEffect(() => {
    const token = getToken();
    const storedUser = getStoredUser();
    setUser(token && storedUser ? storedUser : null);
    setAuthChecked(true);
  }, []);

  return (
    <main>
      <header className="mx-auto flex max-w-7xl items-center justify-between px-5 py-5 lg:px-8">
        <BrandLogo imageClassName="h-14 max-w-44" />
        <nav className="hidden items-center gap-7 text-sm font-semibold text-muted md:flex">
          <a href="#courses" className="hover:text-ink">Courses</a>
          <a href="#platform" className="hover:text-ink">Platform</a>
          <a href="#roles" className="hover:text-ink">Roles</a>
        </nav>
        <div className="flex items-center gap-3">
          <ThemeToggle />
          {!authChecked ? null : user ? (
            <>
              <LinkButton href={dashboardHref} variant="ghost">Dashboard</LinkButton>
              <LinkButton href="/profile" className="hidden sm:inline-flex">Profile</LinkButton>
            </>
          ) : (
            <>
              <LinkButton href="/login" variant="ghost">Login</LinkButton>
              <LinkButton href="/register" className="hidden sm:inline-flex">Join now</LinkButton>
            </>
          )}
        </div>
      </header>

      <section className="mx-auto grid max-w-7xl gap-10 px-5 pb-12 pt-8 lg:grid-cols-[1.02fr_0.98fr] lg:items-center lg:px-8 lg:pb-16">
        <div>
          <div className="inline-flex items-center gap-2 rounded-full bg-white px-4 py-2 text-sm font-semibold text-teal-600 shadow-soft ring-1 ring-slate-100">
            <Sparkles size={16} />
            Learn, track, pay, and certify in one place
          </div>
          <h1 className="sr-only">EduVerse</h1>
          <BrandLogo className="mt-6" imageClassName="h-24 max-w-full sm:h-28 lg:h-32" />
          <p className="mt-5 max-w-2xl text-lg leading-8 text-muted">
            A responsive LMS experience for students, instructors, and admins with courses, enrollments, certificates, payments, and dashboards built around your backend API.
          </p>
          <div className="mt-8 flex flex-col gap-3 sm:flex-row">
            <LinkButton href="/courses">
              Explore courses <ArrowRight size={18} />
            </LinkButton>
            {authChecked && <LinkButton href={user ? dashboardHref : "/login"} variant="ghost">Open dashboard</LinkButton>}
          </div>
          <div className="mt-9 grid gap-4 sm:grid-cols-3">
            <StatCard label="Courses" value="120+" icon={BookOpen} />
            <StatCard label="Students" value="24k" icon={GraduationCap} accent="amber" />
            <StatCard label="Secure roles" value="3" icon={ShieldCheck} accent="coral" />
          </div>
        </div>

        <div className="relative">
          <div className="overflow-hidden rounded-xl2 bg-white shadow-soft ring-1 ring-slate-100">
            <SmartImage
              src={SEED_IMAGES.learningHero}
              fallbackSrc={SEED_IMAGES.learningHero}
              alt="Students learning together"
              className="h-[390px] w-full object-cover"
            />
            <div className="grid gap-4 p-5 sm:grid-cols-3">
              {["JWT Auth", "Payments", "Certificates"].map((item) => (
                <div key={item} className="flex items-center gap-2 rounded-xl bg-slate-50 px-3 py-3 text-sm font-semibold text-ink">
                  <CheckCircle2 size={17} className="text-teal-600" />
                  {item}
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

      <TrendingCoursesSection />

      <section id="platform" className="mx-auto max-w-7xl px-5 py-12 lg:px-8">
        <div className="grid gap-6 lg:grid-cols-3">
          {[
            ["Student workspace", "Enroll, submit assignments, monitor progress, and download certificates."],
            ["Instructor tools", "Manage owned courses, follow enrollments, and review course payments."],
            ["Admin-ready", "Role-based UI is prepared for complete platform administration."]
          ].map(([title, description]) => (
            <div key={title} className="rounded-xl2 bg-white p-6 shadow-soft ring-1 ring-slate-100">
              <div className="grid size-12 place-items-center rounded-xl bg-teal-50 text-teal-600">
                <CheckCircle2 />
              </div>
              <h3 className="mt-5 text-xl font-bold text-ink">{title}</h3>
              <p className="mt-3 text-sm leading-6 text-muted">{description}</p>
            </div>
          ))}
        </div>
      </section>
    </main>
  );
}
