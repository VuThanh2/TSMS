import { useMutation } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { message } from 'antd';
import type { AxiosError } from 'axios';

import { resetPasswordApi } from './reset-password.api';

// Error code → thông báo tiếng Việt thân thiện
const ERROR_MESSAGES: Record<string, string> = {
  'User.NotFound': 'Không tìm thấy tài khoản với email này.',
  'User.AccountIsInactive': 'Tài khoản đã bị vô hiệu hóa, vui lòng liên hệ Admin.',
  'User.PasswordPolicyViolation': 'Mật khẩu không đủ mạnh. Cần ít nhất 6 ký tự, bao gồm chữ hoa, chữ thường và số.',
};

export function useResetPassword() {
  const navigate = useNavigate();

  return useMutation({
    mutationFn: resetPasswordApi,
    onSuccess: () => {
      void message.success('Đặt lại mật khẩu thành công! Vui lòng đăng nhập lại.');
      navigate('/login', { replace: true });
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const code = error.response?.data?.code ?? '';
      const msg = ERROR_MESSAGES[code]
        ?? error.response?.data?.message
        ?? 'Đã có lỗi xảy ra, vui lòng thử lại.';
      void message.error(msg);
    },
  });
}
