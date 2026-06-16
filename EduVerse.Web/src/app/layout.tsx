import type { Metadata } from "next";
import { ThemeProvider } from "@/components/theme-provider";
import { ToastProvider } from "@/components/toast-provider";
import { EDUVERSE_LOGO_SRC } from "@/lib/brand";
import "./globals.css";

export const metadata: Metadata = {
  title: "EduVerse",
  description: "Modern LMS platform for students, instructors, and admins",
  icons: {
    icon: EDUVERSE_LOGO_SRC,
    apple: EDUVERSE_LOGO_SRC
  }
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
