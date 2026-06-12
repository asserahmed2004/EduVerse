import { ArrowRight, BookOpen, Loader2 } from "lucide-react";
import Link from "next/link";
import type { Course } from "@/lib/types";
import { cn, formatCurrency } from "@/lib/utils";

export function Button({
  children,
  className,
  variant = "primary",
  ...props
}: React.ButtonHTMLAttributes<HTMLButtonElement> & { variant?: "primary" | "secondary" | "ghost" }) {
  return (
    <button
      className={cn(
        "inline-flex h-11 items-center justify-center gap-2 rounded-xl px-5 text-sm font-semibold transition",
        variant === "primary" && "bg-gradient-to-r from-teal-500 to-indigo-500 text-white shadow-button hover:-translate-y-0.5 hover:shadow-lg",
        variant === "secondary" && "bg-ink text-white hover:-translate-y-0.5 hover:bg-slate-800",
        variant === "ghost" && "bg-white text-ink ring-1 ring-slate-200 hover:-translate-y-0.5 hover:bg-slate-50 hover:shadow-soft",
        className
      )}
      {...props}
    >
      {children}
    </button>
  );
}

export function LinkButton({
  href,
  children,
  variant = "primary",
  className
}: {
  href: string;
  children: React.ReactNode;
  variant?: "primary" | "secondary" | "ghost";
  className?: string;
}) {
  return (
    <Link
      href={href}
      className={cn(
        "inline-flex h-11 items-center justify-center gap-2 rounded-xl px-5 text-sm font-semibold transition",
        variant === "primary" && "bg-gradient-to-r from-teal-500 to-indigo-500 text-white shadow-button hover:-translate-y-0.5 hover:shadow-lg",
        variant === "secondary" && "bg-ink text-white hover:-translate-y-0.5 hover:bg-slate-800",
        variant === "ghost" && "bg-white text-ink ring-1 ring-slate-200 hover:-translate-y-0.5 hover:bg-slate-50 hover:shadow-soft",
        className
      )}
    >
      {children}
    </Link>
  );
}

export function Badge({ children, tone = "teal" }: { children: React.ReactNode; tone?: "teal" | "amber" | "coral" | "slate" }) {
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full px-3 py-1 text-xs font-semibold tracking-[0.02em] ring-1 ring-current/10",
        tone === "teal" && "bg-teal-50 text-teal-600",
        tone === "amber" && "bg-amber-100 text-amber-500",
        tone === "coral" && "bg-coral-100 text-coral-500",
        tone === "slate" && "bg-slate-100 text-slate-600"
      )}
    >
      {children}
    </span>
  );
}

export function StatCard({ label, value, icon: Icon, accent = "teal" }: { label: string; value: string; icon: any; accent?: "teal" | "amber" | "coral" | "ink" }) {
  return (
    <div className="rounded-xl2 bg-white p-5 shadow-soft ring-1 ring-slate-100 transition duration-200 hover:-translate-y-0.5 hover:shadow-lg">
      <div className="flex items-center justify-between">
        <div>
          <p className="text-sm text-muted">{label}</p>
          <p className="mt-2 text-2xl font-bold text-ink">{value}</p>
        </div>
        <div
          className={cn(
            "grid size-12 place-items-center rounded-xl shadow-sm",
            accent === "teal" && "bg-teal-50 text-teal-600",
            accent === "amber" && "bg-amber-100 text-amber-500",
            accent === "coral" && "bg-coral-100 text-coral-500",
            accent === "ink" && "bg-slate-100 text-ink"
          )}
        >
          <Icon size={22} />
        </div>
      </div>
    </div>
  );
}

export function PageHeader({ eyebrow, title, description, action }: { eyebrow?: string; title: string; description?: string; action?: React.ReactNode }) {
  return (
    <div className="flex flex-col justify-between gap-4 md:flex-row md:items-end">
      <div>
        {eyebrow && <p className="text-sm font-bold uppercase tracking-[0.08em] text-teal-600">{eyebrow}</p>}
        <h1 className="mt-2 text-3xl font-black tracking-normal text-ink md:text-5xl">{title}</h1>
        {description && <p className="mt-4 max-w-2xl text-base leading-7 text-muted">{description}</p>}
      </div>
      {action}
    </div>
  );
}

