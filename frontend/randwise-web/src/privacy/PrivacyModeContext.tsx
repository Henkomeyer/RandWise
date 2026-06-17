import { useCallback, useMemo, useState, type ReactNode } from "react";
import { PrivacyModeContext } from "./privacyModeState";

type PrivacyModeProviderProps = {
  children: ReactNode;
};

export function PrivacyModeProvider({ children }: PrivacyModeProviderProps) {
  const [isPrivacyMode, setIsPrivacyMode] = useState(false);

  const togglePrivacyMode = useCallback(() => {
    setIsPrivacyMode((current) => !current);
  }, []);

  const value = useMemo(
    () => ({ isPrivacyMode, togglePrivacyMode }),
    [isPrivacyMode, togglePrivacyMode]
  );

  return (
    <PrivacyModeContext.Provider value={value}>
      {children}
    </PrivacyModeContext.Provider>
  );
}
