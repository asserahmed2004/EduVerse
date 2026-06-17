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
  organizationId?: string;
  organizationName?: string;
  instructorId?: string;
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
  tags?: string;
  students?: number;
  ratingCount?: number;
  recommendationScore?: number;
  progressPercent?: number;
};

export type RatingResult = {
  courseId: string;
  averageRating: number;
  ratingCount: number;
  userRating: number;
  message?: string;
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
  description?: string;
  videoUrl?: string;
  externalLink?: string;
  isCompleted?: boolean;
  attendanceCode?: string;
  attendanceCodeCreatedAt?: string;
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
  organizationId?: string;
  organizationName?: string;
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
  organizationId?: string;
  organizationName?: string;
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
  organizationId?: string;
  organizationName?: string;
  organizationAdminId: string;
  organizationAdminName: string;
  email: string;
  phoneNumber?: string;
  description?: string;
  websiteUrl?: string;
  status?: string;
  coursesCount: number;
  studentsCount: number;
  enrollmentsCount: number;
  revenue: number;
  averageRating: number;
};

export type OrganizationDetails = OrganizationOverview & {
  id?: string;
  name?: string;
  admins?: {
    userId: string;
    fullName: string;
    userName: string;
    email: string;
    role: string;
  }[];
  instructors?: {
    userId: string;
    fullName: string;
    userName: string;
    email: string;
    role: string;
  }[];
  courses: {
    courseId: string;
    name: string;
    title: string;
    price: number;
    studentsCount: number;
    sessionsCount: number;
    averageRating: number;
  }[];
  recentEnrollments?: RecentEnrollment[];
  recentPayments?: Payment[];
};

export type CourseAdminDetails = {
  courseId: string;
  name: string;
  title: string;
  description: string;
  category?: string;
  organizationOwner?: string;
  organizationOwnerEmail?: string;
  organizationId?: string;
  organizationName?: string;
  instructorId?: string;
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
    dueDate?: string;
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

export type ActivityLog = {
  id: string;
  userId?: string;
  userName: string;
  action: string;
  entityType: string;
  entityId?: string;
  description: string;
  createdAt: string;
};

export type PaginatedResponse<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

export type TrendPoint = {
  label: string;
  date: string;
  value: number;
};

export type RoleCount = {
  role: string;
  count: number;
};

export type TopCourseChart = {
  courseId: string;
  courseName: string;
  enrollments: number;
  revenue: number;
  averageRating: number;
};

export type GlobalSearchResult = {
  users: {
    userId: string;
    fullName: string;
    userName: string;
    email: string;
    role: UserRole;
  }[];
  courses: {
    courseId: string;
    name: string;
    title: string;
    category: string;
    isDeleted: boolean;
  }[];
  organizations: {
    organizationAdminId: string;
    organizationAdminName: string;
    email: string;
  }[];
};

export type AdminUserDetails = {
  userId: string;
  fullName: string;
  userName: string;
  email: string;
  role: UserRole;
  phone?: string;
  organizationId?: string;
  organizationName?: string;
  coursesCount: number;
  sessionsCount: number;
  enrollmentsCount: number;
  createdAt?: string;
  lastLogin?: string;
  recentActivityLogs?: ActivityLog[];
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
  instructorName?: string;
  enrollmentDate: string;
  progression: number;
  progressPercent?: number;
  progressPercentage?: number;
  isCompleted?: boolean;
  completedAt?: string;
  graduationDate?: string;
  fileUrl?: string;
  certificateCode?: string;
};

export type Certificate = {
  id: string;
  courseId?: string;
  courseName: string;
  studentName?: string;
  certificateCode?: string;
  issuedAt: string;
  fileUrl: string;
  status?: string;
  verificationUrl?: string;
};

export type Submission = {
  assignmentId: string;
  courseName: string;
  subject: string;
  submittedAt?: string;
  fileUrl: string;
};

export type StudentAssignment = {
  assignmentId: string;
  title: string;
  description: string;
  courseId: string;
  courseName: string;
  sessionId: string;
  sessionTitle: string;
  sessionNumber: number;
  dueDate?: string;
  submissionStatus: "Not Submitted" | "Submitted" | "Pending" | "Late" | "Missing" | "Graded";
  submittedAt?: string;
  grade?: number;
  feedback?: string;
  assignmentFileUrl?: string;
  fileUrl?: string;
};

export type StudentSubmission = {
  studentId?: string;
  assignmentId: string;
  textAnswer?: string;
  submittedAt?: string;
  fileUrl?: string;
  grade?: number;
  feedback?: string;
  isLate?: boolean;
};

export type CourseProgress = {
  courseId: string;
  courseName: string;
  totalSessions: number;
  doneSessions: number;
  progressPercentage: number;
  isCompleted: boolean;
  completedAt?: string;
  sessions: (CourseSession & {
    isDone: boolean;
    doneAt?: string;
    isCompleted: boolean;
    completedAt?: string;
    materials?: SessionMaterial[];
    assignments?: StudentAssignment[];
  })[];
};

export type ToggleSessionDoneResult = {
  sessionId: string;
  courseId: string;
  isDone: boolean;
  doneAt?: string;
  doneSessions: number;
  totalSessions: number;
  progressPercentage: number;
};

export type AssignmentProgress = {
  courseId: string;
  totalAssignments: number;
  submittedAssignments: number;
  assignmentProgressPercentage: number;
  requiredPercentage: number;
  hasRequiredAssignmentProgress: boolean;
};

export type CertificateEligibility = {
  courseId: string;
  assignmentProgressPercentage: number;
  requiredPercentage: number;
  hasRequiredAssignmentProgress: boolean;
  isCourseDurationFinished: boolean;
  canReceiveCertificate: boolean;
  message: string;
};

export type SessionMaterial = {
  id: string;
  sessionId: string;
  title: string;
  type: string;
  url?: string;
  filePath?: string;
  fileUrl?: string;
  materialUrl?: string;
  link?: string;
  createdAt: string;
};

export type NotificationItem = {
  id: string;
  title: string;
  message: string;
  isRead: boolean;
  createdAt: string;
};

export type InstructorOverview = {
  assignedCourses: number;
  myStudents: number;
  pendingSubmissions: number;
  totalAssignments: number;
  upcomingSessions: InstructorSession[];
  recentSubmissions: InstructorSubmission[];
};

export type InstructorCourse = {
  courseId: string;
  name: string;
  title: string;
  organizationId: string;
  organizationName: string;
  studentsCount: number;
  sessionsCount: number;
  assignmentsCount: number;
};

export type InstructorSession = {
  sessionId: string;
  courseId: string;
  courseName: string;
  title: string;
  sessionNumber: number;
  date: string;
};

export type InstructorStudent = {
  studentId: string;
  studentName: string;
  studentEmail: string;
  courseId: string;
  courseName: string;
  enrollmentDate: string;
  progressPercentage: number;
  submissionSummary: string;
};

export type InstructorSubmission = {
  submissionId?: string;
  studentId: string;
  studentName: string;
  assignmentId: string;
  assignmentTitle: string;
  sessionId?: string;
  sessionTitle?: string;
  courseId: string;
  courseName: string;
  textAnswer?: string;
  filePath?: string;
  submittedAt?: string;
  isLate?: boolean;
  grade?: number;
  feedback?: string;
  fileUrl?: string;
};

export type ServiceResult = {
  success?: boolean;
  succeed?: boolean;
  message?: string;
};
