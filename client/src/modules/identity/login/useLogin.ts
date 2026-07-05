import { useMutation } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { message } from 'antd';
import { jwtDecode } from 'jwt-decode';
import type { AxiosError } from 'axios';

import { useAuth, getDefaultRouteForRole, type Role } from '@/shared/lib/auth-context';
import { loginApi } from './login.api';

export function useLogin() {
  const { login } = useAuth();
  const navigate = useNavigate();

  return useMutation({
    mutationFn: loginApi,
    onSuccess: (response) => {
      const { accessToken } = response.data;
      login(accessToken);

      // Decode role từ JWT để redirect đúng trang chính theo role
      const payload = jwtDecode<{ role?: Role;
        'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: Role;
      }>(accessToken);
      const role = payload.role
        ?? payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
        ?? 'Student';

      void message.success('Đăng nhập thành công!');
      navigate(getDefaultRouteForRole(role), { replace: true });
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      if (error.response?.status === 401) {
        void message.error('Email hoặc mật khẩu không đúng.');
        return;
      }
      const msg = error.response?.data?.message ?? 'Đã có lỗi xảy ra, vui lòng thử lại.';
      void message.error(msg);
    },
  });
}
