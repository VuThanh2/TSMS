import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { message } from 'antd';
import {
  BookOutlined,
  TeamOutlined,
  BarChartOutlined,
  FileTextOutlined,
} from '@ant-design/icons';

import { useAuth } from '@/shared/lib/auth-context';
import { logoutApi } from '@/modules/identity/shared/auth.api';

// Nav items theo role — hiện tại chỉ Admin, sau sẽ mở rộng cho Lecturer/Student
const ADMIN_NAV = [
  { to: '/admin/dashboard', label: 'Courses', icon: BookOutlined },
  { to: '/admin/users', label: 'Users', icon: TeamOutlined },
  { to: '/admin/reports/statistics', label: 'Statistics', icon: BarChartOutlined },
  { to: '/admin/reports', label: 'Reports', icon: FileTextOutlined },
];

export default function AppLayout() {
  const { state, logout } = useAuth();
  const navigate = useNavigate();

  if (state.status !== 'authenticated') return null;

  const { fullName, role } = state.user;
  const initials = fullName
    .split(' ')
    .map((w) => w[0])
    .join('')
    .slice(0, 2)
    .toUpperCase();

  async function handleLogout() {
    try {
      await logoutApi();
    } catch {
      // Server logout chỉ ghi audit log — lỗi không ảnh hưởng client logout
    }
    logout();
    void message.success('Đã đăng xuất.');
    navigate('/login', { replace: true });
  }

  const navItems = role === 'Admin' ? ADMIN_NAV : ADMIN_NAV;

  return (
    <div className="grid min-h-screen" style={{ gridTemplateColumns: '248px 1fr' }}>
      {/* Sidebar */}
      <aside className="sticky top-0 flex h-screen flex-col border-r border-border bg-white px-4 py-6">
        {/* Logo */}
        <div className="flex items-center gap-2.5 px-2 pb-6">
          <div className="flex h-[34px] w-[34px] items-center justify-center rounded-[10px] bg-primary text-[16px] font-bold text-white">
            T
          </div>
          <span className="text-[18px] font-bold tracking-tight">TSMS</span>
        </div>

        {/* Role label */}
        <div className="px-3 pb-2 text-[11px] font-semibold uppercase tracking-widest text-text-muted">
          {role}
        </div>

        {/* Navigation */}
        <nav className="flex flex-col gap-0.5">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === '/admin/reports'}
              className={({ isActive }) =>
                `flex h-10 items-center gap-3 rounded-lg px-3 text-[14px] font-semibold transition-colors ${
                  isActive
                    ? 'bg-bg-card text-text'
                    : 'text-text-muted hover:bg-bg-card hover:text-text'
                }`
              }
            >
              <item.icon style={{ fontSize: 16 }} />
              {item.label}
            </NavLink>
          ))}
        </nav>

        {/* User info + Logout */}
        <div className="mt-auto border-t border-border pt-4">
          <div className="flex items-center gap-2.5 px-2">
            <div className="flex h-9 w-9 flex-none items-center justify-center rounded-full border border-border bg-bg-card text-[14px] font-semibold text-text-secondary">
              {initials}
            </div>
            <div className="min-w-0">
              <div className="truncate text-[14px] font-semibold">{fullName}</div>
              <div className="truncate text-[12px] text-text-muted">{role}</div>
            </div>
          </div>
          <button
            onClick={handleLogout}
            className="mt-2 h-[38px] w-full cursor-pointer rounded-lg border border-border bg-transparent text-[14px] font-semibold text-text-secondary transition-colors hover:bg-bg-card hover:text-text"
          >
            Log out
          </button>
        </div>
      </aside>

      {/* Main content */}
      <main className="flex min-w-0 flex-col overflow-y-auto bg-bg" style={{ maxHeight: '100vh' }}>
        <Outlet />
      </main>
    </div>
  );
}
