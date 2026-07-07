import { Navigate, Link } from 'react-router-dom';
import { Form, Input, Button } from 'antd';
import { LockOutlined, MailOutlined } from '@ant-design/icons';

import { useAuth } from '@/shared/lib/auth-context';
import AuthLayout from '@/modules/identity/shared/AuthLayout';
import { useLogin } from './useLogin';

// Tài khoản demo để test nhanh (khớp với Seed Data trong Backend)
const DEMO_ACCOUNTS = {
  Admin: { email: 'admin@tsms.edu.vn', password: 'Admin@123' },
  Lecturer: { email: 'lecturer1@tsms.edu.vn', password: 'Lecturer@123' },
  Student: { email: 'student1@tsms.edu.vn', password: 'Student@123' },
} as const;

interface LoginFormValues {
  email: string;
  password: string;
}

export default function LoginPage() {
  const { state } = useAuth();
  const { mutate, isPending } = useLogin();

  if (state.status === 'authenticated') {
    return <Navigate to="/" replace />;
  }

  function handleFinish(values: LoginFormValues) {
    mutate(values);
  }

  function handleDemoLogin(role: keyof typeof DEMO_ACCOUNTS) {
    mutate(DEMO_ACCOUNTS[role]);
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
            { required: true, message: 'Vui lòng nhập email' },
            { type: 'email', message: 'Email không hợp lệ' },
          ]}
        >
          <Input prefix={<MailOutlined />} placeholder="you@university.edu" className="h-12" />
        </Form.Item>

        <Form.Item
          name="password"
          rules={[{ required: true, message: 'Vui lòng nhập mật khẩu' }]}
          label={
            <div className="flex w-full items-baseline justify-between">
              <span className="text-[14px] font-semibold">Password</span>
              <Link
                to="/reset-password"
                className="text-[13px] font-semibold text-primary no-underline hover:underline"
              >
                Forgot password?
              </Link>
            </div>
          }
        >
          <Input.Password
            prefix={<LockOutlined />}
            placeholder="Enter your password"
            className="h-12"
          />
        </Form.Item>

        <Form.Item>
          <Button type="primary" htmlType="submit" block loading={isPending} className="h-12">
            Sign in
          </Button>
        </Form.Item>
      </Form>

      {/* Demo quick-login */}
      <div className="mt-7 border-t border-border pt-[22px]">
        <p className="m-0 mb-3 text-[13px] font-medium text-text-muted">
          Demo — sign in as a role
        </p>
        <div className="flex gap-2">
          {(['Admin', 'Lecturer', 'Student'] as const).map((role, index) => (
            <Button
              key={role}
              block
              onClick={() => handleDemoLogin(role)}
              disabled={isPending}
              className={`h-10 flex-1 border-border font-semibold ${
                index === 0
                  ? 'bg-bg-card text-text'
                  : 'bg-white text-text-muted'
              }`}
            >
              {role}
            </Button>
          ))}
        </div>
      </div>
    </AuthLayout>
  );
}
