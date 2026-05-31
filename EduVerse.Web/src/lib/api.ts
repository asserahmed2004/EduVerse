import axios from "axios";
import { clearAuth, getCurrentUserId, getRoleFromToken, getToken, getUserIdFromToken, inferRole, setStoredUser, setToken } from "./auth";
import type {
  AuthUser,
  AdminAssignment,
  AdminSession,
  AdminUserDetails,
  Certificate,
  ChangePasswordPayload,
  Course,
  CourseAdminDetails,
  CourseSession,
  DashboardStats,
  LoginPayload,
  ManagedUser,
  OrganizationOverview,
  OrganizationDetails,
  Payment,
  PaymentFilters,
  RecentCourse,
  RecentActivity,
  RecentEnrollment,
  RegisterPayload,
  ServiceResult,
  TopCourse,
  TopInstructor,
  TopOrganization,
  UpdateProfilePayload
} from "./types";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5153";

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

function normalizeProfilePictureUrl(value?: string) {
  if (!value) return undefined;
  if (value.startsWith("http")) return value;
  if (value.startsWith("/")) return `${API_BASE_URL}${value}`;
  return `${API_BASE_URL}/Cloud/Get/ProfilePicture/${encodeURIComponent(value)}`;
}

function normalizeCourse(course: any): Course {
  return {
    id: course.id ?? course.Id,
    name: course.name ?? course.Name,
    title: course.title ?? course.Title ?? course.name ?? course.Name,
    description: course.description ?? course.Description ?? "",
    price: course.price ?? course.Price ?? 0,
    duration: course.duration ?? course.Duration ?? 0,
    rating: course.rating ?? course.Rating ?? 0,
    userRating: course.userRating ?? course.UserRating ?? 0,
    orgId: course.orgId ?? course.OrgId,
    imageUrl: normalizeImageUrl(course.imageUrl ?? course.ImageUrl),
    categories: course.categories ?? course.Categories ?? [],
    category: course.category ?? course.Category ?? course.categories?.[0]?.name ?? course.Categories?.[0]?.Name,
    instructorName: course.instructorName ?? course.InstructorName,
    organizationOwnerName: course.organizationOwnerName ?? course.OrganizationOwnerName,
    organizationOwnerEmail: course.organizationOwnerEmail ?? course.OrganizationOwnerEmail,
    studentsCount: course.studentsCount ?? course.StudentsCount ?? 0,
    sessionsCount: course.sessionsCount ?? course.SessionsCount ?? 0,
    isDeleted: course.isDeleted ?? course.IsDeleted ?? false,
    deletedAt: course.deletedAt ?? course.DeletedAt,
    deletedById: course.deletedById ?? course.DeletedById,
    deletedByName: course.deletedByName ?? course.DeletedByName,
    restoredAt: course.restoredAt ?? course.RestoredAt,
    restoredById: course.restoredById ?? course.RestoredById,
    restoredByName: course.restoredByName ?? course.RestoredByName
  };
}

