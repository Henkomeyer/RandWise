import { ReactNode, useEffect, useMemo, useState } from "react";
import { ThemeModeContext, type ThemeMode } from "./themeState";

const storageKey = "randwise-theme-mode";

function getInitialThemeMode(): ThemeMode {
  if (typeof window === "undefined") {
    return "light";
  }

  const stored = window.localStorage.getItem(storageKey);
  if (stored === "light" || stored === "dark") {
    return stored;
  }

  if (typeof window.matchMedia !== "function") {
    return "light";
  }

  return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}

export function ThemeModeProvider({ children }: { children: ReactNode }) {
  const [themeMode, setThemeMode] = useState<ThemeMode>(getInitialThemeMode);

  useEffect(() => {
    document.documentElement.classList.toggle("dark", themeMode === "dark");
    document.documentElement.dataset.theme = themeMode;
    window.localStorage.setItem(storageKey, themeMode);
  }, [themeMode]);

  const value = useMemo(
    () => ({
      themeMode,
      isDarkMode: themeMode === "dark",
      toggleThemeMode: () =>
        setThemeMode((current) => (current === "dark" ? "light" : "dark"))
    }),
    [themeMode]
  );

  return <ThemeModeContext.Provider value={value}>{children}</ThemeModeContext.Provider>;
}
