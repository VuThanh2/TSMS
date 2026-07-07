import { Link, Navigate } from 'react-router-dom';
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
      <p className="m-0 mb-8 text-[16px] leading-normal text-text-secondary">
        Enter your email and a new password to reset access.
      </p>

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
        >
          <Input.Password
            prefix={<LockOutlined />}
            placeholder="Re-enter password"
            className="h-12"
          />
        </Form.Item>

        <Form.Item>
          <Button type="primary" htmlType="submit" block loading={isPending} className="h-12">
            Reset password
          </Button>
        </Form.Item>

        <div className="text-center">
          <Link
            to="/login"
            className="flex h-11 items-center justify-center text-[15px] font-medium text-text-secondary no-underline hover:text-text"
          >
            Back to sign in
          </Link>
        </div>
      </Form>
    </AuthLayout>
  );
}
