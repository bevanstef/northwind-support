import { request } from './client';

// ── DTOs ────────────────────────────────────────────────────────────────────

export interface LoginRequest {
  email: string;
  password: string;
}

export interface CurrentUserDto {
  id: number;
  fullName: string;
  email: string;
  isActive: boolean;
}

export interface LoginResponse {
  token: string;
  expiresAtUtc: string;
  user: CurrentUserDto;
}

// ── API calls ────────────────────────────────────────────────────────────────

/** POST /api/auth/login — open endpoint; must NOT attach the current token. */
export function login(body: LoginRequest): Promise<LoginResponse> {
  return request<LoginResponse>('/auth/login', {
    method: 'POST',
    body: JSON.stringify(body),
    // Pass a signal so the custom header injected by client.ts is not needed here.
  });
}

/** GET /api/auth/me — requires a valid token. */
export function me(): Promise<CurrentUserDto> {
  return request<CurrentUserDto>('/auth/me');
}
