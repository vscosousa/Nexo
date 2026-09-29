import {
  createContext,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from "react";
import { resetCsrfToken, setUnauthorizedHandler } from "../shared/http/client";
import { authService, type CurrentAccount } from "./authService";

type AuthStatus = "loading" | "signedIn" | "signedOut";

interface AuthContextValue {
  status: AuthStatus;
  /** The signed-in account from `GET /auth/me`; null while unknown or signed out. */
  account: CurrentAccount | null;
  login: () => void;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

/**
 * Tracks whether there is a session and whose it is. The session itself is an httpOnly cookie script cannot
 * read, so on mount (and after signing in) this asks the API (`GET /auth/me`), and any 401 afterwards marks the
 * user signed out.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<AuthStatus>("loading");
  const [account, setAccount] = useState<CurrentAccount | null>(null);

  const readAccount = async () => {
    try {
      return (await authService.currentAccount()) ?? null;
    } catch {
      return null;
    }
  };

  useEffect(() => {
    setUnauthorizedHandler(() => {
      setStatus("signedOut");
      setAccount(null);
    });
    const check = async () => {
      const current = await readAccount();
      setAccount(current);
      setStatus((status) =>
        status !== "loading" ? status : current ? "signedIn" : "signedOut",
      );
    };
    void check();
  }, []);

  const login = () => {
    resetCsrfToken();
    setStatus("signedIn");
    void readAccount().then(setAccount);
  };

  const logout = async () => {
    await authService.signOut();
    resetCsrfToken();
    setAccount(null);
    setStatus("signedOut");
  };

  return (
    <AuthContext.Provider value={{ status, account, login, logout }}>
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
