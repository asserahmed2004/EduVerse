import type { Config } from "tailwindcss";

const config: Config = {
  darkMode: "class",
  content: [
    "./src/pages/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/components/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/app/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/lib/**/*.{js,ts,jsx,tsx,mdx}"
  ],
  theme: {
    extend: {
      colors: {
        ink: "#111827",
        muted: "#6B7280",
        surface: "#F5F7FB",
        card: "#FFFFFF",
        teal: {
          50: "#EEF1FE",
          100: "#E0E7FF",
          500: "#4A6CF7",
          600: "#3451D1"
        },
        coral: {
          100: "#FEE2E2",
          500: "#EF4444"
        },
        amber: {
          100: "#FEF3C7",
          500: "#F59E0B"
        }
      },
      boxShadow: {
        soft: "0 4px 16px rgba(74,108,247,0.10), 0 2px 8px rgba(0,0,0,0.06)",
        button: "0 12px 26px rgba(74,108,247,0.28)"
      },
      borderRadius: {
        xl2: "1rem"
      }
    }
  },
  plugins: []
};

export default config;
