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
      <p className="m-0 mb-7 text-[16px] leading-normal text-text-secondary">
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
            { required: true, message: 'Please enter your email' },
            { type: 'email', message: 'Invalid email' },
          ]}
          className="!mb-2 [&_.ant-form-item-label]:!pb-1.5"
        >
          <Input prefix={<MailOutlined />} placeholder="you@university.edu" className="h-12" />
        </Form.Item>

        <Form.Item
          label={<span className="text-[14px] font-semibold">New password</span>}
          name="newPassword"
          rules={[
            { required: true, message: 'Please enter a new password' },
            { min: 6, message: 'Password must be at least 6 characters' },
          ]}
          className="!mb-2 [&_.ant-form-item-label]:!pb-1.5"
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
            { required: true, message: 'Please confirm your password' },
            ({ getFieldValue }) => ({
              validator(_, value) {
                if (!value || getFieldValue('newPassword') === value) {
                  return Promise.resolve();
                }
                return Promise.reject(new Error('The confirmation password does not match'));
              },
            }),
          ]}
          className="!mb-6 [&_.ant-form-item-label]:!pb-1.5"
        >
          <Input.Password
            prefix={<LockOutlined />}
            placeholder="Re-enter password"
            className="h-12"
          />
        </Form.Item>

        <Form.Item className="!mb-3">
          <Button type="primary" htmlType="submit" block loading={isPending} className="h-12">
            Reset password
          </Button>
        </Form.Item>

        <div className="text-center text-[14px] text-text-secondary">
          Remembered your password?{' '}
          <button
            type="button"
            onClick={() => navigate('/login')}
            className="cursor-pointer border-none bg-transparent p-0 text-[14px] font-semibold text-primary hover:underline"
          >
            Back to sign in
          </button>
        </div>
      </Form>
    </AuthLayout>
  );
}
