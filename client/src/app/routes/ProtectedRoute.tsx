import { Navigate, Outlet } from 'react-router-dom';
import { useAuth, getDefaultRouteForRole, type Role } from '@/shared/lib/auth-context';

interface ProtectedRouteProps {
  allowedRoles: Role[];
}

function ProtectedRoute({ allowedRoles }: ProtectedRouteProps) {
  const { state } = useAuth();

  // Chưa đọc xong localStorage — KHÔNG được quyết định redirect ở bước này,
  // vì chưa biết chắc user có đăng nhập hay không. Render null (hoặc spinner)
  // thay vì đoán bừa "chưa đăng nhập" rồi đá về /login nhầm mỗi lần F5.
  if (state.status === 'loading') {
    return null; // TODO: thay bằng spinner/skeleton chung của app khi có component đó
  }

  if (state.status === 'unauthenticated') {
    return <Navigate to="/login" replace />;
  }

  if (!allowedRoles.includes(state.user.role)) {
    return <Navigate to={getDefaultRouteForRole(state.user.role)} replace />;
  }

  return <Outlet />;
}

export default ProtectedRoute;