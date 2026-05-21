import type { AuthUser, Certificate, Course, Enrollment, ManagedUser, Payment, Submission } from "./types";

export const mockUser: AuthUser = {
  id: "student-1",
  fullName: "Mohamed Magdy",
  userName: "mohamed123",
  email: "mohamed@test.com",
  role: "Student"
};

export const mockCourses: Course[] = [
  {
    id: "course-frontend",
    name: "Frontend Mastery",
    title: "Build production web apps with React and Next.js",
    description: "A practical path from UI fundamentals to polished dashboards, API integration, and deployment.",
    price: 1200,
    duration: 18,
    rating: 4.8,
    userRating: 5,
    students: 1240,
    level: "Intermediate",
    imageUrl: "https://images.unsplash.com/photo-1555066931-4365d14bab8c?auto=format&fit=crop&w=1200&q=80",
    categories: [{ name: "Web Development" }, { name: "React" }]
  },
  {
    id: "course-design",
    name: "UI Design Systems",
    title: "Design consistent product interfaces",
    description: "Learn spacing, color, component rules, and product thinking through a complete design system workflow.",
    price: 950,
    duration: 12,
    rating: 4.7,
    students: 860,
    level: "Beginner",
    imageUrl: "https://images.unsplash.com/photo-1558655146-9f40138edfeb?auto=format&fit=crop&w=1200&q=80",
    categories: [{ name: "Design" }, { name: "Product" }]
  },
  {
    id: "course-backend",
    name: "Backend APIs",
    title: "Secure APIs with .NET and JWT",
    description: "Create layered backend systems with authentication, authorization, payments, and clean architecture.",
    price: 1500,
    duration: 22,
    rating: 4.9,
    userRating: 4,
    students: 1425,
    level: "Advanced",
    imageUrl: "https://images.unsplash.com/photo-1515879218367-8466d910aaa4?auto=format&fit=crop&w=1200&q=80",
    categories: [{ name: ".NET" }, { name: "APIs" }]
  }
];

export const mockPayments: Payment[] = [
  {
    courseId: "course-frontend",
    studentId: "student-1",
    submittingDate: "2026-05-18T10:30:00",
    totalPrice: 1200,
    paymentMethod: "card",
    paymentStatus: "Paid",
    paymentProvider: "Paymob",
    merchantOrderId: "884201"
  },
  {
    courseId: "course-backend",
    studentId: "student-1",
    submittingDate: "2026-05-19T09:10:00",
    totalPrice: 1500,
    paymentMethod: "wallet",
    paymentStatus: "Pending",
    paymentProvider: "Paymob",
    merchantOrderId: "884355"
  }
];

export const mockEnrollments: Enrollment[] = [
  {
    courseId: "course-frontend",
    courseName: "Frontend Mastery",
    enrollmentDate: "2026-04-20",
    progression: 72
  },
  {
    courseId: "course-backend",
    courseName: "Backend APIs",
    enrollmentDate: "2026-05-01",
    progression: 34
  }
];

export const mockCertificates: Certificate[] = [
  {
    id: "cert-1",
    courseName: "UI Design Systems",
    issuedAt: "2026-05-12",
    fileUrl: "ui-design-certificate.pdf"
  }
];

export const mockSubmissions: Submission[] = [
  {
    assignmentId: "assignment-1",
    courseName: "Frontend Mastery",
    subject: "Responsive Course Card",
    submittedAt: "2026-05-16",
    fileUrl: "submission-1.pdf"
  },
  {
    assignmentId: "assignment-2",
    courseName: "Backend APIs",
    subject: "JWT Authorization Flow",
    submittedAt: "2026-05-18",
    fileUrl: "submission-2.pdf"
  }
];

export const mockUsers: ManagedUser[] = [
  {
    id: "student-1",
    fullName: "Mohamed Magdy",
    userName: "mohamed123",
    email: "mohamed@test.com",
    role: "Student",
    phoneNumber: "01122745308"
  },
  {
    id: "instructor-1",
    fullName: "Sara Ahmed",
    userName: "sara.instructor",
    email: "sara@eduverse.com",
    role: "Instructor",
    phoneNumber: "01000000000"
  },
  {
    id: "admin-1",
    fullName: "Admin User",
    userName: "admin",
    email: "admin@eduverse.com",
    role: "Admin"
  }
];
