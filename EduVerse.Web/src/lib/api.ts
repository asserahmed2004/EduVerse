import axios from "axios";
import { clearAuth, getCurrentUserId, getRoleFromToken, getToken, getUserIdFromToken, inferRole, setStoredUser, setToken } from "./auth";
import type { AuthUser, Certificate, Course, CourseSession, LoginPayload, ManagedUser, Payment, RegisterPayload, ServiceResult } from "./types";

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
    categories: course.categories ?? course.Categories ?? []
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
    providerStatusCode: payment.providerStatusCode ?? payment.ProviderStatusCode
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
    throw new Error(result.message ?? fallbackMessage);
  }

  return result;
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
    profilePicture: user.profilePicture ?? user.ProfilePicture
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
      const profileData = profileResponse.data;
      profile = {
        id: profileData.id ?? profileData.Id ?? profileData.userId ?? profileData.UserId ?? profileData.nameIdentifier ?? profileData.sub,
        userId: profileData.userId ?? profileData.UserId,
        nameIdentifier: profileData.nameIdentifier ?? profileData.NameIdentifier ?? profileData.nameidentifier,
        sub: profileData.sub,
        fullName: profileData.fullName ?? profileData.FullName,
        userName: profileData.userName ?? profileData.UserName,
        email: profileData.email ?? profileData.Email,
        role: inferRole(profileData.role ?? profileData.Role),
        profilePicture: profileData.profilePicture ?? profileData.ProfilePicture
      };
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
      profilePicture: profile.profilePicture,
      token
    };

    setStoredUser(user);
    return user;
  },

  async register(payload: RegisterPayload) {
    const formData = new FormData();
    Object.entries(payload).forEach(([key, value]) => {
      if (value) formData.append(key, String(value));
    });

    const response = await api.post("/Auth/Register", formData);
    return response.data;
  },

  async getProfile() {
    const response = await api.get("/Auth/GetProfile");
    return response.data;
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

  async getById(id: string) {
    const response = await api.get(`/Course/GetById/${id}`);
    return normalizeCourse(response.data);
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
    const response = await api.delete(`/Course/Delete/${id}`);
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

  async addUserToRole(userId: string, role: string): Promise<ServiceResult> {
    const response = await api.post(`/Auth/AddUserToRole/${encodeURIComponent(userId)}/${encodeURIComponent(role)}`);
    return response.data;
  }
};
