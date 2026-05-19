import type { Metadata } from "next";
import { ThemeProvider } from "@/components/theme-provider";
import { ToastProvider } from "@/components/toast-provider";
import "./globals.css";

export const metadata: Metadata = {
  title: "EduVerse",
  description: "Modern LMS platform for students, instructors, and admins"
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en" suppressHydrationWarning>
      <body>
        <ThemeProvider>
          <ToastProvider>{children}</ToastProvider>
        </ThemeProvider>
      </body>
    </html>
  );
}
