"use client";

import { createContext, useContext, useEffect, useMemo, useState } from "react";

type Theme = "light" | "dark";

const THEME_KEY = "eduverse_theme";

const ThemeContext = createContext<{ theme: Theme; toggleTheme: () => void } | null>(null);

export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const [theme, setTheme] = useState<Theme>("light");

  useEffect(() => {
    const storedTheme = localStorage.getItem(THEME_KEY) as Theme | null;
    const preferredTheme = window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
    const nextTheme = storedTheme === "dark" || storedTheme === "light" ? storedTheme : preferredTheme;
    setTheme(nextTheme);
    document.documentElement.classList.toggle("dark", nextTheme === "dark");
  }, []);

  function toggleTheme() {
    setTheme((current) => {
      const nextTheme = current === "dark" ? "light" : "dark";
      localStorage.setItem(THEME_KEY, nextTheme);
      document.documentElement.classList.toggle("dark", nextTheme === "dark");
      return nextTheme;
    });
  }

  const value = useMemo(() => ({ theme, toggleTheme }), [theme]);

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export function useTheme() {
  const context = useContext(ThemeContext);
  if (!context) {
    throw new Error("useTheme must be used inside ThemeProvider");
  }
  return context;
}
