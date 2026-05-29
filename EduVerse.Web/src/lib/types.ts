export type UserRole = "Student" | "Instructor" | "OrganizationAdmin" | "Admin";

export type CourseCategory = {
  id?: string;
  name: string;
  description?: string;
};

export type Course = {
  id: string;
  name: string;
  title: string;
  description: string;
  price: number;
  duration: number;
  rating: number;
  userRating?: number;
  orgId?: string;
  imageUrl?: string;
  categories?: CourseCategory[];
  category?: string;
  instructorName?: string;
  studentsCount?: number;
  sessionsCount?: number;
  isDeleted?: boolean;
  level?: string;
  students?: number;
};

export type CourseSession = {
  id: string;
  courseId: string;
  title: string;
  fileUrl?: string;
  trainerId?: string;
  date?: string;
  duration?: number;
  sessionNumber: number;
};

export type AuthUser = {
  id?: string;
  userId?: string;
  nameIdentifier?: string;
  sub?: string;
  fullName?: string;
  userName?: string;
  email: string;
  role: UserRole;
  profilePicture?: string;
  token?: string;
};

export type ManagedUser = {
  id?: string;
  fullName: string;
  userName: string;
  email: string;
  role: UserRole;
  phoneNumber?: string;
  profilePicture?: string;
};

export type LoginPayload = {
  email: string;
  password: string;
};

export type RegisterPayload = {
  fullName: string;
  userName: string;
  email: string;
  password: string;
  confirmPassword: string;
  phoneNumber: string;
  birth: string;
  role: UserRole;
  confirmationCode?: string;
};

export type Payment = {
  courseId: string;
  studentId: string;
  submittingDate: string;
  totalPrice: number;
  paymentMethod: string;
  paymentStatus: "Paid" | "Pending" | "Failed";
  paymentProvider: string;
  specialReference?: string;
  merchantOrderId?: string;
  providerIntentionId?: string;
  redirectUrl?: string;
  providerStatusCode?: number;
  courseName?: string;
  studentName?: string;
  studentEmail?: string;
};

export type DashboardStats = {
  totalUsers?: number;
  totalOrganizations?: number;
  totalCourses: number;
  deletedCourses: number;
  totalRevenue: number;
  totalPayments: number;
  totalStudents: number;
  totalInstructors: number;
  totalEnrollments?: number;
  totalSessions?: number;
  totalAssignments?: number;
  pendingPayments?: number;
  averageRating?: number;
};

export type Enrollment = {
  courseId: string;
  courseName: string;
  enrollmentDate: string;
  progression: number;
  graduationDate?: string;
  fileUrl?: string;
};

export type Certificate = {
  id: string;
  courseName: string;
  issuedAt: string;
  fileUrl: string;
};

export type Submission = {
  assignmentId: string;
  courseName: string;
  subject: string;
  submittedAt?: string;
  fileUrl: string;
};

export type ServiceResult = {
  success?: boolean;
  succeed?: boolean;
  message?: string;
};
