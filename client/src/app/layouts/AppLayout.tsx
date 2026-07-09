import { Outlet, useLocation, useNavigate } from 'react-router-dom';
import { App } from 'antd';

import { useAuth } from '@/shared/lib/auth-context';
import { logoutApi } from '@/modules/identity/shared/auth.api';

// matchExact: khớp tuyệt đối, không lan sang route con (vd '/admin/reports' không
// được tự khớp '/admin/reports/statistics' dù cùng tiền tố path).
// matchPrefix: khớp cả chính nó lẫn mọi route con phía dưới (vd Course Detail
// '/admin/courses/:id' thuộc về "Courses", Report Detail thuộc về "Reports") —
// khớp với cách mock nhóm screens theo nav item (navByRole[].screens). Cần tách
// riêng 2 loại vì '/admin/reports/statistics' vô tình nằm dưới cùng tiền tố path
// với '/admin/reports' dù về mặt điều hướng là 2 mục hoàn toàn khác nhau.
const ADMIN_NAV = [
  { to: '/admin/dashboard', label: 'Courses', matchExact: ['/admin/dashboard'], matchPrefix: ['/admin/courses'] },
  { to: '/admin/users', label: 'Users', matchExact: ['/admin/users'], matchPrefix: [] },
  { to: '/admin/reports/statistics', label: 'Statistics', matchExact: ['/admin/reports/statistics'], matchPrefix: [] },
  { to: '/admin/reports', label: 'Reports', matchExact: ['/admin/reports'], matchPrefix: ['/admin/reports/courses'] },
];

const LECTURER_NAV = [
  { to: '/lecturer/dashboard', label: 'My Courses', matchExact: ['/lecturer/dashboard'], matchPrefix: ['/lecturer/courses'] },
  { to: '/lecturer/grading', label: 'Grading', matchExact: ['/lecturer/grading'], matchPrefix: [] },
  { to: '/lecturer/attendance', label: 'Attendance', matchExact: ['/lecturer/attendance'], matchPrefix: [] },
  { to: '/lecturer/schedule', label: 'Schedule', matchExact: ['/lecturer/schedule'], matchPrefix: [] },
  { to: '/lecturer/reports', label: 'Reports', matchExact: ['/lecturer/reports'], matchPrefix: ['/lecturer/reports/courses'] },
];

const STUDENT_NAV = [
  { to: '/student/available-courses', label: 'Available Courses', matchExact: ['/student/available-courses'], matchPrefix: [] },
  { to: '/student/courses', label: 'My Courses', matchExact: ['/student/courses'], matchPrefix: [] },
  { to: '/student/schedule', label: 'Schedule', matchExact: ['/student/schedule'], matchPrefix: [] },
  { to: '/student/summary', label: 'My Summary', matchExact: ['/student/summary'], matchPrefix: [] },
];

function isNavItemActive(pathname: string, item: { matchExact: string[]; matchPrefix: string[] }) {
  if (item.matchExact.includes(pathname)) return true;
  return item.matchPrefix.some((p) => pathname === p || pathname.startsWith(`${p}/`));
}

export default function AppLayout() {
  const { message } = App.useApp();
  const { state, logout } = useAuth();
  const navigate = useNavigate();
  const { pathname } = useLocation();

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
    void message.success('Signed out.');
    navigate('/login', { replace: true });
  }

  const navItems =
    role === 'Admin' ? ADMIN_NAV : role === 'Lecturer' ? LECTURER_NAV : STUDENT_NAV;

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
        <div className="px-3 pb-2 text-[11px] font-semibold uppercase tracking-[0.06em] text-text-muted">
          {role}
        </div>

        {/* Navigation */}
        <nav className="flex flex-col gap-0.5">
          {navItems.map((item) => {
            const isActive = isNavItemActive(pathname, item);
            return (
              <button
                key={item.to}
                type="button"
                onClick={() => navigate(item.to)}
                className={`flex h-[42px] w-full cursor-pointer items-center gap-2.5 rounded-[10px] border-none px-3 text-left text-[14px] font-semibold transition-colors ${
                  isActive
                    ? 'bg-primary text-white'
                    : 'bg-transparent text-text-muted hover:bg-bg-card hover:text-text'
                }`}
              >
                <span
                  className="h-2 w-2 flex-none rounded-full"
                  style={{ background: isActive ? '#fff' : '#D8C9BB' }}
                />
                {item.label}
              </button>
            );
          })}
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