export function CourseCard({ course, compact = false }: { course: Course; compact?: boolean }) {
  return (
    <Link href={`/courses/${course.id}`} className="group block overflow-hidden rounded-xl2 bg-white shadow-soft ring-1 ring-slate-100 transition duration-200 hover:-translate-y-1 hover:shadow-xl">
      <div className={cn("relative overflow-hidden bg-slate-100", compact ? "h-40" : "h-52")}>
        {course.imageUrl ? (
          <img src={course.imageUrl} alt={course.name} className="size-full object-cover transition duration-500 group-hover:scale-105" />
        ) : (
          <div className="grid size-full place-items-center bg-teal-50 text-teal-600">
            <BookOpen size={34} />
          </div>
        )}
        <div className="absolute left-4 top-4">
          <Badge tone="amber">{course.category ?? course.categories?.[0]?.name ?? course.level ?? "Course"}</Badge>
        </div>
        {course.price <= 0 && (
          <div className="absolute right-4 top-4">
            <Badge tone="teal">Free</Badge>
          </div>
        )}
      </div>
      <div className="p-5">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-xs font-semibold uppercase text-teal-600">{course.category ?? course.name}</p>
            <h3 className="mt-2 text-lg font-bold leading-snug text-ink">{course.title}</h3>
          </div>
          <span className="rounded-full bg-slate-100 px-2.5 py-1 text-xs font-bold text-ink shadow-sm">{(course.rating ?? 0).toFixed(1)}</span>
        </div>
        <p className="mt-3 line-clamp-2 text-sm leading-6 text-muted">{course.description}</p>
        <div className="mt-4 grid grid-cols-3 gap-2 text-xs font-semibold text-muted">
          <span>Instructor: {course.instructorName ?? "Unassigned"}</span>
          <span>{course.studentsCount ?? course.students ?? 0} students</span>
          <span>{course.sessionsCount ?? 0} sessions</span>
        </div>
        {(course.organizationName || course.organizationOwnerName || course.isDeleted) && (
          <div className="mt-3 flex flex-wrap items-center gap-2 text-xs font-semibold text-muted">
            <span>Organization: {course.organizationName || course.organizationOwnerName || "EduVerseOrganization"}</span>
            <Badge tone={course.isDeleted ? "coral" : "teal"}>{course.isDeleted ? "Deleted" : "Active"}</Badge>
          </div>
        )}
        <div className="mt-5 flex items-center justify-between">
          <p className="font-bold text-ink">{course.price <= 0 ? "Free" : formatCurrency(course.price)}</p>
          <span className="inline-flex items-center gap-1 text-sm font-semibold text-teal-600">
            Details <ArrowRight size={16} />
          </span>
        </div>
      </div>
    </Link>
  );
}

export function ProgressBar({ value }: { value: number }) {
  return (
    <div className="h-2 overflow-hidden rounded-full bg-slate-100">
      <div className="h-full rounded-full bg-gradient-to-r from-teal-500 to-indigo-500 transition-all duration-500" style={{ width: `${Math.min(100, Math.max(0, value))}%` }} />
    </div>
  );
}

export function LoadingState({ label = "Loading" }: { label?: string }) {
  return (
    <div className="grid min-h-48 place-items-center rounded-xl2 bg-white shadow-soft ring-1 ring-slate-100">
      <div className="flex items-center gap-2 text-sm font-semibold text-muted">
        <Loader2 className="animate-spin" size={18} />
        {label}
      </div>
    </div>
  );
}

export function EmptyState({ title, description }: { title: string; description: string }) {
  return (
    <div className="rounded-xl2 bg-white p-8 text-center shadow-soft ring-1 ring-slate-100">
      <div className="mx-auto grid size-14 place-items-center rounded-xl bg-teal-50 text-teal-600 shadow-sm">
        <BookOpen />
      </div>
      <h3 className="mt-4 text-lg font-bold text-ink">{title}</h3>
      <p className="mt-2 text-sm text-muted">{description}</p>
    </div>
  );
}