function normalizeSession(session: any): CourseSession {
  return {
    id: session.id ?? session.Id,
    courseId: session.courseId ?? session.CourseId,
    title: session.title ?? session.Title ?? `Session ${session.sessionNumber ?? session.SessionNumber ?? ""}`,
    fileUrl: session.fileUrl ?? session.FileUrl,
    trainerId: session.trainerId ?? session.TrainerId,
    date: session.date ?? session.Date,
    duration: session.duration ?? session.Duration,
    sessionNumber: session.sessionNumber ?? session.SessionNumber ?? 0
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

function unwrapData(data: any) {
  return data?.data ?? data?.Data ?? data;
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
  return {
    organizationAdminId: item.organizationAdminId ?? item.OrganizationAdminId ?? "",
    organizationAdminName: item.organizationAdminName ?? item.OrganizationAdminName ?? "Organization admin",
    email: item.email ?? item.Email ?? "",
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
    courses: (item.courses ?? item.Courses ?? []).map((course: any) => ({
      courseId: course.courseId ?? course.CourseId ?? "",
      name: course.name ?? course.Name ?? "",
      title: course.title ?? course.Title ?? "",
      price: course.price ?? course.Price ?? 0,
      studentsCount: course.studentsCount ?? course.StudentsCount ?? 0,
      sessionsCount: course.sessionsCount ?? course.SessionsCount ?? 0,
      averageRating: course.averageRating ?? course.AverageRating ?? 0
    }))
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
    organizationOwner: value.organizationOwner ?? value.OrganizationOwner,
    organizationOwnerEmail: value.organizationOwnerEmail ?? value.OrganizationOwnerEmail,
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
      content: assignment.content ?? assignment.Content
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

function normalizeAdminUserDetails(item: any): AdminUserDetails {
  const value = unwrapData(item);
  return {
    userId: value.userId ?? value.UserId ?? value.id ?? value.Id ?? "",
    fullName: value.fullName ?? value.FullName ?? "EduVerse user",
    userName: value.userName ?? value.UserName ?? "",
    email: value.email ?? value.Email ?? "",
    role: inferRole(value.role ?? value.Role),
    phone: value.phone ?? value.Phone ?? value.phoneNumber ?? value.PhoneNumber,
    coursesCount: value.coursesCount ?? value.CoursesCount ?? 0,
    sessionsCount: value.sessionsCount ?? value.SessionsCount ?? 0,
    enrollmentsCount: value.enrollmentsCount ?? value.EnrollmentsCount ?? 0
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

function normalizeServiceResult(data: any): ServiceResult {
  if (typeof data === "string") {
    return { success: true, message: data };
  }

  return {
    success: data?.success ?? data?.Success ?? data?.succeed ?? data?.Succeed ?? true,
    succeed: data?.succeed ?? data?.Succeed,
    message: data?.message ?? data?.Message
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
    courseName: certificate.courseName ?? certificate.CourseName ?? certificate.name ?? certificate.Name ?? `Certificate ${index + 1}`,
    issuedAt: certificate.issuedAt ?? certificate.IssuedAt ?? certificate.graduationDate ?? certificate.GraduationDate ?? new Date().toISOString(),
    fileUrl: normalizeCertificateUrl(certificate.fileUrl ?? certificate.FileUrl ?? certificate.certificateFile ?? certificate.CertificateFile) ?? ""
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
    profilePicture: normalizeProfilePictureUrl(user.profilePicture ?? user.ProfilePicture)
  };
}

export const authService = {
  async login(payload: LoginPayload) {
    clearAuth();
    const response = await api.post("/Auth/Login", payload);
    const data = response.data;
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
      profilePicture: profile.profilePicture,
      token
    };

    setStoredUser(user);
    return user;
  },

  async register(payload: RegisterPayload) {
    const formData = new FormData();
    Object.entries(payload).forEach(([key, value]) => {
      if (value) formData.append(key, key === "role" && value === "OrganizationAdmin" ? "organizationAdmin" : String(value));
    });

    const response = await api.post("/Auth/Register", formData);
    return response.data;
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
    return (response.data as any[]).map(normalizeCourse);
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
    return (response.data as any[]).map(normalizeCourse);
  },

  async restore(id: string): Promise<ServiceResult> {
    const response = await api.post(`/Course/Restore/${id}`);
    return ensureSuccessfulResult(response.data, "Course restore failed.");
  },

  async getById(id: string) {
    const response = await api.get(`/Course/GetById/${id}`);
    return normalizeCourse(response.data);
  },

  async getAdminDetails(id: string) {
    const response = await api.get(`/Course/AdminDetails/${id}`);
    return normalizeCourseAdminDetails(response.data);
  },

  async getSessions(courseId: string) {
    const response = await api.get(`/Course/GetAllSessions/${courseId}`);
    return (response.data as any[]).map(normalizeSession);
  },

  async search(query: string) {
    const response = await api.get(`/Course/search/${encodeURIComponent(query)}`);
    return (response.data as any[]).map(normalizeCourse);
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
    const response = await api.post("/Course/AddSession", formData);
    return ensureSuccessfulResult(response.data, "Session creation failed.");
  },

  async addAssignment(formData: FormData): Promise<ServiceResult> {
    const response = await api.post("/Course/AddAssignment", formData);
    return ensureSuccessfulResult(response.data, "Assignment creation failed.");
  }
};

export const studentService = {
  async getEnrollments() {
    const response = await api.get("/User/my-enrolled-courses");
    return (response.data as any[]).map((item) => ({
      courseId: item.courseId ?? item.CourseId ?? item.id ?? item.Id,
      courseName: item.courseName ?? item.CourseName ?? item.name ?? item.Name,
      enrollmentDate: item.enrollmentDate ?? item.EnrollmentDate ?? new Date().toISOString(),
      progression: item.progression ?? item.Progression ?? 0,
      graduationDate: item.graduationDate ?? item.GraduationDate,
      fileUrl: item.fileUrl ?? item.FileUrl
    }));
  },

  async getCertificates() {
    const response = await api.get("/User/my-certificates");
    return (response.data as any[]).map(normalizeCertificate);
  },

  async getSubmissions() {
    const response = await api.get("/User/my-submissions");
    return response.data;
  },

  async getPayments(): Promise<Payment[]> {
    const response = await api.get("/User/payments");
    return (response.data as any[]).map(normalizePayment);
  },

  async createPayment(courseId: string, method: "card" | "wallet") {
    const response = await api.post(`/User/payment/${courseId}/${method}`);
    return response.data as string;
  }
};

export const instructorService = {
  async getCoursePayments(courseId: string) {
    const response = await api.get(`/User/payments/course/${courseId}`);
    return (response.data as any[]).map(normalizePayment);
  },

  async getEnrolledUsers(courseId: string) {
    const response = await api.get(`/User/enrolledusers/${courseId}`);
    return response.data;
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
    const url = role ? `/Auth/GetAllUsers/${role}` : "/Auth/GetAllUsers";
    const response = await api.get(url);
    return (response.data as any[]).map(normalizeUser);
  },

  async addRole(role: string): Promise<ServiceResult> {
    const response = await api.post(`/Auth/AddRole/${encodeURIComponent(role)}`);
    return response.data;
  },

  async removeRole(role: string): Promise<ServiceResult> {
    const response = await api.delete(`/Auth/RemoveRole/${encodeURIComponent(role)}`);
    return response.data;
  },

  async addUserToRole(userId: string, role: string): Promise<ServiceResult> {
    const response = await api.post(`/Auth/AddUserToRole/${encodeURIComponent(userId)}/${encodeURIComponent(role)}`);
    return response.data;
  }
};
