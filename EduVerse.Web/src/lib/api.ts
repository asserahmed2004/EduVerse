import axios from "axios";
import { clearAuth, getCurrentUserId, getRoleFromToken, getToken, getUserIdFromToken, inferRole, isAuthenticatedStudent, setStoredUser, setToken } from "./auth";
import type {
  ActivityLog,
  AssignmentProgress,
  AuthUser,
  AdminAssignment,
  AdminSession,
  AdminUserDetails,
  Certificate,
  CertificateEligibility,
  ChangePasswordPayload,
  Course,
  CourseAdminDetails,
  CourseProgress,
  CourseSession,
  DashboardStats,
  Enrollment,
  GlobalSearchResult,
  InstructorCourse,
  InstructorOverview,
  InstructorSession,
  InstructorStudent,
  InstructorSubmission,
  LoginPayload,
  ManagedUser,
  NotificationItem,
  OrganizationOverview,
  OrganizationDetails,
  Payment,
  PaymentFilters,
  PaginatedResponse,
  RecentCourse,
  RecentActivity,
  RecentEnrollment,
  RegisterPayload,
  RatingResult,
  RoleCount,
  ServiceResult,
  StudentAssignment,
  StudentSubmission,
  TopCourse,
  TopCourseChart,
  ToggleSessionDoneResult,
  TopInstructor,
  TopOrganization,
  TrendPoint,
  UpdateProfilePayload
} from "./types";

export const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5000";
const API_CONNECTION_MESSAGE = `Could not connect to API. Make sure the backend is running on ${API_BASE_URL}.`;

function isCloudGetUrl(value: string) {
  try {
    const url = new URL(value, API_BASE_URL);
    return /\/Cloud\/Get\//i.test(url.pathname);
  } catch {
    return false;
  }
}

function withDownloadPreference(value: string, download: boolean) {
  try {
    const url = new URL(value, API_BASE_URL);
    if (isCloudGetUrl(url.toString())) {
      url.searchParams.set("download", String(download));
    }
    return url.toString();
  } catch {
    return value;
  }
}

function inferFileName(value: string) {
  try {
    const url = new URL(value, API_BASE_URL);
    const segments = url.pathname.split("/").filter(Boolean);
    return decodeURIComponent(segments[segments.length - 1] ?? "download");
  } catch {
    return "download";
  }
}

const VIDEO_FILE_EXTENSIONS = new Set(["mp4", "webm", "ogg", "ogv", "mov", "mkv"]);
const EMBEDDABLE_VIDEO_EXTENSIONS = new Set(["mp4", "webm", "ogg", "ogv", "mov"]);
const PDF_FILE_EXTENSIONS = new Set(["pdf"]);
const IMAGE_FILE_EXTENSIONS = new Set(["png", "jpg", "jpeg", "gif", "bmp", "webp", "svg"]);
const OFFICE_FILE_EXTENSIONS = new Set(["doc", "docx", "ppt", "pptx", "xls", "xlsx"]);
const ARCHIVE_FILE_EXTENSIONS = new Set(["zip", "rar", "7z"]);

export type FileResourceType = "video" | "pdf" | "image" | "office" | "archive" | "unknown";

