import { createContext, useContext } from "react";

export type PrivacyModeContextValue = {
  isPrivacyMode: boolean;
  togglePrivacyMode: () => void;
};

export const PrivacyModeContext =
  createContext<PrivacyModeContextValue | null>(null);

export function usePrivacyMode() {
  const value = useContext(PrivacyModeContext);

  if (!value) {
    throw new Error("usePrivacyMode must be used within PrivacyModeProvider");
  }

  return value;
}
