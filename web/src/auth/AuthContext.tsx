import {
  createContext,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from "react";
import { resetCsrfToken, setUnauthorizedHandler } from "../shared/http/client";
import { authService } from "./authService";

type AuthStatus = "loading" | "signedIn" | "signedOut";

interface AuthContextValue {
  status: AuthStatus;
  login: () => void;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

/**
 * Tracks whether there is a session. The session itself is an httpOnly cookie script cannot read,
 * so on mount this asks the API (`GET /auth/me`), and any 401 afterwards marks the user signed out.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<AuthStatus>("loading");

  useEffect(() => {
    setUnauthorizedHandler(() => setStatus("signedOut"));
    const check = async () => {
      let account = null;
      try {
        account = await authService.currentAccount();
      } catch {
        account = null;
      }
      setStatus((current) =>
        current !== "loading" ? current : account ? "signedIn" : "signedOut",
      );
    };
    void check();
  }, []);

  const login = () => {
    resetCsrfToken();
    setStatus("signedIn");
  };

  const logout = async () => {
    await authService.signOut();
    resetCsrfToken();
    setStatus("signedOut");
  };

  return (
    <AuthContext.Provider value={{ status, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

/**
 * Reads the current auth state.
 *
 * @throws {Error} If called outside an {@link AuthProvider}.
 */
export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
