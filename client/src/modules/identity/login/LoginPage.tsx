import { Navigate, useNavigate } from 'react-router-dom';
import { Form, Input, Button } from 'antd';
import { LockOutlined, MailOutlined } from '@ant-design/icons';

import { useAuth } from '@/shared/lib/auth-context';
import AuthLayout from '@/modules/identity/shared/AuthLayout';
import { useLogin } from './useLogin';

interface LoginFormValues {
  email: string;
  password: string;
}

export default function LoginPage() {
  const { state } = useAuth();
  const { mutate, isPending } = useLogin();
  const navigate = useNavigate();

  if (state.status === 'authenticated') {
    return <Navigate to="/" replace />;
  }

  function handleFinish(values: LoginFormValues) {
    mutate(values);
  }

  return (
    <AuthLayout>
      <h1 className="m-0 mb-2 text-[34px] font-bold leading-tight tracking-tight">
        Welcome back
      </h1>
      <p className="m-0 mb-8 text-[16px] leading-normal text-text-secondary">
        Sign in to manage your courses, schedules and grades.
      </p>

      <Form<LoginFormValues>
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
        >
          <Input prefix={<MailOutlined />} placeholder="you@university.edu" className="h-12" />
        </Form.Item>

        {/* Đặt row Label/"Forgot password?" ngoài prop `label` của Form.Item —
        antd render label trong <label> width theo nội dung (không stretch full
        row), justify-between bên trong sẽ không có chỗ để đẩy 2 đầu. */}
        <div className="mb-[7px] flex w-full items-baseline justify-between">
          <span className="text-[14px] font-semibold">Password</span>
          <button
            type="button"
            onClick={() => navigate('/reset-password')}
            className="cursor-pointer border-none bg-transparent p-0 text-[13px] font-semibold text-primary hover:underline"
          >
            Forgot password?
          </button>
        </div>
        <Form.Item
          name="password"
          rules={[{ required: true, message: 'Please enter your password' }]}
        >
          <Input.Password
            prefix={<LockOutlined />}
            placeholder="Enter your password"
            className="h-12"
          />
        </Form.Item>

        <Form.Item className="!mb-0">
          <Button type="primary" htmlType="submit" block loading={isPending} className="h-12">
            Sign in
          </Button>
        </Form.Item>
      </Form>
    </AuthLayout>
  );
}
