import type { CurrentUserDto } from '../api/auth';

// ── Storage keys ────────────────────────────────────────────────────────────

const TOKEN_KEY = 'nw_token';
const USER_KEY = 'nw_user';

// ── Token store ──────────────────────────────────────────────────────────────

/**
 * Thin wrapper over sessionStorage so auth data never outlives the browser tab
 * (AU-10) and is never exposed as a cookie that third-party scripts could read.
 */
export const authStore = {
  /** Returns the raw JWT string, or null when not authenticated. */
  getToken(): string | null {
    return sessionStorage.getItem(TOKEN_KEY);
  },

  /** Persists the token AND the user profile. */
  set(token: string, user: CurrentUserDto): void {
    sessionStorage.setItem(TOKEN_KEY, token);
    sessionStorage.setItem(USER_KEY, JSON.stringify(user));
  },

  /** Removes all auth data from sessionStorage. */
  clear(): void {
    sessionStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(USER_KEY);
  },

  /** Returns the cached user profile, or null when not authenticated. */
  getUser(): CurrentUserDto | null {
    const raw = sessionStorage.getItem(USER_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as CurrentUserDto;
    } catch {
      return null;
    }
  },
};
