import { useMutation } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { App } from 'antd';
import type { AxiosError } from 'axios';

import { resetPasswordApi } from './reset-password.api';

// Error code → thông báo thân thiện cho người dùng
const ERROR_MESSAGES: Record<string, string> = {
  'User.NotFound': 'No account found for this email',
  'User.AccountIsInactive': 'Account deactivated — contact an Admin',
  // KHÔNG rút gọn thành "Password is not strong enough": form chỉ validate min 6 ký tự và không
  // nói gì về hoa/thường/số, nên đây là nơi DUY NHẤT user biết luật thật. Cắt là user bị từ chối
  // mà không biết sửa gì. Chỗ đúng cho nội dung này là ngay dưới ô nhập, không phải toast.
  'User.PasswordPolicyViolation': 'Password needs 6+ characters with upper, lower and a number',
};

export function useResetPassword() {
  const { message } = App.useApp();
  const navigate = useNavigate();

  return useMutation({
    mutationFn: resetPasswordApi,
    onSuccess: () => {
      void message.success('Password reset — please sign in');
      navigate('/login', { replace: true });
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const code = error.response?.data?.code ?? '';
      const msg = ERROR_MESSAGES[code]
        ?? error.response?.data?.message
        ?? 'Something went wrong';
      void message.error(msg);
    },
  });
}
