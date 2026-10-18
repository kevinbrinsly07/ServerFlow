const API_URL =
  process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080";

export type AuthUser = {
  id: string;
  fullName: string;
  email: string;
  roles: string[];
};

export type LoginResponse = {
  accessToken: string;
  tokenType: string;
  expiresAtUtc: string;
  user: AuthUser;
};

const TOKEN_KEY = "serveflow_access_token";
const USER_KEY = "serveflow_user";

export function saveSession(data: LoginResponse) {
  localStorage.setItem(TOKEN_KEY, data.accessToken);
  localStorage.setItem(USER_KEY, JSON.stringify(data.user));
}

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function getUser(): AuthUser | null {
  const value = localStorage.getItem(USER_KEY);

  if (!value) return null;

  try {
    return JSON.parse(value) as AuthUser;
  } catch {
    return null;
  }
}

export function logout() {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
  window.location.href = "/login";
}

export function authHeaders(): HeadersInit {
  const token = getToken();

  return token
    ? { Authorization: `Bearer ${token}` }
    : {};
}

export { API_URL };



export async function apiFetch(
  input: string | URL,
  init: RequestInit = {}
): Promise<Response> {
  const token = getToken();

  if (!token) {
    logout();
    throw new Error("You must sign in to continue.");
  }

  const headers = new Headers(init.headers);
  headers.set("Authorization", `Bearer ${token}`);

  const response = await fetch(input, {
    ...init,
    headers,
  });

  if (response.status === 401) {
    logout();
    throw new Error("Your session has expired. Please sign in again.");
  }

  return response;
}
