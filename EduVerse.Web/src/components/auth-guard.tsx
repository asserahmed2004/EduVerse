"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { getStoredUser, getToken } from "@/lib/auth";
import type { UserRole } from "@/lib/types";
import { LoadingState } from "./ui";

export function AuthGuard({ children, roles }: { children: React.ReactNode; roles?: UserRole[] }) {
  const router = useRouter();
  const [allowed, setAllowed] = useState(false);

  useEffect(() => {
    const token = getToken();
    const user = getStoredUser();

    if (!token || !user) {
      router.replace("/login");
      return;
    }

    if (roles?.length && user && !roles.includes(user.role)) {
      router.replace(user.role === "Admin" ? "/admin" : user.role === "OrganizationAdmin" ? "/dashboard/organization" : user.role === "Instructor" ? "/dashboard/instructor" : "/dashboard/student");
      return;
    }

    setAllowed(true);
  }, [router, roles]);

  if (!allowed) return <LoadingState label="Checking access" />;
  return children;
}
