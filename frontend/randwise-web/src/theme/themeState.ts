import { createContext, useContext } from "react";

export type ThemeMode = "light" | "dark";

export type ThemeModeState = {
  themeMode: ThemeMode;
  isDarkMode: boolean;
  toggleThemeMode: () => void;
};

export const ThemeModeContext = createContext<ThemeModeState | null>(null);

export function useThemeMode() {
  const value = useContext(ThemeModeContext);

  if (!value) {
    throw new Error("useThemeMode must be used within ThemeModeProvider.");
  }

  return value;
}
