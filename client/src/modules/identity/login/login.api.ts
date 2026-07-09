import api from '@/shared/lib/axios';

interface LoginRequest {
  email: string;
  password: string;
}

interface LoginResponse {
  accessToken: string;
}

export function loginApi(data: LoginRequest) {
  return api.post<LoginResponse>('/auth/login', data);
}
