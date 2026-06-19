# EduVerse Web

Next.js + Tailwind CSS frontend for the EduVerse LMS backend.

## Stack

```text
Next.js App Router
React
Tailwind CSS
Axios
JWT auth helpers
Role-based UI
Toast notifications
Mock fallback data
```

## Setup

```bash
cd EduVerse.Web
npm install
npm run dev
```

The app runs on:

```text
http://localhost:3000
```

## Environment

Copy `.env.example` to `.env.local`:

```text
NEXT_PUBLIC_API_BASE_URL=http://localhost:5000
```

## Notes

The frontend has mock fallback data so pages can render before the full API/database flow is ready.

Real backend endpoints already prepared:

```text
Auth/Login
Auth/Register
Auth/GetProfile
Course/GetAll
Course/GetById/{id}
User/my-enrolled-courses
User/my-certificates
User/my-submissions
User/payments
User/payment/{courseId}/{method}
User/payments/course/{courseId}
Course/Create
Course/Delete/{id}
Auth/GetAllUsers/{role?}
Auth/AddRole/{role}
Auth/AddUserToRole/{userId}/{role}
```

## Main Routes

```text
/
/login
/register
/courses
/courses/[id]
/dashboard/student
/dashboard/instructor
/instructor/courses
/admin
/profile
/payments
/certificates
/enrollments
```
