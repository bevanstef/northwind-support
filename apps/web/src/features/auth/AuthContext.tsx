import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import type { CurrentUserDto } from '../../api/auth';
import { authStore } from '../../lib/authStore';

// ── Types ────────────────────────────────────────────────────────────────────

interface AuthContextValue {
  /** The currently authenticated user, or null when logged out. */
  user: CurrentUserDto | null;
  /**
   * Persists the token + user profile and updates the context so every
   * consumer re-renders immediately without a page reload.
   */
  signIn(token: string, user: CurrentUserDto): void;
  /** Clears all auth state and kicks the user to /login. */
  signOut(): void;
}

// ── Context ──────────────────────────────────────────────────────────────────

const AuthContext = createContext<AuthContextValue | null>(null);

// ── Provider ─────────────────────────────────────────────────────────────────

/** Wrap the router with this provider so every page has access to auth state. */
export function AuthProvider({ children }: { children: ReactNode }) {
  // Hydrate from sessionStorage so a page refresh does not log the user out.
  const [user, setUser] = useState<CurrentUserDto | null>(() => authStore.getUser());

  const signIn = useCallback((token: string, incoming: CurrentUserDto) => {
    authStore.set(token, incoming);
    setUser(incoming);
  }, []);

  const signOut = useCallback(() => {
    authStore.clear();
    setUser(null);
    window.location.replace('/login');
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({ user, signIn, signOut }),
    [user, signIn, signOut],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

// ── Hook ─────────────────────────────────────────────────────────────────────

/** Returns the current auth context. Must be used inside {@link AuthProvider}. */
export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return ctx;
}
