import type { AuthUser, UserRole } from "./types";

const TOKEN_KEY = "eduverse_token";
const USER_KEY = "eduverse_user";

export function getToken() {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(TOKEN_KEY);
}

export function setToken(token: string) {
  localStorage.setItem(TOKEN_KEY, token);
}

export function clearAuth() {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
}

export function getStoredUser(): AuthUser | null {
  if (typeof window === "undefined") return null;
  const rawUser = localStorage.getItem(USER_KEY);
  if (!rawUser) return null;

  try {
    return JSON.parse(rawUser) as AuthUser;
  } catch {
    return null;
  }
}

export function setStoredUser(user: AuthUser) {
  localStorage.setItem(USER_KEY, JSON.stringify(user));
}

export function inferRole(value?: string): UserRole {
  const normalized = value?.toLowerCase();
  if (normalized === "admin") return "Admin";
  if (normalized === "instructor") return "Instructor";
  return "Student";
}

function normalizeId(value?: unknown) {
  return typeof value === "string" && value.trim() ? value.trim() : undefined;
}

export function decodeJwtPayload(token?: string | null): Record<string, any> | null {
  if (!token) return null;

  try {
    const base64 = token.split(".")[1]?.replace(/-/g, "+").replace(/_/g, "/");
    if (!base64 || typeof atob === "undefined") return null;

    const padded = base64.padEnd(Math.ceil(base64.length / 4) * 4, "=");
    return JSON.parse(atob(padded));
  } catch {
    return null;
  }
}

export function getUserIdFromToken(token?: string | null) {
  const payload = decodeJwtPayload(token);
  if (!payload) return undefined;

  return normalizeId(
    payload.id ??
      payload.Id ??
      payload.userId ??
      payload.UserId ??
      payload.nameIdentifier ??
      payload.NameIdentifier ??
      payload.nameidentifier ??
      payload.sub ??
      payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"] ??
      payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/nameidentifier"]
  );
}

export function getCurrentUserId() {
  const user = getStoredUser() as (AuthUser & Record<string, unknown>) | null;
  const storedUserId = normalizeId(
    user?.id ??
      user?.userId ??
      user?.Id ??
      user?.UserId ??
      user?.nameIdentifier ??
      user?.NameIdentifier ??
      user?.nameidentifier ??
      user?.sub
  );

  return storedUserId ?? getUserIdFromToken(getToken());
}

export function getRoleFromToken(token?: string | null): UserRole | undefined {
  const payload = decodeJwtPayload(token);
  if (!payload) return undefined;

  try {
    const role =
      payload.role ??
      payload.Role ??
      payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];

    if (Array.isArray(role)) {
      return inferRole(role[0]);
    }

    return role ? inferRole(role) : undefined;
  } catch {
    return undefined;
  }
}
