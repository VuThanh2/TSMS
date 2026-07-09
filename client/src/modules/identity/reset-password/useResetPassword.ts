import { useMutation } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { App } from 'antd';
import type { AxiosError } from 'axios';

import { resetPasswordApi } from './reset-password.api';

// Error code → thông báo thân thiện cho người dùng
const ERROR_MESSAGES: Record<string, string> = {
  'User.NotFound': 'No account was found with this email.',
  'User.AccountIsInactive': 'This account has been deactivated, please contact an Admin.',
  'User.PasswordPolicyViolation': 'Password is not strong enough. It needs at least 6 characters, including uppercase, lowercase and a number.',
};

export function useResetPassword() {
  const { message } = App.useApp();
  const navigate = useNavigate();

  return useMutation({
    mutationFn: resetPasswordApi,
    onSuccess: () => {
      void message.success('Password reset successfully! Please sign in again.');
      navigate('/login', { replace: true });
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const code = error.response?.data?.code ?? '';
      const msg = ERROR_MESSAGES[code]
        ?? error.response?.data?.message
        ?? 'Something went wrong, please try again.';
      void message.error(msg);
    },
  });
}
