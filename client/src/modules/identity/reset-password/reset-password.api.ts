import api from '@/shared/lib/axios';

interface ResetPasswordRequest {
  email: string;
  newPassword: string;
}

export function resetPasswordApi(data: ResetPasswordRequest) {
  return api.post('/auth/reset-password', data);
}
