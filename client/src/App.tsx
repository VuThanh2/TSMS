import { Navigate, Route, Routes } from 'react-router-dom';

import ProtectedRoute from '@/app/routes/ProtectedRoute';
import AppLayout from '@/app/layouts/AppLayout';
import { useAuth, getDefaultRouteForRole } from '@/shared/lib/auth-context';
import LoginPage from '@/modules/identity/login/LoginPage';
import ResetPasswordPage from '@/modules/identity/reset-password/ResetPasswordPage';
import CourseGridPage from '@/modules/course-management/course-grid/CourseGridPage';
import CourseDetailPage from '@/modules/course-management/course-detail/CourseDetailPage';
import UserListPage from '@/modules/identity/user-management/UserListPage';
import CourseStatisticsPage from '@/modules/reporting/course-statistics/CourseStatisticsPage';
import CourseReportPage from '@/modules/reporting/course-report/CourseReportPage';

function PlaceholderPage({ title }: { title: string }) {
  return (
    <div style={{ padding: 24 }}>
      <h1>{title}</h1>
      <p>Chưa code — placeholder chờ page thật.</p>
    </div>
  );
}

function RootRedirect() {
  const { state } = useAuth();

  // Cùng lý do với ProtectedRoute: chưa đọc xong localStorage thì chưa được quyết định
  // điều hướng — nếu không sẽ có 1 khoảnh khắc "nháy" về /login rồi mới nhảy lại đúng
  // trang, gây giật UI khó chịu mỗi lần F5.
  if (state.status === 'loading') {
    return null;
  }

  if (state.status === 'unauthenticated') {
    return <Navigate to="/login" replace />;
  }
  return <Navigate to={getDefaultRouteForRole(state.user.role)} replace />;
}

function App() {
  return (
    <Routes>
      <Route path="/" element={<RootRedirect />} />

      <Route path="/login" element={<LoginPage />} />
      <Route path="/reset-password" element={<ResetPasswordPage />} />

      <Route element={<ProtectedRoute allowedRoles={['Admin']} />}>
        <Route element={<AppLayout />}>
          <Route path="/admin/dashboard" element={<CourseGridPage />} />
          <Route path="/admin/users" element={<UserListPage />} />
          <Route path="/admin/courses/:courseId" element={<CourseDetailPage />} />
          <Route path="/admin/reports/statistics" element={<CourseStatisticsPage />} />
          <Route path="/admin/reports/courses/:courseId" element={<CourseReportPage />} />
        </Route>
      </Route>

      <Route element={<ProtectedRoute allowedRoles={['Lecturer']} />}>
        <Route
          path="/lecturer/dashboard"
          element={<PlaceholderPage title="Course Grid (Lecturer)" />}
        />
        <Route
          path="/lecturer/courses/:courseId"
          element={<PlaceholderPage title="Course Student List (Grading)" />}
        />
        <Route path="/lecturer/schedule" element={<PlaceholderPage title="My Schedule (Lecturer)" />} />
        <Route
          path="/lecturer/sessions/:sessionId/attendance"
          element={<PlaceholderPage title="Attendance Marking Screen" />}
        />
        <Route
          path="/lecturer/reports/courses/:courseId"
          element={<PlaceholderPage title="Course Report (Tab Attendance only)" />}
        />
      </Route>

      <Route element={<ProtectedRoute allowedRoles={['Student']} />}>
        <Route path="/student/courses" element={<PlaceholderPage title="Available Courses / My Courses" />} />
        <Route path="/student/schedule" element={<PlaceholderPage title="My Schedule (Student)" />} />
        <Route path="/student/summary" element={<PlaceholderPage title="Personal Summary Screen" />} />
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

export default App;
