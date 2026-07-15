import { useMutation } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { App } from 'antd';
import { jwtDecode } from 'jwt-decode';
import type { AxiosError } from 'axios';

import { useAuth, getDefaultRouteForRole, type Role } from '@/shared/lib/auth-context';
import { loginApi } from './login.api';

export function useLogin() {
  const { message } = App.useApp();
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

      void message.success('Signed in');
      navigate(getDefaultRouteForRole(role), { replace: true });
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      if (error.response?.status === 401) {
        void message.error('Incorrect email or password');
        return;
      }
      const msg = error.response?.data?.message ?? 'Something went wrong, please try again.';
      void message.error(msg);
    },
  });
}
