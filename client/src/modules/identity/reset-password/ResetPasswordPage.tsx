import { Navigate, useNavigate } from 'react-router-dom';
import { Form, Input, Button } from 'antd';
import { LockOutlined, MailOutlined } from '@ant-design/icons';

import { useAuth } from '@/shared/lib/auth-context';
import AuthLayout from '@/modules/identity/shared/AuthLayout';
import { useResetPassword } from './useResetPassword';

interface ResetPasswordFormValues {
  email: string;
  newPassword: string;
  confirmPassword: string;
}

export default function ResetPasswordPage() {
  const { state } = useAuth();
  const { mutate, isPending } = useResetPassword();
  const navigate = useNavigate();

  if (state.status === 'authenticated') {
    return <Navigate to="/" replace />;
  }

  function handleFinish({ email, newPassword }: ResetPasswordFormValues) {
    mutate({ email, newPassword });
  }

  return (
    <AuthLayout>
      <h1 className="m-0 mb-2 text-[34px] font-bold leading-tight tracking-tight">
        Reset password
      </h1>
      <p className="m-0 mb-6 text-[16px] leading-normal text-text-secondary">
        Enter your email and a new password to reset access.
      </p>

      {/* 3 field thay vì 2 như Login nên rút gọn margin mỗi Form.Item (24px mặc định
      của antd -> 16px) để vừa 1 màn hình, không bị dư khoảng phải cuộn nhẹ. */}
      <Form<ResetPasswordFormValues>
        layout="vertical"
        onFinish={handleFinish}
        requiredMark={false}
        size="large"
      >
        <Form.Item
          label={<span className="text-[14px] font-semibold">Email</span>}
          name="email"
          rules={[
            { required: true, message: 'Vui lòng nhập email' },
            { type: 'email', message: 'Email không hợp lệ' },
          ]}
          className="!mb-4"
        >
          <Input prefix={<MailOutlined />} placeholder="you@university.edu" className="h-12" />
        </Form.Item>

        <Form.Item
          label={<span className="text-[14px] font-semibold">New password</span>}
          name="newPassword"
          rules={[
            { required: true, message: 'Vui lòng nhập mật khẩu mới' },
            { min: 6, message: 'Mật khẩu phải có ít nhất 6 ký tự' },
          ]}
          className="!mb-4"
        >
          <Input.Password
            prefix={<LockOutlined />}
            placeholder="At least 8 characters"
            className="h-12"
          />
        </Form.Item>

        <Form.Item
          label={<span className="text-[14px] font-semibold">Confirm new password</span>}
          name="confirmPassword"
          dependencies={['newPassword']}
          rules={[
            { required: true, message: 'Vui lòng xác nhận mật khẩu' },
            ({ getFieldValue }) => ({
              validator(_, value) {
                if (!value || getFieldValue('newPassword') === value) {
                  return Promise.resolve();
                }
                return Promise.reject(new Error('Mật khẩu xác nhận không khớp'));
              },
            }),
          ]}
          className="!mb-5"
        >
          <Input.Password
            prefix={<LockOutlined />}
            placeholder="Re-enter password"
            className="h-12"
          />
        </Form.Item>

        <Form.Item className="!mb-2">
          <Button type="primary" htmlType="submit" block loading={isPending} className="h-12">
            Reset password
          </Button>
        </Form.Item>

        <div className="text-center">
          <button
            type="button"
            onClick={() => navigate('/login')}
            className="h-10 w-full cursor-pointer border-none bg-transparent text-[15px] font-semibold text-primary hover:underline"
          >
            Back to sign in
          </button>
        </div>
      </Form>
    </AuthLayout>
  );
}
