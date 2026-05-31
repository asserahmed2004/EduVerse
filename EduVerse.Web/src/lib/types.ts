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
  organizationOwnerName?: string;
  organizationOwnerEmail?: string;
  studentsCount?: number;
  sessionsCount?: number;
  isDeleted?: boolean;
  deletedAt?: string;
  deletedById?: string;
  deletedByName?: string;
  restoredAt?: string;
  restoredById?: string;
  restoredByName?: string;
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
  phoneNumber?: string;
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

export type UpdateProfilePayload = {
  fullName?: string;
  phoneNumber?: string;
  profilePicture?: File | null;
};

export type ChangePasswordPayload = {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
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

export type OrganizationOverview = {
  organizationAdminId: string;
  organizationAdminName: string;
  email: string;
  coursesCount: number;
  studentsCount: number;
  enrollmentsCount: number;
  revenue: number;
  averageRating: number;
};

export type OrganizationDetails = OrganizationOverview & {
  courses: {
    courseId: string;
    name: string;
    title: string;
    price: number;
    studentsCount: number;
    sessionsCount: number;
    averageRating: number;
  }[];
};

export type CourseAdminDetails = {
  courseId: string;
  name: string;
  title: string;
  description: string;
  category?: string;
  organizationOwner?: string;
  organizationOwnerEmail?: string;
  instructorName?: string;
  price: number;
  imageUrl?: string;
  studentsCount: number;
  sessionsCount: number;
  averageRating: number;
  isDeleted: boolean;
  deletedAt?: string;
  deletedById?: string;
  deletedByName?: string;
  restoredAt?: string;
  restoredById?: string;
  restoredByName?: string;
  sessions: CourseSession[];
  students: {
    studentId: string;
    studentName: string;
    studentEmail: string;
    enrollmentDate: string;
    progression: number;
  }[];
  assignments: {
    id?: string;
    sessionId?: string;
    subject?: string;
    description?: string;
    content?: string;
  }[];
  recentPayments: Payment[];
};

export type RecentEnrollment = {
  courseId: string;
  courseName: string;
  studentId: string;
  studentName: string;
  studentEmail: string;
  enrollmentDate: string;
  progression: number;
};

export type RecentPayment = Payment;

export type RecentCourse = {
  courseId: string;
  courseName: string;
  title: string;
  organizationAdminId: string;
  organizationAdminName: string;
  price: number;
  isDeleted: boolean;
};

export type TopCourse = {
  courseId: string;
  courseName: string;
  title: string;
  organizationAdminName: string;
  studentsCount: number;
  sessionsCount: number;
  revenue: number;
  averageRating: number;
};

export type TopOrganization = {
  organizationAdminId: string;
  organizationAdminName: string;
  email: string;
  coursesCount: number;
  enrollmentsCount: number;
  revenue: number;
  averageRating: number;
};

export type TopInstructor = {
  instructorId: string;
  instructorName: string;
  email: string;
  sessionsCount: number;
  studentsCount: number;
  coursesCount: number;
};

export type RecentActivity = {
  type: string;
  title: string;
  description: string;
  createdAt: string;
};

export type AdminUserDetails = {
  userId: string;
  fullName: string;
  userName: string;
  email: string;
  role: UserRole;
  phone?: string;
  coursesCount: number;
  sessionsCount: number;
  enrollmentsCount: number;
};

export type AdminSession = {
  sessionId: string;
  courseId: string;
  courseName: string;
  title: string;
  instructorName: string;
  date: string;
  sessionNumber: number;
};

export type AdminAssignment = {
  assignmentId: string;
  sessionId: string;
  courseName: string;
  subject: string;
  description: string;
};

export type PaymentFilters = {
  page?: number;
  pageSize?: number;
  status?: string;
  search?: string;
  fromDate?: string;
  toDate?: string;
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