function getUrlPathname(value?: string) {
  if (!value) return "";
  try {
    return decodeURIComponent(new URL(value, API_BASE_URL).pathname);
  } catch {
    return decodeURIComponent(value.split(/[?#]/)[0] ?? value);
  }
}

function getFileExtension(value?: string) {
  const pathname = getUrlPathname(value);
  const match = pathname.match(/\.([a-z0-9]+)$/i);
  return match?.[1]?.toLowerCase();
}

function hasDomainWithoutScheme(value: string) {
  return /^(www\.)?[\w-]+(\.[\w-]+)+(\/|$|\?)/i.test(value);
}

export function isVideoFileUrl(value?: string) {
  const extension = getFileExtension(value);
  return extension ? VIDEO_FILE_EXTENSIONS.has(extension) : false;
}

export function getFileResourceType(value?: string): FileResourceType {
  const extension = getFileExtension(value);
  if (!extension) return "unknown";
  if (VIDEO_FILE_EXTENSIONS.has(extension)) return "video";
  if (PDF_FILE_EXTENSIONS.has(extension)) return "pdf";
  if (IMAGE_FILE_EXTENSIONS.has(extension)) return "image";
  if (OFFICE_FILE_EXTENSIONS.has(extension)) return "office";
  if (ARCHIVE_FILE_EXTENSIONS.has(extension)) return "archive";
  return "unknown";
}

export function isEmbeddableVideoFileUrl(value?: string) {
  const extension = getFileExtension(value);
  return extension ? EMBEDDABLE_VIDEO_EXTENSIONS.has(extension) : false;
}

export function isKnownExternalVideoUrl(value?: string) {
  if (!value) return false;
  try {
    const url = new URL(normalizeExternalUrl(value) ?? value, API_BASE_URL);
    const host = url.hostname.replace(/^www\./i, "").toLowerCase();
    return host === "youtu.be" || host.endsWith("youtube.com") || host.endsWith("vimeo.com");
  } catch {
    return false;
  }
}

function readSuccessFlag(data: any): boolean | undefined {
  if (!data || typeof data !== "object" || Array.isArray(data)) return undefined;

  const value = data.success ?? data.Success ?? data.succeed ?? data.Succeed;
  if (typeof value === "boolean") return value;
  if (typeof value === "string") {
    if (value.toLowerCase() === "true") return true;
    if (value.toLowerCase() === "false") return false;
  }

  return undefined;
}

function readResponseMessage(data: any): string | undefined {
  if (typeof data === "string" && data.trim()) return data;

  const errors = data?.errors ?? data?.Errors;
  if (Array.isArray(errors) && errors.length > 0) {
    return errors.map(String).join("\n");
  }
  if (errors && typeof errors === "object") {
    const messages = Object.values(errors).flatMap((value) => Array.isArray(value) ? value.map(String) : [String(value)]);
    if (messages.length > 0) return messages.join("\n");
  }

  return data?.message ?? data?.Message ?? data?.detail ?? data?.Detail ?? data?.title ?? data?.Title;
}

export function getApiErrorMessage(error: unknown, fallbackMessage = "Request failed."): string {
  const axiosError = error as {
    code?: string;
    response?: { status?: number; data?: any };
  };
  const responseMessage = readResponseMessage(axiosError?.response?.data);
  if (responseMessage) return responseMessage;

  if (!axios.isAxiosError(error) && !axiosError?.response) {
    return error instanceof Error ? error.message : fallbackMessage;
  }

  if (!axiosError?.response) {
    return API_CONNECTION_MESSAGE;
  }

  if (axiosError.response.status === 401) {
    return "You are not authenticated or your session has expired. Please sign in again.";
  }
  if (axiosError.response.status === 403) {
    return "You do not have permission to perform this action.";
  }
  if (axiosError.response.status === 404) {
    return "The requested API resource was not found.";
  }
  if (axiosError.response.status && axiosError.response.status >= 500) {
    return "The API could not complete the request. Check the backend logs and try again.";
  }

  return error instanceof Error && error.message !== "Network Error" ? error.message : fallbackMessage;
}

export function getPreviewFileUrl(value?: string) {
  if (!value) return undefined;
  return withDownloadPreference(value, false);
}

export function getDownloadFileUrl(value?: string) {
  if (!value) return undefined;
  return withDownloadPreference(value, true);
}

export function openFile(value?: string) {
  const url = getPreviewFileUrl(value);
  if (!url || typeof window === "undefined") return;
  window.open(url, "_blank", "noopener,noreferrer");
}

export function downloadFile(value?: string, fileName?: string) {
  const url = getDownloadFileUrl(value);
  if (!url || typeof document === "undefined") return;

  const link = document.createElement("a");
  link.href = url;
  link.target = "_blank";
  link.rel = "noopener noreferrer";
  link.download = fileName ?? inferFileName(url);
  document.body.appendChild(link);
  link.click();
  link.remove();
}

export const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 15000
});

api.interceptors.request.use((config) => {
  const token = getToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

api.interceptors.response.use(
  (response) => {
    if (readSuccessFlag(response.data) === false) {
      const error = new Error(readResponseMessage(response.data) ?? "The API reported that the request failed.");
      Object.assign(error, { response });
      return Promise.reject(error);
    }
    return response;
  },
  (error) => {
    if (error instanceof Error) {
      error.message = getApiErrorMessage(error, error.message);
    }
    return Promise.reject(error);
  }
);

function normalizeImageUrl(value?: string) {
  if (!value) return undefined;
  if (value.startsWith("http")) return value;
  return `${API_BASE_URL}/Cloud/Get/courses/${encodeURIComponent(value)}`;
}

function normalizeCertificateUrl(value?: string) {
  if (!value) return undefined;
  if (value.startsWith("http")) return value;
  if (value.startsWith("/")) return `${API_BASE_URL}${value}`;
  return `${API_BASE_URL}/Cloud/Get/certificates/${encodeURIComponent(value)}`;
}

function normalizeCloudFileUrl(folder: string, value?: string) {
  if (!value) return undefined;
  const trimmed = value.trim();
  if (!trimmed) return undefined;
  if (trimmed.startsWith("http")) return trimmed;
  if (trimmed.startsWith("/")) return `${API_BASE_URL}${trimmed}`;
  if (trimmed.includes("/")) return `${API_BASE_URL}/${trimmed.replace(/^\/+/, "")}`;
  return `${API_BASE_URL}/Cloud/Get/${folder}/${encodeURIComponent(trimmed)}`;
}

function normalizeSessionVideoUrl(value?: string) {
  if (!value) return undefined;
  const trimmed = value.trim();
  if (!trimmed) return undefined;
  if (trimmed.startsWith("http")) return trimmed;
  if (trimmed.startsWith("/")) return `${API_BASE_URL}${trimmed}`;
  if (isVideoFileUrl(trimmed) && !trimmed.includes("/") && !trimmed.includes("\\")) {
    return `${API_BASE_URL}/Cloud/Get/sessions/${encodeURIComponent(trimmed)}`;
  }
  if (hasDomainWithoutScheme(trimmed)) return normalizeExternalUrl(trimmed);
  if (trimmed.includes("/")) return `${API_BASE_URL}/${trimmed.replace(/^\/+/, "")}`;
  return normalizeExternalUrl(trimmed);
}

function normalizeExternalUrl(value?: string) {
  if (!value) return undefined;
  const trimmed = value.trim();
  if (!trimmed) return undefined;
  if (trimmed.startsWith("http") || trimmed.startsWith("/")) return trimmed;
  if (trimmed.startsWith("www.") || /^[\w-]+\.[\w.-]+/.test(trimmed)) return `https://${trimmed}`;
  return trimmed;
}

function normalizeProfilePictureUrl(value?: string) {
  if (!value) return undefined;
  if (value.startsWith("http")) return value;
  if (value.startsWith("/")) return `${API_BASE_URL}${value}`;
  return `${API_BASE_URL}/Cloud/Get/ProfilePicture/${encodeURIComponent(value)}`;
}

function normalizeCourse(course: any): Course {
  const value = unwrapData(course) ?? course ?? {};
  return {
    id: value.id ?? value.Id ?? value.courseId ?? value.CourseId ?? "",
    name: value.name ?? value.Name ?? value.courseName ?? value.CourseName ?? "Course",
    title: value.title ?? value.Title ?? value.name ?? value.Name ?? value.courseName ?? value.CourseName ?? "Course",
    description: value.description ?? value.Description ?? "",
    price: value.price ?? value.Price ?? 0,
    duration: value.duration ?? value.Duration ?? 0,
    rating: value.rating ?? value.Rating ?? 0,
    userRating: value.userRating ?? value.UserRating ?? 0,
    orgId: value.orgId ?? value.OrgId,
    organizationId: value.organizationId ?? value.OrganizationId,
    organizationName: value.organizationName ?? value.OrganizationName ?? "EduVerseOrganization",
    instructorId: value.instructorId ?? value.InstructorId,
    imageUrl: normalizeImageUrl(value.imageUrl ?? value.ImageUrl),
    categories: value.categories ?? value.Categories ?? [],
    category: value.category ?? value.Category ?? value.categories?.[0]?.name ?? value.Categories?.[0]?.Name,
    instructorName: value.instructorName ?? value.InstructorName,
    organizationOwnerName: value.organizationOwnerName ?? value.OrganizationOwnerName ?? value.organizationName ?? value.OrganizationName ?? "EduVerseOrganization",
    organizationOwnerEmail: value.organizationOwnerEmail ?? value.OrganizationOwnerEmail,
    studentsCount: value.studentsCount ?? value.StudentsCount ?? 0,
    sessionsCount: value.sessionsCount ?? value.SessionsCount ?? 0,
    isDeleted: value.isDeleted ?? value.IsDeleted ?? false,
    deletedAt: value.deletedAt ?? value.DeletedAt,
    deletedById: value.deletedById ?? value.DeletedById,
    deletedByName: value.deletedByName ?? value.DeletedByName,
    restoredAt: value.restoredAt ?? value.RestoredAt,
    restoredById: value.restoredById ?? value.RestoredById,
    restoredByName: value.restoredByName ?? value.RestoredByName,
    level: value.level ?? value.Level,
    tags: value.tags ?? value.Tags,
    ratingCount: value.ratingCount ?? value.RatingCount ?? 0,
    recommendationScore: value.recommendationScore ?? value.RecommendationScore,
    progressPercent: value.progressPercent ?? value.ProgressPercent
  };
}

function normalizeCourseList(data: any): Course[] {
  const courses = Array.isArray(data) ? data : unwrapData(data);
  if (!Array.isArray(courses)) return [];

  return courses
    .filter((course) => course && typeof course === "object")
    .map(normalizeCourse);
}

function normalizeRecommendedCourses(data: any): Course[] {
  if (data?.success === false) {
    throw new Error(data.message ?? data.Message ?? "Recommendation request failed.");
  }

  const items = unwrapData(data);
  if (!Array.isArray(items)) {
    return [];
  }

  return items.map(normalizeCourse);
}

function normalizeSession(session: any): CourseSession {
  return {
    id: session.id ?? session.Id ?? session.sessionId ?? session.SessionId ?? "",
    courseId: session.courseId ?? session.CourseId,
    title: session.title ?? session.Title ?? `Session ${session.sessionNumber ?? session.SessionNumber ?? ""}`,
    fileUrl: normalizeCloudFileUrl("sessions", session.fileUrl ?? session.FileUrl ?? session.filePath ?? session.FilePath),
    trainerId: session.trainerId ?? session.TrainerId,
    date: session.date ?? session.Date,
    duration: session.duration ?? session.Duration,
    sessionNumber: session.sessionNumber ?? session.SessionNumber ?? 0,
    description: session.description ?? session.Description,
    videoUrl: normalizeSessionVideoUrl(session.videoUrl ?? session.VideoUrl),
    externalLink: normalizeExternalUrl(session.externalLink ?? session.ExternalLink),
    isCompleted: session.isCompleted ?? session.IsCompleted ?? false,
    attendanceCode: session.attendanceCode ?? session.AttendanceCode,
    attendanceCodeCreatedAt: session.attendanceCodeCreatedAt ?? session.AttendanceCodeCreatedAt
  };
}

function normalizePayment(payment: any): Payment {
  return {
    courseId: payment.courseId ?? payment.CourseId,
    studentId: payment.studentId ?? payment.StudentId,
    submittingDate: payment.submittingDate ?? payment.SubmittingDate ?? new Date().toISOString(),
    totalPrice: payment.totalPrice ?? payment.TotalPrice ?? 0,
    paymentMethod: payment.paymentMethod ?? payment.PaymentMethod ?? "card",
    paymentStatus: payment.paymentStatus ?? payment.PaymentStatus ?? "Pending",
    paymentProvider: payment.paymentProvider ?? payment.PaymentProvider ?? "Paymob",
    specialReference: payment.specialReference ?? payment.SpecialReference,
    merchantOrderId: payment.merchantOrderId ?? payment.MerchantOrderId,
    providerIntentionId: payment.providerIntentionId ?? payment.ProviderIntentionId,
    redirectUrl: payment.redirectUrl ?? payment.RedirectUrl,
    providerStatusCode: payment.providerStatusCode ?? payment.ProviderStatusCode,
    courseName: payment.courseName ?? payment.CourseName,
    studentName: payment.studentName ?? payment.StudentName,
    studentEmail: payment.studentEmail ?? payment.StudentEmail
  };
}

function normalizeActivityLog(item: any): ActivityLog {
  return {
    id: item.id ?? item.Id ?? "",
    userId: item.userId ?? item.UserId,
    userName: item.userName ?? item.UserName ?? "Unknown",
    action: item.action ?? item.Action ?? "",
    entityType: item.entityType ?? item.EntityType ?? "",
    entityId: item.entityId ?? item.EntityId,
    description: item.description ?? item.Description ?? "",
    createdAt: item.createdAt ?? item.CreatedAt ?? new Date().toISOString()
  };
}

function unwrapData(data: any) {
  return data?.data ?? data?.Data ?? data;
}

function unwrapArray(data: any): any[] {
  const value = unwrapData(data);
  return Array.isArray(value) ? value : [];
}

function toBackendRole(role: string) {
  const normalized = role.trim().toLowerCase();
  if (normalized === "organizationadmin") return "organizationAdmin";
  if (normalized === "admin") return "admin";
  if (normalized === "instructor") return "instructor";
  if (normalized === "student") return "student";
  return role;
}

function normalizeStats(data: any): DashboardStats {
  const value = unwrapData(data);
  return {
    totalUsers: value.totalUsers ?? value.TotalUsers,
    totalOrganizations: value.totalOrganizations ?? value.TotalOrganizations,
    totalCourses: value.totalCourses ?? value.TotalCourses ?? 0,
    deletedCourses: value.deletedCourses ?? value.DeletedCourses ?? 0,
    totalRevenue: value.totalRevenue ?? value.TotalRevenue ?? 0,
    totalPayments: value.totalPayments ?? value.TotalPayments ?? 0,
    totalStudents: value.totalStudents ?? value.TotalStudents ?? 0,
    totalInstructors: value.totalInstructors ?? value.TotalInstructors ?? 0,
    totalEnrollments: value.totalEnrollments ?? value.TotalEnrollments ?? 0,
    totalSessions: value.totalSessions ?? value.TotalSessions ?? 0,
    totalAssignments: value.totalAssignments ?? value.TotalAssignments ?? 0,
    pendingPayments: value.pendingPayments ?? value.PendingPayments ?? 0,
    averageRating: value.averageRating ?? value.AverageRating ?? 0
  };
}

function normalizeOrganizationOverview(item: any): OrganizationOverview {
  const organizationId = item.organizationId ?? item.OrganizationId ?? item.id ?? item.Id ?? item.organizationAdminId ?? item.OrganizationAdminId ?? "";
  const organizationName = item.organizationName ?? item.OrganizationName ?? item.name ?? item.Name ?? item.organizationAdminName ?? item.OrganizationAdminName ?? "Organization";
  return {
    organizationId,
    organizationName,
    organizationAdminId: item.organizationAdminId ?? item.OrganizationAdminId ?? organizationId,
    organizationAdminName: item.organizationAdminName ?? item.OrganizationAdminName ?? organizationName,
    email: item.email ?? item.Email ?? "",
    phoneNumber: item.phoneNumber ?? item.PhoneNumber,
    description: item.description ?? item.Description,
    websiteUrl: item.websiteUrl ?? item.WebsiteUrl,
    status: item.status ?? item.Status,
    coursesCount: item.coursesCount ?? item.CoursesCount ?? 0,
    studentsCount: item.studentsCount ?? item.StudentsCount ?? 0,
    enrollmentsCount: item.enrollmentsCount ?? item.EnrollmentsCount ?? 0,
    revenue: item.revenue ?? item.Revenue ?? 0,
    averageRating: item.averageRating ?? item.AverageRating ?? 0
  };
}

function normalizeRecentEnrollment(item: any): RecentEnrollment {
  return {
    courseId: item.courseId ?? item.CourseId ?? "",
    courseName: item.courseName ?? item.CourseName ?? "",
    studentId: item.studentId ?? item.StudentId ?? "",
    studentName: item.studentName ?? item.StudentName ?? "",
    studentEmail: item.studentEmail ?? item.StudentEmail ?? "",
    enrollmentDate: item.enrollmentDate ?? item.EnrollmentDate ?? new Date().toISOString(),
    progression: item.progression ?? item.Progression ?? 0
  };
}

function normalizeRecentCourse(item: any): RecentCourse {
  return {
    courseId: item.courseId ?? item.CourseId ?? "",
    courseName: item.courseName ?? item.CourseName ?? "",
    title: item.title ?? item.Title ?? item.courseName ?? item.CourseName ?? "",
    organizationAdminId: item.organizationAdminId ?? item.OrganizationAdminId ?? "",
    organizationAdminName: item.organizationAdminName ?? item.OrganizationAdminName ?? "",
    price: item.price ?? item.Price ?? 0,
    isDeleted: item.isDeleted ?? item.IsDeleted ?? false
  };
}

function normalizeOrganizationDetails(item: any): OrganizationDetails {
  const overview = normalizeOrganizationOverview(item);
  return {
    ...overview,
    id: overview.organizationId,
    name: overview.organizationName,
    admins: (item.admins ?? item.Admins ?? []).map((user: any) => ({
      userId: user.userId ?? user.UserId ?? "",
      fullName: user.fullName ?? user.FullName ?? "",
      userName: user.userName ?? user.UserName ?? "",
      email: user.email ?? user.Email ?? "",
      role: user.role ?? user.Role ?? ""
    })),
    instructors: (item.instructors ?? item.Instructors ?? []).map((user: any) => ({
      userId: user.userId ?? user.UserId ?? "",
      fullName: user.fullName ?? user.FullName ?? "",
      userName: user.userName ?? user.UserName ?? "",
      email: user.email ?? user.Email ?? "",
      role: user.role ?? user.Role ?? ""
    })),
    courses: (item.courses ?? item.Courses ?? []).map((course: any) => ({
      courseId: course.courseId ?? course.CourseId ?? "",
      name: course.name ?? course.Name ?? "",
      title: course.title ?? course.Title ?? "",
      price: course.price ?? course.Price ?? 0,
      studentsCount: course.studentsCount ?? course.StudentsCount ?? 0,
      sessionsCount: course.sessionsCount ?? course.SessionsCount ?? 0,
      averageRating: course.averageRating ?? course.AverageRating ?? 0
    })),
    recentEnrollments: (item.recentEnrollments ?? item.RecentEnrollments ?? []).map(normalizeRecentEnrollment),
    recentPayments: (item.recentPayments ?? item.RecentPayments ?? []).map(normalizePayment)
  };
}

function normalizeCourseAdminDetails(data: any): CourseAdminDetails {
  const value = unwrapData(data);
  return {
    courseId: value.courseId ?? value.CourseId ?? "",
    name: value.name ?? value.Name ?? "",
    title: value.title ?? value.Title ?? "",
    description: value.description ?? value.Description ?? "",
    category: value.category ?? value.Category,
    organizationOwner: value.organizationOwner ?? value.OrganizationOwner ?? value.organizationName ?? value.OrganizationName ?? "EduVerseOrganization",
    organizationOwnerEmail: value.organizationOwnerEmail ?? value.OrganizationOwnerEmail,
    organizationId: value.organizationId ?? value.OrganizationId,
    organizationName: value.organizationName ?? value.OrganizationName ?? "EduVerseOrganization",
    instructorId: value.instructorId ?? value.InstructorId,
    instructorName: value.instructorName ?? value.InstructorName,
    price: value.price ?? value.Price ?? 0,
    imageUrl: normalizeImageUrl(value.imageUrl ?? value.ImageUrl),
    studentsCount: value.studentsCount ?? value.StudentsCount ?? 0,
    sessionsCount: value.sessionsCount ?? value.SessionsCount ?? 0,
    averageRating: value.averageRating ?? value.AverageRating ?? 0,
    isDeleted: value.isDeleted ?? value.IsDeleted ?? false,
    deletedAt: value.deletedAt ?? value.DeletedAt,
    deletedById: value.deletedById ?? value.DeletedById,
    deletedByName: value.deletedByName ?? value.DeletedByName,
    restoredAt: value.restoredAt ?? value.RestoredAt,
    restoredById: value.restoredById ?? value.RestoredById,
    restoredByName: value.restoredByName ?? value.RestoredByName,
    sessions: (value.sessions ?? value.Sessions ?? []).map(normalizeSession),
    students: (value.students ?? value.Students ?? []).map((student: any) => ({
      studentId: student.studentId ?? student.StudentId ?? "",
      studentName: student.studentName ?? student.StudentName ?? "",
      studentEmail: student.studentEmail ?? student.StudentEmail ?? "",
      enrollmentDate: student.enrollmentDate ?? student.EnrollmentDate ?? new Date().toISOString(),
      progression: student.progression ?? student.Progression ?? 0
    })),
    assignments: (value.assignments ?? value.Assignments ?? []).map((assignment: any) => ({
      id: assignment.id ?? assignment.Id,
      sessionId: assignment.sessionId ?? assignment.SessionId,
      subject: assignment.subject ?? assignment.Subject,
      description: assignment.description ?? assignment.Description,
      content: assignment.content ?? assignment.Content,
      dueDate: assignment.dueDate ?? assignment.DueDate
    })),
    recentPayments: (value.recentPayments ?? value.RecentPayments ?? []).map(normalizePayment)
  };
}

function normalizeRecentActivity(item: any): RecentActivity {
  return {
    type: item.type ?? item.Type ?? "",
    title: item.title ?? item.Title ?? "",
    description: item.description ?? item.Description ?? "",
    createdAt: item.createdAt ?? item.CreatedAt ?? new Date().toISOString()
  };
}

function normalizeTrendPoint(item: any): TrendPoint {
  return {
    label: item.label ?? item.Label ?? "",
    date: item.date ?? item.Date ?? new Date().toISOString(),
    value: item.value ?? item.Value ?? 0
  };
}

function normalizeRoleCount(item: any): RoleCount {
  return {
    role: item.role ?? item.Role ?? "",
    count: item.count ?? item.Count ?? 0
  };
}

function normalizeTopCourseChart(item: any): TopCourseChart {
  return {
    courseId: item.courseId ?? item.CourseId ?? "",
    courseName: item.courseName ?? item.CourseName ?? "",
    enrollments: item.enrollments ?? item.Enrollments ?? 0,
    revenue: item.revenue ?? item.Revenue ?? 0,
    averageRating: item.averageRating ?? item.AverageRating ?? 0
  };
}

function normalizeAdminUserDetails(item: any): AdminUserDetails {
  const value = unwrapData(item);
  return {
    userId: value.userId ?? value.UserId ?? value.id ?? value.Id ?? "",
    fullName: value.fullName ?? value.FullName ?? "EduVerse user",
    userName: value.userName ?? value.UserName ?? "",
    email: value.email ?? value.Email ?? "",
    role: inferRole(value.role ?? value.Role),
    phone: value.phone ?? value.Phone ?? value.phoneNumber ?? value.PhoneNumber,
    organizationId: value.organizationId ?? value.OrganizationId,
    organizationName: value.organizationName ?? value.OrganizationName ?? "EduVerseOrganization",
    coursesCount: value.coursesCount ?? value.CoursesCount ?? 0,
    sessionsCount: value.sessionsCount ?? value.SessionsCount ?? 0,
    enrollmentsCount: value.enrollmentsCount ?? value.EnrollmentsCount ?? 0,
    createdAt: value.createdAt ?? value.CreatedAt,
    lastLogin: value.lastLogin ?? value.LastLogin,
    recentActivityLogs: (value.recentActivityLogs ?? value.RecentActivityLogs ?? []).map(normalizeActivityLog)
  };
}

function normalizeGlobalSearchResult(data: any): GlobalSearchResult {
  const value = unwrapData(data) ?? {};
  return {
    users: (value.users ?? value.Users ?? []).map((user: any) => ({
      userId: user.userId ?? user.UserId ?? "",
      fullName: user.fullName ?? user.FullName ?? "EduVerse user",
      userName: user.userName ?? user.UserName ?? "",
      email: user.email ?? user.Email ?? "",
      role: inferRole(user.role ?? user.Role)
    })),
    courses: (value.courses ?? value.Courses ?? []).map((course: any) => ({
      courseId: course.courseId ?? course.CourseId ?? "",
      name: course.name ?? course.Name ?? "",
      title: course.title ?? course.Title ?? "",
      category: course.category ?? course.Category ?? "",
      isDeleted: course.isDeleted ?? course.IsDeleted ?? false
    })),
    organizations: (value.organizations ?? value.Organizations ?? []).map((organization: any) => ({
      organizationAdminId: organization.organizationAdminId ?? organization.OrganizationAdminId ?? "",
      organizationAdminName: organization.organizationAdminName ?? organization.OrganizationAdminName ?? "Organization admin",
      email: organization.email ?? organization.Email ?? ""
    }))
  };
}

function normalizeAdminSession(item: any): AdminSession {
  return {
    sessionId: item.sessionId ?? item.SessionId ?? "",
    courseId: item.courseId ?? item.CourseId ?? "",
    courseName: item.courseName ?? item.CourseName ?? "",
    title: item.title ?? item.Title ?? "",
    instructorName: item.instructorName ?? item.InstructorName ?? "",
    date: item.date ?? item.Date ?? new Date().toISOString(),
    sessionNumber: item.sessionNumber ?? item.SessionNumber ?? 0
  };
}

function normalizeAdminAssignment(item: any): AdminAssignment {
  return {
    assignmentId: item.assignmentId ?? item.AssignmentId ?? "",
    sessionId: item.sessionId ?? item.SessionId ?? "",
    courseName: item.courseName ?? item.CourseName ?? "",
    subject: item.subject ?? item.Subject ?? "",
    description: item.description ?? item.Description ?? ""
  };
}

function normalizeTopCourse(item: any): TopCourse {
  return {
    courseId: item.courseId ?? item.CourseId ?? "",
    courseName: item.courseName ?? item.CourseName ?? "",
    title: item.title ?? item.Title ?? item.courseName ?? item.CourseName ?? "",
    organizationAdminName: item.organizationAdminName ?? item.OrganizationAdminName ?? "",
    studentsCount: item.studentsCount ?? item.StudentsCount ?? 0,
    sessionsCount: item.sessionsCount ?? item.SessionsCount ?? 0,
    revenue: item.revenue ?? item.Revenue ?? 0,
    averageRating: item.averageRating ?? item.AverageRating ?? 0
  };
}

function normalizeTopOrganization(item: any): TopOrganization {
  return {
    organizationAdminId: item.organizationAdminId ?? item.OrganizationAdminId ?? "",
    organizationAdminName: item.organizationAdminName ?? item.OrganizationAdminName ?? "Organization admin",
    email: item.email ?? item.Email ?? "",
    coursesCount: item.coursesCount ?? item.CoursesCount ?? 0,
    enrollmentsCount: item.enrollmentsCount ?? item.EnrollmentsCount ?? 0,
    revenue: item.revenue ?? item.Revenue ?? 0,
    averageRating: item.averageRating ?? item.AverageRating ?? 0
  };
}

function normalizeTopInstructor(item: any): TopInstructor {
  return {
    instructorId: item.instructorId ?? item.InstructorId ?? "",
    instructorName: item.instructorName ?? item.InstructorName ?? "Instructor",
    email: item.email ?? item.Email ?? "",
    sessionsCount: item.sessionsCount ?? item.SessionsCount ?? 0,
    studentsCount: item.studentsCount ?? item.StudentsCount ?? 0,
    coursesCount: item.coursesCount ?? item.CoursesCount ?? 0
  };
}

function normalizeStudentAssignment(item: any): StudentAssignment {
  return {
    assignmentId: item.assignmentId ?? item.AssignmentId ?? "",
    title: item.title ?? item.Title ?? item.subject ?? item.Subject ?? "Assignment",
    description: item.description ?? item.Description ?? "",
    courseId: item.courseId ?? item.CourseId ?? "",
    courseName: item.courseName ?? item.CourseName ?? "Course",
    sessionId: item.sessionId ?? item.SessionId ?? "",
    sessionTitle: item.sessionTitle ?? item.SessionTitle ?? "Session",
    sessionNumber: item.sessionNumber ?? item.SessionNumber ?? 0,
    dueDate: item.dueDate ?? item.DueDate,
    submissionStatus: item.submissionStatus ?? item.SubmissionStatus ?? "Not Submitted",
    submittedAt: item.submittedAt ?? item.SubmittedAt,
    grade: item.grade ?? item.Grade,
    feedback: item.feedback ?? item.Feedback,
    assignmentFileUrl: normalizeCloudFileUrl(
      "assignments",
      item.assignmentFileUrl ?? item.AssignmentFileUrl ?? item.attachmentUrl ?? item.AttachmentUrl ?? item.content ?? item.Content
    ),
    fileUrl: normalizeCloudFileUrl("submissions", item.fileUrl ?? item.FileUrl)
  };
}

function normalizeStudentSubmission(item: any): StudentSubmission {
  return {
    studentId: item.studentId ?? item.StudentId,
    assignmentId: item.assignmentId ?? item.AssignmentId ?? "",
    textAnswer: item.textAnswer ?? item.TextAnswer,
    submittedAt: item.submittedAt ?? item.SubmittedAt,
    fileUrl: normalizeCloudFileUrl("submissions", item.fileUrl ?? item.FileUrl),
    grade: item.grade ?? item.Grade,
    feedback: item.feedback ?? item.Feedback,
    isLate: item.isLate ?? item.IsLate ?? false
  };
}

function normalizeEnrollment(item: any): Enrollment {
  const value = unwrapData(item) ?? {};

  return {
    courseId: value.courseId ?? value.CourseId ?? value.id ?? value.Id ?? "",
    courseName: value.courseName ?? value.CourseName ?? value.name ?? value.Name ?? "Course",
    instructorName: value.instructorName ?? value.InstructorName,
    enrollmentDate: value.enrollmentDate ?? value.EnrollmentDate ?? new Date().toISOString(),
    progression: value.progression ?? value.Progression ?? value.progressPercent ?? value.ProgressPercent ?? value.progressPercentage ?? value.ProgressPercentage ?? 0,
    progressPercent: value.progressPercent ?? value.ProgressPercent ?? value.progressPercentage ?? value.ProgressPercentage ?? value.progression ?? value.Progression ?? 0,
    progressPercentage: value.progressPercentage ?? value.ProgressPercentage ?? value.progressPercent ?? value.ProgressPercent ?? value.progression ?? value.Progression ?? 0,
    isCompleted: value.isCompleted ?? value.IsCompleted ?? Boolean(value.graduationDate ?? value.GraduationDate),
    completedAt: value.completedAt ?? value.CompletedAt,
    graduationDate: value.graduationDate ?? value.GraduationDate,
    fileUrl: value.fileUrl ?? value.FileUrl,
    certificateCode: value.certificateCode ?? value.CertificateCode
  };
}

function normalizeCourseProgress(data: any): CourseProgress {
  const value = unwrapData(data);
  return {
    courseId: value.courseId ?? value.CourseId ?? "",
    courseName: value.courseName ?? value.CourseName ?? "Course",
    totalSessions: value.totalSessions ?? value.TotalSessions ?? (value.sessions ?? value.Sessions ?? []).length ?? 0,
    doneSessions: value.doneSessions ?? value.DoneSessions ?? 0,
    progressPercentage: value.progressPercentage ?? value.ProgressPercentage ?? 0,
    isCompleted: value.isCompleted ?? value.IsCompleted ?? false,
    completedAt: value.completedAt ?? value.CompletedAt,
    sessions: (value.sessions ?? value.Sessions ?? []).map((session: any) => {
      const isDone = session.isDone ?? session.IsDone ?? session.isCompleted ?? session.IsCompleted ?? false;
      const doneAt = session.doneAt ?? session.DoneAt ?? session.completedAt ?? session.CompletedAt;
      return {
        ...normalizeSession(session),
        isDone,
        doneAt,
        isCompleted: isDone,
        completedAt: doneAt,
        materials: (session.materials ?? session.Materials ?? []).map((material: any) => ({
          id: material.id ?? material.Id ?? "",
          sessionId: material.sessionId ?? material.SessionId ?? "",
          title: material.title ?? material.Title ?? "Material",
          type: material.type ?? material.Type ?? "Link",
          url: normalizeExternalUrl(material.url ?? material.Url ?? material.materialUrl ?? material.MaterialUrl ?? material.link ?? material.Link),
          filePath: normalizeCloudFileUrl("sessions", material.filePath ?? material.FilePath),
          fileUrl: normalizeCloudFileUrl("sessions", material.fileUrl ?? material.FileUrl),
          materialUrl: normalizeExternalUrl(material.materialUrl ?? material.MaterialUrl),
          link: normalizeExternalUrl(material.link ?? material.Link),
          createdAt: material.createdAt ?? material.CreatedAt ?? new Date().toISOString()
        })),
        assignments: (session.assignments ?? session.Assignments ?? []).map(normalizeStudentAssignment)
      };
    })
  };
}

function normalizeToggleSessionDone(data: any): ToggleSessionDoneResult {
  const value = unwrapData(data);
  return {
    sessionId: value.sessionId ?? value.SessionId ?? "",
    courseId: value.courseId ?? value.CourseId ?? "",
    isDone: value.isDone ?? value.IsDone ?? false,
    doneAt: value.doneAt ?? value.DoneAt,
    doneSessions: value.doneSessions ?? value.DoneSessions ?? 0,
    totalSessions: value.totalSessions ?? value.TotalSessions ?? 0,
    progressPercentage: value.progressPercentage ?? value.ProgressPercentage ?? 0
  };
}

function normalizeAssignmentProgress(data: any): AssignmentProgress {
  const value = unwrapData(data);
  return {
    courseId: value.courseId ?? value.CourseId ?? "",
    totalAssignments: value.totalAssignments ?? value.TotalAssignments ?? 0,
    submittedAssignments: value.submittedAssignments ?? value.SubmittedAssignments ?? 0,
    assignmentProgressPercentage: value.assignmentProgressPercentage ?? value.AssignmentProgressPercentage ?? 0,
    requiredPercentage: value.requiredPercentage ?? value.RequiredPercentage ?? 80,
    hasRequiredAssignmentProgress: value.hasRequiredAssignmentProgress ?? value.HasRequiredAssignmentProgress ?? false
  };
}

function normalizeCertificateEligibility(data: any): CertificateEligibility {
  const value = unwrapData(data);
  return {
    courseId: value.courseId ?? value.CourseId ?? "",
    assignmentProgressPercentage: value.assignmentProgressPercentage ?? value.AssignmentProgressPercentage ?? 0,
    requiredPercentage: value.requiredPercentage ?? value.RequiredPercentage ?? 80,
    hasRequiredAssignmentProgress: value.hasRequiredAssignmentProgress ?? value.HasRequiredAssignmentProgress ?? false,
    isCourseDurationFinished: value.isCourseDurationFinished ?? value.IsCourseDurationFinished ?? false,
    canReceiveCertificate: value.canReceiveCertificate ?? value.CanReceiveCertificate ?? false,
    message: value.message ?? value.Message ?? ""
  };
}

function normalizeNotification(item: any): NotificationItem {
  return {
    id: item.id ?? item.Id ?? "",
    title: item.title ?? item.Title ?? "",
    message: item.message ?? item.Message ?? "",
    isRead: item.isRead ?? item.IsRead ?? false,
    createdAt: item.createdAt ?? item.CreatedAt ?? new Date().toISOString()
  };
}

function normalizeInstructorSession(item: any): InstructorSession {
  return {
    sessionId: item.sessionId ?? item.SessionId ?? "",
    courseId: item.courseId ?? item.CourseId ?? "",
    courseName: item.courseName ?? item.CourseName ?? "",
    title: item.title ?? item.Title ?? "Session",
    sessionNumber: item.sessionNumber ?? item.SessionNumber ?? 0,
    date: item.date ?? item.Date ?? new Date().toISOString()
  };
}

function normalizeInstructorCourse(item: any): InstructorCourse {
  return {
    courseId: item.courseId ?? item.CourseId ?? "",
    name: item.name ?? item.Name ?? "",
    title: item.title ?? item.Title ?? item.name ?? item.Name ?? "",
    organizationId: item.organizationId ?? item.OrganizationId ?? "",
    organizationName: item.organizationName ?? item.OrganizationName ?? "EduVerseOrganization",
    studentsCount: item.studentsCount ?? item.StudentsCount ?? 0,
    sessionsCount: item.sessionsCount ?? item.SessionsCount ?? 0,
    assignmentsCount: item.assignmentsCount ?? item.AssignmentsCount ?? 0
  };
}

function normalizeInstructorSubmission(item: any): InstructorSubmission {
  return {
    submissionId: item.submissionId ?? item.SubmissionId,
    studentId: item.studentId ?? item.StudentId ?? "",
    studentName: item.studentName ?? item.StudentName ?? "",
    assignmentId: item.assignmentId ?? item.AssignmentId ?? "",
    assignmentTitle: item.assignmentTitle ?? item.AssignmentTitle ?? "Assignment",
    sessionId: item.sessionId ?? item.SessionId,
    sessionTitle: item.sessionTitle ?? item.SessionTitle,
    courseId: item.courseId ?? item.CourseId ?? "",
    courseName: item.courseName ?? item.CourseName ?? "",
    textAnswer: item.textAnswer ?? item.TextAnswer,
    filePath: normalizeCloudFileUrl("submissions", item.filePath ?? item.FilePath),
    submittedAt: item.submittedAt ?? item.SubmittedAt,
    isLate: item.isLate ?? item.IsLate ?? false,
    grade: item.grade ?? item.Grade,
    feedback: item.feedback ?? item.Feedback,
    fileUrl: normalizeCloudFileUrl("submissions", item.fileUrl ?? item.FileUrl)
  };
}

function normalizeInstructorStudent(item: any): InstructorStudent {
  return {
    studentId: item.studentId ?? item.StudentId ?? "",
    studentName: item.studentName ?? item.StudentName ?? "",
    studentEmail: item.studentEmail ?? item.StudentEmail ?? "",
    courseId: item.courseId ?? item.CourseId ?? "",
    courseName: item.courseName ?? item.CourseName ?? "",
    enrollmentDate: item.enrollmentDate ?? item.EnrollmentDate ?? new Date().toISOString(),
    progressPercentage: item.progressPercentage ?? item.ProgressPercentage ?? 0,
    submissionSummary: item.submissionSummary ?? item.SubmissionSummary ?? ""
  };
}

function normalizeInstructorOverview(data: any): InstructorOverview {
  const value = unwrapData(data);
  return {
    assignedCourses: value.assignedCourses ?? value.AssignedCourses ?? 0,
    myStudents: value.myStudents ?? value.MyStudents ?? 0,
    pendingSubmissions: value.pendingSubmissions ?? value.PendingSubmissions ?? 0,
    totalAssignments: value.totalAssignments ?? value.TotalAssignments ?? 0,
    upcomingSessions: (value.upcomingSessions ?? value.UpcomingSessions ?? []).map(normalizeInstructorSession),
    recentSubmissions: (value.recentSubmissions ?? value.RecentSubmissions ?? []).map(normalizeInstructorSubmission)
  };
}

function normalizeServiceResult(data: any): ServiceResult {
  if (typeof data === "string") {
    return { success: true, message: data };
  }

  const success = readSuccessFlag(data);
  return {
    success: success ?? true,
    succeed: data?.succeed ?? data?.Succeed,
    message: readResponseMessage(data)
  };
}

function ensureSuccessfulResult(data: any, fallbackMessage: string): ServiceResult {
  const result = normalizeServiceResult(data);
  if (result.success === false || result.succeed === false) {
    const errors = data?.errors ?? data?.Errors;
    throw new Error(Array.isArray(errors) && errors.length ? errors.join("\n") : result.message ?? fallbackMessage);
  }

  return result;
}

function normalizeRatingResult(data: any): RatingResult {
  const value = unwrapData(data) ?? {};
  return {
    courseId: value.courseId ?? value.CourseId ?? "",
    averageRating: value.averageRating ?? value.AverageRating ?? value.rating ?? value.Rating ?? 0,
    ratingCount: value.ratingCount ?? value.RatingCount ?? 0,
    userRating: value.userRating ?? value.UserRating ?? value.ratingValue ?? value.RatingValue ?? 0,
    message: readResponseMessage(data)
  };
}

function normalizeAuthProfile(data: any, fallbackRole?: AuthUser["role"]): AuthUser {
  const profile = unwrapData(data) ?? {};
  return {
    id: profile.id ?? profile.Id ?? profile.userId ?? profile.UserId ?? profile.nameIdentifier ?? profile.sub,
    userId: profile.userId ?? profile.UserId,
    nameIdentifier: profile.nameIdentifier ?? profile.NameIdentifier ?? profile.nameidentifier,
    sub: profile.sub,
    fullName: profile.fullName ?? profile.FullName,
    userName: profile.userName ?? profile.UserName,
    email: profile.email ?? profile.Email ?? "",
    role: inferRole(profile.role ?? profile.Role ?? fallbackRole),
    phoneNumber: profile.phoneNumber ?? profile.PhoneNumber,
    organizationId: profile.organizationId ?? profile.OrganizationId,
    organizationName: profile.organizationName ?? profile.OrganizationName ?? "EduVerseOrganization",
    profilePicture: normalizeProfilePictureUrl(profile.profilePicture ?? profile.ProfilePicture)
  };
}

function normalizeCertificate(certificate: any, index: number): Certificate {
  if (typeof certificate === "string") {
    return {
      id: `${index}`,
      courseName: `Certificate ${index + 1}`,
      issuedAt: new Date().toISOString(),
      fileUrl: normalizeCertificateUrl(certificate) ?? ""
    };
  }

  return {
    id: certificate.id ?? certificate.Id ?? `${certificate.courseId ?? certificate.CourseId ?? index}`,
    courseId: certificate.courseId ?? certificate.CourseId,
    courseName: certificate.courseName ?? certificate.CourseName ?? certificate.name ?? certificate.Name ?? `Certificate ${index + 1}`,
    studentName: certificate.studentName ?? certificate.StudentName,
    certificateCode: certificate.certificateCode ?? certificate.CertificateCode,
    issuedAt: certificate.issuedAt ?? certificate.IssuedAt ?? certificate.graduationDate ?? certificate.GraduationDate ?? new Date().toISOString(),
    fileUrl: normalizeCertificateUrl(certificate.fileUrl ?? certificate.FileUrl ?? certificate.certificateFile ?? certificate.CertificateFile) ?? "",
    status: certificate.status ?? certificate.Status,
    verificationUrl: certificate.verificationUrl ?? certificate.VerificationUrl
  };
}

function normalizeUser(user: any): ManagedUser {
  return {
    id: user.id ?? user.Id ?? user.userId ?? user.UserId,
    fullName: user.fullName ?? user.FullName ?? user.userName ?? user.UserName ?? "EduVerse user",
    userName: user.userName ?? user.UserName ?? "",
    email: user.email ?? user.Email ?? "",
    role: inferRole(user.role ?? user.Role),
    phoneNumber: user.phoneNumber ?? user.PhoneNumber ?? user.phoneNumber,
    profilePicture: normalizeProfilePictureUrl(user.profilePicture ?? user.ProfilePicture),
    organizationId: user.organizationId ?? user.OrganizationId,
    organizationName: user.organizationName ?? user.OrganizationName ?? "EduVerseOrganization"
  };
}

export const authService = {
  async login(payload: LoginPayload) {
    clearAuth();
    const response = await api.post("/Auth/Login", payload);
    const data = unwrapData(response.data);
    const token = data.token ?? data.Token ?? data.accessToken ?? data.AccessToken;

    if (!token) {
      throw new Error(data.message ?? data.Message ?? "Login failed. Backend did not return a token.");
    }

    if (token) setToken(token);
    const tokenRole = getRoleFromToken(token);
    const tokenUserId = getUserIdFromToken(token);

    let profile: Partial<AuthUser> = {};
    try {
      const profileResponse = await api.get("/Auth/GetProfile");
      profile = normalizeAuthProfile(profileResponse.data, tokenRole);
    } catch {
      profile = {};
    }

    const user: AuthUser = {
      id: data.id ?? data.Id ?? data.userId ?? data.UserId ?? data.nameIdentifier ?? data.sub ?? profile.id ?? tokenUserId,
      userId: data.userId ?? data.UserId ?? profile.userId,
      nameIdentifier: data.nameIdentifier ?? data.NameIdentifier ?? data.nameidentifier ?? profile.nameIdentifier,
      sub: data.sub ?? profile.sub,
      fullName: data.fullName ?? data.FullName ?? data.userName ?? data.UserName ?? profile.fullName,
      userName: data.userName ?? data.UserName ?? profile.userName,
      email: data.email ?? data.Email ?? profile.email ?? payload.email,
      role: inferRole(data.role ?? data.Role ?? profile.role ?? tokenRole),
      phoneNumber: profile.phoneNumber,
      organizationId: profile.organizationId,
      organizationName: profile.organizationName,
      profilePicture: profile.profilePicture,
      token
    };

    setStoredUser(user);
    return user;
  },

  async register(payload: RegisterPayload) {
    const formData = new FormData();
    formData.append("FullName", payload.fullName.trim());
    formData.append("UserName", payload.userName.trim());
    formData.append("Email", payload.email.trim());
    formData.append("Password", payload.password);
    formData.append("confirmPassword", payload.confirmPassword);
    formData.append("phoneNumber", payload.phoneNumber.trim());
    formData.append("Birth", payload.birth);
    formData.append("role", toBackendRole(payload.role));
    formData.append("ConfirmationCode", payload.confirmationCode.trim());

    const response = await api.post("/Auth/Register", formData);
    return ensureSuccessfulResult(response.data, "Registration failed.");
  },

  async sendConfirmationEmail(email: string): Promise<ServiceResult> {
    const normalizedEmail = email.trim();
    if (!normalizedEmail) throw new Error("Enter your email address first.");
    const response = await api.post(`/Auth/SendConfirmationEmail/${encodeURIComponent(normalizedEmail)}`);
    return ensureSuccessfulResult(response.data, "Could not send the confirmation code.");
  },

  async getProfile() {
    const response = await api.get("/Auth/GetProfile");
    return normalizeAuthProfile(response.data);
  },

  async updateProfile(payload: UpdateProfilePayload) {
    const formData = new FormData();
    if (payload.fullName) formData.append("FullName", payload.fullName);
    if (payload.phoneNumber) formData.append("PhoneNumber", payload.phoneNumber);
    if (payload.profilePicture) formData.append("ProfilePicture", payload.profilePicture);

    const response = await api.put("/Auth/UpdateProfile", formData);
    ensureSuccessfulResult(response.data, "Profile update failed");
    const updatedUser = normalizeAuthProfile(response.data);
    const currentToken = getToken();
    setStoredUser({ ...updatedUser, token: currentToken ?? undefined });
    return updatedUser;
  },

  async changePassword(payload: ChangePasswordPayload) {
    const response = await api.post("/Auth/ChangePassword", payload);
    return ensureSuccessfulResult(response.data, "Password change failed");
  }
};

export const courseService = {
  async getAll() {
    const response = await api.get("/Course/GetAll");
    return normalizeCourseList(response.data);
  },

  async getOwnedByCurrentUser() {
    const currentUserId = getCurrentUserId();
    if (!currentUserId) return [];

    const courses = await this.getAll();
    const ownedCourses = courses.filter((course) => course.orgId?.trim().toLowerCase() === currentUserId.trim().toLowerCase());

    if (process.env.NODE_ENV === "development") {
      console.log("[EduVerse] currentUserId", currentUserId);
      console.log("[EduVerse] course orgIds", courses.map((course) => course.orgId));
      console.log("[EduVerse] ownedCourses count", ownedCourses.length);
    }

    return ownedCourses;
  },

  async getDeleted() {
    const response = await api.get("/Course/GetDeletedCourses");
    return unwrapArray(response.data).map(normalizeCourse);
  },

  async restore(id: string): Promise<ServiceResult> {
    const response = await api.post(`/Course/Restore/${id}`);
    return ensureSuccessfulResult(response.data, "Course restore failed.");
  },

  async getById(id: string) {
    const courseId = id?.trim();
    if (!courseId) {
      throw new Error("Course id is missing.");
    }

    if (process.env.NODE_ENV === "development") {
      console.log("[EduVerse] course details route id", courseId);
    }

    const response = await api.get(`/Course/GetById/${encodeURIComponent(courseId)}`);
    if (process.env.NODE_ENV === "development") {
      console.log("[EduVerse] course details response", response.data);
    }

    const course = normalizeCourse(response.data);
    if (!course.id) {
      throw new Error("Course details response did not include a course id.");
    }

    return course;
  },

  async getAdminDetails(id: string) {
    const response = await api.get(`/Course/AdminDetails/${id}`);
    return normalizeCourseAdminDetails(response.data);
  },

  async getSessions(courseId: string) {
    const response = await api.get(`/Course/GetAllSessions/${courseId}`);
    return unwrapArray(response.data).map(normalizeSession);
  },

  async getAssignmentsCount(courseId: string) {
    const response = await api.get(`/Course/GetAllAssignments/${courseId}`);
    return unwrapArray(response.data).length;
  },

  async search(query: string) {
    const response = await api.get(`/Course/search/${encodeURIComponent(query)}`);
    return normalizeCourseList(response.data);
  },

  async create(formData: FormData): Promise<ServiceResult> {
    const response = await api.post("/Course/Create", formData);
    return ensureSuccessfulResult(response.data, "Course creation failed.");
  },

  async update(formData: FormData): Promise<ServiceResult> {
    const response = await api.put("/Course/Update", formData);
    return ensureSuccessfulResult(response.data, "Course update failed.");
  },

  async delete(id: string): Promise<ServiceResult> {
    const password = window.prompt("Enter your password to delete this course");
    if (!password) throw new Error("Password is required.");
    const response = await api.post(`/Course/DeleteWithPassword/${id}`, { password });
    return ensureSuccessfulResult(response.data, "Course delete failed.");
  },

  async addSession(formData: FormData): Promise<ServiceResult> {
    const file = formData.get("File");
    if (typeof File !== "undefined" && file instanceof File && file.size === 0) {
      formData.delete("File");
    }

    try {
      const response = await api.post("/Course/AddSession", formData, { timeout: 180_000 });
      return ensureSuccessfulResult(response.data, "Session creation failed.");
    } catch (error) {
      if (axios.isAxiosError(error)) {
        if (error.code === "ECONNABORTED") {
          throw new Error("The session upload timed out before the API responded. Check your connection and try again.");
        }
      }

      throw new Error(getApiErrorMessage(error, "Session creation failed."));
    }
  },

  async addAssignment(formData: FormData): Promise<ServiceResult> {
    const response = await api.post("/Course/AddAssignment", formData);
    return ensureSuccessfulResult(response.data, "Assignment creation failed.");
  },

  async addRating(courseId: string, ratingValue: number): Promise<RatingResult> {
    if (!Number.isFinite(ratingValue) || ratingValue < 1 || ratingValue > 5) {
      throw new Error("Rating must be between 1 and 5.");
    }

    const response = await api.post("/Course/AddRating", { courseId, ratingValue });
    ensureSuccessfulResult(response.data, "Rating failed.");
    return normalizeRatingResult(response.data);
  },

  async assignInstructor(courseId: string, instructorId: string): Promise<ServiceResult> {
    const response = await api.post("/Course/AssignInstructor", { courseId, instructorId });
    return ensureSuccessfulResult(response.data, "Instructor assignment failed.");
  }
};

export const studentService = {
  async getEnrollments() {
    const response = await api.get("/User/my-enrolled-courses");
    return unwrapArray(response.data).map(normalizeEnrollment);
  },

  async getEnrollment(courseId: string): Promise<Enrollment> {
    const response = await api.get(`/User/my-enrollment/${courseId}`);
    return normalizeEnrollment(response.data);
  },

  async getCertificates() {
    const response = await api.get("/User/my-certificates");
    return unwrapArray(response.data).map(normalizeCertificate);
  },

  async getSubmissions() {
    const response = await api.get("/User/my-submissions");
    return unwrapArray(response.data).map(normalizeStudentSubmission);
  },

  async getAssignments(): Promise<StudentAssignment[]> {
    const response = await api.get("/User/my-assignments");
    return unwrapArray(response.data).map(normalizeStudentAssignment);
  },

  async getSubmission(assignmentId: string): Promise<StudentSubmission> {
    const response = await api.get(`/User/my-submission/${assignmentId}`);
    return normalizeStudentSubmission(response.data);
  },

  async submitAssignment(assignmentId: string, payload: { textAnswer?: string; file?: File | null }): Promise<ServiceResult> {
    const textAnswer = payload.textAnswer?.trim();
    const file = payload.file && payload.file.size > 0 ? payload.file : null;
    if (!textAnswer && !file) {
      throw new Error("Add a text answer or upload a file before submitting.");
    }

    const formData = new FormData();
    if (textAnswer) formData.append("TextAnswer", textAnswer);
    if (file) formData.append("File", file);
    const response = await api.post(`/Assignment/Submit/${assignmentId}`, formData);
    return ensureSuccessfulResult(response.data, "Assignment submission failed.");
  },

  async getCourseProgress(courseId: string): Promise<CourseProgress> {
    const response = await api.get(`/Progress/Course/${courseId}`);
    return normalizeCourseProgress(response.data);
  },

  async toggleSessionDone(sessionId: string): Promise<ToggleSessionDoneResult> {
    const response = await api.post(`/Progress/ToggleSessionDone/${sessionId}`);
    ensureSuccessfulResult(response.data, "Session progress update failed.");
    return normalizeToggleSessionDone(response.data);
  },

  async markSessionCompleted(sessionId: string): Promise<CourseProgress> {
    const response = await api.post(`/User/mark-session-completed/${sessionId}`);
    ensureSuccessfulResult(response.data, "Session completion failed.");
    return normalizeCourseProgress(response.data);
  },

  async getAssignmentProgress(courseId: string): Promise<AssignmentProgress> {
    const response = await api.get(`/AssignmentProgress/Course/${courseId}`);
    return normalizeAssignmentProgress(response.data);
  },

  async getCertificateEligibility(courseId: string): Promise<CertificateEligibility> {
    const response = await api.get(`/Certificate/Eligibility/${courseId}`);
    return normalizeCertificateEligibility(response.data);
  },

  async enrollFree(courseId: string): Promise<ServiceResult> {
    const response = await api.post(`/User/enroll/${courseId}`);
    return ensureSuccessfulResult(response.data, "Free enrollment failed.");
  },

  async generateCertificate(courseId: string): Promise<Certificate> {
    const response = await api.post(`/Certificate/Generate/${courseId}`);
    ensureSuccessfulResult(response.data, "Certificate generation failed.");
    return normalizeCertificate(unwrapData(response.data), 0);
  },

  async verifyCertificate(code: string) {
    const response = await api.get(`/Certificate/Verify/${encodeURIComponent(code)}`);
    return unwrapData(response.data);
  },

  async getNotifications(): Promise<NotificationItem[]> {
    const response = await api.get("/Notification/MyNotifications");
    return (unwrapData(response.data) ?? []).map(normalizeNotification);
  },

  async markNotificationAsRead(id: string): Promise<ServiceResult> {
    const response = await api.post(`/Notification/MarkAsRead/${id}`);
    return ensureSuccessfulResult(response.data, "Notification update failed.");
  },

  async markAttendance(sessionId: string, attendanceCode: string): Promise<ServiceResult> {
    const response = await api.post(`/Attendance/Mark/${sessionId}`, { attendanceCode });
    return ensureSuccessfulResult(response.data, "Attendance failed.");
  },

  async getPayments(): Promise<Payment[]> {
    const response = await api.get("/User/payments");
    return unwrapArray(response.data).map(normalizePayment);
  },

  async createPayment(courseId: string, method: "card" | "wallet") {
    const response = await api.post(`/User/payment/${courseId}/${method}`);
    const value = unwrapData(response.data);
    if (typeof value === "string") return value;
    return value?.redirectUrl ?? value?.RedirectUrl ?? "";
  }
};

export const instructorService = {
  async getMyCourses(): Promise<InstructorCourse[]> {
    const response = await api.get("/Instructor/MyCourses");
    return (unwrapData(response.data) ?? []).map(normalizeInstructorCourse);
  },

  async getOverview(): Promise<InstructorOverview> {
    const response = await api.get("/Instructor/Overview");
    return normalizeInstructorOverview(response.data);
  },

  async getSessions(): Promise<InstructorSession[]> {
    const response = await api.get("/Instructor/Sessions");
    return (unwrapData(response.data) ?? []).map(normalizeInstructorSession);
  },

  async getStudents(): Promise<InstructorStudent[]> {
    const response = await api.get("/Instructor/Students");
    return (unwrapData(response.data) ?? []).map(normalizeInstructorStudent);
  },

  async getSubmissions(): Promise<InstructorSubmission[]> {
    const response = await api.get("/Instructor/Submissions");
    return (unwrapData(response.data) ?? []).map(normalizeInstructorSubmission);
  },

  async gradeSubmission(assignmentId: string, studentId: string, grade: number, feedback?: string): Promise<ServiceResult> {
    const response = await api.post(`/Instructor/GradeSubmission/${assignmentId}/${studentId}`, { grade, feedback });
    return ensureSuccessfulResult(response.data, "Submission grading failed.");
  },

  async createSessionQr(sessionId: string): Promise<{ sessionId: string; attendanceCode: string; createdAt: string }> {
    const response = await api.post(`/Attendance/CreateSessionQr/${sessionId}`);
    const value = unwrapData(response.data);
    return {
      sessionId: value.sessionId ?? value.SessionId ?? sessionId,
      attendanceCode: value.attendanceCode ?? value.AttendanceCode ?? "",
      createdAt: value.createdAt ?? value.CreatedAt ?? new Date().toISOString()
    };
  },

  async getSessionAttendance(sessionId: string) {
    const response = await api.get(`/Attendance/Session/${sessionId}`);
    return unwrapData(response.data) ?? [];
  },

  async getCoursePayments(courseId: string) {
    const response = await api.get(`/User/payments/course/${courseId}`);
    return unwrapArray(response.data).map(normalizePayment);
  },

  async getEnrolledUsers(courseId: string) {
    const response = await api.get(`/User/enrolledusers/${courseId}`);
    return unwrapArray(response.data);
  }
};

export const dashboardService = {
  async getOrganizationOverview() {
    const response = await api.get("/Dashboard/OrganizationOverview");
    return normalizeStats(response.data);
  },

  async getOrganizationsOverview() {
    const response = await api.get("/Dashboard/OrganizationsOverview");
    const data = unwrapData(response.data);
    return (data ?? []).map(normalizeOrganizationOverview);
  },

  async getOrganizationDetails(id: string) {
    const response = await api.get(`/Dashboard/OrganizationDetails/${encodeURIComponent(id)}`);
    return normalizeOrganizationDetails(unwrapData(response.data));
  },

  async getRecentEnrollments() {
    const response = await api.get("/Dashboard/RecentEnrollments");
    const data = unwrapData(response.data);
    return (data ?? []).map(normalizeRecentEnrollment);
  },

  async getRecentPayments() {
    const response = await api.get("/Dashboard/RecentPayments");
    const data = unwrapData(response.data);
    return (data ?? []).map(normalizePayment);
  },

  async getRecentCourses() {
    const response = await api.get("/Dashboard/RecentCourses");
    const data = unwrapData(response.data);
    return (data ?? []).map(normalizeRecentCourse);
  },

  async getTopCourses() {
    const response = await api.get("/Dashboard/TopCourses");
    const data = unwrapData(response.data);
    return (data ?? []).map(normalizeTopCourse);
  },

  async getTopOrganizations() {
    const response = await api.get("/Dashboard/TopOrganizations");
    const data = unwrapData(response.data);
    return (data ?? []).map(normalizeTopOrganization);
  },

  async getTopInstructors() {
    const response = await api.get("/Dashboard/TopInstructors");
    const data = unwrapData(response.data);
    return (data ?? []).map(normalizeTopInstructor);
  },

  async getRecentActivities() {
    const response = await api.get("/Dashboard/RecentActivities");
    const data = unwrapData(response.data);
    return (data ?? []).map(normalizeRecentActivity);
  },

  async getAdminStudents(): Promise<AdminUserDetails[]> {
    const response = await api.get("/Dashboard/AdminStudents");
    return (unwrapData(response.data) ?? []).map(normalizeAdminUserDetails);
  },

  async getAdminInstructors(): Promise<AdminUserDetails[]> {
    const response = await api.get("/Dashboard/AdminInstructors");
    return (unwrapData(response.data) ?? []).map(normalizeAdminUserDetails);
  },

  async getRecentSessions(): Promise<AdminSession[]> {
    const response = await api.get("/Dashboard/RecentSessions");
    return (unwrapData(response.data) ?? []).map(normalizeAdminSession);
  },

  async getRecentAssignments(): Promise<AdminAssignment[]> {
    const response = await api.get("/Dashboard/RecentAssignments");
    return (unwrapData(response.data) ?? []).map(normalizeAdminAssignment);
  },

  async getTopRatedCourses(): Promise<TopCourse[]> {
    const response = await api.get("/Dashboard/TopRatedCourses");
    return (unwrapData(response.data) ?? []).map(normalizeTopCourse);
  },

  async getAdminUserDetails(userId: string): Promise<AdminUserDetails> {
    const response = await api.get(`/Dashboard/AdminUserDetails/${encodeURIComponent(userId)}`);
    return normalizeAdminUserDetails(response.data);
  },

  async getRevenueTrend(days = 30): Promise<TrendPoint[]> {
    const response = await api.get(`/Dashboard/RevenueTrend?days=${days}`);
    return (unwrapData(response.data) ?? []).map(normalizeTrendPoint);
  },

  async getEnrollmentsTrend(days = 30): Promise<TrendPoint[]> {
    const response = await api.get(`/Dashboard/EnrollmentsTrend?days=${days}`);
    return (unwrapData(response.data) ?? []).map(normalizeTrendPoint);
  },

  async getUsersByRole(): Promise<RoleCount[]> {
    const response = await api.get("/Dashboard/UsersByRole");
    return (unwrapData(response.data) ?? []).map(normalizeRoleCount);
  },

  async getTopCoursesChart(): Promise<TopCourseChart[]> {
    const response = await api.get("/Dashboard/TopCoursesChart");
    return (unwrapData(response.data) ?? []).map(normalizeTopCourseChart);
  }
};

export const organizationService = {
  async getAll(): Promise<OrganizationOverview[]> {
    const response = await api.get("/Organization/GetAll");
    const data = unwrapData(response.data);
    return (data ?? []).map(normalizeOrganizationOverview);
  },

  async getById(id: string): Promise<OrganizationDetails> {
    const response = await api.get(`/Organization/GetById/${encodeURIComponent(id)}`);
    return normalizeOrganizationDetails(unwrapData(response.data));
  },

  async create(payload: {
    name: string;
    description?: string;
    email?: string;
    phoneNumber?: string;
    websiteUrl?: string;
  }): Promise<OrganizationDetails> {
    const response = await api.post("/Organization/Create", payload);
    return normalizeOrganizationDetails(unwrapData(response.data));
  },

  async update(id: string, payload: {
    name: string;
    description?: string;
    email?: string;
    phoneNumber?: string;
    websiteUrl?: string;
  }): Promise<OrganizationDetails> {
    const response = await api.put(`/Organization/Update/${encodeURIComponent(id)}`, payload);
    return normalizeOrganizationDetails(unwrapData(response.data));
  },

  async suspend(id: string) {
    const response = await api.post(`/Organization/Suspend/${encodeURIComponent(id)}`);
    return normalizeOrganizationDetails(unwrapData(response.data));
  },

  async activate(id: string) {
    const response = await api.post(`/Organization/Activate/${encodeURIComponent(id)}`);
    return normalizeOrganizationDetails(unwrapData(response.data));
  },

  async assignAdmin(organizationId: string, userId: string) {
    const response = await api.post("/Organization/AssignAdmin", { organizationId, userId });
    return normalizeOrganizationDetails(unwrapData(response.data));
  },

  async assignInstructor(organizationId: string, userId: string) {
    const response = await api.post("/Organization/AssignInstructor", { organizationId, userId });
    return normalizeOrganizationDetails(unwrapData(response.data));
  }
};

export const paymentService = {
  async getAdminSummary() {
    const response = await api.get("/Payment/AdminSummary");
    return unwrapData(response.data);
  },

  async getAdminTransactions(pageOrFilters: number | PaymentFilters = 1, pageSize = 20) {
    const filters: PaymentFilters = typeof pageOrFilters === "number" ? { page: pageOrFilters, pageSize } : pageOrFilters;
    const params = new URLSearchParams();
    params.set("page", String(filters.page ?? 1));
    params.set("pageSize", String(filters.pageSize ?? pageSize));
    if (filters.status) params.set("status", filters.status);
    if (filters.search) params.set("search", filters.search);
    if (filters.fromDate) params.set("fromDate", filters.fromDate);
    if (filters.toDate) params.set("toDate", filters.toDate);

    const response = await api.get(`/Payment/AdminTransactions?${params.toString()}`);
    const data = unwrapData(response.data);
    return {
      ...data,
      items: (data.items ?? data.Items ?? []).map(normalizePayment)
    };
  },

  async getOrganizationSummary() {
    const response = await api.get("/Payment/OrganizationSummary");
    return unwrapData(response.data);
  },

  async getOrganizationTransactions(filters: PaymentFilters = {}) {
    const params = new URLSearchParams();
    params.set("page", String(filters.page ?? 1));
    params.set("pageSize", String(filters.pageSize ?? 10));
    if (filters.status) params.set("status", filters.status);
    if (filters.search) params.set("search", filters.search);
    if (filters.fromDate) params.set("fromDate", filters.fromDate);
    if (filters.toDate) params.set("toDate", filters.toDate);

    const response = await api.get(`/Payment/OrganizationTransactions?${params.toString()}`);
    const data = unwrapData(response.data);
    return {
      ...data,
      items: (data.items ?? data.Items ?? []).map(normalizePayment)
    };
  }
};

export const adminService = {
  async getUsers(role?: string): Promise<ManagedUser[]> {
    const url = role ? `/Auth/GetAllUsers/${toBackendRole(role)}` : "/Auth/GetAllUsers";
    const response = await api.get(url);
    return unwrapArray(response.data).map(normalizeUser);
  },

  async addRole(role: string): Promise<ServiceResult> {
    const response = await api.post(`/Auth/AddRole/${encodeURIComponent(toBackendRole(role))}`);
    return ensureSuccessfulResult(response.data, "Role creation failed.");
  },

  async removeRole(role: string): Promise<ServiceResult> {
    const response = await api.delete(`/Auth/RemoveRole/${encodeURIComponent(toBackendRole(role))}`);
    return ensureSuccessfulResult(response.data, "Role removal failed.");
  },

  async addUserToRole(userId: string, role: string): Promise<ServiceResult> {
    const response = await api.post(`/Auth/AddUserToRole/${encodeURIComponent(userId)}/${encodeURIComponent(toBackendRole(role))}`);
    return ensureSuccessfulResult(response.data, "Role assignment failed.");
  },

  async getActivityLogs(filters: { page?: number; pageSize?: number; action?: string; entityType?: string; search?: string } = {}): Promise<PaginatedResponse<ActivityLog>> {
    const params = new URLSearchParams();
    params.set("page", String(filters.page ?? 1));
    params.set("pageSize", String(filters.pageSize ?? 20));
    if (filters.action) params.set("action", filters.action);
    if (filters.entityType) params.set("entityType", filters.entityType);
    if (filters.search) params.set("search", filters.search);

    const response = await api.get(`/Admin/ActivityLogs?${params.toString()}`);
    const data = unwrapData(response.data);
    return {
      items: (data.items ?? data.Items ?? []).map(normalizeActivityLog),
      page: data.page ?? data.Page ?? 1,
      pageSize: data.pageSize ?? data.PageSize ?? 20,
      totalCount: data.totalCount ?? data.TotalCount ?? 0,
      totalPages: data.totalPages ?? data.TotalPages ?? 1
    };
  },

  async globalSearch(query: string): Promise<GlobalSearchResult> {
    const response = await api.get(`/Admin/GlobalSearch?q=${encodeURIComponent(query)}`);
    return normalizeGlobalSearchResult(response.data);
  },

  async getUserDetails(userId: string): Promise<AdminUserDetails> {
    const response = await api.get(`/Admin/UserDetails/${encodeURIComponent(userId)}`);
    return normalizeAdminUserDetails(response.data);
  }
};

export const recommendationService = {
  async getPersonalizedRecommendations() {
    if (!isAuthenticatedStudent()) throw new Error("Recommendations are available to students only.");
    const response = await api.get("/Recommendation/ForMe");
    return normalizeRecommendedCourses(response.data);
  },

  async getSimilarCourses(courseId: string) {
    if (!isAuthenticatedStudent()) throw new Error("Recommendations are available to students only.");
    const response = await api.get(`/Recommendation/Similar/${courseId}`);
    return normalizeRecommendedCourses(response.data);
  },

  async getTrendingCourses() {
    if (!isAuthenticatedStudent()) throw new Error("Recommendations are available to students only.");
    const response = await api.get("/Recommendation/Trending");
    return normalizeRecommendedCourses(response.data);
  }
};
