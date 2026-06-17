/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext, useMemo, useState, type ReactNode } from "react";
import { api, type ApiTokens, type MeResponse } from "../api/client";

type AuthState = {
  tokens: ApiTokens | null;
  user: MeResponse | null;
};

type AuthContextValue = AuthState & {
  isAuthenticated: boolean;
  register: (input: { email: string; password: string; displayName: string }) => Promise<void>;
  login: (input: { email: string; password: string }) => Promise<void>;
  logout: () => Promise<void>;
};

const storageKey = "randwise.auth";
const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>(() => readStoredAuth());

  const value = useMemo<AuthContextValue>(
    () => ({
      ...state,
      isAuthenticated: Boolean(state.tokens?.accessToken),
      register: async (input) => {
        const tokens = await api.register(input);
        const user = await api.me(tokens.accessToken);
        storeAuth({ tokens, user });
        setState({ tokens, user });
      },
      login: async (input) => {
        const tokens = await api.login(input);
        const user = await api.me(tokens.accessToken);
        storeAuth({ tokens, user });
        setState({ tokens, user });
      },
      logout: async () => {
        const tokens = state.tokens;
        clearStoredAuth();
        setState({ tokens: null, user: null });
        if (tokens) {
          await api.logout(tokens.refreshToken, tokens.accessToken).catch(() => undefined);
        }
      }
    }),
    [state]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within AuthProvider.");
  }

  return context;
}

function readStoredAuth(): AuthState {
  try {
    const raw = window.localStorage.getItem(storageKey);
    return raw ? (JSON.parse(raw) as AuthState) : { tokens: null, user: null };
  } catch {
    return { tokens: null, user: null };
  }
}

function storeAuth(state: AuthState) {
  window.localStorage.setItem(storageKey, JSON.stringify(state));
}

function clearStoredAuth() {
  window.localStorage.removeItem(storageKey);
}
