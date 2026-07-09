import { createContext, useContext, useEffect, useReducer, type ReactNode } from 'react';
import { jwtDecode } from 'jwt-decode';

export type Role = 'Admin' | 'Lecturer' | 'Student';

export interface AuthUser {
  userId: string;
  fullName: string;
  role: Role;
  isActive: boolean;
}

interface RawJwtPayload {
  sub?: string;
  nameid?: string;
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/nameidentifier'?: string;
  role?: Role;
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: Role;
  fullName?: string;
  isActive?: boolean;
  exp: number;
}

function decodeToken(token: string): AuthUser {
  const payload = jwtDecode<RawJwtPayload>(token);

  const userId =
    payload.sub ??
    payload.nameid ??
    payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/nameidentifier'];
  const role =
    payload.role ?? payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

  if (!userId || !role) {
    throw new Error('JWT thiếu claim userId hoặc role — kiểm tra lại cấu trúc token với Backend.');
  }

  return {
    userId,
    role,
    fullName: payload.fullName ?? '',
    isActive: payload.isActive ?? true,
  };
}

function isTokenExpired(token: string): boolean {
  const { exp } = jwtDecode<{ exp: number }>(token);
  return Date.now() >= exp * 1000;
}

// 3 trạng thái thay vì 2 — 'loading' là trạng thái BẮT BUỘC phải có, không phải tuỳ chọn:
// nó đại diện cho khoảng thời gian ngắn giữa lúc app mount và lúc đọc xong localStorage.
// Thiếu trạng thái này, mọi nơi đọc state (ProtectedRoute, RootRedirect) sẽ thấy
// "unauthenticated" ở lần render đầu tiên dù token vẫn còn hạn — gây redirect nhầm về
// /login mỗi lần F5 trang. Đây là lỗi race condition kinh điển khi trộn useEffect
// (bất đồng bộ, chạy sau render) với quyết định điều hướng (cần chạy đồng bộ theo state).
type AuthState =
  | { status: 'loading' }
  | { status: 'authenticated'; user: AuthUser }
  | { status: 'unauthenticated' };

type AuthAction = { type: 'LOGIN'; user: AuthUser } | { type: 'LOGOUT' };

function authReducer(_state: AuthState, action: AuthAction): AuthState {
  switch (action.type) {
    case 'LOGIN':
      return { status: 'authenticated', user: action.user };
    case 'LOGOUT':
      return { status: 'unauthenticated' };
  }
}

interface AuthContextValue {
  state: AuthState;
  login: (accessToken: string) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

const TOKEN_STORAGE_KEY = 'accessToken';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, dispatch] = useReducer(authReducer, { status: 'loading' });

  useEffect(() => {
    const token = localStorage.getItem(TOKEN_STORAGE_KEY);
    if (!token || isTokenExpired(token)) {
      localStorage.removeItem(TOKEN_STORAGE_KEY);
      dispatch({ type: 'LOGOUT' }); // chuyển từ 'loading' -> 'unauthenticated' tường minh
      return;
    }
    try {
      dispatch({ type: 'LOGIN', user: decodeToken(token) });
    } catch {
      localStorage.removeItem(TOKEN_STORAGE_KEY);
      dispatch({ type: 'LOGOUT' });
    }
  }, []);

  function login(accessToken: string) {
    localStorage.setItem(TOKEN_STORAGE_KEY, accessToken);
    dispatch({ type: 'LOGIN', user: decodeToken(accessToken) });
  }

  function logout() {
    localStorage.removeItem(TOKEN_STORAGE_KEY);
    dispatch({ type: 'LOGOUT' });
  }

  return <AuthContext.Provider value={{ state, login, logout }}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error('useAuth phải được gọi bên trong <AuthProvider>.');
  }
  return ctx;
}

export function getDefaultRouteForRole(role: Role): string {
  switch (role) {
    case 'Admin':
      return '/admin/dashboard';
    case 'Lecturer':
      return '/lecturer/dashboard';
    case 'Student':
      return '/student/courses';
  }
}